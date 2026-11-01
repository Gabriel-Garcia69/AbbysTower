using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importación de las imágenes de la intro (Assets/Resources/Intro, pixel art de 256x144): Point, sin compresión, sin mipmaps, Clamp y,
/// sobre todo, SIN reescalar a potencia de 2. Con el valor por defecto (Non-Power of 2: To nearest) Unity las encogía a 256x128
/// al importarlas: la imagen se aplastaba y se suavizaba, y eso era buena parte de lo borroso de la cinemática.
/// </summary>
public class IntroImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Intro/", StringComparison.OrdinalIgnoreCase) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
        var ti = (TextureImporter)assetImporter;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
    }
}
