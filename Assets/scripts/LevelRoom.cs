using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Cómo SE VE el límite del nivel. La colisión es siempre la misma: un rectángulo jugable con muros invisibles.</summary>
public enum LimitStyle
{
    Room,        // sala cerrada: muros lejanos + techo (el estilo original)
    Void,        // plataforma flotando en el vacío
    Railing,     // plataforma con barandal
    Mist,        // niebla oscura que se disuelve (dither)
    EnergyField  // barrera de energía
}

/// <summary>
/// Genera el escenario de un nivel: piso, límite visible (según LimitStyle) y colliders que bloquean al jugador.
/// Los paneles de límite son de UNA cara (miran hacia adentro): los que quedan entre la cámara y el jugador
/// se vuelven invisibles solos al girar la cámara, pero siguen bloqueando.
///
/// Origen del objeto = centro del piso. Mantén la escala en (1,1,1).
/// Uso: Add Component → clic derecho en el componente → "Generar sala".
/// </summary>
public class LevelRoom : MonoBehaviour
{
    [Header("Cómo se ve el límite")]
    [SerializeField] LimitStyle limitStyle = LimitStyle.Room;

    [Header("Sala y muros del fondo")]
    [Tooltip("Ancho X, alto Y, fondo Z. En Room es la sala. En los demás estilos son los muros del fondo, que quedan detrás del vacío (tiene que ser más grande que Play Area).")]
    [SerializeField] Vector3 size = new Vector3(30f, 8f, 30f);
    [Tooltip("Solo Room: cuánto se meten los muros invisibles hacia adentro. Con la cámara actual (distancia 12, pitch 10°, FOV 25°) usa ~7.5 m y una sala de 40x40 o más.")]
    [SerializeField] float walkInset = 7.5f;
    [Tooltip("Solo Room: techo visible solo desde adentro.")]
    [SerializeField] bool ceiling = true;

    [Header("Plataforma (todos los estilos menos Room)")]
    [Tooltip("Área jugable en metros (ancho X, fondo Z). El piso visible mide exactamente esto; entre él y los muros del fondo queda el vacío.")]
    [SerializeField] Vector2 playArea = new Vector2(24f, 24f);
    [Tooltip("Grosor visible del borde de la plataforma (hacia abajo). 0 = sin borde.")]
    [SerializeField] float platformDepth = 1.5f;
    [Tooltip("Dibuja los muros del fondo (Size) detrás del vacío.")]
    [SerializeField] bool farWalls = true;
    [Tooltip("Cuánto bajan los muros del fondo por debajo del piso, para que el vacío se vea profundo.")]
    [SerializeField] float voidDepth = 8f;
    [Tooltip("Altura del barandal / niebla / barrera. 0 = la del estilo (1 / 3 / 4 m).")]
    [SerializeField] float panelHeight = 0f;
    [SerializeField] Texture2D railingTexture;
    [SerializeField] Texture2D mistTexture;
    [SerializeField] Texture2D energyTexture;

    [Header("Texturas del piso y los muros (opcional)")]
    [SerializeField] Texture2D floorTexture;
    [SerializeField] Texture2D wallTexture;
    [SerializeField] Texture2D ceilingTexture;
    [Tooltip("Metros que cubre una repetición de la textura (128 px = 2 m a 64 px por metro).")]
    [SerializeField] float tileMeters = 2f;

    [Header("Cuadrícula de prototipo (si falta textura)")]
    [SerializeField] Material material;
    [SerializeField] Color baseColor = new Color(0.16f, 0.62f, 0.78f);
    [SerializeField] Color lineColor = new Color(0.85f, 0.95f, 1f);

    [Header("Colisión")]
    [SerializeField] float colliderThickness = 1f;

    const string Prefix = "Room_";
    const float PanelTileWidth = 2f; // las texturas de límite miden 2 m de ancho

