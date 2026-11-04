using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Torre de la Cala (References/logo_torre.png, 02_torre, 10_expedicion, 11_torre_piso15): podio de 5 escalones, 6 pisos
/// escalonados de piedra casi negra con tono violeta, costillas, contrafuertes, ventanas en arco, venas emisivas,
/// 16 púas laterales en 3 anillos (decorativas, sin colisión), corona de 8 púas con llama y remolino, haces y halo.
/// Todo procedural; sin luces nuevas (el brillo es emisivo HDR + Bloom). torreClasica = true vuelve al BuildTower anterior.
/// Medidas en metros, centro en CalaTorre; el portal mira al sur (-Z).
/// </summary>
public partial class ExteriorBuilder
{
    /// <summary>Centro de la torre en la Cala (x, z). ExteriorIntro apunta las tomas aquí.</summary>
    public static readonly Vector2 CalaTorre = new Vector2(-12f, -18f);
    /// <summary>La torre se arma con sus medidas de siempre y se reduce con esta escala (a tamaño completo aplastaba al pueblo).</summary>
    public const float TorreEscala = 0.6f;
    public const float TorreAltura = 165f * TorreEscala;
    public const float PodioRadio = 44f * TorreEscala;   // el terreno es plano dentro de este radio (CalaHeight)
    const float PodioAlto = 1.25f;         // 5 escalones de 0,25 m: Abby los sube sin rampa

    [Header("Cala")]
    [Tooltip("Dibuja la torre anterior (cono liso, cian) en vez de la torre ancha del logo.")]
    public bool torreClasica = false;

#if UNITY_EDITOR
    Material mTorreRoca, mTorrePodio, mTorreVentana, mTorreGlifo, mTorreVena, mTorreHaz, mTorreHalo, mTorreHaloCorona, mTorreLlama, mTorreHumo;

    static readonly string[][] Runas =
    {
        new[] { "...#...", "..###..", ".#.#.#.", "#..#..#", "...#...", "...#...", "..###.." },
        new[] { "#######", "#.....#", "#.###.#", "#.#.#.#", "#.###.#", "#.....#", "#######" },
        new[] { "#.....#", ".#...#.", "..#.#..", "...#...", "..#.#..", ".#...#.", "#.....#" },
        new[] { "..###..", ".#...#.", "#.....#", "#..#..#", "#.....#", ".#...#.", "..###.." },
        new[] { "#.#.#.#", "#.#.#.#", "#.###.#", "...#...", "..###..", "...#...", "..#.#.." },
        new[] { "###....", "..#....", "..###..", "....#..", "..###..", "..#....", "###...." },
        new[] { "...#...", "..#.#..", ".#...#.", "#######", ".#...#.", "..#.#..", "...#..." },
        new[] { "#..#..#", "#..#..#", "#######", "...#...", "#######", "#..#..#", "#..#..#" },
    };

