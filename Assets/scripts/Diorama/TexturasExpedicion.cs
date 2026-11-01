#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Texturas pixel art de la gruta del tutorial (Expedicion). Lee los ZIP de ambientCG de References/Texturas, toma SOLO el mapa
/// _Color, lo reduce por promedio de área (64x64 o 128x128: ~32 px por metro con los UV en metros que usa el mapa), lo posteriza
/// a 20 colores (k-means) y lo guarda en Assets/Textures/Expedicion con Point, sin compresión, repeat y sin mipmaps.
/// También crea los decales de suelo (grietas, charcos y su reflejo). El DioramaBuilder llama a Asegurar() al generar Expedicion;
/// el menú Abby → Expedición → Importar texturas las rehace a la fuerza.
/// </summary>
public static class TexturasExpedicion
{
    public const string Dir = "Assets/Textures/Expedicion";
    const string Origen = "References/Texturas";
    const int Colores = 20;

    // nombre final, ZIP de origen, lado en píxeles (64 = 2 m por repetición; 128 = 4 m)
    static readonly (string nombre, string zip, int lado)[] Lista =
    {
        ("suelo_gruta", "suelo_gruta.zip", 64),
        ("suelo_grava", "suelo_variacion.zip", 64),
        ("pared_roca", "pared_roca.zip", 128),
        ("pared_alternativa", "pared_alternativa.zip", 128),
        ("sendero_losas", "sendero.zip", 64),
        ("lona_tienda", "lona_tienda.zip", 64),
    };

    public static string Ruta(string nombre) => Dir + "/" + nombre + ".png";

    [MenuItem("Abby/Expedición/Importar texturas")]
    static void Menu() => Asegurar(true);

