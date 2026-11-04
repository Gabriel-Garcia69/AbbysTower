using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Cala Sombría de noche (horaDelDia = Noche): cielo estrellado azul-violeta con aurora, luna al SSE y planeta al NNO, luz de luna
/// fría, luces cálidas en el pueblo, torre morada como foco, reflejo falso de la luna en la bahía y postproceso propio.
/// Todo vive en el grupo "Noche" de Ext_Cala.prefab (cielo, volumen, ExteriorNoche) más unos pocos ajustes a la luz direccional y
/// a ExteriorSky de la escena. Con horaDelDia = Atardecer no se genera nada de esto y la escena vuelve a como estaba.
/// </summary>
public partial class ExteriorBuilder
{
    public enum Hora { Atardecer, Noche }

    [Tooltip("Atardecer: el cielo de siempre. Noche: cielo estrellado con aurora, luna y planeta, luces cálidas en el pueblo y postproceso nocturno.")]
    public Hora horaDelDia = Hora.Noche;
    [Tooltip("De noche hay 3 luces con sombra: la luna y los 2 braseros. Con esto, la sombra de un brasero pasa a la fogata del campamento.")]
    public bool sombraEnFogata = false;

    public bool EsNoche => forma == Forma.Cala && horaDelDia == Hora.Noche;

    /// <summary>Dirección en el cielo: az en grados desde el norte (+Z) hacia el este (+X), el en grados sobre el horizonte.</summary>
    public static Vector3 DirCielo(float az, float el)
    {
        float a = az * Mathf.Deg2Rad, e = el * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
    }

    /// <summary>Luna al SSE (33° al este del sur), 24° de altura: queda en el encuadre de las tomas de la bahía y su reflejo cae en el agua.</summary>
    public static readonly Vector3 LunaDir = DirCielo(147f, 24f);
    /// <summary>Planeta al NNO (28° al oeste del norte), 12° de altura: sobre la bahía, a la izquierda de la torre vista desde el pueblo.</summary>
    public static readonly Vector3 PlanetaDir = DirCielo(332f, 12f);

#if UNITY_EDITOR
    const float RadioCielo = 450f;
    const string PerfilNochePath = "Assets/Settings/ExteriorProfile_Noche.asset";
    Material mNSky, mNAurora, mNDestellos, mNLuna, mNHaloLuna, mNPlaneta, mNNubes, mNVentana;
    VolumeProfile perfilNoche;

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    // ---------------- materiales y texturas