    void CreateTorreAssets()
    {
        var roca = Tex("TorreRoca", 64, (x, y) =>
        {
            int row = y / 16, ox = (row % 2) * 16, cx = (x + ox) % 32, cy = y % 16;
            if (cx == 0 || cy == 0) return new Color(0.03f, 0.02f, 0.05f);
            float v = 0.75f + Hash((x + ox) / 32 + row * 5, row) * 0.35f + (Hash(x, y) - 0.5f) * 0.12f;
            if (cy == 1) v += 0.15f;
            return new Color(0.13f * v, 0.09f * v, 0.2f * v);
        });
        // glifos: 2 filas x 4 runas de 7x7 (a 2x), en degradado violeta y con rieles; nada blanco
        var glifo = Tex("TorreGlifo", 64, (x, y) =>
        {
            var nada = new Color(0f, 0f, 0f, 0f);
            if (y == 3 || y == 60) return new Color(0.4f, 0.14f, 0.7f, 1f);
            int fila = y >= 10 && y < 24 ? 0 : (y >= 38 && y < 52 ? 1 : -1);
            int cx = x % 16;
            if (fila < 0 || cx == 0 || cx == 15) return nada;
            int ly = y - (fila == 0 ? 10 : 38), gx = (cx - 1) / 2, gy = ly / 2;
            int id = (int)(Hash(x / 16 + 7 * fila, 11 + fila * 3) * 8f) % 8;
            if (Runas[id][6 - gy][gx] != '#') return nada;
            float k = ly / 13f;
            return new Color(Mathf.Lerp(0.55f, 0.9f, k), Mathf.Lerp(0.2f, 0.55f, k), 1f, 1f);
        });
        var vena = Tex("TorreVena", 64, (x, y) =>
        {
            float cx = 32f + 9f * Mathf.Sin(y * 0.22f) + (Hash(y / 4, 5) - 0.5f) * 8f;
            float d = Mathf.Abs(x - cx);
            int by = y % 20;
            float bx = cx - by * 1.8f;
            bool rama = by < 8 && x < cx && Mathf.Abs(x - bx) < 1.2f && (y / 20) % 2 == 0;
            float fade = S01(y / 8f) * S01((63 - y) / 8f);
            if (d < 1.2f || rama) return new Color(0.95f, 0.7f, 1f, fade);
            if (d < 2.6f) return new Color(0.45f, 0.15f, 0.9f, 0.7f * (1f - d / 2.6f) * fade);
            return new Color(0f, 0f, 0f, 0f);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var haz = Tex("TorreHaz", 64, (x, y) =>
        {
            float u = Mathf.Abs((x + 0.5f) / 64f * 2f - 1f), v = (y + 0.5f) / 64f;
            return new Color(1f, 1f, 1f, Mathf.Pow(1f - u, 2.5f) * S01(v / 0.15f) * Mathf.Pow(1f - v, 1.3f));
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var halo = Tex("TorreHalo", 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
            return new Color(1f, 1f, 1f, d < 1f ? Mathf.Pow(1f - d, 2.2f) : 0f);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var dot = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/SoftDot.png");
        var cloud = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/CloudPuff.png");

        mTorreRoca = Lit("TorreRoca", roca, Color.white, 0.12f);
        mTorrePodio = Lit("TorrePodio", (Texture2D)mStone.GetTexture("_BaseMap"), new Color(0.4f, 0.34f, 0.5f), 0.15f);
        mTorreVentana = Emissive("TorreVentana", new Color(0.8f, 0.35f, 1f), 4.5f);
        mTorreGlifo = Particula("TorreGlifo", glifo, new Color(0.62f, 0.22f, 1f) * 1.35f, true);
        mTorreVena = Particula("TorreVena", vena, new Color(0.65f, 0.25f, 1.1f) * 1.5f, true);
        mTorreHaz = Particula("TorreHaz", haz, new Color(0.75f, 0.3f, 1.2f) * 1.5f, true);
        mTorreHalo = Particula("TorreHalo", halo, new Color(0.5f, 0.18f, 0.95f) * 1.2f, true);
        mTorreHaloCorona = Particula("TorreHaloCorona", halo, new Color(0.9f, 0.4f, 1.4f) * 1.6f, true);
        mTorreLlama = Particula("TorreLlama", dot, new Color(1.8f, 0.7f, 2.8f) * (EsNoche ? 0.75f : 1f), true);   // de noche con menos brillo: las partículas aditivas suman y se queman
        mTorreHumo = Particula("TorreHumo", cloud, new Color(0.22f, 0.14f, 0.32f), false);
        AssetDatabase.SaveAssets();
    }

    static Material Particula(string name, Texture2D tex, Color color, bool aditivo)
    {
        color.a = 1f;
        var m = GetOrCreate(name, "Abby/Particle");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", aditivo ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Vector3 Dir(float grados) { float a = grados * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
    static Quaternion Mirando(float grados) => Quaternion.LookRotation(-Dir(grados));   // la cara visible del quad mira a -Z local

    // ---------------- torre

    void BuildTorreCala(Transform parent)
    {
        var root = Group("AbbysTower", parent);
        root.localPosition = new Vector3(CalaTorre.x, 0f, CalaTorre.y);

        var temp = new List<Mesh>();
        var roca = new List<CombineInstance>();
        var glifos = new List<CombineInstance>();
        var ventanas = new List<CombineInstance>();
        var venas = new List<CombineInstance>();
        var haces = new List<CombineInstance>();
        void Add(List<CombineInstance> lista, Mesh m, Vector3 pos, Quaternion rot)
        {
            temp.Add(m);
            lista.Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, rot, Vector3.one) });
        }
        Mesh Fusionar(List<CombineInstance> lista, string nombre)
        {
            var m = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.CombineMeshes(lista.ToArray(), true, true);
            m.RecalculateBounds();
            return m;
        }

        // podio: 5 escalones de 0,25 m con collider propio
        float[] pr = { 44f, 40f, 36f, 32f, 28f };
        for (int i = 0; i < pr.Length; i++)
        {
            var m = Cylinder(pr[i], pr[i], 0.25f * (i + 1), 96, 2f, true);
            var go = MeshObject(root, "Podio_" + i, m, mTorrePodio, false, false);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = m;
            mc.convex = true;
        }

        // pisos: alto, radio de abajo y de arriba, bahías (una costilla entre cada par) y ventanas
        float[] h = { 36f, 30f, 25f, 20f, 16f, 14f };
        float[] ra = { 25f, 20f, 16f, 12.5f, 10.5f, 9.5f };
        float[] rb = { 22.5f, 17.5f, 14f, 11f, 9.8f, 8.5f };
        int[] bahias = { 12, 12, 10, 8, 8, 6 };
        float[] arcoA = { 2.2f, 1.8f, 1.5f, 0f, 0f, 1.5f };
        float[] arcoHs = { 8f, 7f, 6f, 0f, 0f, 4.4f };
        Mesh rend7 = QuadUV(1.6f, 7f, 1f, 1f), rend6 = QuadUV(1.6f, 6f, 1f, 1f), rend5 = QuadUV(1.6f, 5f, 1f, 1f), rombo = Rombo(2.6f, 4.2f);
        temp.Add(rend7); temp.Add(rend6); temp.Add(rend5); temp.Add(rombo);

        float y = PodioAlto;
        var cornisas = new List<(float y, float r)>();
        for (int i = 0; i < h.Length; i++)
        {
            float yb = y, yt = y + h[i];
            float Radio(float yy) => Mathf.Lerp(ra[i], rb[i], (yy - yb) / h[i]);
            Add(roca, Cylinder(ra[i], rb[i], h[i], 40, 5f, true), new Vector3(0f, yb, 0f), Quaternion.identity);
            Add(roca, Cylinder(rb[i], rb[i] + 2.2f, 2.5f, 40, 5f, true), new Vector3(0f, yt - 2.5f, 0f), Quaternion.identity);   // cornisa gruesa
            cornisas.Add((yt, rb[i] + 1.1f));
            float bb = yt - 5.6f;   // banda de glifos de 2,6 m
            Add(glifos, Cylinder(Radio(bb) + 0.12f, Radio(bb + 2.6f) + 0.12f, 2.6f, 40, 2.6f, false), new Vector3(0f, bb, 0f), Quaternion.identity);

            int nb = bahias[i];
            float pend = Mathf.Atan2(ra[i] - rb[i], h[i]) * Mathf.Rad2Deg;
            for (int k = 0; k < nb; k++)
            {
                float ang = k * 360f / nb, angR = ang + 180f / nb;
                bool portal = i == 0 && Mathf.Abs(Mathf.DeltaAngle(ang, 180f)) < 35f;
                // costilla (o contrafuerte en las bahías impares de los tres pisos de abajo) entre bahías
                if (!(i == 0 && Mathf.Abs(Mathf.DeltaAngle(angR, 180f)) < 32f))
                {
                    var d = Dir(angR);
                    var rot = Quaternion.AngleAxis(-pend, Vector3.Cross(Vector3.up, d)) * Quaternion.Euler(0f, angR, 0f);
                    if (i < 3 && k % 2 == 1)
                    {
                        float[] cAlto = { 24f, 20f, 16f }, cSal = { 6.5f, 5f, 4f }, cAncho = { 4f, 3.4f, 3f };
                        Add(roca, Cuna(cAncho[i], cSal[i], cAlto[i]), d * (ra[i] - 0.5f) + Vector3.up * yb, rot);
                    }
                    else
                    {
                        Add(roca, Costilla(h[i] - 2.7f, 1.6f, 1.3f), d * (ra[i] - 0.3f) + Vector3.up * yb, rot);
                        if (k % 2 == 0)
                        {
                            float largo = R(4f, 8f), yv = yb + R(4f, h[i] - 9f - largo * 0.3f);
                            Add(venas, QuadUV(0.9f, largo, 1f, 1f), d * (Radio(yv) + 1.06f) + Vector3.up * (yv + largo / 2f), Mirando(angR));
                        }
                    }
                }
                if (portal) continue;
                bool arco = i < 3 ? (i == 0 ? k % 2 == 0 : k % 3 == 0) : i == 5;
                if (arco)
                {
                    float ya = yb + (i == 0 ? 4f : i == 5 ? 0.8f : 3.2f), rr = Radio(ya);
                    var pos = Dir(ang) * rr + Vector3.up * ya;
                    Add(roca, ArcoMarco(arcoA[i], arcoHs[i], 0f, 1.2f, -1f, 0.6f, 8), pos, Mirando(ang));
                    Add(ventanas, ArcoPanel(arcoA[i], arcoHs[i], 8, -0.45f), pos, Mirando(ang));
                }
                else
                {
                    float yc = yb + 0.28f * h[i], rr = Radio(yc) + 0.15f;
                    Add(ventanas, i < 2 ? rend7 : i == 2 ? rend6 : rend5, Dir(ang) * rr + Vector3.up * yc, Mirando(ang));
                    if (i != 4)
                    {
                        float yd = yt - 8.6f;
                        Add(ventanas, rombo, Dir(ang) * (Radio(yd) + 0.15f) + Vector3.up * yd, Mirando(ang));
                    }
                }
            }
            y = yt;
        }
        float yTop = y;   // 142,25

        // brasa de la corona: disco emisivo sobre el último piso
        Add(ventanas, Cylinder(7.5f, 7.5f, 0.6f, 24, 5f, true), new Vector3(0f, yTop + 0.05f, 0f), Quaternion.identity);

        // púas laterales (16, sin colisión) en 3 anillos + garras, y corona de 8
        void Pico(float ang, float rBase, float yBase, float inclin, float rad, float alto, float curva, int lados, int tramos, int garras)
        {
            var d = Dir(ang);
            var rot = Quaternion.AngleAxis(inclin, Vector3.Cross(Vector3.up, d)) * Quaternion.Euler(0f, ang, 0f);   // +Z local = hacia afuera
            var basePos = d * rBase + Vector3.up * yBase;
            Add(roca, PicoCurvo(lados, rad, alto, curva, tramos), basePos, rot);
            for (int g = 0; g < garras; g++)
            {
                float t = g == 0 ? 0.42f : 0.62f, lado = g % 2 == 0 ? 1f : -1f;
                var p = new Vector3(0f, alto * t, curva * alto * t * t);
                Add(roca, PicoCurvo(4, rad * 0.4f, alto * 0.22f, 0.1f, 3), basePos + rot * p, rot * Quaternion.AngleAxis(48f * lado, Vector3.right));
            }
        }
        float[] aAng = { 20f, 80f, 140f, 220f, 280f, 340f }, aLen = { 62f, 70f, 56f, 58f, 68f, 60f }, aInc = { 14f, 17f, 12f, 13f, 16f, 15f };
        for (int k = 0; k < aAng.Length; k++)
            Pico(aAng[k], 34f, 0.5f, aInc[k] + R(-1f, 1f), 3.5f, aLen[k], k % 2 == 0 ? 0.12f : -0.08f, 4, 5, k % 3 == 0 ? 2 : 1);
        float[] bAng = { 40f, 90f, 150f, 210f, 270f, 320f }, bLen = { 45f, 50f, 38f, 40f, 48f, 42f }, bInc = { 18f, 20f, 16f, 17f, 22f, 19f }, bY = { 10f, 16f, 8f, 9f, 15f, 12f };
        for (int k = 0; k < bAng.Length; k++)
            Pico(bAng[k], Mathf.Lerp(ra[0], rb[0], (bY[k] - PodioAlto) / h[0]) - 0.8f, bY[k], bInc[k], 2.8f, bLen[k], k % 2 == 0 ? 0.1f : 0f, 4, 5, k % 2);
        float[] cAng = { 30f, 150f, 210f, 330f }, cLen = { 38f, 32f, 30f, 36f }, cInc = { 22f, 25f, 24f, 20f }, cY = { 44f, 50f, 46f, 52f };
        for (int k = 0; k < cAng.Length; k++)
            Pico(cAng[k], Mathf.Lerp(ra[1], rb[1], (cY[k] - 37.25f) / h[1]) - 0.6f, cY[k], cInc[k], 2.2f, cLen[k], 0.06f, 4, 4, 1);
        float[] kLen = { 24f, 16f, 20f, 15f, 26f, 17f, 22f, 15f };
        for (int k = 0; k < kLen.Length; k++)
            Pico(k * 45f + R(-8f, 8f), 6.5f, yTop - 0.5f, R(8f, 16f), 2.6f, kLen[k], R(-0.08f, 0.12f), 5, 5, 0);

        // portal gótico en la base: 3 arcos anidados que entran 6 m, pilastras y púas
        var puerta = Group("Puerta", root);
        puerta.localPosition = new Vector3(0f, 0f, -24.9f);
        Vector3 po = puerta.localPosition;
        const float pa = 6f, phs = 15.6f;   // semiancho y arranque del arco (la punta queda a 26 m)
        float[,] anillos = { { 3.6f, 6f, -6.4f }, { 1.8f, 3.6f, -4.4f }, { 0f, 1.8f, -2.4f } };
        for (int r = 0; r < 3; r++) Add(roca, ArcoMarco(pa, phs, anillos[r, 0], anillos[r, 1], anillos[r, 2], 1f, 10), po, Quaternion.identity);
        MeshObject(puerta, "Portal", ArcoPanel(pa, phs, 10, -0.5f), mPortal, false, false);
        // el abismo adentro: arcos cada vez más chicos y oscuros (borde morado brillante, centro casi negro), como un túnel
        MeshObject(puerta, "PortalMedio", ArcoPanel(pa - 1.4f, phs - 1.8f, 10, -0.55f), Emissive("PortalMedio", new Color(0.3f, 0.1f, 0.62f), 1f), false, false);
        MeshObject(puerta, "PortalHondo", ArcoPanel(pa - 2.9f, phs - 3.8f, 10, -0.6f), Emissive("PortalHondo", new Color(0.13f, 0.03f, 0.3f), 1f), false, false);
        MeshObject(puerta, "PortalNucleo", ArcoPanel(pa - 4.2f, phs - 5.8f, 10, -0.65f), Emissive("PortalNucleo", new Color(0.03f, 0f, 0.08f), 1f), false, false);
        foreach (float sx in new[] { -1f, 1f })
        {
            Solido(puerta, "Jamba", new Vector3(sx * 9.05f, 11f, -2.7f), new Vector3(6.1f, 22f, 7.6f));
            Solido(puerta, "Pilastra", new Vector3(sx * 14.2f, 12f, -2.5f), new Vector3(3f, 24f, 4.5f));
            Add(roca, BoxMesh(new Vector3(3f, 24f, 4.5f), 5f), po + new Vector3(sx * 14.2f, 12f, -2.5f), Quaternion.identity);
            Add(roca, Piramide(4, 1.6f, 6f), po + new Vector3(sx * 14.2f, 24f, -2.5f), Quaternion.AngleAxis(-sx * 6f, Vector3.forward));
            Add(glifos, QuadUV(2.2f, 18f, 1f, 8.2f), po + new Vector3(sx * 14.2f, 11f, -4.78f), Quaternion.identity);
        }
        // 7 púas sobre el arco exterior
        var perfil = PerfilArco(pa, phs, 6f, 10);
        int[] pIdx = { 3, 6, 9, 11, 13, 16, 19 };
        float[] pLen = { 3.2f, 4.5f, 5.8f, 8.5f, 5.8f, 4.5f, 3.2f }, pRad = { 0.8f, 1f, 1.1f, 1.4f, 1.1f, 1f, 0.8f };
        for (int k = 0; k < pIdx.Length; k++)
        {
            Vector2 p = perfil[pIdx[k]], n2 = (p - new Vector2(0f, phs * 0.6f)).normalized;
            var n3 = new Vector3(n2.x, n2.y, 0f);
            Add(roca, Piramide(4, pRad[k], pLen[k]), po + new Vector3(p.x, p.y, -5.8f) - n3 * 0.4f, Quaternion.FromToRotation(Vector3.up, n3));
        }
        AddLight(puerta, "LuzPortal", new Vector3(0f, 8f, -9f), Hex("#9A5CFF"), EsNoche ? 70f : 26f, 14f, LightShadows.None).gameObject.AddComponent<LightFlicker>().amount = 0.1f;
        var entrada = new GameObject("Entrada");
        entrada.transform.SetParent(puerta, false);
        entrada.transform.localPosition = new Vector3(0f, 1.25f, -5.5f);
        var salida = entrada.AddComponent<FloorExit>();
        salida.nextScene = towerScene;
        salida.radius = 2.4f;

        // haces de luz: uno central y dos finos, cada uno con dos planos cruzados
        foreach (var (x, ancho, largo) in new[] { (0f, 11f, 190f), (-9f, 5f, 140f), (9f, 5f, 140f) })
            for (int k = 0; k < 2; k++)
                Add(haces, QuadUV(ancho, largo, 1f, 1f), new Vector3(x, yTop + largo / 2f, 0f), Quaternion.Euler(0f, k * 90f, 0f));

        // malla única por material
        MeshObject(root, "Roca", Fusionar(roca, "TorreRoca"), mTorreRoca, true, true);
        MeshObject(root, "Glifos", Fusionar(glifos, "TorreGlifos"), mTorreGlifo, false, false);
        MeshObject(root, "Ventanas", Fusionar(ventanas, "TorreVentanas"), mTorreVentana, false, false);
        MeshObject(root, "Venas", Fusionar(venas, "TorreVenas"), mTorreVena, false, false);
        MeshObject(root, "Haces", Fusionar(haces, "TorreHaces"), mTorreHaz, false, false);
        foreach (var m in temp) if (m != null) Object.DestroyImmediate(m);

        var cuerpo = new GameObject("Collider_Cuerpo");
        cuerpo.transform.SetParent(root, false);
        cuerpo.transform.localPosition = new Vector3(0f, PodioAlto, 0f);
        var cc = cuerpo.AddComponent<MeshCollider>();
        cc.sharedMesh = Cylinder(25.2f, 25.2f, 40f, 40, 1f, true);
        cc.convex = true;

        // corona: llama grande, humo y remolino
        var corona = Group("Corona", root);
        corona.localPosition = new Vector3(0f, yTop, 0f);
        Particles(corona, "Llama", new Vector3(0f, 3.5f, 0f), mTorreLlama, ps =>
        {
            var pm = ps.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
            pm.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
            pm.startColor = new Color(1f, 0.75f, 1f, 0.5f);
            pm.maxParticles = 90;
            pm.prewarm = true;
            pm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 32f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = 7f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
        });
        Particles(corona, "Humo", new Vector3(0f, 8f, 0f), mTorreHumo, ps =>
        {
            var pm = ps.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(7f, 10f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            pm.startSize = new ParticleSystem.MinMaxCurve(10f, 16f);
            pm.startColor = new Color(1f, 1f, 1f, 0.35f);
            pm.maxParticles = 40;
            pm.prewarm = true;
            pm.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 6f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
        });
        Particles(corona, "Remolino", new Vector3(0f, 24f, 0f), mTorreLlama, ps =>
        {
            var pm = ps.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5f);
            pm.startSpeed = 0f;
            pm.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            pm.startColor = new Color(1f, 0.8f, 1f, 0.9f);
            pm.maxParticles = 70;
            pm.prewarm = true;
            pm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 20f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 9f; sh.radiusThickness = 0f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = 0f; vel.y = 5f; vel.z = 0f;
            vel.orbitalX = 0f; vel.orbitalY = 1.8f; vel.orbitalZ = 0f;
            vel.radial = -1.6f;
        });
        // pequeñas llamas moradas en cada cornisa
        for (int i = 0; i < cornisas.Count; i++)
        {
            float rEdge = cornisas[i].r;
            Particles(root, "LlamasCornisa_" + i, new Vector3(0f, cornisas[i].y, 0f), mTorreLlama, ps =>
            {
                var pm = ps.main;
                pm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                pm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                pm.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
                pm.startColor = new Color(1f, 0.8f, 1f, 0.7f);
                pm.maxParticles = 16;
                pm.prewarm = true;
                pm.simulationSpace = ParticleSystemSimulationSpace.Local;
                var em = ps.emission; em.rateOverTime = 9f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = rEdge; sh.radiusThickness = 0f; sh.rotation = new Vector3(-90f, 0f, 0f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            });
        }
        // brasas moradas flotando hacia arriba alrededor de la torre
        Particles(root, "Brasas", new Vector3(0f, 2f, 0f), mTorreLlama, ps =>
        {
            var pm = ps.main;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            pm.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            pm.startColor = new Color(1f, 0.7f, 1f, 0.9f);
            pm.maxParticles = 60;
            pm.prewarm = true;
            pm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 40f; sh.radiusThickness = 0.5f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var ns = ps.noise; ns.enabled = true; ns.strength = 1.5f; ns.frequency = 0.3f;
        });

        // halos detrás de la torre (siguen a la cámara: TorreHalo)
        void Halo(string nombre, float lado, float cy, float atras, Material mat)
        {
            var go = MeshObject(root, nombre, QuadUV(lado, lado, 1f, 1f), mat, false, false);
            var hl = go.AddComponent<TorreHalo>();
            hl.centro = new Vector3(0f, cy, 0f); hl.atras = atras;
            go.transform.localPosition = hl.centro + new Vector3(0f, 0f, atras);
        }
        Halo("Halo", 260f, 82f, 40f, mTorreHalo);
        Halo("HaloCorona", 100f, yTop + 10f, 20f, mTorreHaloCorona);

        root.localScale = Vector3.one * TorreEscala;
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) { var pm = ps.main; pm.scalingMode = ParticleSystemScalingMode.Hierarchy; }
    }

    // ---------------- verificación al terminar de generar

    /// <summary>Imprime los objetos a menos de 46 m del centro de la torre, los que quedan fuera de los límites invisibles y la comprobación de la cámara.</summary>
    void ReportarCala(Transform root)
    {
        var sb = new System.Text.StringBuilder();
        Vector3 centro = root.TransformPoint(new Vector3(CalaTorre.x, 0f, CalaTorre.y));
        sb.AppendLine($"[Exterior] Verificación de la torre (centro {CalaTorre}, podio r {PodioRadio}):");

        // 1) objetos a menos de 46 m del centro (sin la torre, ni suelo y calles)
        sb.AppendLine("  Objetos a menos de 46 m del centro:");
        int cerca = 0;
        foreach (var gn in new[] { "Casas", "Puerto", "Plaza", "Vegetacion" })
        {
            var g = root.Find(gn);
            if (g == null) continue;
            foreach (Transform o in g)
            {
                var rs = o.GetComponentsInChildren<Renderer>();
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(o.position, Vector3.zero);
                foreach (var r in rs) b.Encapsulate(r.bounds);
                Vector3 q = b.ClosestPoint(new Vector3(centro.x, b.center.y, centro.z));
                float d = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(centro.x, centro.z));
                if (d >= 46f) continue;
                cerca++;
                sb.AppendLine($"    {gn}/{o.name}: {d:F1} m (posición {root.InverseTransformPoint(o.position):F1})" + (o.name.StartsWith("Brasero") ? "  <- intencional, sobre el podio" : ""));
            }
        }
        if (cerca == 0) sb.AppendLine("    (ninguno)");

        // 2) objetos fuera de los límites invisibles (x -61,8…42; z CalaSur…malecón)
        sb.AppendLine("  Objetos fuera de los límites invisibles:");
        int fuera = 0;
        var porGrupo = new Dictionary<string, int>();
        foreach (var gn in new[] { "Casas", "Plaza", "Vegetacion" })
        {
            var g = root.Find(gn);
            if (g == null) continue;
            foreach (Transform o in g)
            {
                Vector3 p = root.InverseTransformPoint(o.position);
                if (p.x >= CalaOeste && p.x <= CalaEste && p.z >= CalaSur && p.z <= CalaMalecon) continue;
                fuera++;
                porGrupo[gn] = porGrupo.TryGetValue(gn, out int n) ? n + 1 : 1;
                if (fuera <= 15) sb.AppendLine($"    {gn}/{o.name}: {p:F1}");
            }
        }
        if (fuera == 0) sb.AppendLine("    (ninguno)");
        else sb.AppendLine($"    total {fuera}: " + string.Join(", ", porGrupo) + " (los pinos del cabo y del acantilado van fuera a propósito)");

        // 3) cámara de Abby del spawn al portal, con los 4 giros del rig
        var rig = FindAnyObjectByType<CameraRig>();
        var cam = rig != null ? rig.GetComponentInChildren<Camera>() : null;
        if (cam == null) sb.AppendLine("  Cámara: no hay CameraRig en la escena, sin comprobar.");
        else
        {
            Physics.SyncTransforms();
            sb.AppendLine($"  Far clip de la cámara: {cam.farClipPlane:F0} m");
            var sky = FindAnyObjectByType<ExteriorSky>();
            if (sky != null) sb.AppendLine($"  Niebla hasta {sky.fogEnd:F0} m (far clip {(cam.farClipPlane >= sky.fogEnd ? "llega" : "NO llega")}); mar 1800x1500 m desde z {CalaSur:F0} a 1390 y cielo (Skybox) sin límite de far clip");
            Vector3 off = rig.transform.InverseTransformPoint(cam.transform.position);
            Vector3 ini = PlayerSpawn, fin = root.TransformPoint(new Vector3(CalaTorre.x, PodioAlto, CalaTorre.y - 30.4f));
            float largo = Vector3.Distance(ini, fin);
            for (int giro = 0; giro < 4; giro++)
            {
                int tapadas = 0, dentro = 0; float primera = -1f;
                for (float t = 0f; t <= largo; t += 1f)
                {
                    Vector3 abby = Vector3.Lerp(ini, fin, t / largo);
                    if (Physics.Raycast(abby + Vector3.up * 50f, Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore)) abby.y = hit.point.y;
                    Vector3 cp = abby + rig.transform.rotation * Quaternion.Euler(0f, giro * 90f, 0f) * off;
                    bool inside = Physics.CheckSphere(cp, cam.nearClipPlane + 0.3f, ~0, QueryTriggerInteraction.Ignore);
                    bool tapa = Physics.Linecast(abby + Vector3.up * 1.5f, cp, ~0, QueryTriggerInteraction.Ignore);
                    if (inside) dentro++;
                    if (tapa) { tapadas++; if (primera < 0f) primera = t; }
                }
                sb.AppendLine($"  Cámara con giro {giro * 90}°: {(dentro == 0 ? "nunca dentro de un collider" : $"DENTRO de un collider en {dentro} de {largo:F0} puntos")}; " +
                              (tapadas == 0 ? "sin objetos entre Abby y la cámara" : $"algo se interpone en {tapadas} puntos (primero a {primera:F0} m del spawn)"));
            }
        }
        Debug.Log(sb.ToString());
    }

    // ---------------- mallas de la torre

    /// <summary>Malla de caras planas: cada cara con sus vértices y el giro corregido para que mire hacia <c>hacia</c>.</summary>
    sealed class Malla
    {
        readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> t = new List<int>();

        public void Cara(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hacia)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hacia) < 0f) { var x = b; b = d; d = x; }
            int i = v.Count;
            foreach (var p in new[] { a, b, c, d }) { v.Add(p); uv.Add(new Vector2(p.x + p.z, p.y) / 5f); }
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 hacia)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hacia) < 0f) { var x = b; b = c; c = x; }
            int i = v.Count;
            foreach (var p in new[] { a, b, c }) { v.Add(p); uv.Add(new Vector2(p.x + p.z, p.y) / 5f); }
            t.AddRange(new[] { i, i + 1, i + 2 });
        }

        public Mesh Crear(string nombre)
        {
            var m = new Mesh { name = nombre };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Pirámide de caras planas con la base en y = 0 y la punta en y = alto.</summary>
    static Mesh Piramide(int lados, float radio, float alto)
    {
        var m = new Malla();
        for (int s = 0; s < lados; s++)
        {
            float a0 = s / (float)lados * Mathf.PI * 2f, a1 = (s + 1) / (float)lados * Mathf.PI * 2f, am = (a0 + a1) / 2f;
            m.Tri(new Vector3(Mathf.Sin(a0) * radio, 0f, Mathf.Cos(a0) * radio), new Vector3(Mathf.Sin(a1) * radio, 0f, Mathf.Cos(a1) * radio),
                  new Vector3(0f, alto, 0f), new Vector3(Mathf.Sin(am), 0.2f, Mathf.Cos(am)));
        }
        return m.Crear("Piramide");
    }

    /// <summary>Púa con base ancha y punta fina que se curva hacia +Z local (la espina: x = 0, y = alto·t, z = curva·alto·t²).</summary>
    static Mesh PicoCurvo(int lados, float radio, float alto, float curva, int tramos)
    {
        var m = new Malla();
        Vector3 Punto(int k, int s)
        {
            float t = k / (float)tramos, a = s / (float)lados * Mathf.PI * 2f;
            float r = k == tramos ? 0f : radio * Mathf.Pow(1f - t, 0.85f);
            return new Vector3(Mathf.Sin(a) * r, alto * t, curva * alto * t * t + Mathf.Cos(a) * r);
        }
        for (int k = 0; k < tramos; k++)
            for (int s = 0; s < lados; s++)
            {
                float am = (s + 0.5f) / lados * Mathf.PI * 2f;
                m.Cara(Punto(k, s), Punto(k, s + 1), Punto(k + 1, s + 1), Punto(k + 1, s), new Vector3(Mathf.Sin(am), 0.1f, Mathf.Cos(am)));
            }
        return m.Crear("PicoCurvo");
    }

    /// <summary>Costilla: prisma que se afina hacia arriba, con la base en y = 0 y la cara de adelante en +Z local (entra 0,8 m en el muro).</summary>
    static Mesh Costilla(float alto, float ancho, float fondo)
    {
        var m = new Malla();
        float w = ancho / 2f, tw = ancho * 0.35f, z0 = -0.8f, z1 = fondo;
        Vector3 b00 = new Vector3(-w, 0f, z0), b10 = new Vector3(w, 0f, z0), b11 = new Vector3(w, 0f, z1), b01 = new Vector3(-w, 0f, z1);
        Vector3 t00 = new Vector3(-tw, alto, z0), t10 = new Vector3(tw, alto, z0), t11 = new Vector3(tw, alto, z1), t01 = new Vector3(-tw, alto, z1);
        m.Cara(b01, b11, t11, t01, new Vector3(0f, 0f, 1f));
        m.Cara(b00, b01, t01, t00, Vector3.left);
        m.Cara(b10, b11, t11, t10, Vector3.right);
        m.Cara(t00, t10, t11, t01, Vector3.up);
        return m.Crear("Costilla");
    }

    /// <summary>Contrafuerte: cuña que sale del muro (z = -0,8) hasta <c>saliente</c> abajo y muere en la altura <c>alto</c>.</summary>
    static Mesh Cuna(float ancho, float saliente, float alto)
    {
        var m = new Malla();
        float w = ancho / 2f;
        Vector3 b00 = new Vector3(-w, 0f, -0.8f), b10 = new Vector3(w, 0f, -0.8f), b11 = new Vector3(w, 0f, saliente), b01 = new Vector3(-w, 0f, saliente);
        Vector3 t0 = new Vector3(-w * 0.35f, alto, -0.3f), t1 = new Vector3(w * 0.35f, alto, -0.3f);
        m.Cara(b01, b11, t1, t0, new Vector3(0f, 0.5f, 1f));
        m.Tri(b00, b01, t0, Vector3.left);
        m.Tri(b10, b11, t1, Vector3.right);
        m.Cara(b00, b10, b11, b01, Vector3.down);
        return m.Crear("Cuna");
    }

    /// <summary>Contorno de un arco apuntado (dos arcos de radio 2a + o con centro en ±a), de abajo a la izquierda hasta abajo a la derecha. o = desfase hacia afuera.</summary>
    static List<Vector2> PerfilArco(float a, float hs, float o, int n)
    {
        float R = 2f * a + o, phiMax = Mathf.Acos(a / R);
        var izq = new List<Vector2> { new Vector2(-(a + o), 0f), new Vector2(-(a + o), hs) };
        for (int i = 1; i <= n; i++)
        {
            float phi = phiMax * i / n;
            izq.Add(new Vector2(a - R * Mathf.Cos(phi), hs + R * Mathf.Sin(phi)));
        }
        var pts = new List<Vector2>(izq);
        for (int i = izq.Count - 2; i >= 0; i--) pts.Add(new Vector2(-izq[i].x, izq[i].y));
        return pts;
    }

    /// <summary>Marco de un arco: cara de adelante en z = zF (visible desde -Z), reborde interior y exterior hasta zB.</summary>
    static Mesh ArcoMarco(float a, float hs, float o1, float o2, float zF, float zB, int n)
    {
        var c1 = PerfilArco(a, hs, o1, n); var c2 = PerfilArco(a, hs, o2, n);
        var m = new Malla();
        float ym = hs * 0.6f;
        Vector3 P(Vector2 p, float z) => new Vector3(p.x, p.y, z);
        for (int j = 0; j < c1.Count - 1; j++)
        {
            m.Cara(P(c1[j], zF), P(c1[j + 1], zF), P(c2[j + 1], zF), P(c2[j], zF), new Vector3(0f, 0f, -1f));
            Vector2 mi = (c1[j] + c1[j + 1]) / 2f, mo = (c2[j] + c2[j + 1]) / 2f;
            m.Cara(P(c1[j], zF), P(c1[j + 1], zF), P(c1[j + 1], zB), P(c1[j], zB), new Vector3(-mi.x, ym - mi.y, 0f));
            m.Cara(P(c2[j], zF), P(c2[j + 1], zF), P(c2[j + 1], zB), P(c2[j], zB), new Vector3(mo.x, mo.y - ym, 0f));
        }
        return m.Crear("ArcoMarco");
    }

    /// <summary>Hueco de un arco (relleno plano en z, visible desde -Z).</summary>
    static Mesh ArcoPanel(float a, float hs, int n, float z)
    {
        var c = PerfilArco(a, hs, 0f, n);
        var m = new Malla();
        var centro = new Vector3(0f, hs * 0.5f, z);
        for (int j = 0; j < c.Count - 1; j++)
            m.Tri(centro, new Vector3(c[j].x, c[j].y, z), new Vector3(c[j + 1].x, c[j + 1].y, z), new Vector3(0f, 0f, -1f));
        // la base (el perfil empieza y acaba en el suelo): sin este triángulo quedaba un hueco negro abajo del portal
        m.Tri(centro, new Vector3(c[c.Count - 1].x, 0f, z), new Vector3(c[0].x, 0f, z), new Vector3(0f, 0f, -1f));
        return m.Crear("ArcoPanel");
    }

    /// <summary>Quad vertical (visible desde -Z) con UV de 0 a tx / 0 a ty.</summary>
    static Mesh QuadUV(float w, float h, float tx, float ty)
    {
        var m = new Mesh { name = "QuadUV" };
        m.vertices = new[] { new Vector3(-w / 2, -h / 2, 0f), new Vector3(w / 2, -h / 2, 0f), new Vector3(-w / 2, h / 2, 0f), new Vector3(w / 2, h / 2, 0f) };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(tx, 0f), new Vector2(0f, ty), new Vector2(tx, ty) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Rombo vertical (visible desde -Z).</summary>
    static Mesh Rombo(float w, float h)
    {
        var m = new Mesh { name = "Rombo" };
        m.vertices = new[] { new Vector3(0f, -h / 2, 0f), new Vector3(w / 2, 0f, 0f), new Vector3(-w / 2, 0f, 0f), new Vector3(0f, h / 2, 0f) };
        m.uv = new[] { new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 1f) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
#endif
}
