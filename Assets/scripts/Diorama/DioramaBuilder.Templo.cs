using UnityEngine;

/// <summary>
/// Piso 5 — El Templo de Arena (noche estrellada). Se llega por un camino de braseros entre dunas hasta un templo de
/// sillar en tres patios escalonados (estilo Octopath): columnatas, obeliscos, colosos sentados y un santuario con su
/// puerta de piedra. Al oeste, un oasis con palmeras y el campamento del mercader.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect TempleArea = new Rect(-80f, -70f, 160f, 140f);
    static readonly Rect TempleWalk = new Rect(-50f, -56f, 100f, 106f);
    static readonly Vector3 TempleSpawn = new Vector3(0f, 0.1f, -48f);
    static readonly Vector2 Oasis = new Vector2(-32f, -20f);

    float TempleHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (Mathf.Abs(x) < 26f && z > -3f && z < 50f) return 0f;          // planta del templo
        if (Mathf.Abs(x) < 4.5f && z <= -3f) return 0f;                   // camino de braseros
        float dOasis = Vector2.Distance(new Vector2(x, z), Oasis);
        if (dOasis < 6.5f + e * 0.6f) return -1.2f;
        if (dOasis < 14f) return 0f;
        float ring = Mathf.Max(Mathf.Abs(x) - 48f, Mathf.Max(z - 52f, -z - 56f)) + e * 2f;
        if (ring > 14f) return 6.4f;
        if (ring > 7f) return 4.8f;
        if (ring > 0f) return 3.2f;
        return 0f;
    }

    /// <summary>Plataforma de sillar (con colisión) y su piso de losas.</summary>
    void Platform(Transform parent, float x0, float x1, float z0, float z1, float h)
    {
        Box(parent, "Plataforma", new Vector3((x0 + x1) / 2f, (h - 1f) / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, h + 1f, z1 - z0), mSandstone, true, 0.25f);
        Flat(parent, "Losas", new Vector3((x0 + x1) / 2f, h + 0.01f, (z0 + z1) / 2f), x1 - x0 - 0.4f, z1 - z0 - 0.4f, mStoneBrick, 0.25f);
        // cornisa
        Box(parent, "Cornisa", new Vector3((x0 + x1) / 2f, h - 0.12f, z0 - 0.1f), new Vector3(x1 - x0 + 0.4f, 0.25f, 0.4f), mSandstone, false, 0.25f);
    }

    void TempleColumn(Transform parent, Vector3 pos, float height, bool broken)
    {
        var c = Group("Columna", parent);
        c.localPosition = pos;
        Box(c, "Basa", new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.5f, 1.4f), mSandstone, false, 0.25f);
        float shaft = broken ? height * R(0.3f, 0.6f) : height - 0.9f;
        Prim(c, PrimitiveType.Cylinder, "Fuste", new Vector3(0f, 0.5f + shaft / 2f, 0f), new Vector3(0.95f, shaft / 2f, 0.95f), mSandstone);
        if (!broken)
        {
            Box(c, "Capitel", new Vector3(0f, height - 0.25f, 0f), new Vector3(1.5f, 0.5f, 1.5f), mSandstone, false, 0.25f);
            Box(c, "Glifo", new Vector3(0f, height * 0.55f, -0.49f), new Vector3(0.3f, 0.5f, 0.02f), mGlow, false);
        }
        else
        {
            var piece = Prim(c, PrimitiveType.Cylinder, "Trozo", new Vector3(R(1.2f, 1.8f), 0.45f, R(-0.6f, 0.6f)), new Vector3(0.9f, 0.7f, 0.9f), mSandstone);
            piece.transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), 90f);
        }
        var col = c.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, height / 2f, 0f); col.size = new Vector3(1.2f, height, 1.2f);
    }

    void Colossus(Transform parent, Vector3 pos, float yaw)
    {
        var s = Group("Coloso", parent);
        s.localPosition = pos;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(s, "Trono", new Vector3(0f, 1.2f, 0.6f), new Vector3(3.4f, 2.4f, 3f), mSandstone, true, 0.25f);
        Box(s, "Respaldo", new Vector3(0f, 4f, 1.7f), new Vector3(3.4f, 5f, 0.8f), mSandstone, false, 0.25f);
        Box(s, "Cuerpo", new Vector3(0f, 4f, 0.8f), new Vector3(2.2f, 3.4f, 1.6f), mSandstone, false, 0.25f);
        Box(s, "Cabeza", new Vector3(0f, 6.4f, 0.8f), new Vector3(1.4f, 1.5f, 1.3f), mSandstone, false, 0.25f);
        Box(s, "Tocado", new Vector3(0f, 7.4f, 0.9f), new Vector3(1.8f, 0.6f, 1.5f), mSandstone, false, 0.25f);
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(s, "Brazo", new Vector3(sx * 1.4f, 3.2f, -0.2f), new Vector3(0.7f, 0.7f, 2.4f), mSandstone, false, 0.25f);
            Box(s, "Pierna", new Vector3(sx * 0.6f, 1.2f, -1.2f), new Vector3(0.9f, 2.4f, 1f), mSandstone, false, 0.25f);
            Box(s, "Ojo", new Vector3(sx * 0.3f, 6.5f, 0.12f), new Vector3(0.22f, 0.12f, 0.05f), mGlow, false);
        }
    }

    void BuildTemple()
    {
        BuildTerrain();
        var g = Group("Templo", root);
        var lvl = Group("Nivel", root);

        // patios escalonados y escalinatas
        Platform(g, -20f, 20f, 0f, 16f, 1.2f);
        Platform(g, -16f, 16f, 16f, 32f, 2.8f);
        Platform(g, -12f, 12f, 32f, 46f, 4.6f);
        Stairs(lvl, "Escalinata0", new Vector3(0f, 0f, -3.8f), new Vector3(0f, 1.2f, 0.3f), 5f, mSandstone);
        Stairs(lvl, "Escalinata1", new Vector3(0f, 1.2f, 11.4f), new Vector3(0f, 2.8f, 16.3f), 5f, mSandstone);
        Stairs(lvl, "Escalinata2", new Vector3(0f, 2.8f, 27.4f), new Vector3(0f, 4.6f, 32.3f), 5f, mSandstone);

        // columnatas
        for (float z = 2.5f; z < 15f; z += 3.6f)
            foreach (float sx in new[] { -1f, 1f }) TempleColumn(g, new Vector3(sx * 18f, 1.2f, z), 6f, rng.NextDouble() < 0.25);
        for (float z = 18.5f; z < 31f; z += 3.6f)
            foreach (float sx in new[] { -1f, 1f }) TempleColumn(g, new Vector3(sx * 14.2f, 2.8f, z), 6.5f, rng.NextDouble() < 0.2);
        // obeliscos de la entrada
        foreach (float sx in new[] { -1f, 1f })
        {
            var ob = Group("Obelisco", g);
            ob.localPosition = new Vector3(sx * 6.5f, 0f, -7f);
            Box(ob, "Base", new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 2.2f), mSandstone, true, 0.25f);
            var shaft = MeshObject(ob, "Aguja", Cone(1f, 1f, 4), mSandstone, true);
            shaft.transform.localPosition = new Vector3(0f, 1f, 0f);
            shaft.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            shaft.transform.localScale = new Vector3(1.1f, 9f, 1.1f);
            Box(ob, "Runa", new Vector3(0f, 3.2f, -0.62f), new Vector3(0.35f, 1.4f, 0.02f), mGlow, false);
        }
        // colosos, puerta del santuario y altar
        Colossus(g, new Vector3(-8.5f, 4.6f, 42.5f), 0f);
        Colossus(g, new Vector3(8.5f, 4.6f, 42.5f), 0f);
        var gate = Group("Portada", g);
        gate.localPosition = new Vector3(0f, 4.6f, 45.4f);
        Box(gate, "Muro", new Vector3(0f, 4f, 0f), new Vector3(10f, 8f, 1.2f), mSandstone, true, 0.25f);
        Box(gate, "Vano", new Vector3(0f, 2.4f, -0.62f), new Vector3(3f, 4.8f, 0.05f), mGlow, false);
        Box(gate, "Remate", new Vector3(0f, 8.4f, 0f), new Vector3(11f, 0.8f, 1.6f), mSandstone, false, 0.25f);
        AddLight(gate, new Vector3(0f, 2.5f, -2f), new Color(1f, 0.7f, 0.35f), 12f, 4f);
        var altar = Group("Altar", g);
        altar.localPosition = new Vector3(0f, 4.6f, 40.5f);
        Box(altar, "Mesa", new Vector3(0f, 0.6f, 0f), new Vector3(3f, 1.2f, 1.6f), mSandstone, true, 0.25f);
        var disk = Prim(altar, PrimitiveType.Cylinder, "Disco", new Vector3(0f, 2.2f, 0.3f), new Vector3(1.6f, 0.06f, 1.6f), mGlow);
        disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // camino de braseros entre las dunas
        for (float z = -44f; z < -4f; z += 8f)
            foreach (float sx in new[] { -1f, 1f }) Brazier(g, new Vector3(sx * 3.8f, 0f, z));
        foreach (var p in new[] { new Vector3(-17f, 1.2f, 1.5f), new Vector3(17f, 1.2f, 1.5f), new Vector3(-13.5f, 2.8f, 17.5f), new Vector3(13.5f, 2.8f, 17.5f), new Vector3(-10f, 4.6f, 33.5f), new Vector3(10f, 4.6f, 33.5f) })
            Brazier(g, p);
        // dunas suaves (montículos de arena)
        for (int i = 0, tries = 0; i < 26 && tries < 600; tries++)
        {
            var q = new Vector2(R(-46f, 46f), R(-52f, 48f));
            if ((Mathf.Abs(q.x) < 28f && q.y > -6f) || Mathf.Abs(q.x) < 7f || Vector2.Distance(q, Oasis) < 16f) continue;
            var d = Prim(g, PrimitiveType.Sphere, "Duna", new Vector3(q.x, -R(1.2f, 2.2f), q.y), new Vector3(R(10f, 18f), R(3.5f, 5f), R(7f, 12f)), mSandPix, true);
            d.transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), 0f);
            d.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            blocked.Add(new Rect(q.x - 8f, q.y - 8f, 16f, 16f));
            i++;
        }
        // ruinas medio enterradas y rocas en las dunas
        for (int i = 0; i < 14; i++)
        {
            var q = new Vector2(R(-46f, 46f), R(-50f, 46f));
            if (Mathf.Abs(q.x) < 27f && q.y > -6f) continue;
            if (Vector2.Distance(q, Oasis) < 15f || Mathf.Abs(q.x) < 6f) continue;
            float h = TempleHeight(q.x, q.y);
            if (rng.NextDouble() < 0.5)
            {
                var b = Box(g, "Sillar", new Vector3(q.x, h + 0.3f, q.y), new Vector3(R(1f, 2.6f), R(0.8f, 1.6f), R(1f, 2f)), mSandstone, true, 0.25f);
                b.transform.localRotation = Quaternion.Euler(R(-12f, 12f), R(0f, 90f), R(-12f, 12f));
            }
            else TempleColumn(g, new Vector3(q.x, h, q.y), R(3f, 5f), true);
        }

        // oasis: agua, palmeras, campamento del mercader
        var oa = Group("Oasis", g);
        var pool = Flat(oa, "Agua", new Vector3(Oasis.x, -0.45f, Oasis.y), 16f, 16f, mWater, 0.15f);
        pool.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 9; i++)
        {
            float a = i / 9f * Mathf.PI * 2f + R(-0.2f, 0.2f);
            Palm(oa, new Vector3(Oasis.x + Mathf.Cos(a) * R(8f, 11f), 0f, Oasis.y + Mathf.Sin(a) * R(8f, 11f)), R(1f, 1.3f));
        }
        blocked.Add(new Rect(Oasis.x - 13f, Oasis.y - 13f, 26f, 26f));
        MerchantPost(lvl, new Vector3(Oasis.x + 10f, 0f, Oasis.y + 9f), 210f);
        HealShrine(lvl, new Vector3(Oasis.x + 11.5f, 0f, Oasis.y - 3f));
        Tent(lvl, new Vector3(Oasis.x - 9f, 0f, Oasis.y + 9f), 120f, mCloth);
        Campfire(lvl, new Vector3(Oasis.x + 6.5f, 0f, Oasis.y + 3.5f));

        Arena(lvl, "El patio exterior", new Vector3(0f, 1.2f, 7f), new Vector2(34f, 11f), 6);
        Arena(lvl, "El patio de las columnas", new Vector3(0f, 2.8f, 23f), new Vector2(26f, 10f), 6);
        var boss = Arena(lvl, "Jefe", new Vector3(0f, 4.6f, 37.5f), new Vector2(20f, 8f), 1, true);
        Exit(lvl, new Vector3(0f, 4.6f, 43.4f), boss);
        Chest(lvl, new Vector3(-17.5f, 2.8f, 30f), 30, 90f);
        Chest(lvl, new Vector3(Oasis.x - 11f, 0f, Oasis.y - 5f), 26, 30f);
        Note(lvl, new Vector3(5.5f, 0f, -44f), 180f, LoreNote.Kind.Inscripcion, "dio5_camino", "Al pie del camino",
             "Los que suben a la torre traen fuego.\nLos que bajan lo dejan aquí.\nCuenta los braseros: así sabrás cuántos volvieron.");
        Note(lvl, new Vector3(Oasis.x + 5f, 0f, Oasis.y + 10f), 200f, LoreNote.Kind.Diario, "dio5_diario", "Noche en el desierto",
             "Encontré mi propia letra en una piedra del templo: \"sigue subiendo\".\nNo recuerdo haberla escrito. Recuerdo el frío de esta arena.", "— A.");

        WalkBoundary(TempleWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 6.4f, true, true, true, true, 0);
        EnsurePlantMeshes();
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f;
            var m = MeshObject(bg, "Meseta", blobMeshes[rng.Next(blobMeshes.Length)], mSandstone, false);
            m.transform.localPosition = new Vector3(Mathf.Cos(a) * R(260f, 360f), 6f, Mathf.Sin(a) * R(260f, 360f));
            m.transform.localScale = new Vector3(R(60f, 120f), R(30f, 60f), R(60f, 120f));
        }

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 30f, 100f, 32f, 4f, new Vector3(0f, 0f, 20f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Llegada", new Vector3(0f, 0f, -10f), new Vector2(8f, 8f), 0f, 10f, 26f, 36f, 12f, new Vector3(0f, 2f, 12f), 2f, 2,
             "EL TEMPLO DE ARENA", "Bajo las estrellas, la puerta sigue abierta");
        Shot(shots, "Oasis", new Vector3(Oasis.x + 8f, 0f, Oasis.y + 4f), new Vector2(8f, 10f), 220f, 18f, 20f, 36f, 6f, new Vector3(-6f, 0f, -4f), 2f, 2,
             "EL OASIS", "", still: true);
        FinishWorld(false, false);
    }
}
