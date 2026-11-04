using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Forma "Cala": el pueblo costero de Cala Sombría con la torre en medio (forma = Cala en el ExteriorBuilder).
/// - Bahía al norte (malecón en z = 34, agua a y = -1.4 como en el Puerto: muelle, embarcadero y barcas encajan sin ajustes).
/// - Torre (ExteriorBuilder.Torre.cs, al 60 %) al fondo, en CalaTorre (-12, -18), junto a la bahía y con el portal al sur.
/// - Abby llega por el sur (z -235) y camina por la Calle Mayor, cruzando el pueblo y la Plaza del Mercado (z -150), hasta la torre.
/// - Calles: Calle Mayor, 3 cruzadas (z -100, -150, -200), 2 avenidas hacia el puerto (x -85 y 65) y el paseo del muelle.
/// - Barrios: piedra al sur, entramado en el centro, pescadores al oeste y frente al mar, pizarra al este y en su terraza (y +3).
/// - Lomas suaves y lejanas al este, oeste y sur (no encierran al pueblo); siluetas de montes al otro lado de la bahía.
/// Casas, muelles, botes, faroles, braseros, carpas y pinos son los prefabs de Assets/Prefabs/Exterior (Abby → Exterior →
/// Importar piezas del Diorama). Todo se guarda en Assets/Prefabs/Exterior/Generado/Ext_Cala.prefab: la escena solo
/// tiene la instancia, así regenerar casi no cambia Exterior.unity. Materiales y texturas: Materials/Cala y Textures/Cala.
/// </summary>
public partial class ExteriorBuilder
{
    public enum Forma { Abismo, Cala }

    // ------------------------------------------------------------------ trazado (metros; x = este, z = norte)
    // público: ExteriorIntro arma sus tomas con estos datos

    public static readonly Vector2 CalaSpawn = new Vector2(-12f, -235f);
    public static readonly Vector2 CalaCampamento = new Vector2(24f, -238f);
    public static readonly Vector2 CalaMercado = new Vector2(-12f, -150f);
    public static readonly Vector2 CalaEmbarcadero = new Vector2(-31f, 46f);
    public const float CalaPlazaRadio = 34f;   // centrada en CalaTorre; se recorta contra el paseo (z 14)
    public const float CalaMalecon = 34f;   // borde del malecón; al norte, la bahía
    public const float CalaSur = -270f;     // límite sur del pueblo; detrás sube la loma
    public const float CalaOeste = -120f, CalaEste = 98f;   // límites a los lados
    public const float CalaMar = -1.4f;     // nivel del agua
    const float CalaTerraza = 3f;           // altura del barrio de pizarra

    static float S01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    /// <summary>Altura del suelo de la Cala: pueblo plano, fondo de la bahía, terraza este, acantilado, cabo y loma.</summary>
    public static float CalaHeight(float x, float z)
    {
        bool tierra = z <= CalaMalecon + 0.01f;
        float h = tierra ? CalaTerraza * S01((x - 70f) / 7f) * S01((z + 48f) / 6f) * S01((26f - z) / 6f) : -6f;
        // lomas anchas y bajas: se ven como paisaje a lo lejos, no como paredes junto a las casas
        float este = S01((x - CalaEste - 2f) / 30f) * S01((130f - z) / 20f)
                     * (13f + 11f * Mathf.PerlinNoise(3.1f, z * 0.02f + 7f) + 3f * (Mathf.PerlinNoise(x * 0.06f, z * 0.06f) - 0.5f));
        float oeste = S01((CalaOeste - 2f - x) / 30f) * S01((100f - z) / 15f)
                      * (10f + 9f * Mathf.PerlinNoise(z * 0.02f + 11f, 1.7f) + 3f * (Mathf.PerlinNoise(x * 0.07f + 5f, z * 0.07f) - 0.5f));
        float sur = S01((CalaSur - 2f - z) / 34f) * (10f + 8f * Mathf.PerlinNoise(x * 0.02f + 2f, 4.4f));
        // las colinas valen 0 lejos de ellas: solo cuentan donde suben, si no el fondo de la bahía (-6) quedaba en 0, encima del agua, con pasto
        float colinas = Mathf.Max(este, Mathf.Max(oeste, sur));
        if (colinas > 0.05f) h = Mathf.Max(h, colinas);
        // bajo el podio de la torre el suelo es plano; se mezcla con el terreno entre PodioRadio y PodioRadio + 6
        float d = Mathf.Sqrt((x - CalaTorre.x) * (x - CalaTorre.x) + (z - CalaTorre.y) * (z - CalaTorre.y));
        return h * S01((d - PodioRadio) / 6f);
    }

#if UNITY_EDITOR
    const string PiezasDir = "Assets/Prefabs/Exterior";
    const string GenDir = "Assets/Prefabs/Exterior/Generado";
    const string GenMallas = "Assets/Prefabs/Exterior/Generado/Mallas";
    Material mDirt, mSea;
    int lucesCala;

