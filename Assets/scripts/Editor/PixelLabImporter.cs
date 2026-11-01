using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación de los sprites de Abby de PixelLab (Assets/sprites/Abby/PixelLab) y de la expedición (Assets/sprites/Expedicion): Sprite único, Point, sin
/// compresión, 64 píxeles por unidad como los sprites actuales y pivote abajo, centrado en X.
/// Los lienzos van de 128 a 160 px según la animación y la dirección, y los pies no están a la misma altura del borde,
/// así que el pivote en Y se calcula en píxeles: el pie más bajo (sin contar la espada) de todos los cuadros de la misma
/// animación y dirección. Así el pie apoyado queda en el suelo en cada clip y no hay salto al cambiar de clip o de dirección.
/// Las rotaciones (un solo cuadro por dirección) usan su propio pie. Subir <see cref="GetVersion"/> reimporta todo.
/// </summary>
public class PixelLabImporter : AssetPostprocessor
{
    // la carpeta del proyecto es Assets/sprites (minúscula): el assetPath la trae así y la comparación no puede distinguir mayúsculas
    public const string Raiz = "Assets/sprites/Abby/PixelLab/";
    public const string RaizExpedicion = "Assets/sprites/Expedicion/";
    const float PixelsPorUnidad = 64f;

    public override uint GetVersion() => 3;

    void OnPreprocessTexture()
    {
        if (!(assetPath.StartsWith(Raiz, StringComparison.OrdinalIgnoreCase) || assetPath.StartsWith(RaizExpedicion, StringComparison.OrdinalIgnoreCase))
            || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PixelsPorUnidad;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        var (pies, alto) = PiesYAlto(assetPath);
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = new Vector2(0.5f, alto > 0 ? pies / (float)alto : 0.14f);
        ti.SetTextureSettings(s);
    }

    /// <summary>Filas vacías bajo el pie más bajo: el del cuadro, o el menor de la carpeta si es una animación (frame_NNN.png).</summary>
    static (int pies, int alto) PiesYAlto(string assetPath)
    {
        string abs = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        var (pies, alto) = LeerPies(abs);
        if (!Path.GetFileName(assetPath).StartsWith("frame_")) return (pies, alto);
        foreach (var f in Directory.GetFiles(Path.GetDirectoryName(abs), "frame_*.png"))
        {
            var (p, _) = LeerPies(f);
            if (p >= 0 && p < pies) pies = p;
        }
        return (pies, alto);
    }

    /// <summary>Pie más bajo = primera fila (desde abajo) con un píxel opaco que no sea de la espada (claro y poco saturado).</summary>
    static (int pies, int alto) LeerPies(string ruta)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(ruta))) return (0, 0);
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.a < 128) continue;
                    int mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b)), mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (mn > 150 && mx - mn < 50) continue;   // espada
                    return (y, h);
                }
            return (0, h);
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }
}
