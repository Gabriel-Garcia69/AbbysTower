using UnityEngine;

/// <summary>Estética y emoción de un piso. Base = piso 1, Galerias = piso 2; los demás son los siete pecados capitales.</summary>
public enum FloorTheme { Base, Pereza, Gula, Avaricia, Envidia, Lujuria, Ira, Soberbia, Galerias }

/// <summary>Colores, niebla, luces y nombres de cada tema. Lo usan FloorLayout (al generar) y FloorDirector (en juego).</summary>
public struct ThemePalette
{
    public string key;            // prefijo de las texturas en Assets/Textures/Pisos
    public string sin;            // nombre del pecado ("" en Base)
    public string emotion;        // cómo se debe sentir (documentación / subtítulo)
    public Color torch;           // fuego de antorchas y braseros
    public Color center;          // luz principal de cada sala
    public Color corridor;        // luz de pasillos
    public Color accent;          // brillos mágicos, runas, cristales
    public Color rug, rugBorder, banner;
    public Color motes;           // partículas que flotan en las salas
    public Color ambient;         // luz ambiente de la escena
    public Color fog; public float fogDensity;
    public Color enemyBody, enemyEye;
    public float floorTile, wallTile;   // metros que cubre una repetición de la textura
    public float floorSmooth, wallSmooth;
    public bool emissiveFloor, emissiveWall;
}

