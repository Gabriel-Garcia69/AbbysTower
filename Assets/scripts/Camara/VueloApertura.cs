using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// La entrada a cada mundo: un vuelo de cámara hecho a mano por el paisaje que termina a espaldas de Abby, con franjas de cine.
/// Usa una cámara propia, así que la del juego sigue a Abby sin enterarse y al final se empalma con ella.
/// Solo sale la primera vez que se entra al piso en la partida; cualquier tecla lo salta.
/// </summary>
public class VueloApertura : MonoBehaviour
{
    public static bool Activo { get; private set; }

    struct Punto
    {
        public Vector3 pos, mira; public float fov;
        public Punto(float x, float y, float z, float mx, float my, float mz, float f) { pos = new Vector3(x, y, z); mira = new Vector3(mx, my, mz); fov = f; }
    }

    // cada mapa: hacia dónde se mira al entrar (lo más vistoso) y de qué lado arranca la cámara
    static readonly Dictionary<string, Vector4> Rutas = new Dictionary<string, Vector4>
    {
        { "Diorama", new Vector4(0f, 4f, 30f, 1f) },
        { "Puerto", new Vector4(30f, 8f, 25f, -1f) },
        { "Cascadas", new Vector4(14f, 8f, 120f, -1f) },
        { "Acantilado", new Vector4(0f, 8f, 45f, -1f) },
        { "Cueva", new Vector4(0f, 3f, -5f, 1f) },
        { "Lago", new Vector4(8f, 2f, 90f, 1f) },
        { "Nevado", new Vector4(0f, 4f, 0f, -1f) },
        { "Cima", new Vector4(0f, 6f, 0f, 1f) },
    };

    const float Tramo = 4.2f, Barras = 0.07f;
    static readonly HashSet<string> vistos = new HashSet<string>();

    Punto[] ruta;
    Vector4 meta;
    Camera cam, principal;
    GameObject volumen;
    float t, barra;
    bool saltado;
    float tEmpalme;
    Vector3 poseP; Quaternion poseR; float poseF;
    readonly List<Behaviour> apagados = new List<Behaviour>();