    void BuildCala()
    {
        MatDir = "Assets/Materials/Cala"; TexDir = "Assets/Textures/Cala";
        CreateAssets();
        CreateCalaAssets();
        if (!torreClasica) CreateTorreAssets();
        if (EsNoche) CreateNocheAssets();
        lucesCala = 0;

        var root = Group("Cala");
        CalaTerreno(root);
        CalaMaleconYMar(root);
        CalaCalles(root);
        if (torreClasica) BuildTower(root, Vector3.zero, true);
        else BuildTorreCala(root);
        lucesCala++;   // la del portal
        CalaCasas(root);
        foreach (Transform c in root.Find("Casas")) c.gameObject.AddComponent<OccluderFade>();   // la que tape a Abby deja de dibujarse
        CalaPuerto(root);
        CalaPlaza(root);
        CalaVegetacion(root);
        CalaFondo(root);
        CalaLimites(root);
        if (EsNoche) { BuildNoche(root); NocheAjustes(root); }

        GuardarComoPrefab(root.gameObject);
        AplicarEscenaHora();
        PlacePlayer();
        if (!torreClasica) ReportarCala(root);
        EditorUtility.SetDirty(gameObject);
        Debug.Log($"[Exterior] Cala generada en {GenDir}/Ext_Cala.prefab: {lucesCala} luces puntuales encendidas (+ el sol).");
    }