    public static void Asegurar(bool forzar = false)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures")) AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Textures", "Expedicion");
        foreach (var (nombre, zip, lado) in Lista)
        {
            if (!forzar && AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta(nombre)) != null) continue;
            string ruta = Path.Combine(Directory.GetCurrentDirectory(), Origen, zip);
            if (!File.Exists(ruta)) { Debug.LogWarning("[Expedicion] Falta " + ruta + ": " + nombre + " queda sin textura."); continue; }
            byte[] jpg = null;
            using (var fs = File.OpenRead(ruta))
            using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                foreach (var e in za.Entries)
                    if (e.FullName.EndsWith("_Color.jpg"))
                        using (var s = e.Open()) using (var ms = new MemoryStream()) { s.CopyTo(ms); jpg = ms.ToArray(); break; }
            if (jpg == null) { Debug.LogWarning("[Expedicion] " + zip + " no trae un mapa _Color."); continue; }
            Guardar(nombre, PosterizarReducido(jpg, lado), lado, lado, false);
        }
        Decales(forzar);
        AssetDatabase.SaveAssets();
    }

    // ---------------- reducción y posterizado

    static Color32[] PosterizarReducido(byte[] jpg, int lado)
    {
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            ImageConversion.LoadImage(src, jpg);
            var px = src.GetPixels32();
            int w = src.width, h = src.height, bx = Mathf.Max(1, w / lado), by = Mathf.Max(1, h / lado);
            var pequena = new Vector3[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    float r = 0f, g = 0f, b = 0f; int n = 0;
                    for (int yy = y * by; yy < Mathf.Min(h, (y + 1) * by); yy++)
                        for (int xx = x * bx; xx < Mathf.Min(w, (x + 1) * bx); xx++) { var c = px[yy * w + xx]; r += c.r; g += c.g; b += c.b; n++; }
                    pequena[y * lado + x] = new Vector3(r, g, b) / Mathf.Max(1, n) / 255f;
                }
            return KMedias(pequena, Colores);
        }
        finally { Object.DestroyImmediate(src); }
    }

    /// <summary>k-medias determinista: centros iniciales por cuantiles de luminosidad, 12 pasadas.</summary>
    static Color32[] KMedias(Vector3[] px, int k)
    {
        var orden = new List<int>();
        for (int i = 0; i < px.Length; i++) orden.Add(i);
        orden.Sort((a, b) => Lum(px[a]).CompareTo(Lum(px[b])));
        var centros = new Vector3[k];
        for (int i = 0; i < k; i++) centros[i] = px[orden[Mathf.Min(px.Length - 1, (int)((i + 0.5f) / k * px.Length))]];
        var asignado = new int[px.Length];
        for (int it = 0; it < 12; it++)
        {
            var suma = new Vector3[k]; var cuenta = new int[k];
            for (int i = 0; i < px.Length; i++)
            {
                int mejor = 0; float dmin = float.MaxValue;
                for (int c = 0; c < k; c++) { float d = (px[i] - centros[c]).sqrMagnitude; if (d < dmin) { dmin = d; mejor = c; } }
                asignado[i] = mejor; suma[mejor] += px[i]; cuenta[mejor]++;
            }
            for (int c = 0; c < k; c++) if (cuenta[c] > 0) centros[c] = suma[c] / cuenta[c];
        }
        var res = new Color32[px.Length];
        for (int i = 0; i < px.Length; i++)
        {
            var c = centros[asignado[i]];
            res[i] = new Color32((byte)Mathf.RoundToInt(c.x * 255f), (byte)Mathf.RoundToInt(c.y * 255f), (byte)Mathf.RoundToInt(c.z * 255f), 255);
        }
        return res;
    }

    static float Lum(Vector3 c) => 0.299f * c.x + 0.587f * c.y + 0.114f * c.z;

    // ---------------- guardado e importación

    static void Guardar(string nombre, Color32[] px, int w, int h, bool decal)
    {
        string ruta = Ruta(nombre);
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels32(px);
        t.Apply();
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), ruta), t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.wrapMode = decal ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        imp.alphaIsTransparency = decal;
        imp.SaveAndReimport();
    }

    // ---------------- decales del suelo

    static void Decales(bool forzar)
    {
        if (forzar || AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("decal_grietas")) == null)
        {
            const int n = 64;
            var px = new Color32[n * n];
            var rng = new System.Random(11);
            var oscuro = new Color32(8, 8, 12, 235);
            for (int rama = 0; rama < 5; rama++)
            {
                float x = n / 2f + rng.Next(-6, 7), y = n / 2f + rng.Next(-6, 7), a = (float)(rng.NextDouble() * Mathf.PI * 2f);
                int largo = rng.Next(14, 30);
                for (int i = 0; i < largo; i++)
                {
                    a += (float)(rng.NextDouble() - 0.5) * 0.9f;
                    x += Mathf.Cos(a); y += Mathf.Sin(a);
                    int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
                    if (ix < 1 || iy < 1 || ix >= n - 1 || iy >= n - 1) break;
                    px[iy * n + ix] = oscuro;
                    if (i % 6 == 0) px[(iy + 1) * n + ix] = new Color32(8, 8, 12, 150);
                }
            }
            Guardar("decal_grietas", px, n, n, true);
        }
        if (forzar || AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("decal_charco")) == null)
        {
            const int n = 64;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f - n / 2f) / (n * 0.46f), dy = (y + 0.5f - n / 2f) / (n * 0.34f);
                    float ruido = Mathf.PerlinNoise(x * 0.18f + 3f, y * 0.18f) * 0.28f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) + ruido - 0.14f;
                    if (d > 1f) continue;
                    px[y * n + x] = d > 0.86f ? new Color32(70, 84, 104, 235) : new Color32(14, 20, 32, 225);   // orilla clara, centro oscuro de agua
                }
            Guardar("decal_charco", px, n, n, true);
        }
        if (forzar || AssetDatabase.LoadAssetAtPath<Texture2D>(Ruta("decal_reflejo")) == null)
        {
            const int n = 32;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    if (d >= 1f) continue;
                    float a = Mathf.Floor(Mathf.Pow(1f - d, 1.6f) * 5f) / 5f;   // en escalones, como pixel art
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            Guardar("decal_reflejo", px, n, n, true);
        }
    }
}
#endif
