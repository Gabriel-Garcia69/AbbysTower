using UnityEngine;

/// <summary>
/// Tutorial — la expedición en el piso 20 (propuesta del guion: "que sea la expedición en el piso 20 y muestre cómo
/// fueron derrotados"). Una gruta oscura: el campamento de la expedición, un escalón para aprender a saltar, la zona de
/// entrenamiento, la barrera de energía que se cruza con impulso, la pelea junto a la expedición y el borde del abismo
/// donde aparece la criatura que los derrota. Lo maneja ExpedicionPiso20.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect ExpArea = new Rect(-70f, -84f, 140f, 280f);
    static readonly Rect ExpWalk = new Rect(-24f, -66f, 48f, 196f);
    static readonly Vector3 ExpSpawn = new Vector3(0f, 0.1f, -58f);
    const float ExpLedgeZ0 = -30f, ExpLedgeZ1 = -14f, ExpVeilZ = 4f, ExpEdgeZ = 128f;

    float ExpHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (z > ExpEdgeZ + e * 0.8f) return -40f;   // el abismo
        float w = Mathf.Abs(x) - 22f + e * 2f;
        if (w > 0f || z < -68f + e * 2f) return w > 6f || z < -74f ? 14f : 6f;   // paredes de la gruta
        if (z > ExpLedgeZ0 && z < ExpLedgeZ1) return 0.9f;   // el escalón (hay que saltar)
        return 0f;
    }

    void BuildExpedition()
    {
        BuildTerrain();
        var g = Group("Gruta", root);
        var lvl = Group("Nivel", root);

        // cristales y estalagmitas por las paredes
        for (int i = 0; i < 60; i++)
        {
            float z = R(-64f, 126f), x = (rng.NextDouble() < 0.5 ? -1f : 1f) * R(17f, 21f);
            if (rng.NextDouble() < 0.5) Crystals(g, new Vector3(x, 0f, z), R(0.6f, 1.6f), rng.NextDouble() < 0.4, i % 4 == 0);
            else Stalagmite(g, new Vector3(x, 0f, z), R(2f, 6f));
        }
        // el campamento de la expedición
        Tent(g, new Vector3(-10f, 0f, -56f), 30f, mCloth);
        Tent(g, new Vector3(10f, 0f, -54f), -30f, mClothB);
        Campfire(g, new Vector3(0f, 0f, -50f));
        Clutter(g, new Vector3(-6f, 0f, -60f));
        Clutter(g, new Vector3(7f, 0f, -61f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var tri = Group("Reflector", g);
            tri.localPosition = new Vector3(sx * 12f, 0f, -44f);
            Box(tri, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), mMetal, false);
            Box(tri, "Foco", new Vector3(0f, 3.1f, 0.2f), new Vector3(0.7f, 0.5f, 0.6f), mMetal, false);
            var l = new GameObject("Haz").AddComponent<Light>();
            l.transform.SetParent(tri, false);
            l.transform.localPosition = new Vector3(0f, 3.1f, 0.6f);
            l.transform.localRotation = Quaternion.Euler(25f, -sx * 15f, 0f);
            l.type = LightType.Spot; l.spotAngle = 55f; l.range = 50f; l.intensity = 14f; l.color = new Color(1f, 0.95f, 0.85f); l.shadows = LightShadows.None;
        }
        // un letrero de la expedición: "PISO 20"
        var sign = Group("Letrero", g);
        sign.localPosition = new Vector3(5f, 0f, -45f);
        Box(sign, "Poste", new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), mTimber, false);
        Box(sign, "Tabla", new Vector3(0f, 1.7f, -0.05f), new Vector3(1.4f, 0.6f, 0.08f), mPlank, false);
        Box(sign, "Marca", new Vector3(0f, 1.7f, -0.1f), new Vector3(1f, 0.25f, 0.02f), Glow("Dio_MarcaPiso", new Color(1f, 0.8f, 0.4f), 1.2f), false);

        // la barrera de energía (solo se cruza con impulso)
        var veil = new GameObject("VeloDeFase");
        veil.transform.SetParent(lvl, false);
        veil.transform.localPosition = new Vector3(0f, 3f, ExpVeilZ);
        veil.AddComponent<BoxCollider>().size = new Vector3(48f, 8f, 0.4f);
        foreach (float off in new[] { -0.04f, 0.04f })
        {
            var face = Prim(veil.transform, PrimitiveType.Cube, "Cara", new Vector3(0f, 0f, off), new Vector3(44f, 6f, 0.02f), Glow("Dio_Velo", new Color(0.55f, 0.25f, 0.9f), 0.9f), false);
            face.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        veil.AddComponent<PhaseVeil>();

        // el borde del abismo y la oscuridad de abajo
        for (int i = 0; i < 9; i++) Crystals(g, new Vector3(-16f + i * 4f, 0f, ExpEdgeZ - 1.2f), R(0.4f, 0.7f), true, i % 3 == 0);
        Flat(g, "Fondo", new Vector3(0f, -39f, ExpEdgeZ + 60f), 200f, 120f, Plain("Dio_Negro", Color.black, 0f), 0.1f);
        // la criatura que espera abajo (la cinemática la sube)
        var colosoPf = Resources.Load<GameObject>("Enemigos/Jefe");
        var coloso = Group("Coloso", lvl);
        coloso.localPosition = new Vector3(0f, -60f, ExpEdgeZ + 22f);
        if (colosoPf != null)
        {
            var v = (GameObject)Instantiate(colosoPf, coloso);
            v.transform.localScale = Vector3.one * 5.5f;
            v.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var prop = coloso.gameObject.AddComponent<CriaturaProp>();
            prop.cuerpo = new Color(0.04f, 0.02f, 0.06f); prop.ojos = new Color(1f, 0.15f, 0.1f); prop.bob = 0.6f;
        }
        AddLight(coloso, new Vector3(0f, 16f, -8f), new Color(1f, 0.2f, 0.15f), 50f, 6f);

        // la expedición: el capitán y los cazadores (ExpedicionPiso20 los va moviendo)
        var exp = Group("Expedicion", lvl);
        var capP = new Persona { torso = new Color(0.14f, 0.17f, 0.26f), piernas = new Color(0.1f, 0.11f, 0.16f), piel = new Color(0.72f, 0.55f, 0.43f),
                                 pelo = new Color(0.4f, 0.38f, 0.36f), escala = 1.05f, sombrero = 2, medallas = true, arma = 2, energia = new Color(1f, 0.8f, 0.4f), mochila = true };
        Figura(exp, "Capitan", new Vector3(-2f, 0f, -48f), 180f, capP);
        Color[] e = { new Color(0.35f, 0.8f, 1f), new Color(1f, 0.55f, 0.2f), new Color(0.5f, 1f, 0.6f), new Color(0.9f, 0.4f, 1f), new Color(0.35f, 0.8f, 1f), new Color(1f, 0.55f, 0.2f) };
        for (int i = 0; i < 6; i++)
        {
            var pe = i % 3 == 2 ? Persona.Soldado(true) : Persona.Cazador(e[i]);
            if (i == 3) pe.arma = 3;
            Figura(exp, "Miembro" + i, new Vector3(-8f + i * 3f, 0f, -52f + (i % 2) * 2f), 0f, pe);
        }

        // combate del tutorial: entrenamiento y la oleada con la expedición
        var a1 = Arena(lvl, "Entrenamiento", new Vector3(0f, 0f, 30f), new Vector2(30f, 18f), 3);
        a1.roster = new[] { TowerEnemy.Kind.Sombra };
        a1.leash = 10f;
        var a2 = Arena(lvl, "La oleada", new Vector3(0f, 0f, 78f), new Vector2(34f, 22f), 6);
        a2.roster = new[] { TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Bruto, TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Saltador };
        a2.leash = 12f;

        Trail(g, 4f, new Vector2(0f, -60f), new Vector2(0f, ExpLedgeZ0 - 1f));
        Trail(g, 4f, new Vector2(0f, ExpLedgeZ1 + 1f), new Vector2(0f, ExpEdgeZ - 3f));
        for (float z = -40f; z < ExpEdgeZ; z += 16f) foreach (float sx in new[] { -1f, 1f }) Brazier(lvl, new Vector3(sx * 7f, 0f, z));

        WalkBoundary(ExpWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 14f, true, true, true, true, 0);

        var dir = new GameObject("Tutorial");
        dir.transform.SetParent(root, false);
        dir.AddComponent<ExpedicionPiso20>();
        BuildAtmosphere();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }
}
