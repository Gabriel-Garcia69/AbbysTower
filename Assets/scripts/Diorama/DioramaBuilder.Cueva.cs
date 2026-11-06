using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 5 — La Gruta de Cristal. Una caverna enorme inundada: un lago turquesa con islas de bloques unidas por puentes. En la
/// primera isla, el pueblo minero de donde salían los minerales; en la tercera, la torre del pozo; al fondo, el Corazón de Geoda.
/// Cerrada por todos lados con paredes altas y un techo de bloques con cristales colgando, para verse bien desde cualquier ángulo.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect CaveArea = new Rect(-80f, -104f, 160f, 244f);
    static readonly Rect CaveWalk = new Rect(-52f, -72f, 104f, 196f);
    static readonly Vector3 CaveSpawn = new Vector3(0f, 3.6f, -50f);
    const float CaveLakeY = -1.2f, CaveIslaY = 0.6f, CaveEntradaY = 3.5f, CaveParedY = 40f;
    // islas: x, z, radio en x, radio en z
    static readonly Vector4[] CaveIslas = { new Vector4(-4f, -6f, 22f, 18f), new Vector4(22f, 30f, 14f, 12f), new Vector4(-16f, 62f, 17f, 12f), new Vector4(0f, 104f, 22f, 17f) };
    static readonly Vector2[][] CavePuentes =
    {
        new[] { new Vector2(6f, 7f), new Vector2(15f, 20f) },
        new[] { new Vector2(12f, 38f), new Vector2(-6f, 54f) },
        new[] { new Vector2(-11f, 71f), new Vector2(-5f, 90f) },
    };

    float CaveHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        // la caverna se cierra por los cuatro lados
        if (Mathf.Abs(x) > 62f + e * 2.5f || z > 128f + e * 2.5f || z < -90f + e * 2.5f) return CaveParedY;
        // la explanada de la entrada y su escalinata
        if (Mathf.Abs(x) < 4.2f && z > -33f && z < -20f) return CaveIslaY;
        if (Mathf.Abs(x) < 20f + e * 1.5f && z <= -33f && z > -74f + e * 1.5f) return CaveEntradaY;
        foreach (var i in CaveIslas)
        {
            float dx = (x - i.x) / i.z, dz = (z - i.y) / i.w, r = Mathf.Sqrt(dx * dx + dz * dz) + e * 0.06f;
            if (r < 1f) return CaveIslaY;
            if (r < 1.12f) return -0.8f;   // la orilla, bajo el agua
        }
        return -4f;
    }

    Vector3 CaveEn(float x, float z) => new Vector3(x, CaveHeight(x, z), z);

    void BuildCave()
    {
        VolumenExpedicion();
        MaterialesExpedicion();
        BuildTerrain();
        var g = Group("Gruta", root);
        var lvl = Group("Nivel", root);
        var agua = AguaDe("Dio_AguaCristal", mWater, new Color(0.08f, 0.72f, 0.78f, 0.7f), new Color(0.02f, 0.12f, 0.3f, 0.97f), 3f, 0.95f, 0.02f, 0.55f, 0.4f, 0.22f, 0.18f, new Color(0.5f, 0.95f, 1f));
        var water = Flat(g, "Lago", new Vector3(0f, CaveLakeY, 18f), 160f, 244f, agua, 0.08f);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        foreach (var c in new[] { new Vector2(0f, -55f), new Vector2(-30f, -8f), new Vector2(28f, 0f), new Vector2(30f, 44f), new Vector2(-30f, 50f), new Vector2(0f, 80f), new Vector2(0f, 110f) })
            AddLight(g, new Vector3(c.x, 26f, c.y), Mathf.Abs(c.x) < 1f ? new Color(0.45f, 0.75f, 1f) : new Color(0.4f, 0.92f, 0.86f), 60f, 900f);

        TechoDeCristal(g);
        Stairs(lvl, "Bajada", new Vector3(0f, CaveIslaY, -22.5f), new Vector3(0f, CaveEntradaY, -32f), 7f, mStone);
        Entrada(g, lvl);
        PuentesGruta(g, lvl);
        PuebloMinero(g, lvl);
        Bosque(g, lvl);
        TorreDelPozo(g, lvl);
        Corazon(g, lvl);
        LagoCristales(g);

        Arena(lvl, "El pueblo minero", CaveEn(-4f, -8f), new Vector2(16f, 10f), 5);
        Arena(lvl, "El bosque de cristal", CaveEn(22f, 30f), new Vector2(14f, 11f), 5);
        Arena(lvl, "La torre del pozo", CaveEn(-12f, 60f), new Vector2(14f, 10f), 4);
        var boss = Arena(lvl, "Jefe", CaveEn(0f, 98f), new Vector2(16f, 12f), 1, true);
        Exit(lvl, CaveEn(0f, 110f), boss);
        Chest(lvl, CaveEn(30f, 34f), 30, -90f);
        Chest(lvl, CaveEn(-26f, 66f), 24, 90f);
        Note(lvl, new Vector3(-8f, CaveEntradaY, -50f), 90f, LoreNote.Kind.Diario, "dio4_diario", "Diario de la expedición — día 9",
             "El piso 5 es una gruta llena de cristales. Brillan con la misma energía que usamos en las armas.\nAquí los minerales salen más grandes. Con lo de este piso pagaríamos todas las deudas, Abby. Todas.", "— " + Historia.Hermano);
        Note(lvl, CaveEn(-8f, 104f), -90f, LoreNote.Kind.Inscripcion, "dio4_geoda", "Grabado en la piedra",
             "Al fondo de la gruta, una piedra que late como un corazón.\nLos canalizadores dicen que la energía de la torre sale de aquí, o pasa por aquí. Nadie lo ha confirmado.");

        WalkBoundary(CaveWalk);
        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 12f, 25f, 54f, 42f, 3f, new Vector3(0f, 0f, 20f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Pueblo", CaveEn(-10f, -2f), new Vector2(12f, 8f), -20f, 16f, 22f, 40f, 6f, new Vector3(-2f, 1f, 3f), 2.2f, 2,
             "EL PUEBLO MINERO", "Aquí nadie apagó las luces", still: true);
        Shot(shots, "Corazon", CaveEn(0f, 100f), new Vector2(14f, 8f), 0f, 12f, 18f, 44f, 8f, new Vector3(0f, 1.5f, 4f), 2.2f, 3,
             "EL CORAZÓN DE GEODA", "", still: true);
        FinishWorld(false, false);
    }

    /// <summary>El techo: bloques a distintas alturas, sin curvas, con cristales colgando que brillan como un cielo.</summary>
    void TechoDeCristal(Transform g)
    {
        const float cel = 7f, x0 = -84f, z0 = -108f;
        int nx = 24, nz = 36;
        float[,] alto = new float[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                alto[i, j] = 32f + Mathf.Round(Noise(i * 0.42f + 3f, j * 0.42f) * 5f) * 3f;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        void Cara(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nor, float u, float h)
        {
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int q = 0; q < 4; q++) n.Add(nor);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, h)); uv.Add(new Vector2(0f, h));
            t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float a = alto[i, j], xa = x0 + i * cel, xb = xa + cel, za = z0 + j * cel, zb = za + cel;
                Cara(new Vector3(xa, a, za), new Vector3(xb, a, za), new Vector3(xb, a, zb), new Vector3(xa, a, zb), Vector3.down, 1.75f, 1.75f);
                // costados hacia el vecino más alto (mirando hacia abajo se ve el escalón)
                if (i + 1 < nx && alto[i + 1, j] > a) Cara(new Vector3(xb, a, za), new Vector3(xb, alto[i + 1, j], za), new Vector3(xb, alto[i + 1, j], zb), new Vector3(xb, a, zb), Vector3.left, 1.75f, (alto[i + 1, j] - a) / 4f);
                if (i > 0 && alto[i - 1, j] > a) Cara(new Vector3(xa, a, zb), new Vector3(xa, alto[i - 1, j], zb), new Vector3(xa, alto[i - 1, j], za), new Vector3(xa, a, za), Vector3.right, 1.75f, (alto[i - 1, j] - a) / 4f);
                if (j + 1 < nz && alto[i, j + 1] > a) Cara(new Vector3(xb, a, zb), new Vector3(xb, alto[i, j + 1], zb), new Vector3(xa, alto[i, j + 1], zb), new Vector3(xa, a, zb), Vector3.back, 1.75f, (alto[i, j + 1] - a) / 4f);
                if (j > 0 && alto[i, j - 1] > a) Cara(new Vector3(xa, a, za), new Vector3(xa, alto[i, j - 1], za), new Vector3(xb, alto[i, j - 1], za), new Vector3(xb, a, za), Vector3.forward, 1.75f, (alto[i, j - 1] - a) / 4f);
            }
        var m = new Mesh { name = "Techo", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateTangents(); m.RecalculateBounds();
        var techo = MeshObject(g, "Techo", m, mCaveRock, false);
        techo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var colgantes = Group("CristalesTecho", g);
        for (int k = 0; k < 90; k++)
        {
            int i = rng.Next(1, nx - 1), j = rng.Next(1, nz - 1);
            var c = Group("Colgante", colgantes);
            c.localPosition = new Vector3(x0 + (i + R(0.1f, 0.9f)) * cel, alto[i, j] + 0.3f, z0 + (j + R(0.1f, 0.9f)) * cel);
            c.localRotation = Quaternion.Euler(180f + R(-14f, 14f), R(0f, 360f), 0f);
            Crystals(c, Vector3.zero, R(1.4f, 3.6f), k % 2 == 0, false);
        }
    }

    void Entrada(Transform g, Transform lvl)
    {
        // la puerta de la mina en la pared del sur, por donde se llega, con su campamento
        var p = Group("PuertaMina", g);
        p.localPosition = new Vector3(0f, CaveEntradaY, -74f);
        foreach (float sx in new[] { -4f, 4f }) Box(p, "Poste", new Vector3(sx, 3.2f, 0f), new Vector3(0.8f, 6.4f, 0.8f), mTimber, true);
        Box(p, "Dintel", new Vector3(0f, 6.6f, 0f), new Vector3(10f, 0.8f, 0.9f), mTimber, false);
        Box(p, "Tablas", new Vector3(0f, 3.1f, -0.4f), new Vector3(7.4f, 6.2f, 0.3f), mDoor, false);
        Tent(lvl, new Vector3(-11f, CaveEntradaY, -58f), 40f, mClothB);
        Campfire(lvl, new Vector3(9f, CaveEntradaY, -56f));
        HealShrine(lvl, new Vector3(10f, CaveEntradaY, -44f));
        foreach (var q in new[] { new Vector2(-6f, -36f), new Vector2(6f, -36f), new Vector2(-8f, -64f), new Vector2(8f, -64f) })
            PortLamp(lvl, new Vector3(q.x, CaveEntradaY, q.y));
        AnotarPlaza(mCobble, new Vector2(0f, -52f), new Vector2(14f, 38f));
        blocked.Add(new Rect(-7f, -72f, 14f, 40f));
    }

    void PuentesGruta(Transform g, Transform lvl)
    {
        foreach (var b in CavePuentes)
        {
            RopeBridge(lvl, "Puente", CaveEn(b[0].x, b[0].y), CaveEn(b[1].x, b[1].y), 5.5f);
            Vector2 d = (b[1] - b[0]).normalized, l = new Vector2(-d.y, d.x);
            foreach (var extremo in new[] { b[0] - d, b[1] + d })
                foreach (float s in new[] { -3.4f, 3.4f })
                {
                    var q = extremo + l * s;
                    PortLamp(lvl, CaveEn(q.x, q.y));
                }
        }
        // caminos de piedra que unen los puentes en cada isla
        AnotarTramo(mCobble, new Vector2(0f, -22f), CavePuentes[0][0], 5f);
        AnotarTramo(mCobble, CavePuentes[0][1], new Vector2(20f, 30f), 5f);
        AnotarTramo(mCobble, new Vector2(20f, 30f), CavePuentes[1][0], 5f);
        AnotarTramo(mCobble, CavePuentes[1][1], new Vector2(-13f, 62f), 5f);
        AnotarTramo(mCobble, new Vector2(-13f, 62f), CavePuentes[2][0], 5f);
        AnotarTramo(mCobble, CavePuentes[2][1], new Vector2(0f, 108f), 5f);
        // barrera a lo largo de la orilla, menos en puentes y escalinata
        var o = Group("Limites", lvl);
        const float c = 2.5f;
        for (float x = CaveWalk.xMin; x < CaveWalk.xMax; x += c)
            for (float z = CaveWalk.yMin; z < CaveWalk.yMax; z += c)
            {
                if (CaveHeight(x, z) > 0f) continue;
                if (CaveHeight(x + c, z) <= 0f && CaveHeight(x - c, z) <= 0f && CaveHeight(x, z + c) <= 0f && CaveHeight(x, z - c) <= 0f) continue;
                var p = new Vector2(x, z);
                bool puente = false;
                foreach (var b in CavePuentes) if (DistSeg(p, b[0], b[1], out _) < 4f) puente = true;
                if (puente) continue;
                SolidBox(o, "Limite_Orilla", new Vector3(x, 3f, z), new Vector3(c, 20f, c));
            }
    }

    void PuebloMinero(Transform g, Transform lvl)
    {
        var v = Group("Pueblo", g);
        // casas de cara a la plaza, con las ventanas prendidas
        House(v, CaveEn(-20f, -10f), 90f, 7f, 5.5f, 3.6f, true);
        House(v, CaveEn(-18f, 2f), 110f, 6f, 5f, 3.4f, true);
        House(v, CaveEn(13f, -13f), -90f, 6.5f, 5f, 3.6f, true);
        House(v, CaveEn(-6f, 8f), 180f, 6f, 5f, 3.4f, false);
        AnotarPlaza(mCobble, new Vector2(-4f, -8f), new Vector2(20f, 14f));
        Stall(v, CaveEn(-10f, -15f), 20f);
        Stall(v, CaveEn(4f, -17f), -15f);
        MerchantPost(lvl, CaveEn(9f, -4f), -60f);
        HealShrine(lvl, CaveEn(6f, -18f));
        Campfire(lvl, CaveEn(-11f, -5f));
        foreach (var q in new[] { new Vector2(-14f, -4f), new Vector2(7f, 2f), new Vector2(-12f, -16f), new Vector2(11f, -9f) }) PortLamp(lvl, CaveEn(q.x, q.y));
        foreach (var q in new[] { new Vector2(-15f, 8f), new Vector2(14f, -3f), new Vector2(-22f, -3f) }) Clutter(v, CaveEn(q.x, q.y));
        Carrito(v, CaveEn(0f, 3f), 30f);
        Carrito(v, CaveEn(-16f, -14f), -40f);
        Rieles(v, new Vector2(0f, 3f), new Vector2(-16f, -14f));
        blocked.Add(new Rect(-24f, -22f, 40f, 34f));
    }

    void Rieles(Transform m, Vector2 a, Vector2 b)
    {
        float largo = Vector2.Distance(a, b), yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
        var rot = Quaternion.Euler(0f, yaw, 0f);
        for (float s = 0f; s < largo; s += 1.2f)
        {
            var p = Vector2.Lerp(a, b, s / largo);
            var q = CaveEn(p.x, p.y);
            Box(m, "Durmiente", q + Vector3.up * 0.06f, new Vector3(1.8f, 0.1f, 0.34f), mTimber, false).transform.localRotation = rot;
            foreach (float sx in new[] { -0.62f, 0.62f })
                Box(m, "Riel", q + rot * new Vector3(sx, 0.16f, 0f), new Vector3(0.1f, 0.1f, 1.26f), mHierro, false).transform.localRotation = rot;
        }
    }

    void Carrito(Transform m, Vector3 q, float yaw)
    {
        var car = Group("Carrito", m);
        car.localPosition = q;
        car.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(car, "Caja", new Vector3(0f, 0.75f, 0f), new Vector3(1.5f, 0.9f, 2.2f), mTimber, true);
        foreach (float sx in new[] { -0.8f, 0.8f })
            foreach (float sz in new[] { -0.7f, 0.7f })
                Prim(car, PrimitiveType.Cylinder, "Rueda", new Vector3(sx, 0.28f, sz), new Vector3(0.5f, 0.05f, 0.5f), mHierro).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Crystals(car, new Vector3(0f, 1.1f, 0f), 1.1f, rng.NextDouble() < 0.5, true);
    }

    void Bosque(Transform g, Transform lvl)
    {
        // la segunda isla: cristales grandes alrededor, libres en medio para pelear
        var i = CaveIslas[1];
        for (int k = 0; k < 13; k++)
        {
            float a = k / 13f * Mathf.PI * 2f + R(-0.15f, 0.15f);
            var q = CaveEn(i.x + Mathf.Cos(a) * i.z * R(0.72f, 0.9f), i.y + Mathf.Sin(a) * i.w * R(0.72f, 0.9f));
            bool enPuente = false;
            foreach (var b in CavePuentes) if (DistSeg(new Vector2(q.x, q.z), b[0], b[1], out _) < 5f) enPuente = true;
            if (enPuente || q.y < 0f) continue;
            Crystals(g, q, R(3f, 5f), k % 2 == 0, k % 3 == 0);
        }
        blocked.Add(new Rect(i.x - i.z, i.y - i.w, i.z * 2f, i.w * 2f));
    }

    void TorreDelPozo(Transform g, Transform lvl)
    {
        // la tercera isla: la torre de madera del pozo de la mina, con su polea y el pozo que brilla
        var t = Group("TorrePozo", g);
        t.localPosition = CaveEn(-25f, 65f);
        foreach (float sx in new[] { -3.2f, 3.2f })
            foreach (float sz in new[] { -3.2f, 3.2f })
            {
                Box(t, "Pata", new Vector3(sx, 8f, sz), new Vector3(0.6f, 16f, 0.6f), mTimber, true);
            }
        foreach (float y in new[] { 4f, 8f, 12f })
        {
            foreach (float sz in new[] { -3.2f, 3.2f }) Box(t, "Travesano", new Vector3(0f, y, sz), new Vector3(7f, 0.4f, 0.4f), mTimber, false);
            foreach (float sx in new[] { -3.2f, 3.2f }) Box(t, "Travesano", new Vector3(sx, y, 0f), new Vector3(0.4f, 0.4f, 7f), mTimber, false);
        }
        Box(t, "Plataforma", new Vector3(0f, 16.2f, 0f), new Vector3(8.4f, 0.5f, 8.4f), mPlank, false);
        var polea = MeshObject(t, "Polea", Ring(2f, 1.7f, 0.4f, 20), mHierro, false);
        polea.transform.localPosition = new Vector3(0f, 18.6f, 0f);
        polea.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        foreach (float sx in new[] { -1.4f, 1.4f }) Box(t, "Soporte", new Vector3(sx, 17.5f, 0f), new Vector3(0.35f, 2.2f, 0.35f), mTimber, false);
        Box(t, "Cable", new Vector3(0f, 9f, 0f), new Vector3(0.08f, 16f, 0.08f), mHierro, false);
        Prim(t, PrimitiveType.Cylinder, "Pozo", new Vector3(0f, 0.08f, 0f), new Vector3(5.2f, 0.04f, 5.2f), Glow("Dio_PozoMina", new Color(0.3f, 0.95f, 1f), 2.6f));
        foreach (float sx in new[] { -2.6f, 2.6f })
            foreach (float sz in new[] { -2.6f, 2.6f }) Box(t, "Brocal", new Vector3(sx * 0.7f, 0.4f, sz), new Vector3(sx == 0f ? 1f : 4f, 0.8f, 0.5f), mStone, true);
        foreach (var y in new[] { 6f, 14f })
        {
            var l = AddLight(t, new Vector3(4f, y, 0f), new Color(1f, 0.64f, 0.32f), 18f, 40f);
            Prim(t, PrimitiveType.Sphere, "Farol", new Vector3(3.8f, y, 0f), Vector3.one * 0.5f, Glow("Dio_FarolMina", new Color(1f, 0.62f, 0.26f), 3f));
        }
        AddLight(t, new Vector3(0f, 2f, 0f), new Color(0.3f, 0.95f, 1f), 16f, 60f);
        House(g, CaveEn(-6f, 68f), -110f, 6f, 5f, 3.4f, true);
        Carrito(g, CaveEn(-20f, 56f), 60f);
        Rieles(g, new Vector2(-20f, 56f), new Vector2(-25f, 61f));
        blocked.Add(new Rect(-33f, 48f, 36f, 26f));
    }

    void Corazon(Transform g, Transform lvl)
    {
        // la última isla: la geoda enorme, con un círculo de cristales que la rodea
        var c = Group("Corazon", g);
        c.localPosition = CaveEn(0f, 113f);
        Crystals(c, Vector3.zero, 9f, true, true);
        Crystals(c, new Vector3(-4f, 0f, -1f), 5.5f, false, true);
        Crystals(c, new Vector3(4f, 0f, -1f), 5.5f, true, false);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            if (Mathf.Sin(a) < -0.7f) continue;   // hueco para entrar a la sala
            Crystals(c, new Vector3(Mathf.Cos(a) * 19f, 0f, Mathf.Sin(a) * 6f - 9f), R(2.4f, 4f), i % 2 == 0, i % 3 == 0);
        }
        var lt = AddLight(c, new Vector3(0f, 7f, -2f), new Color(0.8f, 0.5f, 1f), 34f, 160f);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.12f;
        foreach (var q in new[] { new Vector2(-9f, 92f), new Vector2(9f, 92f) }) Brazier(lvl, CaveEn(q.x, q.y));
        blocked.Add(new Rect(-22f, 88f, 44f, 34f));
    }

    void LagoCristales(Transform g)
    {
        // cristales gigantes que salen del agua y cascadas en las paredes: se ven desde cualquier lado
        foreach (var q in new[] { new Vector3(40f, -4f, -18f), new Vector3(-42f, -4f, 24f), new Vector3(46f, -4f, 66f), new Vector3(-44f, -4f, 92f), new Vector3(36f, -4f, 108f), new Vector3(-36f, -4f, -48f), new Vector3(38f, -4f, -56f), new Vector3(-32f, -4f, 34f) })
        {
            Crystals(g, q, R(9f, 14f), q.x < 0f, true);
            AddLight(g, q + Vector3.up * 8f, q.x < 0f ? new Color(0.8f, 0.45f, 1f) : new Color(0.35f, 0.95f, 1f), 34f, 200f);
        }
        foreach (var f in new[] { new Vector4(55f, 34f, -1f, 0f), new Vector4(-55f, 76f, 1f, 0f), new Vector4(14f, 123f, 0f, -1f), new Vector4(-55f, -30f, 1f, 0f) })
        {
            var dir = new Vector3(f.z, 0f, f.w);
            Waterfall(g, new Vector3(f.x, (28f + CaveLakeY) / 2f, f.y), dir, 8f, 28f - CaveLakeY);
            Foam(g, new Vector3(f.x, CaveLakeY + 0.1f, f.y) + dir * 3f, 5f, 40f);
        }
    }
}