    void CreateNocheAssets()
    {
        var clear = new Color(0f, 0f, 0f, 0f);
        var luna = Tex("NocheLuna", 128, (x, y) =>
        {
            var p = new Vector2(x + 0.5f - 64f, y + 0.5f - 64f) / 62f;
            float d = p.magnitude;
            if (d > 1f) return clear;
            float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.6f + Mathf.PerlinNoise(x * 0.2f + 9f, y * 0.2f) * 0.4f;
            float v = Mathf.Lerp(0.95f, 0.7f, Mathf.Clamp01((n - 0.35f) * 1.6f));
            return new Color(v * 0.93f, v * 0.97f, v, Mathf.Clamp01((1f - d) * 12f));
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var halo = Tex("NocheHalo", 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
            return new Color(1f, 1f, 1f, d < 1f ? Mathf.Pow(1f - d, 2.4f) : 0f);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        // planeta poco saturado: bandas suaves, cráteres, lado oscuro y un borde de atmósfera
        var planeta = Tex("NochePlaneta", 256, (x, y) =>
        {
            var p = new Vector2(x + 0.5f - 128f, y + 0.5f - 128f) / 124f;
            float d2 = p.sqrMagnitude;
            if (d2 > 1f) return clear;
            float z = Mathf.Sqrt(1f - d2);
            float l = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x, p.y, z), new Vector3(0.5f, 0.5f, 0.7f).normalized));
            float banda = Mathf.PerlinNoise(p.x * 1.5f + 3f, p.y * 9f + Mathf.PerlinNoise(p.x * 4f, p.y * 4f) * 1.3f);
            float crater = Mathf.PerlinNoise(p.x * 14f + 2f, p.y * 14f);
            Color c = Color.Lerp(new Color(0.42f, 0.40f, 0.54f), new Color(0.58f, 0.52f, 0.64f), banda) * Mathf.Lerp(0.85f, 1.05f, crater);
            c = c * (0.25f + 0.75f * l) + new Color(0.45f, 0.4f, 0.7f) * (Mathf.Pow(1f - z, 3f) * 0.55f);
            c.a = Mathf.Clamp01((1f - Mathf.Sqrt(d2)) * 40f);
            return c;
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var nube = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/CloudPuff.png");

        mNSky = GetOrCreate("SkyNoche", "Abby/SkyNoche");
        mNSky.SetColor("_Zenith", Hex("#03030C")); mNSky.SetColor("_Mid", Hex("#0A0812"));
        mNSky.SetColor("_Horizon", Hex("#17101F")); mNSky.SetColor("_Ground", Hex("#07070C"));
        mNSky.SetColor("_MoonGlow", new Color(0.2f, 0.2f, 0.27f)); mNSky.SetFloat("_StarDensity", 0.93f);
        EditorUtility.SetDirty(mNSky);
        mNLuna = Cielo("NocheLuna", luna, new Color(0.62f, 0.62f, 0.7f), true);
        mNHaloLuna = Cielo("NocheHaloLuna", halo, new Color(0.3f, 0.29f, 0.38f) * 0.55f, true);
        mNPlaneta = Cielo("NochePlaneta", planeta, Color.white, false);
        mNNubes = Cielo("NocheNubes", nube, Color.white, false);
        mNAurora = GetOrCreate("NocheAurora", "Abby/Aurora");
        mNAurora.SetColor("_ColorA", Hex("#6A3FD0")); mNAurora.SetColor("_ColorB", Hex("#9A5CFF")); mNAurora.SetFloat("_Intensity", 0.14f);   // aura de la torre, no aurora boreal
        EditorUtility.SetDirty(mNAurora);
        mNDestellos = GetOrCreate("NocheDestellos", "Abby/Destellos");
        mNDestellos.SetColor("_Color", new Color(0.42f, 0.42f, 0.5f));
        EditorUtility.SetDirty(mNDestellos);
        mNVentana = Emissive("NocheVentana", new Color(1f, 0.56f, 0.22f), 1.25f);
        mLampGlow = Emissive("LampGlow", new Color(1f, 0.62f, 0.3f), 1.6f);
        CrearPerfilNoche();
        AssetDatabase.SaveAssets();
    }

    static Material Cielo(string name, Texture2D tex, Color color, bool aditivo)
    {
        color.a = 1f;
        var m = GetOrCreate(name, "Abby/CieloSprite");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", aditivo ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------- postproceso

    void CrearPerfilNoche()
    {
        perfilNoche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PerfilNochePath);
        if (perfilNoche == null)
        {
            perfilNoche = ScriptableObject.CreateInstance<VolumeProfile>();
            perfilNoche.name = "ExteriorProfile_Noche";
            AssetDatabase.CreateAsset(perfilNoche, PerfilNochePath);
        }
        else
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(PerfilNochePath)) if (o is VolumeComponent) Object.DestroyImmediate(o, true);
            perfilNoche.components.Clear();
        }
        T Add<T>() where T : VolumeComponent
        {
            var c = perfilNoche.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, perfilNoche);
            return c;
        }
        var bloom = Add<Bloom>();
        bloom.threshold.value = 1.15f; bloom.intensity.value = 0.45f; bloom.scatter.value = 0.6f; bloom.tint.value = Hex("#FFF0E0");
        var split = Add<SplitToning>();
        split.shadows.value = Hex("#56505F"); split.highlights.value = Hex("#807A78"); split.balance.value = 0f;
        var wb = Add<WhiteBalance>();
        wb.temperature.value = -5f; wb.tint.value = 3f;
        // sombras hundidas: la noche pesa, solo se lee lo que tiene una luz cerca
        var smh = Add<ShadowsMidtonesHighlights>();
        smh.shadows.value = new Vector4(1f, 1f, 1.02f, 0f);
        smh.midtones.value = new Vector4(1f, 1f, 1f, 0f);
        smh.highlights.value = new Vector4(1f, 1f, 1f, 0f);
        var ca = Add<ColorAdjustments>();
        ca.postExposure.value = 0.55f; ca.saturation.value = -16f; ca.contrast.value = 8f;
        var vig = Add<Vignette>();
        vig.intensity.value = 0.3f; vig.smoothness.value = 0.55f; vig.color.value = Color.black;
        var dof = Add<DepthOfField>();
        dof.mode.value = DepthOfFieldMode.Off;
        EditorUtility.SetDirty(perfilNoche);
    }

    // ---------------- la noche: cielo, reflejo, volumen y luces

    void BuildNoche(Transform root)
    {
        var g = Group("Noche", root);
        var cmp = g.gameObject.AddComponent<ExteriorNoche>();
        cmp.skybox = mNSky;
        var cielo = Group("Cielo", g);
        cmp.cielo = cielo;

        // luna con su halo, y planeta: quads que miran al centro del cielo (el nodo sigue a la cámara)
        void Pegatina(string nombre, Vector3 dir, float lado, Material mat)
        {
            var go = MeshObject(cielo, nombre, QuadUV(lado, lado, 1f, 1f), mat, false, false);
            go.transform.localPosition = dir * RadioCielo;
            go.transform.localRotation = Quaternion.LookRotation(dir);
        }
        Pegatina("HaloLuna", LunaDir, 380f, mNHaloLuna);
        Pegatina("Luna", LunaDir, 40f, mNLuna);
        Pegatina("Planeta", PlanetaDir, 2f * RadioCielo * Mathf.Tan(15f * Mathf.Deg2Rad), mNPlaneta);   // 15° de radio angular
        MeshObject(cielo, "Nubes", MallaNubes(), mNNubes, false, false);

        // aurora: 2 cintas en arco (una alrededor de la luna, otra sobre el planeta)
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        CintaAurora(v, uv, tri, 40f, 170f, 14f, 10f, 30f, 48);
        CintaAurora(v, uv, tri, 270f, 350f, 26f, 8f, 20f, 36);
        var am = new Mesh { name = "Aurora", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        am.SetVertices(v); am.SetUVs(0, uv); am.SetTriangles(tri, 0); am.RecalculateBounds();
        MeshObject(cielo, "Aurora", am, mNAurora, false, false);

        // reflejo falso de la luna en la bahía: una franja alineada con el azimut de la luna, centrada donde la ven las tomas de la bahía
        Vector2 u = new Vector2(LunaDir.x, LunaDir.z).normalized, s = new Vector2(u.y, -u.x);
        Vector2 c = new Vector2(-5f, 140f);
        float largo = 75f, ancho = 18f, y = CalaMar + 0.1f;
        var rm = new Mesh { name = "ReflejoLuna" };
        Vector3 P(Vector2 p) => new Vector3(p.x, y, p.y);
        rm.SetVertices(new[] { P(c - u * largo - s * ancho), P(c - u * largo + s * ancho), P(c + u * largo - s * ancho), P(c + u * largo + s * ancho) });
        rm.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
        rm.SetTriangles(new[] { 0, 2, 3, 0, 3, 1 }, 0);
        rm.RecalculateBounds();
        MeshObject(g, "ReflejoLuna", rm, mNDestellos, false, false);

        // neblina baja y morada que se arrastra por las calles: el aura del Abismo
        Particles(g, "Neblina", new Vector3((CalaOeste + CalaEste) / 2f, 1.2f, (CalaSur + CalaMalecon) / 2f), mTorreHumo, ps =>
        {
            var pm = ps.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(14f, 22f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            pm.startSize = new ParticleSystem.MinMaxCurve(16f, 30f);
            pm.startColor = new Color(0.42f, 0.3f, 0.62f, 0.09f);
            pm.maxParticles = 90;
            pm.prewarm = true;
            pm.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(CalaEste - CalaOeste, 1.5f, CalaMalecon - CalaSur);
        });

        // postproceso nocturno, por encima del perfil de siempre
        var vgo = new GameObject("VolumenNoche");
        vgo.transform.SetParent(g, false);
        var vol = vgo.AddComponent<Volume>();
        vol.isGlobal = true; vol.priority = 10f; vol.weight = 1f; vol.sharedProfile = perfilNoche;
    }

    Mesh MallaNubes()
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
        var horizLuna = new Vector3(LunaDir.x, 0f, LunaDir.z).normalized;
        void Nube(float az, float el, float w, float h, Color cc)
        {
            Vector3 d = DirCielo(az, el), right = Vector3.Cross(Vector3.up, d).normalized, up = Vector3.Cross(d, right).normalized, c = d * RadioCielo;
            int b = v.Count;
            v.Add(c - right * w / 2f - up * h / 2f); v.Add(c + right * w / 2f - up * h / 2f);
            v.Add(c - right * w / 2f + up * h / 2f); v.Add(c + right * w / 2f + up * h / 2f);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(0f, 1f)); uv.Add(new Vector2(1f, 1f));
            for (int k = 0; k < 4; k++) col.Add(cc);
            tri.AddRange(new[] { b, b + 2, b + 3, b, b + 3, b + 1 });
        }
        // banco bajo en el horizonte y dos capas altas; las que miran a la luna apenas se aclaran, las demás son casi negras
        for (int capa = 0; capa < 3; capa++)
        {
            int cuantas = capa == 0 ? 16 : 22;
            for (int i = 0; i < cuantas; i++)
            {
                float az = i * 360f / cuantas + R(-9f, 9f);
                Vector3 h0 = DirCielo(az, 0f);
                float lit = Mathf.Clamp01(Vector3.Dot(h0, horizLuna) * 0.5f + 0.5f);
                var cc = Color.Lerp(new Color(0.05f, 0.04f, 0.1f, 0.8f), new Color(0.24f, 0.22f, 0.42f, 0.8f), lit * lit * lit);
                float el = capa == 0 ? R(2f, 9f) : capa == 1 ? R(14f, 26f) : R(32f, 52f);
                Nube(az, el, R(190f, 300f), capa == 0 ? R(34f, 56f) : R(70f, 120f), cc);
            }
        }
        var m = new Mesh { name = "NubesNoche" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateBounds();
        return m;
    }

    void CintaAurora(List<Vector3> v, List<Vector2> uv, List<int> tri, float az0, float az1, float e0, float arco, float alto, int cols)
    {
        const int filas = 8;
        int b = v.Count;
        for (int i = 0; i <= cols; i++)
        {
            float t = i / (float)cols, az = Mathf.Lerp(az0, az1, t);
            float baseEl = e0 + arco * Mathf.Sin(t * Mathf.PI) + 3f * Mathf.Sin(t * 9f + az0);
            for (int j = 0; j < filas; j++)
            {
                float vv = j / (float)(filas - 1);
                v.Add(DirCielo(az, baseEl + vv * alto * (0.8f + 0.4f * Mathf.Sin(t * 5f + 1f))) * (RadioCielo - 30f));
                uv.Add(new Vector2(t, vv));
            }
        }
        for (int i = 0; i < cols; i++)
            for (int j = 0; j < filas - 1; j++)
            {
                int a = b + i * filas + j, c = a + filas;
                tri.AddRange(new[] { a, a + 1, c, a + 1, c + 1, c });
            }
    }

    // ---------------- luces, ventanas y partículas del pueblo (después de colocar las piezas)

    static void AfinarLuz(Light l, Color c, float rango, float intensidad, bool sombra)
    {
        l.color = c; l.range = rango; l.intensity = intensidad;
        l.shadows = sombra ? LightShadows.Soft : LightShadows.None;
        if (sombra) l.shadowStrength = 0.85f;
    }

    /// <summary>Reduce el tope de partículas de un objeto (y su emisión en la misma proporción) a un total aproximado.</summary>
    static void LimitarParticulas(GameObject go, int total)
    {
        var sistemas = go.GetComponentsInChildren<ParticleSystem>(true);
        int suma = 0;
        foreach (var ps in sistemas) suma += ps.main.maxParticles;
        if (suma <= total) return;
        float k = total / (float)suma;
        foreach (var ps in sistemas)
        {
            var m = ps.main; m.maxParticles = Mathf.Max(1, Mathf.RoundToInt(m.maxParticles * k));
            var e = ps.emission; e.rateOverTimeMultiplier *= k;
        }
    }

    void NocheAjustes(Transform root)
    {
        var plaza = root.Find("Plaza");
        int braseros = 0;
        if (plaza != null)
            foreach (Transform o in plaza)
            {
                if (o.name.StartsWith("Farol"))
                    foreach (var l in o.GetComponentsInChildren<Light>(true)) { if (l.enabled) AfinarLuz(l, Hex("#FFA040"), 14f, 4f, false); }
                else if (o.name.StartsWith("Brasero"))
                {
                    bool sombra = !sombraEnFogata || braseros == 0;   // sombras: luna + 2 braseros (o + brasero y fogata)
                    braseros++;
                    foreach (var l in o.GetComponentsInChildren<Light>(true)) AfinarLuz(l, Hex("#FF7A2E"), 16f, 5f, sombra);
                    LimitarParticulas(o.gameObject, 50);
                }
                else if (o.name.StartsWith("Fogata"))
                {
                    foreach (var l in o.GetComponentsInChildren<Light>(true)) AfinarLuz(l, Hex("#FF8A3A"), 18f, 6f, sombraEnFogata);
                    LimitarParticulas(o.gameObject, 60);
                }
            }

        // casas: el vidrio cálido y emisivo en la mitad (el resto oscuro), humo solo en las habitadas, y 2 ventanas con luz real
        var casas = root.Find("Casas");
        if (casas == null) return;
        int n = 0, conLuz = 0;
        foreach (Transform c in casas)
        {
            Vector3 p = c.localPosition;
            bool luz = conLuz < 6 && n % 13 == 0;   // unas pocas ventanas con luz real, repartidas por el pueblo
            bool habitada = luz || n % 3 != 2;
            n++;
            foreach (var r in c.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Vidrio") r.sharedMaterial = habitada ? mNVentana : mWindowDark;
            foreach (var ps in c.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!habitada) ps.gameObject.SetActive(false);
                else LimitarParticulas(ps.gameObject, 12);
            }
            if (luz)
            {
                var l = AddLight(root.Find("Noche") != null ? root.Find("Noche") : root, "LuzVentana", root.InverseTransformPoint(c.TransformPoint(new Vector3(0f, 2.2f, 2.5f))),
                                 Hex("#FFB454"), 8f, 3.5f, LightShadows.None);
                conLuz++;
            }
        }
        lucesCala += conLuz;
    }

    // ---------------- la escena: luz de luna y ExteriorSky (solo valores de esta escena; los de fábrica no cambian)

    void AplicarEscenaHora()
    {
        var sun = RenderSettings.sun;
        if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        var sky = FindAnyObjectByType<ExteriorSky>();
        bool noche = EsNoche;
        if (sun != null)
        {
            if (noche)
            {
                sun.name = "Luna";
                sun.transform.rotation = Quaternion.LookRotation(-transform.TransformDirection(LunaDir));   // el builder puede ir girado
                sun.color = Hex("#8C8DA6"); sun.intensity = 0.5f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f;
            }
            else
            {
                sun.name = "Directional Light";
                sun.transform.localRotation = new Quaternion(0.042717468f, 0.95267874f, -0.15942374f, 0.25526953f);
                sun.color = new Color(1f, 0.74f, 0.5f); sun.intensity = 1.7f;
            }
            EditorUtility.SetDirty(sun);
        }
        if (sky != null)
        {
            // el agua refleja estos colores (los publica ExteriorSky); ambiente y niebla los pone ExteriorNoche
            if (noche)
            {
                sky.zenith = Hex("#040408"); sky.horizon = Hex("#17101F"); sky.ground = Hex("#07070C");
                sky.sunColor = new Color(0.5f, 0.5f, 0.62f); sky.sunSize = 0.004f;
                sky.cloudColor = Hex("#1E1C2A"); sky.cloudCover = 0.6f;
                sky.driveAmbient = false; sky.driveFog = false;
            }
            else
            {
                sky.zenith = new Color(0.16f, 0.28f, 0.58f); sky.horizon = new Color(1f, 0.6f, 0.38f); sky.ground = new Color(0.22f, 0.2f, 0.24f);
                sky.sunColor = new Color(1f, 0.78f, 0.52f); sky.sunSize = 0.0025f;
                sky.cloudColor = new Color(1f, 0.9f, 0.88f); sky.cloudCover = 0.55f;
                sky.driveAmbient = true; sky.driveFog = true;
            }
            EditorUtility.SetDirty(sky);
        }
        if (noche) RenderSettings.skybox = mNSky;
        else
        {
            var dia = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("019e25e35d3c1ab44a985fb18c796e29"));
            if (dia != null) RenderSettings.skybox = dia;
            RenderSettings.fogMode = FogMode.Linear;
        }
    }
#endif
}
