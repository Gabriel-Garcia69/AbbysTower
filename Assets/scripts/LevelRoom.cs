using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sala de prototipo: piso y 4 muros visibles con cuadrícula de 1 m + colliders que bloquean al jugador.
///
/// Los muros son de UNA sola cara (miran hacia adentro): los que quedan entre la cámara y el jugador
/// se vuelven invisibles solos al girar la cámara (back-face culling), pero siguen bloqueando.
///
/// Origen del objeto = centro del piso. Mantén la escala en (1,1,1).
/// Uso: Add Component → clic derecho en el componente → "Generar sala".
/// </summary>
public class LevelRoom : MonoBehaviour
{
    [Header("Tamaño (metros)")]
    [SerializeField] Vector3 size = new Vector3(30f, 8f, 30f); // ancho X, alto Y, fondo Z

    [Header("Apariencia")]
    [Tooltip("Opcional. Si está vacío se crea un material con cuadrícula de 1 m (en el editor se guarda en Assets/Materials).")]
    [SerializeField] Material material;
    [SerializeField] Color baseColor = new Color(0.16f, 0.62f, 0.78f);
    [SerializeField] Color lineColor = new Color(0.85f, 0.95f, 1f);

    [Header("Colisión")]
    [SerializeField] float colliderThickness = 1f;
    [Tooltip("Cuánto se meten los muros invisibles hacia adentro del borde visual. Así el jugador no llega a donde la cámara mostraría el exterior. Con la cámara de este proyecto (distancia 12, pitch 10°, FOV 25°) usa ~7.5 m y una sala de 40x40 o más.")]
    [SerializeField] float walkInset = 7.5f;
    [Tooltip("Agrega techo (visible solo desde adentro) y su collider. Con la cámara actual no se ve en Game, pero tapa el cielo en Scene y sirve si luego subes el pitch de la cámara.")]
    [SerializeField] bool ceiling = true;

    const string Prefix = "Room_";

    void Awake()
    {
        // Red de seguridad: si olvidaste generarla en el editor, se crea al iniciar.
        if (transform.Find(Prefix + "Floor") == null) Build();
    }

    void OnValidate()
    {
        size = Vector3.Max(size, new Vector3(2f, 2f, 2f));
        colliderThickness = Mathf.Max(0.1f, colliderThickness);
        walkInset = Mathf.Clamp(walkInset, 0f, Mathf.Min(size.x, size.z) / 2f - 1f);
    }

    [ContextMenu("Generar sala")]
    public void Build()
    {
        Clear();

        float w = size.x, h = size.y, d = size.z, t = colliderThickness;
        Material mat = GetMaterial();

        // Visual: nombre, centro local, normal hacia adentro, ancho y alto del quad
        CreateFace("Floor", new Vector3(0f, 0f, 0f),     Vector3.up,      w, d, mat);
        CreateFace("WallN", new Vector3(0f, h / 2, d / 2),  Vector3.back,    w, h, mat);
        CreateFace("WallS", new Vector3(0f, h / 2, -d / 2), Vector3.forward, w, h, mat);
        CreateFace("WallE", new Vector3(w / 2, h / 2, 0f),  Vector3.left,    d, h, mat);
        CreateFace("WallW", new Vector3(-w / 2, h / 2, 0f), Vector3.right,   d, h, mat);

        // Colisión: el piso cubre todo; los muros invisibles se meten "walkInset" hacia adentro
        // para que el jugador nunca se acerque a donde la cámara mostraría el exterior.
        float ix = w / 2 - walkInset; // límite jugable en X
        float iz = d / 2 - walkInset; // límite jugable en Z
        CreateCollider("Floor", new Vector3(0f, -t / 2, 0f),          new Vector3(w + 2 * t, t, d + 2 * t));
        CreateCollider("WallN", new Vector3(0f, h / 2, iz + t / 2),   new Vector3(w + 2 * t, h, t));
        CreateCollider("WallS", new Vector3(0f, h / 2, -iz - t / 2),  new Vector3(w + 2 * t, h, t));
        CreateCollider("WallE", new Vector3(ix + t / 2, h / 2, 0f),   new Vector3(t, h, d + 2 * t));
        CreateCollider("WallW", new Vector3(-ix - t / 2, h / 2, 0f),  new Vector3(t, h, d + 2 * t));

        // Techo: cara visible hacia abajo (solo se ve desde adentro) + collider
        if (ceiling)
        {
            CreateFace("Ceiling", new Vector3(0f, h, 0f), Vector3.down, w, d, mat);
            CreateCollider("Ceiling", new Vector3(0f, h + t / 2, 0f), new Vector3(w + 2 * t, t, d + 2 * t));
        }
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

    void CreateFace(string faceName, Vector3 center, Vector3 inwardNormal, float width, float height, Material mat)
    {
        var go = new GameObject(Prefix + faceName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        if (inwardNormal == Vector3.up)        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        else if (inwardNormal == Vector3.down) go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        else go.transform.localRotation = Quaternion.LookRotation(-inwardNormal, Vector3.up); // la cara visible del quad mira a -Z local

        go.AddComponent<MeshFilter>().sharedMesh = MakeQuad(width, height);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        // Los muros de cara trasera (los cercanos a la cámara) no deben proyectar sombra sobre el piso
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void CreateCollider(string colliderName, Vector3 center, Vector3 colliderSize)
    {
        var go = new GameObject(Prefix + "Collider_" + colliderName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = colliderSize;
    }

    /// <summary>Quad de una cara (visible desde -Z) con UVs en metros: 1 celda de cuadrícula = 1 m.</summary>
    static Mesh MakeQuad(float w, float h)
    {
        var m = new Mesh { name = "RoomQuad" };
        m.vertices = new[]
        {
            new Vector3(-w / 2, -h / 2, 0f), new Vector3(w / 2, -h / 2, 0f),
            new Vector3(-w / 2,  h / 2, 0f), new Vector3(w / 2,  h / 2, 0f),
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(0, h), new Vector2(w, h) };
        m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateBounds();
        return m;
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

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { name = "PrototypeGrid" };
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
        Gizmos.DrawWireCube(new Vector3(0f, size.y / 2, 0f), size); // sala visible (amarillo)
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(0f, size.y / 2, 0f),
            new Vector3(size.x - 2f * walkInset, size.y, size.z - 2f * walkInset)); // área jugable (verde)
    }
}
