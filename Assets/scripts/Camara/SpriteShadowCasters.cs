using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sombras realistas para un personaje sprite (billboard).
///
/// Problema: el sprite siempre mira a la cámara, así que su sombra es la de un plano mirando a la cámara:
/// con una luz de lado la sombra se vuelve una línea, y al girar la cámara la sombra gira con ella.
///
/// Solución: copias invisibles del sprite que SOLO proyectan sombra, una por cada luz importante cercana
/// (el sol + los faroles/antorchas más fuertes sobre el personaje), cada una girada de frente a su luz.
/// Con Rendering Layers de URP cada copia proyecta sombra solo para SU luz (sin sombras dobles).
/// Las luces elegidas reciben sombras aunque no las tuvieran; al alejarse se restauran como estaban.
///
/// Va en el objeto con el SpriteRenderer (Visual). Necesita "Rendering Layers" activo en el URP Asset.
/// </summary>
[DefaultExecutionOrder(110)]   // después del billboard y del animador de sprites
public class SpriteShadowCasters : MonoBehaviour
{
    [Tooltip("Cuántas luces proyectan la sombra del personaje a la vez (incluye el sol).")]
    [Range(1, 6)] public int maxLights = 3;
    [Tooltip("Cada cuánto se recalcula qué luces son las más importantes (segundos).")]
    public float refreshInterval = 0.15f;
    [Tooltip("Las luces cuyo aporte sea menor que esto no proyectan sombra del personaje.")]
    public float minInfluence = 0.15f;
    public bool includeSun = true;

    class Slot
    {
        public SpriteRenderer sr;
        public Light light;
        public uint bit;
    }

    struct LightState { public bool customLayers; public uint shadowLayers; public LightShadows shadows; }

    SpriteRenderer source;
    Transform rootParent;
    Vector3 baseLocalPos, baseScale;
    Slot[] slots;
    readonly Dictionary<Light, LightState> original = new Dictionary<Light, LightState>();
    Light[] sceneLights = new Light[0];
    float refreshTimer, rescanTimer;
    UnityEngine.Rendering.ShadowCastingMode sourceMode;

    void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        rootParent = transform.parent;
        baseLocalPos = transform.localPosition;
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (source == null) return;
        sourceMode = source.shadowCastingMode;
        source.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // el sprite visible ya no da sombra

        slots = new Slot[maxLights];
        for (int k = 0; k < maxLights; k++)
        {
            var go = new GameObject("SombraSprite_" + k);
            go.transform.SetParent(rootParent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = source.sharedMaterial;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            sr.receiveShadows = false;
            uint bit = 1u << (k + 1);                 // capa 0 = Default (todo lo demás); 1..N = una por copia
            sr.renderingLayerMask = bit;
            sr.enabled = false;
            slots[k] = new Slot { sr = sr, bit = bit };
        }
        rescanTimer = 0f;
        refreshTimer = 0f;
    }

    void OnDisable()
    {
        if (slots != null)
            foreach (var s in slots)
            {
                if (s.light != null) Release(s.light, s.bit);
                if (s.sr != null) Destroy(s.sr.gameObject);
            }
        slots = null;
        if (source != null) source.shadowCastingMode = sourceMode;
    }

    void LateUpdate()
    {
        if (slots == null) return;

        rescanTimer -= Time.deltaTime;
        if (rescanTimer <= 0f) { sceneLights = FindObjectsByType<Light>(FindObjectsSortMode.None); rescanTimer = 2f; }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f) { PickLights(); refreshTimer = refreshInterval; }

        Vector3 pos = rootParent != null ? rootParent.TransformPoint(baseLocalPos) : transform.position;
        foreach (var s in slots)
        {
            if (s.light == null) { s.sr.enabled = false; continue; }
            s.sr.enabled = true;
            s.sr.sprite = source.sprite;
            s.sr.flipX = source.flipX;
            s.sr.transform.position = pos;
            s.sr.transform.localScale = baseScale;

            // la copia mira hacia su luz (solo giro horizontal: el personaje sigue de pie)
            Vector3 toLight = s.light.type == LightType.Directional ? -s.light.transform.forward : s.light.transform.position - pos;
            toLight.y = 0f;
            if (toLight.sqrMagnitude > 0.0001f) s.sr.transform.rotation = Quaternion.LookRotation(-toLight.normalized, Vector3.up);
        }
    }

    /// <summary>Elige las luces que más iluminan al personaje y reparte las copias entre ellas.</summary>
    void PickLights()
    {
        Vector3 pos = transform.position;
        var scored = new List<KeyValuePair<float, Light>>();
        foreach (var l in sceneLights)
        {
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
            float score;
            if (l.type == LightType.Directional)
            {
                if (!includeSun || OriginalShadows(l) == LightShadows.None) continue;
                score = 1000f;   // el sol siempre primero
            }
            else
            {
                float d = Vector3.Distance(l.transform.position, pos);
                if (d > l.range) continue;
                float fall = 1f - d / l.range;
                score = l.intensity * fall * fall / (1f + d * d);
                if (score < minInfluence) continue;
            }
            scored.Add(new KeyValuePair<float, Light>(score, l));
        }
        scored.Sort((a, b) => b.Key.CompareTo(a.Key));

        var wanted = new List<Light>();
        for (int i = 0; i < scored.Count && wanted.Count < slots.Length; i++) wanted.Add(scored[i].Value);

        // libera las que ya no están; conserva el mismo slot para las que siguen (evita parpadeos)
        foreach (var s in slots)
            if (s.light != null && !wanted.Contains(s.light)) { Release(s.light, s.bit); s.light = null; }
        foreach (var l in wanted)
        {
            bool assigned = false;
            foreach (var s in slots) if (s.light == l) { assigned = true; break; }
            if (assigned) continue;
            foreach (var s in slots)
                if (s.light == null) { Claim(l, s.bit); s.light = l; break; }
        }
    }

    LightShadows OriginalShadows(Light l) => original.TryGetValue(l, out var st) ? st.shadows : l.shadows;

    void Claim(Light l, uint bit)
    {
        var data = l.GetUniversalAdditionalLightData();
        if (!original.ContainsKey(l))
            original[l] = new LightState { customLayers = data.customShadowLayers, shadowLayers = (uint)data.shadowRenderingLayers, shadows = l.shadows };
        var st = original[l];
        uint baseMask = st.customLayers ? st.shadowLayers : (uint)data.renderingLayers;
        data.customShadowLayers = true;
        data.shadowRenderingLayers = baseMask | bit;
        if (l.shadows == LightShadows.None) l.shadows = LightShadows.Soft;   // la luz cercana sí debe dar sombra
    }

    void Release(Light l, uint bit)
    {
        if (l == null || !original.TryGetValue(l, out var st)) return;
        var data = l.GetUniversalAdditionalLightData();
        data.customShadowLayers = st.customLayers;
        data.shadowRenderingLayers = st.shadowLayers;
        l.shadows = st.shadows;
        original.Remove(l);
    }
}