    void Awake()
    {
        // Red de seguridad: si olvidaste generarla en el editor, se crea al iniciar.
        if (transform.Find(Prefix + "Floor") == null) Build();
    }

    void OnValidate()
    {
        size = Vector3.Max(size, new Vector3(2f, 2f, 2f));
        playArea = Vector2.Max(playArea, new Vector2(4f, 4f));
        panelHeight = Mathf.Max(0f, panelHeight);
        platformDepth = Mathf.Max(0f, platformDepth);
        voidDepth = Mathf.Max(1f, voidDepth);
        if (limitStyle != LimitStyle.Room)
            size = new Vector3(Mathf.Max(size.x, playArea.x + 4f), size.y, Mathf.Max(size.z, playArea.y + 4f));
        colliderThickness = Mathf.Max(0.1f, colliderThickness);
        tileMeters = Mathf.Max(0.25f, tileMeters);
        walkInset = Mathf.Clamp(walkInset, 0f, Mathf.Min(size.x, size.z) / 2f - 1f);
    }

    /// <summary>Mitad del área jugable en X y en Z.</summary>
    void HalfPlayArea(out float ix, out float iz)
    {
        if (limitStyle == LimitStyle.Room) { ix = size.x / 2f - walkInset; iz = size.z / 2f - walkInset; }
        else { ix = playArea.x / 2f; iz = playArea.y / 2f; }
    }

    [ContextMenu("Generar sala")]
    public void Build()
    {
        Clear();

        bool isRoom = limitStyle == LimitStyle.Room;
        float w = size.x, h = size.y, d = size.z, t = colliderThickness;
        float ix, iz;
        HalfPlayArea(out ix, out iz);
        float k = 1f / tileMeters;

        // Los materiales se crean solo si se necesitan; la cuadrícula de prototipo es el respaldo si falta una textura.
        Material grid = null;
        Material Grid() { if (grid == null) grid = GetMaterial(); return grid; }
        Material FloorMat()   { return floorTexture   ? TexturedMaterial(floorTexture,   "Room_Floor")   : Grid(); }
        Material WallMat()    { return wallTexture    ? TexturedMaterial(wallTexture,    "Room_Wall")    : Grid(); }
        Material CeilingMat() { return ceilingTexture ? TexturedMaterial(ceilingTexture, "Room_Ceiling") : Grid(); }

        // ---- Visual ----
        if (isRoom)
        {
            Material wallMat = WallMat();
            CreateFace("Floor", Vector3.zero, Vector3.up, w, d, FloorMat(), k, k);
            CreateFace("WallN", new Vector3(0f, h / 2, d / 2),  Vector3.back,    w, h, wallMat, k, k);
            CreateFace("WallS", new Vector3(0f, h / 2, -d / 2), Vector3.forward, w, h, wallMat, k, k);
            CreateFace("WallE", new Vector3(w / 2, h / 2, 0f),  Vector3.left,    d, h, wallMat, k, k);
            CreateFace("WallW", new Vector3(-w / 2, h / 2, 0f), Vector3.right,   d, h, wallMat, k, k);
            if (ceiling) CreateFace("Ceiling", new Vector3(0f, h, 0f), Vector3.down, w, d, CeilingMat(), k, k);
        }
        else
        {
            Material floorMat = FloorMat();

            // 1) plataforma jugable
            CreateFace("Floor", Vector3.zero, Vector3.up, 2 * ix, 2 * iz, floorMat, k, k);

            // 2) borde con grosor (se ve desde afuera de la plataforma)
            if (platformDepth > 0f)
            {
                float pd = platformDepth;
                CreateFace("Lip_N", new Vector3(0f, -pd / 2, iz),  Vector3.forward, 2 * ix, pd, floorMat, k, k);
                CreateFace("Lip_S", new Vector3(0f, -pd / 2, -iz), Vector3.back,    2 * ix, pd, floorMat, k, k);
                CreateFace("Lip_E", new Vector3(ix, -pd / 2, 0f),  Vector3.right,   2 * iz, pd, floorMat, k, k);
                CreateFace("Lip_W", new Vector3(-ix, -pd / 2, 0f), Vector3.left,    2 * iz, pd, floorMat, k, k);
            }

            // 3) muros del fondo, detrás del vacío (bajan por debajo del piso)
            if (farWalls)
            {
                Material wallMat = WallMat();
                float fh = h + voidDepth, cy = (h - voidDepth) / 2f;
                CreateFace("WallN", new Vector3(0f, cy, d / 2),  Vector3.back,    w, fh, wallMat, k, k);
                CreateFace("WallS", new Vector3(0f, cy, -d / 2), Vector3.forward, w, fh, wallMat, k, k);
                CreateFace("WallE", new Vector3(w / 2, cy, 0f),  Vector3.left,    d, fh, wallMat, k, k);
                CreateFace("WallW", new Vector3(-w / 2, cy, 0f), Vector3.right,   d, fh, wallMat, k, k);
            }

            // 4) marca del límite sobre la plataforma (barandal / niebla / energía; Void no dibuja nada)
            BuildLimitPanels(ix, iz);
        }

        // ---- Colisión: mismo rectángulo jugable en todos los estilos ----
        float wallH = isRoom ? h : 8f;
        CreateCollider("Floor", new Vector3(0f, -t / 2, 0f),          new Vector3(2 * ix + 2 * t, t, 2 * iz + 2 * t));
        CreateCollider("WallN", new Vector3(0f, wallH / 2, iz + t / 2),  new Vector3(2 * ix + 2 * t, wallH, t));
        CreateCollider("WallS", new Vector3(0f, wallH / 2, -iz - t / 2), new Vector3(2 * ix + 2 * t, wallH, t));
        CreateCollider("WallE", new Vector3(ix + t / 2, wallH / 2, 0f),  new Vector3(t, wallH, 2 * iz + 2 * t));
        CreateCollider("WallW", new Vector3(-ix - t / 2, wallH / 2, 0f), new Vector3(t, wallH, 2 * iz + 2 * t));
        if (isRoom && ceiling)
            CreateCollider("Ceiling", new Vector3(0f, h + t / 2, 0f), new Vector3(w + 2 * t, t, d + 2 * t));
    }

