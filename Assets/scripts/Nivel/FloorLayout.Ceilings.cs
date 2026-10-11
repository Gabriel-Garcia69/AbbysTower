using UnityEngine;

/// <summary>
/// Techos de salas, pasillos y escondites: bóveda con la textura del muro, moldura de piedra alrededor y vigas que
/// forman casetones (madera, piedra o latón según el tema). Se disfrutan con la cámara baja (F3), desde adentro.
/// El plano del techo es de una cara (mira hacia abajo): desde arriba (cámaras altas) es invisible, y CeilingView
/// oculta las vigas y la moldura cuando la cámara está por encima. Llevan collider para que la cámara no lo atraviese.
/// </summary>
public partial class FloorLayout
{
    [Header("Techos")]
    [Tooltip("Salas, pasillos y escondites con techo.")]
    public bool ceilings = true;
    [Tooltip("Multiplica el alto de diseño de los muros de cada sala (techos altos).")]
    public float roomHeightScale = 1.5f;
    [Tooltip("Alto mínimo de una sala (m).")]
    public float minRoomHeight = 9f;
    [Tooltip("Alto mínimo de puertas y pasillos (m): que la cámara baja quepa dentro.")]
    public float minDoorHeight = 5f;
    [Tooltip("Separación entre vigas del techo (m).")]
    public float beamSpacing = 3.2f;

    /// <summary>Altura del techo de la sala que se está generando (local), o NaN. Las lámparas cuelgan sus cadenas hasta aquí.</summary>
    float ceilingY = float.NaN;

    /// <summary>Techo de una sala: bóveda, moldura en el borde de los muros y retícula de vigas (casetones).</summary>
    void RoomCeiling(Transform parent, float w, float d, float y)
    {
        var root = NewChild(parent, "Techo");
        root.localPosition = new Vector3(0f, y, 0f);
        CeilingPlane(root, w, d);

        var trim = CapMaterial();
        var beam = CeilingBeamMaterial();
        float t = 0.5f, mh = 0.7f;
        // moldura (cornisa) pegada a los cuatro muros
        Ornament(root, "Moldura", new Vector3(0f, -mh / 2f, d / 2f - t / 2f), new Vector3(w, mh, t), trim);
        Ornament(root, "Moldura", new Vector3(0f, -mh / 2f, -d / 2f + t / 2f), new Vector3(w, mh, t), trim);
        Ornament(root, "Moldura", new Vector3(w / 2f - t / 2f, -mh / 2f, 0f), new Vector3(t, mh, d - 2f * t), trim);
        Ornament(root, "Moldura", new Vector3(-w / 2f + t / 2f, -mh / 2f, 0f), new Vector3(t, mh, d - 2f * t), trim);
        // una segunda moldura más fina, un poco más abajo: da escalón y sombra
        float t2 = 0.25f, y2 = -mh - 0.12f;
        Ornament(root, "Friso", new Vector3(0f, y2, d / 2f - t2 / 2f), new Vector3(w, 0.18f, t2), beam);
        Ornament(root, "Friso", new Vector3(0f, y2, -d / 2f + t2 / 2f), new Vector3(w, 0.18f, t2), beam);
        Ornament(root, "Friso", new Vector3(w / 2f - t2 / 2f, y2, 0f), new Vector3(t2, 0.18f, d - 2f * t2), beam);
        Ornament(root, "Friso", new Vector3(-w / 2f + t2 / 2f, y2, 0f), new Vector3(t2, 0.18f, d - 2f * t2), beam);

        // vigas maestras a lo corto (más gruesas) y viguetas a lo largo: casetones
        bool alongX = w <= d;   // las maestras cruzan el lado corto
        float span = alongX ? w : d, run = alongX ? d : w;
        int main = Mathf.Max(1, Mathf.RoundToInt(run / beamSpacing) - 1);
        for (int i = 1; i <= main; i++)
        {
            float p = -run / 2f + run * i / (main + 1f);
            Vector3 pos = alongX ? new Vector3(0f, -0.3f, p) : new Vector3(p, -0.3f, 0f);
            Vector3 size = alongX ? new Vector3(span - 2f * t, 0.6f, 0.45f) : new Vector3(0.45f, 0.6f, span - 2f * t);
            Ornament(root, "Viga", pos, size, beam);
        }
        int cross = Mathf.Max(1, Mathf.RoundToInt(span / beamSpacing) - 1);
        for (int i = 1; i <= cross; i++)
        {
            float p = -span / 2f + span * i / (cross + 1f);
            Vector3 pos = alongX ? new Vector3(p, -0.15f, 0f) : new Vector3(0f, -0.15f, p);
            Vector3 size = alongX ? new Vector3(0.28f, 0.3f, run - 2f * t) : new Vector3(run - 2f * t, 0.3f, 0.28f);
            Ornament(root, "Vigueta", pos, size, beam);
        }
        // rosetón en cada cruce de viga maestra con la central (donde cuelgan las lámparas se ve rico)
        for (int i = 1; i <= main; i++)
        {
            float p = -run / 2f + run * i / (main + 1f);
            Vector3 pos = alongX ? new Vector3(0f, -0.66f, p) : new Vector3(p, -0.66f, 0f);
            Ornament(root, "Roseton", pos, new Vector3(0.7f, 0.14f, 0.7f), trim);
        }

        // luz de bóveda: cálida y sin sombra, debajo del techo, para que vigas y casetones se lean desde abajo
        float big = Mathf.Max(w, d);
        Color c = theme == FloorTheme.Base ? new Color(1f, 0.72f, 0.48f) : Color.Lerp(palette.center, new Color(1f, 0.8f, 0.6f), 0.4f);
        var lt = AddLight(root, new Vector3(0f, -Mathf.Min(3f, y * 0.3f), 0f), c, big * 0.85f, centerLightIntensity * 0.45f * Mathf.Max(1f, big / 16f), false);
        lt.name = "LuzBoveda";

        root.gameObject.AddComponent<CeilingView>();
    }