    void CreateCalaAssets()
    {
        var dirt = Tex("Tierra", 64, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.13f, y * 0.13f) * 0.6f + Hash(x + 91, y) * 0.4f;
            Color[] pal = { new Color(0.27f, 0.21f, 0.16f), new Color(0.33f, 0.26f, 0.19f), new Color(0.39f, 0.31f, 0.22f), new Color(0.45f, 0.36f, 0.26f) };
            return pal[Mathf.Clamp((int)(n * 4f), 0, 3)];
        });
        mDirt = Lit("Tierra", dirt, Color.white, 0.03f);
        mSea = EsNoche ? Water("Mar", new Color(0.04f, 0.09f, 0.2f, 0.6f), new Color(0.01f, 0.02f, 0.07f, 0.97f), 4f, 0.85f)   // agua más oscura de noche
                       : Water("Mar", new Color(0.12f, 0.28f, 0.34f, 0.55f), new Color(0.02f, 0.05f, 0.1f, 0.97f), 4f, 0.85f);
        // mar abierto, no estanque: olas más grandes y vivas, y espuma donde pega con el malecón, los muelles y los barcos
        mSea.SetFloat("_WaveSpeed", 0.6f);
        mSea.SetFloat("_WaveHeight", 0.07f);
        mSea.SetFloat("_WaveScale", 0.5f);
        mSea.SetFloat("_NormalStrength", 0.6f);
        mSea.SetFloat("_FoamDistance", 0.7f);
        EditorUtility.SetDirty(mSea);
        AssetDatabase.SaveAssets();
    }

    // ---------------- terreno: una malla con dos materiales (pasto arriba, roca en pendientes y bajo el agua)

    void CalaTerreno(Transform root)
    {
        const float cell = 3f, x0 = -240f, x1 = 222f, z0 = -372f, z1 = 222f;
        int nx = Mathf.RoundToInt((x1 - x0) / cell), nz = Mathf.RoundToInt((z1 - z0) / cell);
        var v = new Vector3[(nx + 1) * (nz + 1)];
        var uv = new Vector2[v.Length];
        for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
            {
                float x = x0 + i * cell, z = z0 + j * cell;
                v[j * (nx + 1) + i] = new Vector3(x, CalaHeight(x, z), z);
                uv[j * (nx + 1) + i] = new Vector2(x, z) / 4f;
            }
        var pasto = new List<int>(); var roca = new List<int>();
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                foreach (var t in new[] { new[] { a, c, b }, new[] { b, c, d } })
                {
                    Vector3 n = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]).normalized;
                    bool bajoAgua = v[t[0]].y < CalaMar && v[t[1]].y < CalaMar && v[t[2]].y < CalaMar;
                    (n.y < 0.75f || bajoAgua ? roca : pasto).AddRange(t);
                }
            }
        var m = new Mesh { name = "CalaTerreno", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v, uv = uv, subMeshCount = 2 };
        m.SetTriangles(pasto, 0); m.SetTriangles(roca, 1);
        m.RecalculateNormals(); m.RecalculateBounds();
        var go = MeshObject(root, "Terreno", m, mGrass, true, false);
        go.GetComponent<MeshRenderer>().sharedMaterials = new[] { mGrass, mCliff };
        go.AddComponent<MeshCollider>().sharedMesh = m;
    }

    // ---------------- malecón, orilla y mar

    void CalaMaleconYMar(Transform root)
    {
        var g = Group("Malecon", root);
        float xa = CalaOeste, xb = CalaEste;
        var muro = MeshObject(g, "Muro", BoxMesh(new Vector3(xb - xa, 4.1f, 1.2f), 2f), mStone, true, false);
        muro.transform.localPosition = new Vector3((xa + xb) / 2f, 0.1f - 4.1f / 2f, CalaMalecon);
        muro.AddComponent<BoxCollider>();
        // no se cae nadie al mar: muro invisible en la orilla, con huecos para el embarcadero y el muelle de piedra
        float[] huecos = { -38f, -32f, 13f, 19f };
        float desde = xa;
        for (int i = 0; i <= huecos.Length; i += 2)
        {
            float hasta = i < huecos.Length ? huecos[i] : xb;
            if (hasta - desde > 0.2f) Solido(g, "Collider_Orilla", new Vector3((desde + hasta) / 2f, 1f, CalaMalecon + 0.3f), new Vector3(hasta - desde, 2f, 0.4f));
            if (i + 1 < huecos.Length) desde = huecos[i + 1];
        }
        // el mar empieza en el malecón (antes seguía 150 m por debajo del pueblo, donde nadie lo ve)
        var mar = MeshObject(root, "Mar", Quad(1800f, 1400f, 4f), mSea, false, false);
        mar.transform.localPosition = new Vector3(0f, CalaMar, CalaMalecon - 1f + 700f);
    }

    // ---------------- calles y plaza (un poco por encima del suelo para no parpadear con él)

    void CalaCalles(Transform root)
    {
        var g = Group("Calles", root);
        // la plaza de la torre se recorta contra el paseo del muelle (z 14); el podio pasa por encima
        var disco = Disc(CalaPlazaRadio, 128, 18, 2f, 0.03f);
        var pv = disco.vertices; var puv = new Vector2[pv.Length];
        for (int i = 0; i < pv.Length; i++)
        {
            pv[i].z = Mathf.Min(pv[i].z, 14f - CalaTorre.y);
            puv[i] = new Vector2(pv[i].x, pv[i].z) / 2f;
        }
        disco.vertices = pv; disco.uv = puv; disco.RecalculateBounds();
        MeshObject(g, "Plaza", disco, mStone, false, false).transform.localPosition = new Vector3(CalaTorre.x, 0f, CalaTorre.y);
        MeshObject(g, "Atrio", Quad(18f, 8f, 2f), mStone, false, false).transform.localPosition = new Vector3(CalaTorre.x, 0.035f, CalaTorre.y - CalaPlazaRadio - 2f);
        MeshObject(g, "PlazaMercado", Disc(21f, 64, 10, 2f, 0.05f), mStone, false, false).transform.localPosition = new Vector3(CalaMercado.x, 0f, CalaMercado.y);
        float cx = CalaTorre.x;
        Calle(g, "CalleMayorSur", new Vector2(cx, CalaSur), new Vector2(cx, -200f), 8f, mDirt);
        Calle(g, "CalleMayor", new Vector2(cx, -200f), new Vector2(cx, CalaTorre.y - CalaPlazaRadio - 4f), 8f, mStone);
        foreach (float z in new[] { -100f, -150f, -200f })
            Calle(g, "Cruzada", new Vector2(-85f, z), new Vector2(65f, z), 6f, mDirt);
        Calle(g, "AvenidaOeste", new Vector2(-85f, -200f), new Vector2(-85f, 27f), 6f, mDirt);
        Calle(g, "AvenidaEste", new Vector2(65f, -200f), new Vector2(65f, 27f), 6f, mDirt);
        Calle(g, "Paseo", new Vector2(CalaOeste + 0.5f, 30.2f), new Vector2(CalaEste - 2f, 30.2f), 6.4f, mStone);
        Calle(g, "CalleTerraza", new Vector2(65f, -10f), new Vector2(80f, -10f), 5f, mDirt);
        Calle(g, "BajadaAtrio", new Vector2(cx, 14f), new Vector2(cx, 27f), 6f, mStone);
    }

    /// <summary>Franja de calle de a hacia b que sigue el suelo (sirve igual en plano y en la subida a la terraza).</summary>
    void Calle(Transform parent, string name, Vector2 a, Vector2 b, float width, Material mat)
    {
        Vector2 d = b - a; float len = d.magnitude; Vector2 dir = d / len, side = new Vector2(dir.y, -dir.x);
        int n = Mathf.Max(1, Mathf.CeilToInt(len));
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int i = 0; i <= n; i++)
        {
            float k = len * i / n;
            Vector2 p = a + dir * k;
            foreach (float s in new[] { -1f, 1f })
            {
                Vector2 q = p + side * width / 2f * s;
                v.Add(new Vector3(q.x, CalaHeight(q.x, q.y) + 0.025f, q.y));
                uv.Add(new Vector2(s < 0f ? 0f : width, k) / 2f);
            }
            if (i < n) tri.AddRange(new[] { 2 * i, 2 * i + 2, 2 * i + 3, 2 * i, 2 * i + 3, 2 * i + 1 });
        }
        var m = new Mesh { name = "Calle_" + name };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        MeshObject(parent, name, m, mat, false, false);
    }

    // ---------------- casas (frente = +Z local del prefab): a lo largo de las calles, con su frente hacia la calle

    /// <summary>Lugares que quedan libres para caminar (x0, z0, x1, z1): las calles con su banqueta.</summary>
    // se arma al pedirla: CalaTorre vive en otro archivo parcial y como campo estático aquí todavía valía (0, 0)
    static Rect[] calaLibres;
    static Rect[] CalaLibres => calaLibres ??= new[]
    {
        Rect.MinMaxRect(CalaTorre.x - 7f, CalaSur - 4f, CalaTorre.x + 7f, CalaTorre.y - CalaPlazaRadio),   // Calle Mayor
        Rect.MinMaxRect(-88f, -106f, 68f, -94f), Rect.MinMaxRect(-88f, -156f, 68f, -144f), Rect.MinMaxRect(-88f, -206f, 68f, -194f),   // cruzadas
        Rect.MinMaxRect(-91f, -206f, -79f, 30f), Rect.MinMaxRect(59f, -206f, 71f, 30f),   // avenidas
        Rect.MinMaxRect(CalaOeste - 2f, 25.5f, CalaEste, 35f),   // paseo del muelle
        Rect.MinMaxRect(-42f, 14f, -28f, 36f), Rect.MinMaxRect(9f, 14f, 23f, 36f),   // bajadas al embarcadero y al muelle
        Rect.MinMaxRect(CalaTorre.x - 8f, 12f, CalaTorre.x + 8f, 36f),   // de la plaza de la torre al paseo
        Rect.MinMaxRect(62f, -14f, 82f, -6f),   // subida a la terraza
    };

    static bool EnLibre(Vector2 p, float margen)
    {
        foreach (var r in CalaLibres) if (r.Contains(p) || DistRect(r, p) < margen) return true;
        return Vector2.Distance(p, CalaTorre) < CalaPlazaRadio + margen || Vector2.Distance(p, CalaMercado) < 21f + margen
            || Vector2.Distance(p, CalaCampamento) < 15f + margen || Vector2.Distance(p, CalaSpawn) < 9f + margen;
    }

    readonly List<Bounds> ocupado = new List<Bounds>();

    /// <summary>Pone la pieza si no choca con lo ya puesto; si choca, la quita.</summary>
    GameObject Colocar(Transform g, string prefab, float x, float z, float yaw, float holgura)
    {
        var go = Pieza(g, prefab, x, z, yaw);
        if (go == null) return null;
        var b = Limites(go);
        var bb = b; bb.Expand(new Vector3(holgura, 0f, holgura));
        foreach (var o in ocupado) if (bb.Intersects(o)) { Object.DestroyImmediate(go); return null; }
        ocupado.Add(b);
        return go;
    }

    /// <summary>Barrio según el lugar: pescadores al oeste, pizarra al este, piedra al sur y entramado en el centro.</summary>
    static string TipoCasa(float x, float z, int n)
    {
        string letra = n % 2 == 0 ? "A" : "B";
        if (z > -100f && x < -40f) return "CasaPescador_" + letra;
        if (z > -100f && x > 35f) return "CasaPizarra_" + letra;
        if (z < -170f) return (n % 5 == 0 ? "CasaEntramado_" : "CasaPiedra_") + letra;
        return (n % 3 == 0 ? "CasaPiedra_" : "CasaEntramado_") + letra;
    }

    void CalaCasas(Transform root)
    {
        var g = Group("Casas", root);
        ocupado.Clear();
        int n = 0, casas = 0;
        void Casa(string prefab, float x, float z, float yaw)
        {
            n++;
            var p = new Vector2(x, z);
            if (EnLibre(p, 5.5f)) return;   // la casa mide unos 8-10 m: desde su centro, sin invadir la calle
            float h0 = CalaHeight(x, z), dh = 0f;
            for (int k = 0; k < 4; k++) dh = Mathf.Max(dh, Mathf.Abs(CalaHeight(x + (k < 2 ? -5f : 5f), z + (k % 2 == 0 ? -5f : 5f)) - h0));
            if (dh > 0.4f || h0 < -0.2f) return;   // pendiente, loma o agua
            var go = Colocar(g, prefab ?? TipoCasa(x, z, n), x, z, yaw + R(-4f, 4f), 2f);
            if (go == null) return;
            Luces(go, l => false);   // las ventanas brillan con su material: sin luces de más
            casas++;
        }
        const float retiro = 9f;   // de la orilla de la calle a la casa: calles anchas, nada de pasillos
        // Calle Mayor: frente a la calle
        for (float z = -262f; z < -60f; z += 18f)
        {
            Casa(null, CalaTorre.x - 4f - retiro, z + R(-1.5f, 1.5f), 90f);
            Casa(null, CalaTorre.x + 4f + retiro, z + R(-1.5f, 1.5f), -90f);
        }
        // cruzadas
        foreach (float zc in new[] { -100f, -150f, -200f })
            for (float x = -76f; x < 62f; x += 22f)
            {
                Casa(null, x + R(-2f, 2f), zc - 3f - retiro, 0f);
                Casa(null, x + R(-2f, 2f), zc + 3f + retiro, 180f);
            }
        // avenidas
        foreach (float xa in new[] { -85f, 65f })
            for (float z = -190f; z < 16f; z += 22f)
            {
                Casa(null, xa - 3f - retiro, z + R(-2f, 2f), 90f);
                Casa(null, xa + 3f + retiro, z + R(-2f, 2f), -90f);
            }
        // frente al mar y terraza de pizarra
        for (float x = -112f; x < 92f; x += 15f) Casa("CasaPescador_" + (n % 2 == 0 ? "A" : "B"), x, 18.5f, 0f);
        for (float z = -40f; z < 24f; z += 13f) Casa("CasaPizarra_" + (n % 2 == 0 ? "A" : "B"), 86f, z, -90f);
        Debug.Log($"[Exterior] Pueblo: {casas} casas.");
    }

    static float DistRect(Rect r, Vector2 p)
    {
        float dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax), dz = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static Bounds Limites(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
        return b;
    }

    // ---------------- puerto: muelle de piedra, embarcadero, barcas y veleros

    void CalaPuerto(Transform root)
    {
        var g = Group("Puerto", root);
        // el muelle y el embarcadero traen el pivote del mapa del Puerto: su z = 0 es la línea del malecón
        var muelle = Pieza(g, "MuelleDePiedra", 13f, CalaMalecon, 0f, 0f);
        Luces(muelle, l => muelle.transform.InverseTransformPoint(l.transform.position).z > 40f);   // solo los 2 faroles del final
        var emb = Pieza(g, "Embarcadero", -12.5f, CalaMalecon, 0f, 0f);
        Luces(emb, l => true);
        foreach (float x in new[] { -18f, -10f, -2f, 6f })
            Pieza(g, "Barca", x, 36.2f, R(-8f, 8f), CalaMar + 0.05f);
        Pieza(g, "Velero", 27f, CalaMalecon + 17f, 0f, CalaMar);   // misma distancia al muelle que en el Puerto: las amarras llegan a los bolardos
        Pieza(g, "Velero", 70f, 150f, 70f, CalaMar);                // fondeado en la bahía
    }

    // ---------------- plaza: braseros del portal, faroles, campamento y letrero

    void CalaPlaza(Transform root)
    {
        var g = Group("Plaza", root);
        foreach (float x in new[] { -8f, 8f })   // sobre el podio, a los lados del marco del portal
            Luces(Pieza(g, "Brasero", CalaTorre.x + x, CalaTorre.y - Mathf.Sqrt(20.4f * 20.4f - x * x), 0f, 0.45f), l => true);

        // Plaza del Mercado: fuente al centro, carpas alrededor y braseros
        BuildFountain();
        var fuente = transform.Find("Ext_Fuente");
        if (fuente == null) fuente = transform.Find("Fuente");
        if (fuente != null) { fuente.SetParent(g, false); fuente.localPosition = new Vector3(CalaMercado.x, 0f, CalaMercado.y); fuente.name = "FuenteMercado"; }
        foreach (var (dx, dz, yaw) in new[] { (-13f, -9f, 40f), (13f, -9f, -40f), (-13f, 9f, 140f), (13f, 9f, -140f) })
            ocupado.Add(Limites(Pieza(g, "Carpa", CalaMercado.x + dx, CalaMercado.y + dz, yaw)));
        foreach (var (dx, dz) in new[] { (-17f, 0.5f), (17f, 0.5f) })
            Luces(Pieza(g, "Brasero", CalaMercado.x + dx, CalaMercado.y + dz, 0f, 0.05f), l => true);

        // faroles a lo largo de las calles: solo algunos con luz real (los demás brillan con su material)
        int f = 0;
        void Farol(float x, float z, bool luz)
        {
            var go = Colocar(g, "Farol", x, z, 0f, 0.2f);
            if (go != null) { Luces(go, l => luz); f++; }
        }
        for (float z = -258f; z < -56f; z += 26f)
        {
            bool luz = ((int)(-z / 26f)) % 2 == 0;
            Farol(CalaTorre.x - 5.5f, z, luz); Farol(CalaTorre.x + 5.5f, z + 13f, !luz);
        }
        foreach (float zc in new[] { -100f, -150f, -200f })
            foreach (float x in new[] { -66f, -38f, 14f, 42f }) Farol(x, zc + 4f, false);
        foreach (float xa in new[] { -85f, 65f })
            for (float z = -180f; z < 20f; z += 44f) Farol(xa + 4f, z, z > -60f);
        foreach (var (x, z, luz) in new[] { (-60f, 26f, false), (-20f, 26f, true), (4f, 26f, true), (40f, 26f, false) }) Farol(x, z, luz);

        // campamento de la expedición, a la entrada del pueblo: lo primero que se ve al llegar
        ocupado.Add(Limites(Pieza(g, "Carpa", CalaCampamento.x - 8f, CalaCampamento.y - 4f, 20f)));
        ocupado.Add(Limites(Pieza(g, "Carpa", CalaCampamento.x + 8f, CalaCampamento.y - 6f, -25f)));
        Luces(Pieza(g, "Fogata", CalaCampamento.x, CalaCampamento.y, 0f), l => true);
        Letrero(g, new Vector3(-27f, 0f, 29.5f));
        Letrero(g, new Vector3(CalaTorre.x + 9f, 0f, -252f));   // a la entrada, donde llega Abby
        Debug.Log($"[Exterior] {f} faroles.");
    }

    /// <summary>Letrero de madera con el nombre del pueblo, mirando al sur (hacia quien llega por el mirador).</summary>
    void Letrero(Transform parent, Vector3 pos)
    {
        var t = Group("Letrero", parent);
        t.localPosition = pos;
        foreach (float x in new[] { -1.9f, 1.9f }) Primitive(t, PrimitiveType.Cube, "Poste", new Vector3(x, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f), mBark);
        Primitive(t, PrimitiveType.Cube, "Tabla", new Vector3(0f, 1.95f, 0f), new Vector3(4.3f, 0.95f, 0.1f), mPlank);
        var font = Resources.Load<Font>("Fuentes/PressStart2P-Regular");
        if (font == null) return;
        var txt = new GameObject("Nombre");
        txt.transform.SetParent(t, false);
        txt.transform.localPosition = new Vector3(0f, 1.95f, -0.06f);
        var tm = txt.AddComponent<TextMesh>();
        // la fuente pixel no trae mayúsculas con acento (salía "SOMBRíA"): sin acentos en el letrero
        tm.text = Historia.Ciudad.ToUpperInvariant().Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U');
        tm.font = font;
        tm.fontSize = 60; tm.characterSize = 0.05f;   // ~0.3 m de alto
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.86f, 0.76f, 0.55f);
        txt.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    // ---------------- vegetación: pinos en el cabo, la loma y arriba del acantilado; pocos árboles en el pueblo

    void CalaVegetacion(Transform root)
    {
        var g = Group("Vegetacion", root);
        var puestos = new List<Vector2>();
        void Zona(Rect r, int n)
        {
            for (int k = 0, tries = 0; k < n && tries < n * 60; tries++)
            {
                var p = new Vector2(R(r.xMin, r.xMax), R(r.yMin, r.yMax));
                float h = CalaHeight(p.x, p.y);
                float slope = new Vector2(CalaHeight(p.x + 1f, p.y) - CalaHeight(p.x - 1f, p.y), CalaHeight(p.x, p.y + 1f) - CalaHeight(p.x, p.y - 1f)).magnitude / 2f;
                if (h < 1f || slope > 0.7f) continue;
                bool cerca = false;
                foreach (var o in puestos) if ((o - p).sqrMagnitude < 3.5f * 3.5f) { cerca = true; break; }
                if (cerca) continue;
                puestos.Add(p);
                var pino = Pieza(g, "Pino", p.x, p.y, R(0f, 360f), h - 0.1f);
                if (pino != null) pino.transform.localScale = Vector3.one * R(0.9f, 1.4f);
                k++;
            }
        }
        Zona(Rect.MinMaxRect(-200f, -280f, -126f, 88f), 45);   // loma oeste
        Zona(Rect.MinMaxRect(-130f, -350f, 110f, -278f), 40);   // loma sur
        Zona(Rect.MinMaxRect(105f, -280f, 190f, 105f), 40);    // loma este
        // en el pueblo: árboles y algún pino en los patios, nunca sobre una calle
        for (int k = 0, puestos2 = 0; k < 400 && puestos2 < 70; k++)
        {
            var p = new Vector2(R(CalaOeste + 3f, CalaEste - 3f), R(CalaSur + 3f, 12f));
            if (EnLibre(p, 3f) || Mathf.Abs(CalaHeight(p.x, p.y)) > 0.4f) continue;
            var go = Colocar(g, k % 4 == 0 ? "Pino" : "Arbol", p.x, p.y, R(0f, 360f), 1.5f);
            if (go != null) puestos2++;
        }
    }

    // ---------------- fondo: siluetas de montes al otro lado de la bahía (la niebla las aclara)

    void CalaFondo(Transform root)
    {
        var g = Group("Montes", root);
        foreach (var (z, alto, semilla) in new[] { (430f, 70f, 1.3f), (680f, 150f, 6.1f) })
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (float x = -800f, i = 0; x <= 800f; x += 20f, i++)
            {
                float top = alto * (0.5f + Mathf.PerlinNoise(x * 0.004f + semilla, semilla)) + 25f * Mathf.PerlinNoise(x * 0.02f, semilla * 3f);
                float zz = z + 60f * (Mathf.PerlinNoise(x * 0.003f, semilla + 9f) - 0.5f);
                v.Add(new Vector3(x, -10f, zz)); v.Add(new Vector3(x, top, zz));
                uv.Add(new Vector2(x / 8f, 0f)); uv.Add(new Vector2(x / 8f, top / 8f));
                if (i > 0) { int b = v.Count - 4; tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }   // de frente al pueblo (-Z)
            }
            var m = new Mesh { name = "Montes_" + z };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            MeshObject(g, "Montes", m, mCliff, false, false);
        }
    }

    // ---------------- límites invisibles (al norte cierran el malecón, el muelle y el embarcadero con sus pretiles)

    void CalaLimites(Transform root)
    {
        var g = Group("Limites", root);
        Solido(g, "Oeste", new Vector3(CalaOeste, 5f, (CalaSur + CalaMalecon) / 2f), new Vector3(0.6f, 10f, CalaMalecon - CalaSur));
        Solido(g, "Este", new Vector3(CalaEste, 5f, (CalaSur + CalaMalecon) / 2f), new Vector3(0.6f, 10f, CalaMalecon - CalaSur));
        Solido(g, "Sur", new Vector3((CalaOeste + CalaEste) / 2f, 5f, CalaSur), new Vector3(CalaEste - CalaOeste, 10f, 0.6f));
    }

    // ---------------- ayudantes

    GameObject Pieza(Transform parent, string prefab, float x, float z, float yaw, float? y = null)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PiezasDir}/{prefab}.prefab");
        if (asset == null) { Debug.LogWarning($"[Exterior] Falta {PiezasDir}/{prefab}.prefab (Abby → Exterior → Importar piezas del Diorama)."); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.localPosition = new Vector3(x, y ?? CalaHeight(x, z), z);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    /// <summary>Deja encendidas solo las luces que pide <paramref name="keep"/> (las otras quedan apagadas en la instancia).</summary>
    void Luces(GameObject go, System.Func<Light, bool> keep)
    {
        if (go == null) return;
        foreach (var l in go.GetComponentsInChildren<Light>(true))
        {
            l.enabled = keep(l);
            l.shadows = LightShadows.None;
            if (l.enabled) lucesCala++;
        }
    }

    static void Solido(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    /// <summary>Caja con UV en metros / tile en cada cara (un cubo escalado estira la textura).</summary>
    static Mesh BoxMesh(Vector3 size, float tile)
    {
        Vector3 h = size / 2f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        void Face(Vector3 a, Vector3 u, Vector3 w)
        {
            int i = v.Count;
            v.Add(a); v.Add(a + u); v.Add(a + w); v.Add(a + u + w);
            float lu = u.magnitude / tile, lw = w.magnitude / tile;
            uv.Add(Vector2.zero); uv.Add(new Vector2(lu, 0f)); uv.Add(new Vector2(0f, lw)); uv.Add(new Vector2(lu, lw));
            tri.AddRange(new[] { i, i + 2, i + 3, i, i + 3, i + 1 });
        }
        Face(new Vector3(h.x, -h.y, h.z), new Vector3(-size.x, 0f, 0f), new Vector3(0f, size.y, 0f));    // +Z
        Face(new Vector3(-h.x, -h.y, -h.z), new Vector3(size.x, 0f, 0f), new Vector3(0f, size.y, 0f));   // -Z
        Face(new Vector3(h.x, -h.y, -h.z), new Vector3(0f, 0f, size.z), new Vector3(0f, size.y, 0f));    // +X
        Face(new Vector3(-h.x, -h.y, h.z), new Vector3(0f, 0f, -size.z), new Vector3(0f, size.y, 0f));   // -X
        Face(new Vector3(-h.x, h.y, -h.z), new Vector3(size.x, 0f, 0f), new Vector3(0f, 0f, size.z));    // +Y
        Face(new Vector3(-h.x, -h.y, h.z), new Vector3(size.x, 0f, 0f), new Vector3(0f, 0f, -size.z));   // -Y
        var m = new Mesh { name = "Caja" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>
    /// Guarda las mallas generadas como assets (nombre + huella del contenido: la misma malla da el mismo archivo y
    /// mallas iguales se comparten), borra las que ya no se usan, marca lo propio como estático y guarda el prefab.
    /// </summary>
    void GuardarComoPrefab(GameObject root)
    {
        EnsureFolder(GenMallas);
        var usadas = new HashSet<string>();
        var cache = new Dictionary<Mesh, Mesh>();
        Mesh Guardar(Mesh m)
        {
            if (m == null || EditorUtility.IsPersistent(m)) return m;   // ya es asset (o primitiva de Unity)
            if (cache.TryGetValue(m, out var hecha)) return hecha;
            string path = $"{GenMallas}/{m.name}_{Huella(m):x16}.asset";
            usadas.Add(path);
            var a = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (a == null) { AssetDatabase.CreateAsset(m, path); a = m; }
            cache[m] = a;
            return a;
        }
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (!PrefabUtility.IsPartOfPrefabInstance(mf)) mf.sharedMesh = Guardar(mf.sharedMesh);
        foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true))
            if (!PrefabUtility.IsPartOfPrefabInstance(mc)) mc.sharedMesh = Guardar(mc.sharedMesh);
        foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { GenMallas }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (!usadas.Contains(p)) AssetDatabase.DeleteAsset(p);
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (!PrefabUtility.IsPartOfPrefabInstance(t) && t.GetComponent<TextMesh>() == null && t.GetComponent<Light>() == null && t.GetComponent<TorreHalo>() == null && t.GetComponentInParent<ExteriorNoche>(true) == null)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, GenDir + "/Ext_Cala.prefab", InteractionMode.AutomatedAction);
    }

    static ulong Huella(Mesh m)
    {
        ulong h = 14695981039346656037UL;
        void Mix(int x) { unchecked { for (int s = 0; s < 32; s += 8) { h ^= (byte)(x >> s); h *= 1099511628211UL; } } }
        foreach (var p in m.vertices) { Mix(System.BitConverter.SingleToInt32Bits(p.x)); Mix(System.BitConverter.SingleToInt32Bits(p.y)); Mix(System.BitConverter.SingleToInt32Bits(p.z)); }
        foreach (var p in m.uv) { Mix(System.BitConverter.SingleToInt32Bits(p.x)); Mix(System.BitConverter.SingleToInt32Bits(p.y)); }
        for (int s = 0; s < m.subMeshCount; s++) { Mix(-1); foreach (int i in m.GetTriangles(s)) Mix(i); }
        return h;
    }

    /// <summary>Pone a Abby y al rig de cámara en el punto de inicio, mirando hacia el norte del mapa (la torre).</summary>
    void PlacePlayer()
    {
        var rot = transform.rotation;
        var p = GameObject.FindWithTag("Player");
        if (p != null) p.transform.SetPositionAndRotation(PlayerSpawn + Vector3.up * 0.05f, rot);
        var rig = FindAnyObjectByType<CameraRig>();
        if (rig != null)
        {
            rig.transform.SetPositionAndRotation(PlayerSpawn, rot);
            // la torre mide ~165 m y la niebla llega a fogEnd: el far clip no puede cortar antes que la niebla
            var cam = rig.GetComponentInChildren<Camera>();
            var sky = FindAnyObjectByType<ExteriorSky>();
            float far = Mathf.Max(600f, (sky != null ? sky.fogEnd : 650f) + 50f);
            if (cam != null && cam.farClipPlane < far) { cam.farClipPlane = far; EditorUtility.SetDirty(cam); }
        }
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }
#endif
}