    void BuildLimitPanels(float ix, float iz)
    {
        Texture2D tex; string matName; float defaultH; bool glow;
        switch (limitStyle)
        {
            case LimitStyle.Railing:     tex = railingTexture; matName = "Limit_Railing"; defaultH = 1f; glow = false; break;
            case LimitStyle.Mist:        tex = mistTexture;    matName = "Limit_Mist";    defaultH = 3f; glow = false; break;
            case LimitStyle.EnergyField: tex = energyTexture;  matName = "Limit_Energy";  defaultH = 4f; glow = true;  break;
            default: return; // Void: sin panel
        }
        if (tex == null)
        {
            Debug.LogWarning("LevelRoom: falta la textura del estilo " + limitStyle + ". Asígnala en el Inspector (carpeta Assets/Textures/Limits).", this);
            return;
        }

        float ph = panelHeight > 0f ? panelHeight : defaultH;
        Material m = CutoutMaterial(tex, matName, glow);
        float us = 1f / PanelTileWidth, vs = 1f / ph; // la textura cubre el panel de alto una sola vez
        CreateFace("Limit_N", new Vector3(0f, ph / 2, iz),  Vector3.back,    2 * ix, ph, m, us, vs);
        CreateFace("Limit_S", new Vector3(0f, ph / 2, -iz), Vector3.forward, 2 * ix, ph, m, us, vs);
        CreateFace("Limit_E", new Vector3(ix, ph / 2, 0f),  Vector3.left,    2 * iz, ph, m, us, vs);
        CreateFace("Limit_W", new Vector3(-ix, ph / 2, 0f), Vector3.right,   2 * iz, ph, m, us, vs);
    }