    /// <summary>Techo de pasillo: bóveda plana con arcos (fajones) cada pocos metros.</summary>
    void CorridorCeiling(Transform parent, float width, float len, float y)
    {
        var root = NewChild(parent, "Techo");
        root.localPosition = new Vector3(0f, y, 0f);
        CeilingPlane(root, width, len);
        var beam = CeilingBeamMaterial();
        var trim = CapMaterial();
        Ornament(root, "Moldura", new Vector3(width / 2f - 0.15f, -0.2f, 0f), new Vector3(0.3f, 0.4f, len), trim);
        Ornament(root, "Moldura", new Vector3(-width / 2f + 0.15f, -0.2f, 0f), new Vector3(0.3f, 0.4f, len), trim);
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
        for (int i = 0; i <= n; i++)
        {
            float z = -len / 2f + len * i / n;
            z = Mathf.Clamp(z, -len / 2f + 0.2f, len / 2f - 0.2f);
            Ornament(root, "Fajon", new Vector3(0f, -0.25f, z), new Vector3(width, 0.5f, 0.35f), beam);
        }
        root.gameObject.AddComponent<CeilingView>();
    }

    /// <summary>Techo sencillo (escondites).</summary>
    void PlainCeiling(Transform parent, float w, float d, float y)
    {
        var root = NewChild(parent, "Techo");
        root.localPosition = new Vector3(0f, y, 0f);
        CeilingPlane(root, w, d);
        Ornament(root, "Viga", new Vector3(0f, -0.2f, 0f), new Vector3(w, 0.4f, 0.3f), CeilingBeamMaterial());
        root.gameObject.AddComponent<CeilingView>();
    }

    void CeilingPlane(Transform root, float w, float d)
    {
        CreateFace(root, "Boveda", Vector3.zero, Vector3.down, w, d, CeilingMaterial(), WallK, WallK, false);
        CreateCollider(root, "Collider_Techo", new Vector3(0f, colliderThickness / 2f, 0f), new Vector3(w, colliderThickness, d));
    }

    void Ornament(Transform root, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var g = Prim(root, PrimitiveType.Cube, name, pos, size, mat, false);
        g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // sin costo de sombras
    }

    /// <summary>La bóveda: la textura del muro del tema, más oscura (queda lejos de las antorchas).</summary>
    Material CeilingMaterial() => SavedMaterial("Techo_" + palette.key, () =>
    {
        var m = wallMaterial != null ? new Material(wallMaterial) : new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * 0.62f);
        return m;
    });

    /// <summary>Vigas: madera en los pisos cálidos, latón en los ricos, piedra en el resto.</summary>
    Material CeilingBeamMaterial()
    {
        switch (theme)
        {
            case FloorTheme.Avaricia:
            case FloorTheme.Soberbia:
                return BrassMaterial();
            case FloorTheme.Envidia:
            case FloorTheme.Lujuria:
                return ThemeStone();
            default:
                return LitMaterial("MaderaVigas", new Color(0.24f, 0.15f, 0.09f));
        }
    }

    /// <summary>Alarga la cadena de una lámpara colgante hasta el techo de la sala (si hay).</summary>
    void ChainToCeiling(Transform lamp, Vector3 pos, float top, Material metal, float thickness)
    {
        if (float.IsNaN(ceilingY)) return;
        float len = ceilingY - pos.y - top;
        if (len < 0.1f) return;
        var c = Prim(lamp, PrimitiveType.Cube, "CadenaTecho", new Vector3(0f, top + len / 2f, 0f), new Vector3(thickness, len, thickness), metal, false);
        c.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // anclaje en el techo
        var a = Prim(lamp, PrimitiveType.Cylinder, "Anclaje", new Vector3(0f, top + len - 0.05f, 0f), new Vector3(0.35f, 0.05f, 0.35f), metal, false);
        a.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
