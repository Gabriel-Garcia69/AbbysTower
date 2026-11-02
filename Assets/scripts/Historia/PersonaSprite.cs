using UnityEngine;

/// <summary>
/// Cambia una figura de bloques de las cinemáticas (DioramaBuilder.Figura) o el coloso por su pixel art de PixelLab, si
/// existen las 8 rotaciones en Resources/Personajes/&lt;carpeta&gt;/&lt;dirección&gt;.png. Las piezas 3D se ocultan (las luces
/// se quedan), el sprite mira siempre a la cámara y elige la dirección según hacia dónde mira la figura, así que los
/// movimientos de las cinemáticas siguen funcionando. El tamaño y los pies vienen del .meta (pixels por unidad y pivote).
/// </summary>
public class PersonaSprite : MonoBehaviour
{
    static readonly string[] Dirs = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    public string carpeta;
    Sprite[] sprites;
    SpriteRenderer sr;
    int dir = 6;
    Vector3 ultima;
    float paso;

    /// <summary>Pone el sprite que le toca a cada figura con nombre conocido debajo de raiz (o en toda la escena si es null).</summary>
    public static void Aplicar(Transform raiz = null)
    {
        Material();   // antes de que la cinemática apague a Abby (de ella sale el material iluminado)
        var todos = raiz != null ? raiz.GetComponentsInChildren<Transform>(true) : FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in todos)
        {
            string c = Carpeta(t);
            if (c == null || t.GetComponent<PersonaSprite>() != null) continue;
            t.gameObject.AddComponent<PersonaSprite>().carpeta = c;
        }
    }

    static string Carpeta(Transform t)
    {
        string n = t.name;
        if (n == "Coloso") return "Coloso";
        if (t.Find("Torso") == null) return null;   // solo las figuras de bloques
        if (n == "Capitan") return "Capitan";
        if (n == "Soldado" || t.Find("Cabeza/Casco") != null) return "Soldado";
        if (n.StartsWith("Miembro") || n == "Recolector" || n == "Canalizador") return "Cazador";
        if (n == "Persona" || n.StartsWith("Invitad")) return "Civil";
        return null;
    }

    void Start()
    {
        sprites = new Sprite[8];
        bool hay = false;
        for (int i = 0; i < 8; i++) { sprites[i] = Resources.Load<Sprite>($"Personajes/{carpeta}/{Dirs[i]}"); hay |= sprites[i] != null; }
        if (!hay) { Destroy(this); return; }

        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        var go = new GameObject("Sprite");
        go.transform.SetParent(transform, false);
        // compensa la escala de la figura (el coloso es un prefab escalado): el .meta ya da el tamaño en metros
        Vector3 s = transform.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, s.x), 1f / Mathf.Max(0.001f, s.y), 1f / Mathf.Max(0.001f, s.z));
        sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = Material();
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        sr.receiveShadows = true;
        sr.sprite = sprites[6] != null ? sprites[6] : System.Array.Find(sprites, x => x != null);
        ultima = transform.position;
    }

    /// <summary>El mismo material que el sprite de Abby (iluminado por las luces 3D); si no hay Abby, el de sprites.</summary>
    static Material mat;
    static Material Material()
    {
        if (mat != null) return mat;
        var p = GameObject.FindWithTag("Player");
        var abby = p != null ? p.GetComponentInChildren<AbbySpriteAnimator>(true) : null;
        var a = abby != null ? abby.GetComponent<SpriteRenderer>() : null;
        mat = a != null && a.sharedMaterial != null ? a.sharedMaterial : new Material(Shader.Find("Sprites/Default"));
        return mat;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr == null) return;
        Vector3 f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();

        // al caminar se mece un poco (las cinemáticas mueven la figura entera)
        Vector3 d = transform.position - ultima; d.y = 0f;
        ultima = transform.position;
        float v = Time.deltaTime > 0f ? d.magnitude / Time.deltaTime : 0f;
        paso += Time.deltaTime * Mathf.Clamp(v, 0f, 6f) * 2.2f;
        float bote = v > 0.2f ? Mathf.Abs(Mathf.Sin(paso * Mathf.PI)) * 0.04f : 0f;

        sr.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
        sr.transform.position = transform.position + Vector3.up * bote;

        Vector3 mira = -transform.forward; mira.y = 0f;   // las figuras de bloques tienen la cara hacia -Z (visera, medallas)
        float ang = Mathf.Atan2(Vector3.Dot(mira, f), Vector3.Dot(mira, r)) * Mathf.Rad2Deg;
        int k = ((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8;
        if (sprites[k] != null) dir = k;
        if (sprites[dir] != null) sr.sprite = sprites[dir];
    }
}
