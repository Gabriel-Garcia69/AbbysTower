using UnityEngine;

/// <summary>
/// Degradados para la interfaz, en una sola textura estirada. Dibujarlos por tiras deja líneas: donde dos tiras se tocan, el velo
/// se oscurece el doble.
/// </summary>
public static class Degradado
{
    static Texture2D horizontal, vertical, centro;

    static Texture2D Crear(int ancho, int alto, System.Func<float, float> alfa)
    {
        var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        int n = Mathf.Max(ancho, alto);
        for (int i = 0; i < n; i++)
        {
            var c = new Color(1f, 1f, 1f, alfa(i / (n - 1f)));
            for (int k = 0; k < Mathf.Min(ancho, alto); k++) t.SetPixel(ancho > alto ? i : k, ancho > alto ? k : i, c);
        }
        t.Apply();
        return t;
    }

    /// <summary>Opaco a la izquierda, transparente a la derecha.</summary>
    public static Texture2D Horizontal => horizontal != null ? horizontal : horizontal = Crear(512, 2, k => 1f - k * k);
    /// <summary>Transparente arriba, opaco abajo. (Las texturas van de abajo hacia arriba.)</summary>
    public static Texture2D Vertical => vertical != null ? vertical : vertical = Crear(2, 512, k => 1f - k);
    /// <summary>Transparente en las orillas, opaco en medio.</summary>
    public static Texture2D Centro => centro != null ? centro : centro = Crear(512, 2, k => 1f - Mathf.Abs(k * 2f - 1f));
}
