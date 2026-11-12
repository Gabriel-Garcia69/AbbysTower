using UnityEngine;

/// <summary>
/// El logo de la torre (Resources/Logo/logo_torre) como emblema en la esquina del menú principal y de la pausa: con un
/// halo morado que respira detrás y flotando muy despacio, como la torre del juego.
/// </summary>
public static class EmblemaTorre
{
    static Texture2D logo, halo;
    static bool buscado;

    public static Texture2D Logo
    {
        get
        {
            if (!buscado) { buscado = true; logo = Resources.Load<Texture2D>("Logo/logo_torre"); }
            return logo;
        }
    }

    /// <summary>Dibuja el emblema con su esquina superior izquierda en (x, y) y la altura dada. Devuelve el ancho usado.</summary>
    public static float Dibujar(float x, float y, float alto, float alpha, float t)
    {
        var tex = Logo;
        if (tex == null) return 0f;
        float ancho = alto * tex.width / tex.height;
        float flota = Mathf.Sin(t * 1.3f) * alto * 0.012f;
        var prev = GUI.color;
        // halo detrás: morado, respirando
        float pulso = 0.5f + 0.5f * Mathf.Sin(t * 1.7f);
        GUI.color = new Color(0.62f, 0.3f, 0.95f, (0.32f + 0.14f * pulso) * alpha);
        float hr = alto * 1.05f;
        GUI.DrawTexture(new Rect(x + ancho / 2f - hr / 2f, y + alto * 0.42f - hr / 2f + flota, hr, hr), Halo());
        // sombra suave y el logo
        GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
        GUI.DrawTexture(new Rect(x + alto * 0.02f, y + alto * 0.025f + flota, ancho, alto), tex, ScaleMode.ScaleToFit, true);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(x, y + flota, ancho, alto), tex, ScaleMode.ScaleToFit, true);
        GUI.color = prev;
        return ancho;
    }

    static Texture2D Halo()
    {
        if (halo != null) return halo;
        const int n = 96;
        halo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
            }
        halo.SetPixels(px); halo.Apply();
        return halo;
    }
}