    [ContextMenu("Borrar sala")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject c = transform.GetChild(i).gameObject;
            if (!c.name.StartsWith(Prefix)) continue;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
    }

    void CreateFace(string faceName, Vector3 center, Vector3 inwardNormal, float width, float height, Material mat, float uScale, float vScale)
    {
        var go = new GameObject(Prefix + faceName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        if (inwardNormal == Vector3.up)        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        else if (inwardNormal == Vector3.down) go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        else go.transform.localRotation = Quaternion.LookRotation(-inwardNormal, Vector3.up); // la cara visible del quad mira a -Z local

        go.AddComponent<MeshFilter>().sharedMesh = MakeQuad(width, height, uScale, vScale);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void CreateCollider(string colliderName, Vector3 center, Vector3 colliderSize)
    {
        var go = new GameObject(Prefix + "Collider_" + colliderName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = colliderSize;
    }

    /// <summary>Quad de una cara (visible desde -Z). uScale/vScale = repeticiones de textura por metro.</summary>
    static Mesh MakeQuad(float w, float h, float uScale, float vScale)
    {
        float u = w * uScale, v = h * vScale;
        var m = new Mesh { name = "RoomQuad" };
        m.vertices = new[]
        {
            new Vector3(-w / 2, -h / 2, 0f), new Vector3(w / 2, -h / 2, 0f),
            new Vector3(-w / 2,  h / 2, 0f), new Vector3(w / 2,  h / 2, 0f),
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(u, 0), new Vector2(0, v), new Vector2(u, v) };
        m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ materiales

    static Shader LitShader()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        return sh;
    }

    /// <summary>Material opaco con la textura dada. En el editor se guarda (y reutiliza) en Assets/Materials.</summary>
    Material TexturedMaterial(Texture2D tex, string matName)
    {
        return GetOrCreateMaterial(matName, m => ApplyTex(m, tex));
    }

    /// <summary>Material con recorte por alfa (bordes nítidos, sin transparencia): para barandal, niebla dither y barrera.</summary>
    Material CutoutMaterial(Texture2D tex, string matName, bool glow)
    {
        return GetOrCreateMaterial(matName, m =>
        {
            ApplyTex(m, tex);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            m.EnableKeyword("_ALPHATEST_ON");
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 2f); // una sola cara (back-face culling)
            m.renderQueue = 2450;
            if (glow && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", new Color(1.2f, 1.2f, 1.2f));
            }
        });
    }

    Material GetOrCreateMaterial(string matName, System.Action<Material> configure)
    {
#if UNITY_EDITOR
        const string dir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = dir + "/" + matName + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { configure(existing); EditorUtility.SetDirty(existing); return existing; }
#endif
        var m = new Material(LitShader()) { name = matName };
        configure(m);
#if UNITY_EDITOR
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
#endif
        return m;
    }

    static void ApplyTex(Material m, Texture2D tex)
    {
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
    }

    Material GetMaterial()
    {
        if (material != null) return material;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, true)
        {
            name = "PrototypeGrid_Tex",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 8
        };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tex.SetPixel(x, y, (x < 2 || y < 2) ? lineColor : baseColor);
        tex.Apply(true);

        var m = new Material(LitShader()) { name = "PrototypeGrid" };
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); else m.mainTexture = tex;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);

#if UNITY_EDITOR
        const string dir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(tex, AssetDatabase.GenerateUniqueAssetPath(dir + "/PrototypeGrid_Tex.asset"));
        AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(dir + "/PrototypeGrid.mat"));
        AssetDatabase.SaveAssets();
        material = m; // se reutiliza en las siguientes generaciones y en otros niveles
#endif
        return m;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0f, size.y / 2, 0f), size); // sala / muros del fondo (amarillo)
        float ix, iz;
        HalfPlayArea(out ix, out iz);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0f, 2f, 0f), new Vector3(2 * ix, 4f, 2 * iz)); // área jugable (verde)
    }
}