    // sin recarga de dominio al darle Play, lo estático sobrevive: cada partida empieza de cero
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { vistos.Clear(); Activo = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void AlCargar(Scene s, LoadSceneMode m)
    {
        if (!Rutas.ContainsKey(s.name) || !vistos.Add(s.name)) return;
        new GameObject("VueloApertura").AddComponent<VueloApertura>().meta = Rutas[s.name];
        Activo = true;
    }

    void Start()
    {
        principal = Camera.main;
        if (principal == null) { Terminar(); return; }
        var go = new GameObject("CamaraVuelo");
        go.transform.SetParent(transform, false);
        cam = go.AddComponent<Camera>();
        cam.CopyFrom(principal);
        cam.depth = principal.depth + 10f;
        cam.farClipPlane = Mathf.Max(principal.farClipPlane, 1500f);
        cam.nearClipPlane = 0.2f;
        var datos = cam.GetUniversalAdditionalCameraData();
        var datosP = principal.GetUniversalAdditionalCameraData();
        datos.renderPostProcessing = datosP.renderPostProcessing;
        datos.volumeLayerMask = datosP.volumeLayerMask;
        datos.antialiasing = datosP.antialiasing;
        // sin el desenfoque de maqueta: el paisaje se tiene que ver completo
        volumen = new GameObject("VolumenVuelo");
        volumen.transform.SetParent(transform, false);
        var v = volumen.AddComponent<Volume>();
        v.isGlobal = true; v.priority = 900f;
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        perfil.Add<DepthOfField>(true).mode.Override(DepthOfFieldMode.Off);
        v.sharedProfile = perfil;
        var p = GameObject.FindWithTag("Player");
        if (p != null)
            foreach (var b in p.GetComponents<PlayerInputReader>()) { if (b.enabled) { b.enabled = false; apagados.Add(b); } }
        // un poco más alta y a un lado de donde quedará, mirando el paisaje; de ahí baja suave hasta la cámara del juego
        Vector3 p0 = principal.transform.position + new Vector3(meta.w * 6f, 7f, 2f);
        ruta = new[] { new Punto(p0.x, p0.y, p0.z, meta.x, meta.y, meta.z, Mathf.Max(principal.fieldOfView + 8f, 34f)) };
        Colocar(0f);
    }

    float Duracion => Tramo * ruta.Length;

    static Vector3 Catmull(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u)
    {
        float u2 = u * u, u3 = u2 * u;
        return 0.5f * (2f * b + (c - a) * u + (2f * a - 5f * b + 4f * c - d) * u2 + (3f * b - a - 3f * c + d) * u3);
    }

    /// <summary>Un punto de la curva: los de la ruta y, al final, donde esté la cámara del juego.</summary>
    Vector3 PosDe(int k) => k < ruta.Length ? ruta[Mathf.Max(k, 0)].pos : principal.transform.position;
    Vector3 MiraDe(int k) => k < ruta.Length ? ruta[Mathf.Max(k, 0)].mira : principal.transform.position + principal.transform.forward * 30f;
    float FovDe(int k) => k < ruta.Length ? ruta[Mathf.Max(k, 0)].fov : principal.fieldOfView;

    void Colocar(float s)
    {
        int n = ruta.Length;   // tramos: de cada punto al siguiente, y el último hasta la cámara del juego
        s = Mathf.Clamp(s, 0f, n - 0.0001f);
        int i = Mathf.FloorToInt(s);
        float u = s - i;
        var pos = Catmull(PosDe(i - 1), PosDe(i), PosDe(i + 1), PosDe(Mathf.Min(i + 2, n)), u);
        var mira = Catmull(MiraDe(i - 1), MiraDe(i), MiraDe(i + 1), MiraDe(Mathf.Min(i + 2, n)), Mathf.SmoothStep(0f, 1f, u));
        var rot = Quaternion.LookRotation(mira - pos);
        // en el último tramo la vista termina exactamente en la del juego
        float fin = Mathf.Clamp01(s - (n - 1f));
        float k = fin * fin * (3f - 2f * fin);
        if (k > 0f) rot = Quaternion.Slerp(rot, principal.transform.rotation, k);
        // nunca por debajo del terreno (salvo ya al llegar)
        if (k < 0.5f && Physics.Raycast(pos + Vector3.up * 60f, Vector3.down, out var h, 120f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith("Terreno") && pos.y < h.point.y + 2.5f)
            pos.y = h.point.y + 2.5f;
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.fieldOfView = Mathf.Lerp(FovDe(i), FovDe(i + 1), u);
    }

    void LateUpdate()
    {
        if (cam == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (AlgoPulsado()) saltado = true;
        if (saltado)
        {
            // saltar: se desliza desde donde vaya hasta la vista del juego, sin corte
            barra = Mathf.MoveTowards(barra, 0f, dt * 2.5f);
            tEmpalme += dt;
            float k = Mathf.SmoothStep(0f, 1f, tEmpalme / 0.7f);
            if (tEmpalme <= dt) { poseP = cam.transform.position; poseR = cam.transform.rotation; poseF = cam.fieldOfView; }
            cam.transform.SetPositionAndRotation(Vector3.Lerp(poseP, principal.transform.position, k), Quaternion.Slerp(poseR, principal.transform.rotation, k));
            cam.fieldOfView = Mathf.Lerp(poseF, principal.fieldOfView, k);
            if (tEmpalme >= 0.7f) Terminar();
            return;
        }
        t += dt;
        float avance = Mathf.Clamp01(t / Duracion);
        // arranca suave y frena hasta detenerse justo en la cámara del juego: sin rebote
        float e = Mathf.SmoothStep(0f, 1f, avance);
        Colocar(e * ruta.Length);
        barra = Mathf.MoveTowards(barra, avance > 0.82f ? 0f : 1f, dt * 1.2f);
        if (avance >= 1f) Terminar();
    }

    static bool AlgoPulsado()
    {
        var kb = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame);
    }

    void Terminar()
    {
        Activo = false;
        foreach (var b in apagados) if (b != null) b.enabled = true;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (this != null && Activo) Activo = false;
    }

    void OnGUI()
    {
        float h = Screen.height * Barras * Mathf.SmoothStep(0f, 1f, barra);
        if (h < 1f) return;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - h, Screen.width, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
