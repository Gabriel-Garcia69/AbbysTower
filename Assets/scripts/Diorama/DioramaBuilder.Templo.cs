using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 7 — El Templo de Arena (noche estrellada). Un viaje largo por el desierto: el campamento de la caravana entre
/// dunas, un desfiladero serpenteante entre mesetas de arenisca, el pueblo del oasis con su mercado, la avenida de los
/// obeliscos y, al final, el templo de sillar en tres patios escalonados donde espera el Guardián Dorado.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect TempleArea = new Rect(-110f, -82f, 220f, 364f);
    static readonly Rect TempleWalk = new Rect(-56f, -66f, 112f, 316f);
    static readonly Vector3 TempleSpawn = new Vector3(0f, 0.1f, -58f);
    static readonly Vector2 Oasis = new Vector2(-24f, 74f);
    const float TempleZ = 190f;                          // dónde empieza el templo
    const float CanyonZ0 = -24f, CanyonZ1 = 40f;          // el desfiladero
    const float MesaY = 9.6f;

    static float CanyonX(float z) => 12f * Mathf.Sin((z - CanyonZ0) * 0.07f);
    static float CanyonHalf(float z) => z > 6f && z < 28f ? 13f : 7.5f;   // a la mitad se abre una cámara

    float TempleHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (Mathf.Abs(x) < 26f && z > TempleZ - 3f && z < TempleZ + 60f) return 0f;   // planta del templo
        // el desfiladero: mesetas de arenisca y un pasillo que serpentea
        if (z > CanyonZ0 + e * 1.2f && z < CanyonZ1 + e * 1.2f)
            return Mathf.Abs(x - CanyonX(z)) < CanyonHalf(z) + e * 0.6f ? 0f : MesaY;
        float dOasis = Vector2.Distance(new Vector2(x, z), Oasis);
        if (dOasis < 7f + e * 0.6f) return -1.2f;
        // bancales de arena a los lados, al sur y detrás del templo (llegan a la altura de las mesetas de afuera)
        float ring = Mathf.Max(Mathf.Abs(x) - 50f, Mathf.Max(z - (TempleZ + 62f), -z - 66f)) + e * 2f;
        if (ring > 13f) return MesaY;
        if (ring > 7f) return 6.4f;
        if (ring > 0f) return 3.2f;
        return 0f;
    }

    /// <summary>Plataforma de sillar (con colisión) y su piso de losas.</summary>
    void Platform(Transform parent, float x0, float x1, float z0, float z1, float h)
    {
        Box(parent, "Plataforma", new Vector3((x0 + x1) / 2f, (h - 1f) / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, h + 1f, z1 - z0), mSandstone, true, 0.25f);
        Flat(parent, "Losas", new Vector3((x0 + x1) / 2f, h + 0.01f, (z0 + z1) / 2f), x1 - x0 - 0.4f, z1 - z0 - 0.4f, mStoneBrick, 0.25f);
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
        blocked.Add(new Rect(pos.x - 1.5f, pos.z - 1.5f, 3f, 3f));
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

    void Obelisk(Transform parent, Vector3 pos, float h)
    {
        var ob = Group("Obelisco", parent);
        ob.localPosition = pos;
        Box(ob, "Base", new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 2.2f), mSandstone, true, 0.25f);
        var shaft = MeshObject(ob, "Aguja", Cone(1f, 1f, 4), mSandstone, true);
        shaft.transform.localPosition = new Vector3(0f, 1f, 0f);
        shaft.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        shaft.transform.localScale = new Vector3(1.1f, h, 1.1f);
        Box(ob, "Runa", new Vector3(0f, h * 0.36f, -0.62f), new Vector3(0.35f, 1.4f, 0.02f), mGlow, false);
        blocked.Add(new Rect(pos.x - 1.6f, pos.z - 1.6f, 3.2f, 3.2f));
    }

    /// <summary>Camello echado (bloques), para la caravana.</summary>
    void Camel(Transform parent, Vector3 pos, float yaw)
    {
        var c = Group("Camello", parent);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var fur = mDoor;
        Box(c, "Cuerpo", new Vector3(0f, 0.7f, 0f), new Vector3(1.1f, 0.9f, 2.4f), fur, true);
        Box(c, "Joroba", new Vector3(0f, 1.3f, 0.1f), new Vector3(0.8f, 0.5f, 0.9f), fur, false);
        Box(c, "Cuello", new Vector3(0f, 1.2f, -1.3f), new Vector3(0.4f, 1f, 0.4f), fur, false).transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
        Box(c, "Cabeza", new Vector3(0f, 1.75f, -1.75f), new Vector3(0.4f, 0.35f, 0.7f), fur, false);
        Box(c, "Manta", new Vector3(0f, 1.18f, 0.1f), new Vector3(1.2f, 0.12f, 1.2f), mClothB, false);
        blocked.Add(new Rect(pos.x - 2f, pos.z - 2f, 4f, 4f));
    }

    void BuildTemple()
    {
        BuildTerrain();
        var g = Group("Desierto", root);
        var lvl = Group("Nivel", root);

        // ------------------------------------------------ 1. las dunas y la caravana
        var camp = new Vector3(-22f, 0f, -48f);
        Tent(g, camp + new Vector3(0f, 0f, 4f), 20f, mCloth);
        Tent(g, camp + new Vector3(-8f, 0f, -1f), -40f, mClothB);
        Campfire(g, camp + new Vector3(4f, 0f, -2f));
        Camel(g, camp + new Vector3(10f, 0f, 6f), 70f);
        Camel(g, camp + new Vector3(-12f, 0f, 8f), 200f);
        Clutter(g, camp + new Vector3(6f, 0f, 3f));
        for (int i = 0, tries = 0; i < 34 && tries < 900; tries++)
        {
            var q = new Vector2(R(-54f, 54f), R(-64f, -28f));
            if (!Free(q, 3f) || Mathf.Abs(q.x) < 8f) continue;
            var d = Prim(g, PrimitiveType.Sphere, "Duna", new Vector3(q.x, -R(1.2f, 2.2f), q.y), new Vector3(R(9f, 16f), R(3.5f, 5f), R(6f, 11f)), mSandPix);
            d.transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), 0f);
            blocked.Add(new Rect(q.x - 6f, q.y - 6f, 12f, 12f));
            i++;
        }
        for (float z = -60f; z < CanyonZ0; z += 9f)
            foreach (float sx in new[] { -1f, 1f }) Brazier(g, new Vector3(sx * 4.2f, 0f, z));

        // ------------------------------------------------ 2. el desfiladero
        for (float z = CanyonZ0 + 4f; z < CanyonZ1 - 2f; z += 9f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float hw = CanyonHalf(z) - 1.4f;
                Brazier(g, new Vector3(CanyonX(z) + sx * hw, 0f, z));
            }
        // runas y nichos tallados en las paredes del desfiladero
        for (float z = CanyonZ0 + 6f; z < CanyonZ1 - 4f; z += 7f)
        {
            float sx = (Mathf.RoundToInt(z) / 7) % 2 == 0 ? 1f : -1f;
            float wx = CanyonX(z) + sx * (CanyonHalf(z) + 0.4f);
            var n = Box(g, "Nicho", new Vector3(wx - sx * 0.25f, 2.6f, z), new Vector3(0.5f, 2.6f, 1.6f), mStoneBrick, false);
            Box(g, "Runa", new Vector3(wx - sx * 0.55f, 2.8f, z), new Vector3(0.05f, 1.1f, 0.35f), mGlow, false);
        }
        // fragmentos de ruina en la cámara del desfiladero
        for (int i = 0; i < 6; i++)
        {
            float z = R(10f, 24f), x = CanyonX(z) + R(-9f, 9f);
            var b = Box(g, "Sillar", new Vector3(x, 0.4f, z), new Vector3(R(1f, 2.2f), R(0.8f, 1.4f), R(1f, 1.8f)), mSandstone, true, 0.25f);
            b.transform.localRotation = Quaternion.Euler(R(-10f, 10f), R(0f, 90f), R(-10f, 10f));
        }

        // ------------------------------------------------ 3. el pueblo del oasis
        var oa = Group("Oasis", g);
        var pool = Flat(oa, "Agua", new Vector3(Oasis.x, -0.45f, Oasis.y), 17f, 17f, mRiver, 0.15f);
        pool.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 11; i++)
        {
            float a = i / 11f * Mathf.PI * 2f + R(-0.2f, 0.2f);
            Palm(oa, new Vector3(Oasis.x + Mathf.Cos(a) * R(8.5f, 11f), 0f, Oasis.y + Mathf.Sin(a) * R(8.5f, 11f)), R(1f, 1.35f));
        }
        blocked.Add(new Rect(Oasis.x - 13f, Oasis.y - 13f, 26f, 26f));
        var ring = new List<Vector3>();
        for (int k = 0; k <= 32; k++) { float a = k / 32f * Mathf.PI * 2f; ring.Add(new Vector3(Oasis.x + Mathf.Cos(a) * 7.6f, 0f, Oasis.y + Mathf.Sin(a) * 7.6f)); }
        BankGuard(lvl, ring, p => false);
        // casas de adobe y mercado
        foreach (var hp in new[] { new Vector4(32f, 58f, -90f, 7f), new Vector4(34f, 80f, -90f, 6f), new Vector4(30f, 102f, -90f, 7f), new Vector4(-42f, 96f, 90f, 6f), new Vector4(-44f, 52f, 90f, 6.5f) })
            House(g, new Vector3(hp.x, 0f, hp.y), hp.z, hp.w, 5f, 3.6f, false);
        Stall(g, new Vector3(12f, 0f, 64f), -90f);
        Stall(g, new Vector3(14f, 0f, 76f), -90f);
        Stall(g, new Vector3(-10f, 0f, 96f), 90f);
        Well(g, new Vector3(8f, 0f, 88f));
        Clutter(g, new Vector3(22f, 0f, 70f));
        Clutter(g, new Vector3(-30f, 0f, 94f));
        MerchantPost(lvl, new Vector3(-8f, 0f, 70f), 120f);
        HealShrine(lvl, new Vector3(-6f, 0f, 84f));
        Campfire(lvl, new Vector3(-12f, 0f, 78f));
        foreach (var lp in new[] { new Vector3(4f, 0f, 50f), new Vector3(-4f, 0f, 62f), new Vector3(4f, 0f, 92f), new Vector3(-4f, 0f, 108f) }) Brazier(lvl, lp);

        // ------------------------------------------------ 4. la avenida de los obeliscos
        for (float z = 124f; z < TempleZ - 6f; z += 14f)
            foreach (float sx in new[] { -1f, 1f })
            {
                Obelisk(g, new Vector3(sx * 8f, 0f, z), z > 165f ? 11f : 9f);
                Brazier(g, new Vector3(sx * 5f, 0f, z + 7f));
            }
        Colossus(g, new Vector3(-22f, 0f, 150f), 90f);
        Colossus(g, new Vector3(22f, 0f, 150f), -90f);
        blocked.Add(new Rect(-25f, 146f, 6f, 8f)); blocked.Add(new Rect(19f, 146f, 6f, 8f));
        for (int i = 0; i < 12; i++)
        {
            var q = new Vector2(R(-48f, 48f), R(118f, 186f));
            if (Mathf.Abs(q.x) < 12f || !Free(q, 2f)) continue;
            if (rng.NextDouble() < 0.5)
            {
                var b = Box(g, "Sillar", new Vector3(q.x, 0.3f, q.y), new Vector3(R(1f, 2.6f), R(0.8f, 1.6f), R(1f, 2f)), mSandstone, true, 0.25f);
                b.transform.localRotation = Quaternion.Euler(R(-12f, 12f), R(0f, 90f), R(-12f, 12f));
                blocked.Add(new Rect(q.x - 2f, q.y - 2f, 4f, 4f));
            }
            else TempleColumn(g, new Vector3(q.x, 0f, q.y), R(3f, 5f), true);
        }

        // ------------------------------------------------ 5. el templo
        var tg = Group("Templo", g);
        tg.localPosition = new Vector3(0f, 0f, TempleZ);
        var tl = Group("NivelTemplo", lvl);
        tl.localPosition = new Vector3(0f, 0f, TempleZ);
        Platform(tg, -20f, 20f, 0f, 16f, 1.2f);
        Platform(tg, -16f, 16f, 16f, 32f, 2.8f);
        Platform(tg, -12f, 12f, 32f, 46f, 4.6f);
        Stairs(tl, "Escalinata0", new Vector3(0f, 0f, -3.8f), new Vector3(0f, 1.2f, 0.3f), 7f, mSandstone);
        Stairs(tl, "Escalinata1", new Vector3(0f, 1.2f, 11.4f), new Vector3(0f, 2.8f, 16.3f), 7f, mSandstone);
        Stairs(tl, "Escalinata2", new Vector3(0f, 2.8f, 27.4f), new Vector3(0f, 4.6f, 32.3f), 7f, mSandstone);
        for (float z = 2.5f; z < 15f; z += 3.6f)
            foreach (float sx in new[] { -1f, 1f }) TempleColumn(tg, new Vector3(sx * 18f, 1.2f, z), 6f, rng.NextDouble() < 0.25);
        for (float z = 18.5f; z < 31f; z += 3.6f)
            foreach (float sx in new[] { -1f, 1f }) TempleColumn(tg, new Vector3(sx * 14.2f, 2.8f, z), 6.5f, rng.NextDouble() < 0.2);
        Colossus(tg, new Vector3(-8.5f, 4.6f, 42.5f), 0f);
        Colossus(tg, new Vector3(8.5f, 4.6f, 42.5f), 0f);
        var gate = Group("Portada", tg);
        gate.localPosition = new Vector3(0f, 4.6f, 45.4f);
        Box(gate, "Muro", new Vector3(0f, 4f, 0f), new Vector3(10f, 8f, 1.2f), mSandstone, true, 0.25f);
        Box(gate, "Vano", new Vector3(0f, 2.4f, -0.62f), new Vector3(3f, 4.8f, 0.05f), mGlow, false);
        Box(gate, "Remate", new Vector3(0f, 8.4f, 0f), new Vector3(11f, 0.8f, 1.6f), mSandstone, false, 0.25f);
        AddLight(gate, new Vector3(0f, 2.5f, -2f), new Color(1f, 0.7f, 0.35f), 12f, 4f);
        var altar = Group("Altar", tg);
        altar.localPosition = new Vector3(0f, 4.6f, 40.5f);
        Box(altar, "Mesa", new Vector3(0f, 0.6f, 0f), new Vector3(3f, 1.2f, 1.6f), mSandstone, true, 0.25f);
        var disk = Prim(altar, PrimitiveType.Cylinder, "Disco", new Vector3(0f, 2.2f, 0.3f), new Vector3(1.6f, 0.06f, 1.6f), mGlow);
        disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        foreach (var p in new[] { new Vector3(-17f, 1.2f, 1.5f), new Vector3(17f, 1.2f, 1.5f), new Vector3(-13.5f, 2.8f, 17.5f), new Vector3(13.5f, 2.8f, 17.5f), new Vector3(-10f, 4.6f, 33.5f), new Vector3(10f, 4.6f, 33.5f) })
            Brazier(tg, p);
        blocked.Add(new Rect(-22f, TempleZ - 6f, 44f, 54f));

        // sendero
        var pts = new List<Vector2> { new Vector2(0f, -62f), new Vector2(0f, CanyonZ0 - 4f) };
        for (float z = CanyonZ0; z <= CanyonZ1 + 0.1f; z += 4f) pts.Add(new Vector2(CanyonX(z), z));
        pts.Add(new Vector2(-4f, 56f)); pts.Add(new Vector2(2f, 80f)); pts.Add(new Vector2(0f, 110f)); pts.Add(new Vector2(0f, TempleZ - 5f));
        Trail(g, 4.2f, pts.ToArray());

        // combate
        Arena(lvl, "Las dunas", new Vector3(14f, 0f, -40f), new Vector2(22f, 14f), 5);
        Arena(lvl, "El desfiladero", new Vector3(CanyonX(17f), 0f, 17f), new Vector2(22f, 16f), 5);
        Arena(lvl, "La avenida de los obeliscos", new Vector3(0f, 0f, 150f), new Vector2(26f, 16f), 6);
        Arena(lvl, "El patio exterior", new Vector3(0f, 1.2f, TempleZ + 7f), new Vector2(34f, 11f), 6);
        Arena(lvl, "El patio de las columnas", new Vector3(0f, 2.8f, TempleZ + 23f), new Vector2(26f, 10f), 6);
        var boss = Arena(lvl, "Jefe", new Vector3(0f, 4.6f, TempleZ + 37.5f), new Vector2(20f, 8f), 1, true);
        Exit(lvl, new Vector3(0f, 4.6f, TempleZ + 43.4f), boss);
        Chest(lvl, new Vector3(-17.5f, 2.8f, TempleZ + 30f), 30, 90f);
        Chest(lvl, new Vector3(Oasis.x - 11f, 0f, Oasis.y - 6f), 26, 30f);
        Chest(lvl, new Vector3(CanyonX(20f) + 10f, 0f, 20f), 22, -90f);
        Note(lvl, new Vector3(6f, 0f, 120f), 180f, LoreNote.Kind.Inscripcion, "dio5_camino", "Al pie del camino",
             "Cada viajero que sube enciende un brasero en este camino.\nLos que se rinden lo apagan al bajar.\nCuenta los que siguen encendidos: son los que todavía no se han rendido.");
        Note(lvl, new Vector3(-12f, 0f, 88f), 160f, LoreNote.Kind.Diario, "dio5_diario", "Noche en el desierto",
             "En una piedra del templo encontré grabado \"sigue subiendo\", con mi letra.\nYa estuve aquí antes. El Guardián Dorado se defiende con escudo: espera a que ataque y pégale por la espalda.", "— A.");

        WalkBoundary(TempleWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, MesaY, true, true, true, true, 0);
        EnsurePlantMeshes();
        for (int i = 0; i < 22; i++)
        {
            float a = i / 22f * Mathf.PI * 2f;
            var m = MeshObject(bg, "Meseta", blobMeshes[rng.Next(blobMeshes.Length)], mSandstone, false);
            m.transform.localPosition = new Vector3(Mathf.Cos(a) * R(320f, 420f), MesaY - 2f, 100f + Mathf.Sin(a) * R(360f, 460f));
            m.transform.localScale = new Vector3(R(60f, 120f), R(30f, 60f), R(60f, 120f));
        }

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 30f, 140f, 34f, 4f, new Vector3(0f, 0f, 60f), 2.6f, 10, intro: 5f);
        Shot(shots, "Caravana", camp + new Vector3(6f, 0f, -4f), new Vector2(8f, 8f), 200f, 16f, 22f, 38f, 6f, new Vector3(-2f, 0f, -4f), 2f, 2,
             "LA CARAVANA", "", still: true);
        Shot(shots, "Desfiladero", new Vector3(CanyonX(2f), 0f, 2f), new Vector2(8f, 6f), 0f, 34f, 30f, 36f, 2f, new Vector3(0f, 0f, 10f), 2f, 2,
             "EL DESFILADERO", "");
        Shot(shots, "Oasis", new Vector3(Oasis.x + 10f, 0f, Oasis.y), new Vector2(8f, 10f), 240f, 18f, 22f, 38f, 6f, new Vector3(-6f, 0f, -2f), 2f, 2,
             "EL PUEBLO DEL OASIS", "", still: true);
        Shot(shots, "Llegada", new Vector3(0f, 0f, 168f), new Vector2(10f, 8f), 0f, 10f, 30f, 38f, 12f, new Vector3(0f, 2f, 16f), 2f, 2,
             "EL TEMPLO DE ARENA", "Bajo las estrellas, la puerta sigue abierta");
        FinishWorld(false, false);
    }
}
