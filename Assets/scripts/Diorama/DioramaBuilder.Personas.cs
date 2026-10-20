using UnityEngine;

/// <summary>
/// Personas para las cinemáticas y el tutorial: figuras de bloques al estilo maqueta (piernas, torso, brazos, cabeza,
/// pelo y accesorios: casco, gorra, mochila, rifle, arma de energía, linterna, paraguas). Abis va con su sprite.
/// </summary>
public partial class DioramaBuilder
{
    public struct Persona
    {
        public Color torso, piernas, piel, pelo;
        public float escala;
        public bool peloLargo, abrigo, mochila, linterna, paraguas, medallas;
        public int sombrero;   // 0 nada, 1 casco, 2 gorra de oficial
        public int arma;       // 0 nada, 1 rifle, 2 espada de energía, 3 bastón de hechizo
        public Color energia;  // color del arma de energía / aura

        public static Persona Civil(Color ropa, Color pelo, float escala = 1f) =>
            new Persona { torso = ropa, piernas = ropa * 0.7f, piel = new Color(0.86f, 0.68f, 0.55f), pelo = pelo, escala = escala };
        public static Persona Soldado(bool linterna) =>
            new Persona { torso = new Color(0.27f, 0.32f, 0.22f), piernas = new Color(0.22f, 0.25f, 0.18f), piel = new Color(0.8f, 0.62f, 0.5f),
                          pelo = new Color(0.15f, 0.1f, 0.06f), escala = 1f, sombrero = 1, arma = 1, mochila = true, linterna = linterna };
        public static Persona Cazador(Color energia) =>
            new Persona { torso = new Color(0.16f, 0.2f, 0.3f), piernas = new Color(0.12f, 0.13f, 0.18f), piel = new Color(0.82f, 0.64f, 0.52f),
                          pelo = new Color(0.12f, 0.08f, 0.05f), escala = 1f, arma = 2, mochila = true, energia = energia };
    }