public static class FloorThemes
{
    public static ThemePalette Get(FloorTheme t)
    {
        switch (t)
        {
            case FloorTheme.Pereza:
                return new ThemePalette
                {
                    key = "pereza", sin = "PEREZA", emotion = "Melancolía, sopor, silencio de biblioteca",
                    torch = new Color(1f, 0.72f, 0.42f), center = new Color(0.62f, 0.75f, 0.9f), corridor = new Color(0.55f, 0.7f, 0.85f),
                    accent = new Color(0.45f, 0.85f, 0.95f), rug = new Color(0.16f, 0.22f, 0.3f), rugBorder = new Color(0.55f, 0.48f, 0.3f),
                    banner = new Color(0.2f, 0.28f, 0.38f), motes = new Color(0.75f, 0.85f, 1f),
                    ambient = new Color(0.16f, 0.19f, 0.25f), fog = new Color(0.12f, 0.15f, 0.2f), fogDensity = 0.018f,
                    enemyBody = new Color(0.12f, 0.16f, 0.24f), enemyEye = new Color(0.55f, 0.85f, 1f),
                    floorTile = 3f, wallTile = 3f, floorSmooth = 0.6f, wallSmooth = 0.5f,
                };
            case FloorTheme.Gula:
                return new ThemePalette
                {
                    key = "gula", sin = "GULA", emotion = "Exceso grotesco, calor de cocina, festín que no acaba",
                    torch = new Color(1f, 0.6f, 0.25f), center = new Color(1f, 0.78f, 0.5f), corridor = new Color(1f, 0.65f, 0.35f),
                    accent = new Color(1f, 0.55f, 0.2f), rug = new Color(0.45f, 0.12f, 0.08f), rugBorder = new Color(0.8f, 0.62f, 0.25f),
                    banner = new Color(0.55f, 0.2f, 0.08f), motes = new Color(1f, 0.75f, 0.4f),
                    ambient = new Color(0.24f, 0.16f, 0.1f), fog = new Color(0.22f, 0.14f, 0.08f), fogDensity = 0.012f,
                    enemyBody = new Color(0.32f, 0.16f, 0.08f), enemyEye = new Color(1f, 0.7f, 0.2f),
                    floorTile = 2f, wallTile = 2.5f, floorSmooth = 0.7f, wallSmooth = 0.4f,
                };
            case FloorTheme.Avaricia:
                return new ThemePalette
                {
                    key = "avaricia", sin = "AVARICIA", emotion = "Tentación, brillo de oro, tensión de una trampa",
                    torch = new Color(1f, 0.82f, 0.45f), center = new Color(1f, 0.9f, 0.62f), corridor = new Color(0.95f, 0.85f, 0.55f),
                    accent = new Color(1f, 0.82f, 0.3f), rug = new Color(0.06f, 0.26f, 0.18f), rugBorder = new Color(0.85f, 0.68f, 0.28f),
                    banner = new Color(0.08f, 0.3f, 0.2f), motes = new Color(1f, 0.88f, 0.45f),
                    ambient = new Color(0.14f, 0.17f, 0.12f), fog = new Color(0.1f, 0.12f, 0.08f), fogDensity = 0.01f,
                    enemyBody = new Color(0.25f, 0.2f, 0.06f), enemyEye = new Color(1f, 0.85f, 0.25f),
                    floorTile = 3f, wallTile = 3f, floorSmooth = 1f, wallSmooth = 0.9f,
                };
            case FloorTheme.Envidia:
                return new ThemePalette
                {
                    key = "envidia", sin = "ENVIDIA", emotion = "Paranoia, sentirse observada, ver a otra que es tú",
                    torch = new Color(0.78f, 1f, 0.7f), center = new Color(0.8f, 0.95f, 0.82f), corridor = new Color(0.65f, 0.88f, 0.7f),
                    accent = new Color(0.45f, 1f, 0.6f), rug = new Color(0.05f, 0.2f, 0.1f), rugBorder = new Color(0.7f, 0.66f, 0.35f),
                    banner = new Color(0.08f, 0.32f, 0.16f), motes = new Color(0.55f, 1f, 0.6f),
                    ambient = new Color(0.08f, 0.16f, 0.1f), fog = new Color(0.05f, 0.12f, 0.07f), fogDensity = 0.022f,
                    enemyBody = new Color(0.04f, 0.18f, 0.08f), enemyEye = new Color(0.45f, 1f, 0.35f),
                    floorTile = 3f, wallTile = 3f, floorSmooth = 0.9f, wallSmooth = 0.6f,
                };
            case FloorTheme.Lujuria:
                return new ThemePalette
                {
                    key = "lujuria", sin = "LUJURIA", emotion = "Ensueño, anhelo, algo hermoso que atrae y lastima",
                    torch = new Color(1f, 0.55f, 0.7f), center = new Color(1f, 0.72f, 0.85f), corridor = new Color(0.95f, 0.55f, 0.8f),
                    accent = new Color(1f, 0.4f, 0.75f), rug = new Color(0.45f, 0.05f, 0.18f), rugBorder = new Color(0.9f, 0.7f, 0.5f),
                    banner = new Color(0.5f, 0.06f, 0.22f), motes = new Color(1f, 0.6f, 0.8f),
                    ambient = new Color(0.22f, 0.12f, 0.18f), fog = new Color(0.25f, 0.1f, 0.18f), fogDensity = 0.02f,
                    enemyBody = new Color(0.3f, 0.05f, 0.15f), enemyEye = new Color(1f, 0.45f, 0.75f),
                    floorTile = 3f, wallTile = 2.5f, floorSmooth = 1f, wallSmooth = 0.6f,
                };
            case FloorTheme.Ira:
                return new ThemePalette
                {
                    key = "ira", sin = "IRA", emotion = "Caos, calor insoportable, golpes que no paran",
                    torch = new Color(1f, 0.45f, 0.15f), center = new Color(1f, 0.5f, 0.3f), corridor = new Color(1f, 0.4f, 0.2f),
                    accent = new Color(1f, 0.35f, 0.08f), rug = new Color(0.25f, 0.04f, 0.02f), rugBorder = new Color(0.6f, 0.25f, 0.08f),
                    banner = new Color(0.35f, 0.04f, 0.02f), motes = new Color(1f, 0.5f, 0.15f),
                    ambient = new Color(0.2f, 0.07f, 0.05f), fog = new Color(0.2f, 0.05f, 0.02f), fogDensity = 0.016f,
                    enemyBody = new Color(0.2f, 0.04f, 0.02f), enemyEye = new Color(1f, 0.35f, 0.1f),
                    floorTile = 3f, wallTile = 2.5f, floorSmooth = 1f, wallSmooth = 1f, emissiveFloor = true, emissiveWall = true,
                };
            case FloorTheme.Soberbia:
                return new ThemePalette
                {
                    key = "soberbia", sin = "SOBERBIA", emotion = "Grandeza fría, juicio, quedar pequeña ante el trono",
                    torch = new Color(1f, 0.9f, 0.7f), center = new Color(0.95f, 0.95f, 1f), corridor = new Color(0.9f, 0.9f, 1f),
                    accent = new Color(1f, 0.85f, 0.45f), rug = new Color(0.5f, 0.06f, 0.1f), rugBorder = new Color(0.95f, 0.78f, 0.35f),
                    banner = new Color(0.55f, 0.07f, 0.12f), motes = new Color(1f, 0.95f, 0.8f),
                    ambient = new Color(0.3f, 0.3f, 0.34f), fog = new Color(0.55f, 0.55f, 0.6f), fogDensity = 0.008f,
                    enemyBody = new Color(0.82f, 0.8f, 0.76f), enemyEye = new Color(1f, 0.82f, 0.3f),
                    floorTile = 3f, wallTile = 3f, floorSmooth = 1f, wallSmooth = 0.8f,
                };
            case FloorTheme.Galerias:
                return new ThemePalette
                {
                    key = "galerias", sin = "", emotion = "Vértigo, viento, vacío bajo los pies",
                    torch = new Color(1f, 0.68f, 0.38f), center = new Color(0.75f, 0.78f, 1f), corridor = new Color(1f, 0.72f, 0.45f),
                    accent = new Color(0.6f, 0.55f, 1f), rug = new Color(0.22f, 0.18f, 0.32f), rugBorder = new Color(0.7f, 0.6f, 0.4f),
                    banner = new Color(0.28f, 0.22f, 0.45f), motes = new Color(0.8f, 0.8f, 1f),
                    ambient = new Color(0.17f, 0.17f, 0.25f), fog = new Color(0.1f, 0.1f, 0.16f), fogDensity = 0f,
                    enemyBody = new Color(0.12f, 0.1f, 0.2f), enemyEye = new Color(0.65f, 0.6f, 1f),
                    floorTile = 3f, wallTile = 3f, floorSmooth = 0.6f, wallSmooth = 0.5f,
                };
            default:
                return new ThemePalette
                {
                    key = "base", sin = "", emotion = "Misterio, el principio de la subida",
                    torch = new Color(1f, 0.6f, 0.3f), center = new Color(0.8f, 0.85f, 1f), corridor = new Color(0.55f, 0.75f, 1f),
                    accent = new Color(0.35f, 0.85f, 1f), rug = new Color(0.38f, 0.07f, 0.08f), rugBorder = new Color(0.75f, 0.58f, 0.25f),
                    banner = new Color(0.55f, 0.1f, 0.12f), motes = new Color(1f, 0.7f, 0.45f),
                    ambient = new Color(0.14f, 0.15f, 0.2f), fog = new Color(0.06f, 0.07f, 0.1f), fogDensity = 0.012f,
                    enemyBody = new Color(0.1f, 0.07f, 0.17f), enemyEye = new Color(0.75f, 0.45f, 1f),
                    floorTile = 2.5f, wallTile = 2.5f, floorSmooth = 0.7f, wallSmooth = 0.5f,
                };
        }
    }

    /// <summary>Aplica la luz ambiente del tema a la escena actual (al generar el piso y en juego, al empezarlo).</summary>
    public static void ApplyAtmosphere(FloorTheme t)
    {
        var p = Get(t);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = p.ambient;
        // sin niebla del motor: con la cámara lejos y las partículas aditivas lava la imagen. La penumbra la dan
        // las salas ocultas (AreaReveal) y la luz ambiente baja.
        RenderSettings.fog = false;
    }
}