    /// <summary>Una persona de pie en pos mirando hacia yaw (grados). Devuelve la raíz (sus hijos tienen nombre: Cabeza, BrazoI, BrazoD…).</summary>
    Transform Figura(Transform parent, string name, Vector3 pos, float yaw, Persona p)
    {
        var f = Group(name, parent);
        f.localPosition = pos;
        f.localRotation = Quaternion.Euler(0f, yaw, 0f);
        f.localScale = Vector3.one * (p.escala <= 0f ? 1f : p.escala);
        Material Mat(Color c, string tag) => Plain("Dio_Persona_" + tag + ColorUtility.ToHtmlStringRGB(c), c, 0.15f);
        var torso = Mat(p.torso, "T"); var legs = Mat(p.piernas, "P"); var skin = Mat(p.piel, "S"); var hair = Mat(p.pelo, "H");
        var shoe = Mat(new Color(0.08f, 0.07f, 0.07f), "Z");

        foreach (float sx in new[] { -1f, 1f })
        {
            Box(f, "Pierna", new Vector3(sx * 0.11f, 0.42f, 0f), new Vector3(0.17f, 0.8f, 0.19f), legs, false);
            Box(f, "Zapato", new Vector3(sx * 0.11f, 0.05f, -0.04f), new Vector3(0.18f, 0.1f, 0.28f), shoe, false);
        }
        if (p.abrigo) Box(f, "Abrigo", new Vector3(0f, 0.75f, 0f), new Vector3(0.52f, 0.55f, 0.3f), torso, false);
        Box(f, "Torso", new Vector3(0f, 1.12f, 0f), new Vector3(0.5f, 0.62f, 0.28f), torso, false);
        Box(f, "Cuello", new Vector3(0f, 1.46f, 0f), new Vector3(0.12f, 0.08f, 0.12f), skin, false);
        foreach (float sx in new[] { -1f, 1f })
        {
            var arm = Group(sx < 0f ? "BrazoI" : "BrazoD", f);
            arm.localPosition = new Vector3(sx * 0.33f, 1.38f, 0f);   // pivote en el hombro
            Box(arm, "Manga", new Vector3(0f, -0.27f, 0f), new Vector3(0.13f, 0.56f, 0.15f), torso, false);
            Box(arm, "Mano", new Vector3(0f, -0.6f, 0f), new Vector3(0.11f, 0.11f, 0.12f), skin, false);
        }
        var head = Group("Cabeza", f);
        head.localPosition = new Vector3(0f, 1.66f, 0f);
        Prim(head, PrimitiveType.Sphere, "Cara", Vector3.zero, new Vector3(0.32f, 0.36f, 0.32f), skin);
        Prim(head, PrimitiveType.Sphere, "Pelo", new Vector3(0f, 0.07f, 0.03f), new Vector3(0.35f, 0.3f, 0.35f), hair);
        if (p.peloLargo) Box(head, "Melena", new Vector3(0f, -0.14f, 0.12f), new Vector3(0.32f, 0.36f, 0.1f), hair, false);
        if (p.sombrero == 1)
        {
            var helm = Mat(new Color(0.24f, 0.28f, 0.2f), "C");
            Prim(head, PrimitiveType.Sphere, "Casco", new Vector3(0f, 0.09f, 0f), new Vector3(0.4f, 0.3f, 0.42f), helm);
            Box(head, "Visera", new Vector3(0f, 0.02f, -0.2f), new Vector3(0.34f, 0.04f, 0.08f), helm, false);
        }
        else if (p.sombrero == 2)
        {
            var cap = Mat(new Color(0.12f, 0.14f, 0.2f), "G");
            Prim(head, PrimitiveType.Cylinder, "Gorra", new Vector3(0f, 0.17f, 0f), new Vector3(0.36f, 0.06f, 0.36f), cap);
            Box(head, "Visera", new Vector3(0f, 0.12f, -0.17f), new Vector3(0.28f, 0.03f, 0.12f), Mat(new Color(0.05f, 0.05f, 0.06f), "V"), false);
            Box(head, "Insignia", new Vector3(0f, 0.2f, -0.17f), new Vector3(0.07f, 0.06f, 0.02f), Glow("Dio_Insignia", new Color(1f, 0.82f, 0.4f), 1.2f), false);
        }
        if (p.medallas)
            for (int i = 0; i < 3; i++) Box(f, "Medalla", new Vector3(-0.12f + i * 0.08f, 1.24f, -0.15f), new Vector3(0.05f, 0.07f, 0.02f), Glow("Dio_Medalla", new Color(1f, 0.8f, 0.35f), 1f), false);
        if (p.mochila) Box(f, "Mochila", new Vector3(0f, 1.1f, 0.24f), new Vector3(0.38f, 0.48f, 0.2f), Mat(new Color(0.3f, 0.26f, 0.18f), "M"), false);
        var right = f.Find("BrazoD");
        if (p.arma == 1)
        {
            var rifle = Box(right, "Rifle", new Vector3(0.05f, -0.45f, -0.25f), new Vector3(0.07f, 0.1f, 0.9f), Mat(new Color(0.1f, 0.1f, 0.1f), "R"), false);
            rifle.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        }
        else if (p.arma == 2 || p.arma == 3)
        {
            var e = p.energia == default ? new Color(0.35f, 0.8f, 1f) : p.energia;
            var glow = Glow("Dio_Energia" + ColorUtility.ToHtmlStringRGB(e), e, 2.2f);
            if (p.arma == 2)
            {
                var blade = Box(right, "Hoja", new Vector3(0f, -0.62f, -0.55f), new Vector3(0.06f, 0.08f, 0.9f), glow, false);
                blade.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                Box(right, "Baston", new Vector3(0f, -0.3f, -0.05f), new Vector3(0.05f, 1.4f, 0.05f), Mat(new Color(0.3f, 0.2f, 0.12f), "B"), false);
                Prim(right, PrimitiveType.Sphere, "Orbe", new Vector3(0f, 0.42f, -0.05f), Vector3.one * 0.18f, glow);
            }
            AddLight(f, new Vector3(0.4f, 1f, -0.5f), e, 4f, 1.6f);
        }
        if (p.linterna)
        {
            var lamp = new GameObject("Linterna").AddComponent<Light>();
            lamp.transform.SetParent(f, false);
            lamp.transform.localPosition = new Vector3(0.2f, 1.3f, -0.3f);
            lamp.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            lamp.type = LightType.Spot; lamp.spotAngle = 38f; lamp.range = 30f; lamp.intensity = 6f;
            lamp.color = new Color(1f, 0.95f, 0.8f); lamp.shadows = LightShadows.None;
        }
        if (p.paraguas)
        {
            var um = Group("Paraguas", right);
            um.localPosition = new Vector3(0f, -0.55f, 0f);
            Box(um, "Mango", new Vector3(0f, 0.55f, 0f), new Vector3(0.03f, 1.1f, 0.03f), Mat(new Color(0.1f, 0.1f, 0.1f), "U"), false);
            var top = MeshObject(um, "Tela", Cone(1f, 1f, 8), Mat(new Color(0.06f, 0.06f, 0.07f), "N"), true);
            top.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            top.transform.localScale = new Vector3(0.62f, 0.32f, 0.62f);
            right.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        return f;
    }

#if UNITY_EDITOR
    /// <summary>Abis con su sprite de frente (el mismo personaje que se juega), en un billboard que mira a la cámara.</summary>
    Transform AbisSprite(Transform parent, Vector3 pos, float escala, string dir = "south")
    {
        var t = Group("Abis", parent);
        t.localPosition = pos;
        var vis = new GameObject("Visual");
        vis.transform.SetParent(t, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        Sprite sprite = null;
        foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Abby/Idle/rotations/" + dir + ".png"))
            if (o is Sprite s) { sprite = s; break; }
        sr.sprite = sprite;
        var player = GameObject.FindWithTag("Player");
        var psr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (psr != null) { sr.sharedMaterial = psr.sharedMaterial; vis.transform.localScale = psr.transform.localScale * escala; }
        else vis.transform.localScale = Vector3.one * escala;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        vis.AddComponent<SpriteBillboard>();
        return t;
    }
#endif
}
