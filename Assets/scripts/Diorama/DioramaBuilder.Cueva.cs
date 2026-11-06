using UnityEngine;

/// <summary>
/// Piso 5 — La Gruta de Cristal. Ya no es un círculo que hay que rodear: una galería larga que avanza siempre hacia adelante, con
/// curvas suaves. Se entra por una boca de roca, se cruza una sala de cristales junto a un estanque con cascada, un puente pasa el
/// río subterráneo y una subida lleva a la última sala, donde late el Corazón de Geoda.
/// La cámara mira a lo largo de la galería, así que el camino siempre queda enfrente.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect CaveArea = new Rect(-78f, -62f, 156f, 202f);
    static readonly Rect CaveWalk = new Rect(-50f, -53.5f, 100f, 180f);
    static readonly Vector3 CaveSpawn = new Vector3(0f, 3.6f, -50f);
    const float CaveLakeY = -1.2f, CaveRioZ = 60f, CaveCorazonZ = 106f, CaveSubida = 2.5f;

    /// <summary>El centro de la galería en cada punto: curvas amplias, y recto en la entrada.</summary>
    static float CaveCx(float z) => 10f * Mathf.Sin(z * 0.03f) * Liso(-34f, -6f, z);
    static float CaveMedia(float z) => Mathf.Lerp(8f, 23f + 5f * Mathf.Sin(z * 0.05f + 1f), Liso(-36f, -20f, z));
    const float CaveEscalaZ0 = 76f, CaveEscalaZ1 = 84f;
    static Vector2 CaveEstanque => new Vector2(CaveCx(32f) - 17f, 32f);

    /// <summary>Alturas por escalones, igual que el piso 1: cada celda del terreno es un bloque.</summary>
    float CaveHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        // la entrada: túnel alto y escalinata de bajada
        if (Mathf.Abs(x) < 3.6f && z > -32f && z < -24f) return 0f;
        if (z <= -32f) return Mathf.Abs(x) < 7f + e * 0.4f && z > -54f ? 3.5f : 5.5f;
        float media = CaveMedia(z), d = Mathf.Abs(x - CaveCx(z)) + e * 2.5f;
        // lo alto de la última sala se sube por la escalinata
        float xe = CaveCx((CaveEscalaZ0 + CaveEscalaZ1) / 2f);
        bool pasillo = Mathf.Abs(x - xe) < 6.4f && z > CaveEscalaZ0 - 1f && z <= CaveEscalaZ1;
        float suelo = z > CaveEscalaZ0 && !pasillo ? CaveSubida : 0f;
        if (z < -24f) return 5.5f;
        if (z > 124f + e * 2f) return suelo + 14f;
        // paredes en bancales: bajan hacia el paso
        if (d > media + 14f) return suelo + 14f;
        if (d > media + 7f) return suelo + 7f;
        if (d > media) return suelo + 3.5f;
        // el estanque del lado oeste y el río que cruza la galería
        Vector2 c = CaveEstanque;
        float dx = (x - c.x) / 13f, dz = (z - c.y) / 11f;
        if (Mathf.Sqrt(dx * dx + dz * dz) + e * 0.07f < 1f) return -4f;
        if (Mathf.Abs(z - CaveRioZ + e * 1.2f) < 4f) return -4f;
        return suelo;
    }

    Vector3 CaveEn(float x, float z) => new Vector3(x, CaveHeight(x, z), z);
    Vector3 CaveCentro(float z, float lado = 0f) { float x = CaveCx(z) + lado; return CaveEn(x, z); }

    void BuildCave()
    {
        VolumenExpedicion();
        MaterialesExpedicion();   // la misma roca natural y los mismos cristales de la cueva del tutorial
        BuildTerrain();
        var g = Group("Gruta", root);
        var lvl = Group("Nivel", root);
        var aguaGruta = AguaDe("Dio_AguaGruta", mWater, new Color(0.22f, 0.2f, 0.56f, 0.62f), new Color(0.015f, 0.01f, 0.07f, 0.97f), 2.2f, 0.95f, 0.015f, 0.55f, 0.4f, 0.22f, 0.18f, new Color(0.62f, 0.55f, 1f));
        var water = Flat(g, "Lago", new Vector3(0f, CaveLakeY, 40f), 160f, 200f, aguaGruta, 0.08f);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float xEscala = CaveCx((CaveEscalaZ0 + CaveEscalaZ1) / 2f);
        // la luz de los cristales del techo, a lo largo de la galería
        for (float z = -24f; z < 124f; z += 24f)
            AddLight(g, new Vector3(CaveCx(z), 24f, z), Mathf.Repeat(z, 48f) < 24f ? new Color(0.42f, 0.72f, 1f) : new Color(0.4f, 0.9f, 0.84f), 56f, 760f);

        Boca(g);
        Mina(g, lvl);
        Stairs(lvl, "Bajada", new Vector3(0f, 0f, -24.5f), new Vector3(0f, 3.5f, -32f), 6f, mExpParedAlt);
        Stairs(lvl, "SubidaFinal", new Vector3(xEscala, 0f, CaveEscalaZ0), new Vector3(xEscala, CaveSubida, CaveEscalaZ1), 12f, mExpParedAlt);
        Rio(g, lvl);
        Estanque(g);
        CristalesYRoca(g);
        Corazon(g);

        // campamento antes del río
        MerchantPost(lvl, CaveCentro(46f, 13f), -90f);
        HealShrine(lvl, CaveCentro(40f, 11f));
        Campfire(lvl, CaveCentro(44f, 8f));
        HealShrine(lvl, CaveCentro(72f, -11f));

        Arena(lvl, "La sala de los cristales", CaveCentro(-6f), new Vector2(16f, 12f), 5);
        Arena(lvl, "La orilla del estanque", CaveCentro(26f, 3f), new Vector2(14f, 11f), 5);
        Arena(lvl, "Después del río", CaveCentro(71f), new Vector2(16f, 8f), 4);
        var boss = Arena(lvl, "Jefe", CaveCentro(CaveCorazonZ - 8f), new Vector2(16f, 12f), 1, true);
        Exit(lvl, CaveCentro(CaveCorazonZ + 8f), boss);
        Chest(lvl, CaveCentro(14f, -16f), 30, 90f);
        Chest(lvl, CaveCentro(90f, 15f), 24, -90f);
        Note(lvl, new Vector3(-4f, 3.5f, -40f), 90f, LoreNote.Kind.Diario, "dio4_diario", "Diario de la expedición — día 9",
             "El piso 5 es una gruta llena de cristales. Brillan con la misma energía que usamos en las armas.\nAquí los minerales salen más grandes. Con lo de este piso pagaríamos todas las deudas, Abby. Todas.", "— " + Historia.Hermano);
        Note(lvl, CaveCentro(CaveCorazonZ - 3f, -7f), -90f, LoreNote.Kind.Inscripcion, "dio4_geoda", "Grabado en la piedra",
             "Al fondo de la gruta, una piedra que late como un corazón.\nLos canalizadores dicen que la energía de la torre sale de aquí, o pasa por aquí. Nadie lo ha confirmado.");

        WalkBoundary(CaveWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 16f, true, false, true, true, 0);
        OuterLand(bg, 5.5f, false, true, false, false, 0);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 34f, 80f, 34f, 0f, new Vector3(0f, 0f, 22f), 2.6f, 10, intro: 5f);
        Shot(shots, "Estanque", new Vector3(CaveCx(30f) - 3f, 0f, 30f), new Vector2(12f, 10f), -38f, 14f, 22f, 42f, 6f, new Vector3(-4f, 1f, 3f), 2.2f, 2,
             "EL ESTANQUE", "Aquí el agua no hace ruido", still: true);
        Shot(shots, "Puente", new Vector3(CaveCx(CaveRioZ), 0f, CaveRioZ), new Vector2(8f, 12f), 30f, 15f, 24f, 42f, 6f, new Vector3(2f, 1f, 3f), 2f, 2,
             "EL RÍO SIN FONDO", "");
        Shot(shots, "Corazon", new Vector3(CaveCx(CaveCorazonZ), CaveSubida, CaveCorazonZ - 6f), new Vector2(14f, 8f), 0f, 12f, 18f, 44f, 8f, new Vector3(0f, 1.5f, 4f), 2.2f, 3,
             "EL CORAZÓN DE GEODA", "", still: true);
        FinishWorld(false, false);
    }

    void Boca(Transform g)
    {
        // la boca de la cueva: un arco de roca al final del túnel
        var gate = Group("Entrada", g);
        gate.localPosition = new Vector3(0f, 3.5f, -34f);
        const int piezas = 13;
        for (int i = 0; i <= piezas; i++)
        {
            float t = i / (float)piezas * Mathf.PI, grueso = Mathf.Lerp(3f, 1.8f, Mathf.Sin(t)) * R(0.9f, 1.15f);
            var r = MeshObject(gate, "RocaArco", RocaIrregular(grueso, grueso * 1.8f, k => Mathf.Sin(Mathf.PI * Mathf.Clamp(k, 0.04f, 0.96f)) * 0.9f + 0.1f, 9, 6), mExpPared, true);
            r.transform.localPosition = new Vector3(Mathf.Cos(t) * 9f, Mathf.Sin(t) * 9f - 1f, R(-0.5f, 0.5f));
            r.transform.localRotation = Quaternion.Euler(R(-12f, 12f), R(0f, 360f), R(-12f, 12f));
        }
        foreach (float sx in new[] { -1f, 1f }) Brazier(gate, new Vector3(sx * 5.4f, 0f, 2f));
    }

    /// <summary>La expedición explotaba esta gruta: la vía del carrito marca el camino y los faroles rompen el morado.</summary>
    void Mina(Transform g, Transform lvl)
    {
        MaterialesFuego();
        var m = Group("Mina", g);
        for (float z = -28f; z < CaveCorazonZ - 2f; z += 1.25f)
        {
            if (Mathf.Abs(z - CaveRioZ) < 9f || (z > CaveEscalaZ0 - 3f && z < CaveEscalaZ1 + 2f)) continue;
            float x = CaveCx(z) + 5.5f, x2 = CaveCx(z + 1.25f) + 5.5f, yaw = Mathf.Atan2(x2 - x, 1.25f) * Mathf.Rad2Deg;
            var q = CaveEn(x, z);
            var dur = Box(m, "Durmiente", q + Vector3.up * 0.06f, new Vector3(1.8f, 0.1f, 0.34f), mTimber, false);
            dur.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (float sx in new[] { -0.62f, 0.62f })
            {
                var r = Box(m, "Riel", q + Quaternion.Euler(0f, yaw, 0f) * new Vector3(sx, 0.16f, 0f), new Vector3(0.1f, 0.1f, 1.32f), mHierro, false);
                r.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
        // marcos de madera con farol, cada tanto; carritos con mineral
        foreach (float z in new[] { -18f, 6f, 30f, 46f, 72f, 96f })
        {
            var q = CaveCentro(z);
            var f = Group("MarcoMina", m);
            f.localPosition = q + Vector3.right * 2f;
            foreach (float sx in new[] { -7f, 7f })
            {
                Box(f, "Poste", new Vector3(sx, 3.2f, 0f), new Vector3(0.5f, 6.4f, 0.5f), mTimber, false);
                SolidBox(f, "Collider_Poste", new Vector3(sx, 3f, 0f), new Vector3(0.6f, 6f, 0.6f));
            }
            foreach (float sx in new[] { -5.75f, 5.75f }) Box(f, "Brazo", new Vector3(sx, 6.3f, 0f), new Vector3(3f, 0.3f, 0.35f), mTimber, false);   // sin viga de lado a lado: tapaba la vista
            foreach (float sx in new[] { -4.5f, 4.5f })
            {
                Box(f, "Cuerda", new Vector3(sx, 5.8f, 0f), new Vector3(0.05f, 0.9f, 0.05f), mHierro, false);
                Prim(f, PrimitiveType.Sphere, "Farol", new Vector3(sx, 5.2f, 0f), Vector3.one * 0.42f, Glow("Dio_FarolMina", new Color(1f, 0.62f, 0.26f), 3f));
                AddLight(f, new Vector3(sx, 5f, 0f), new Color(1f, 0.62f, 0.3f), 16f, 26f);
            }
        }
        foreach (var c in new[] { new Vector2(-2f, 11f), new Vector2(38f, 12f), new Vector2(88f, 9f) })
        {
            var q = CaveCentro(c.x, c.y);
            var car = Group("Carrito", m);
            car.localPosition = q;
            car.localRotation = Quaternion.Euler(0f, R(-20f, 20f), R(-6f, 6f));
            Box(car, "Caja", new Vector3(0f, 0.75f, 0f), new Vector3(1.5f, 0.9f, 2.2f), mTimber, true);
            foreach (float sx in new[] { -0.8f, 0.8f }) foreach (float sz in new[] { -0.7f, 0.7f })
                Prim(car, PrimitiveType.Cylinder, "Rueda", new Vector3(sx, 0.28f, sz), new Vector3(0.5f, 0.05f, 0.5f), mHierro).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Crystals(car, new Vector3(0f, 1.1f, 0f), 1.1f, rng.NextDouble() < 0.5, true);
        }
    }

    void Rio(Transform g, Transform lvl)
    {
        float x = CaveCx(CaveRioZ);
        RopeBridge(lvl, "PuenteRio", CaveEn(x, CaveRioZ - 7.5f), CaveEn(x, CaveRioZ + 7.5f), 6.5f);
        foreach (float sx in new[] { -1f, 1f })
        {
            PortLamp(lvl, CaveEn(x + sx * 4.6f, CaveRioZ - 8.5f));
            PortLamp(lvl, CaveEn(x + sx * 4.6f, CaveRioZ + 8.5f));
        }
        // la cascada que alimenta al río, al este
        float xe = x + CaveMedia(CaveRioZ) + 3f;
        Waterfall(g, new Vector3(xe, (13f + CaveLakeY) / 2f, CaveRioZ), Vector3.left, 7f, 13f - CaveLakeY);
        Foam(g, new Vector3(xe - 2.5f, CaveLakeY + 0.1f, CaveRioZ), 4f, 30f);
        // barrera a lo largo del agua, menos en el puente
        var o = Group("Limites", lvl);
        const float c = 2.5f;
        for (float bx = CaveWalk.xMin; bx < CaveWalk.xMax; bx += c)
            for (float bz = CaveWalk.yMin; bz < CaveWalk.yMax; bz += c)
            {
                if (CaveHeight(bx, bz) > -0.6f) continue;
                if (CaveHeight(bx + c, bz) <= -0.6f && CaveHeight(bx - c, bz) <= -0.6f && CaveHeight(bx, bz + c) <= -0.6f && CaveHeight(bx, bz - c) <= -0.6f) continue;
                if (Mathf.Abs(bx - x) < 4.5f && Mathf.Abs(bz - CaveRioZ) < 10f) continue;
                SolidBox(o, "Limite_Orilla", new Vector3(bx, 3f, bz), new Vector3(c, 20f, c));
            }
    }

    void Estanque(Transform g)
    {
        Vector2 c = CaveEstanque;
        float xw = c.x - 15f;
        Waterfall(g, new Vector3(xw, (11f + CaveLakeY) / 2f, c.y), Vector3.right, 5f, 11f - CaveLakeY);
        Foam(g, new Vector3(xw + 2.2f, CaveLakeY + 0.1f, c.y), 3f, 26f);
        // cristales que salen del agua y se reflejan
        foreach (var q in new[] { new Vector2(-6f, -4f), new Vector2(4f, 5f), new Vector2(-2f, 7f), new Vector2(7f, -3f) })
            Crystals(g, new Vector3(c.x + q.x, CaveLakeY - 0.3f, c.y + q.y), R(2.4f, 3.6f), rng.NextDouble() < 0.5, true);
        blocked.Add(new Rect(c.x - 15f, c.y - 13f, 30f, 26f));
    }

    void CristalesYRoca(Transform g)
    {
        var p = Group("Roca", g);
        // a los dos lados de la galería, alternando: los cristales marcan el camino y alumbran el piso
        bool izq = true;
        for (float z = -20f; z < 116f; z += 6f)
        {
            if (Mathf.Abs(z - CaveRioZ) < 8f) continue;
            float media = CaveMedia(z), lado = (izq ? -1f : 1f) * (media - R(4f, 9f));
            izq = !izq;
            var q = CaveCentro(z + R(-3f, 3f), lado);
            if (q.y < 0f || !Free(new Vector2(q.x, q.z), 1f)) continue;
            Crystals(g, q, R(2.4f, 4.4f), rng.NextDouble() < 0.5, true);
        }
        // cúmulos enormes que alumbran su sala, y rocas al pie de las paredes para que no sean una rampa lisa
        foreach (var c in new[] { new Vector2(-12f, 15f), new Vector2(8f, -14f), new Vector2(20f, 17f), new Vector2(46f, -14f), new Vector2(72f, 16f), new Vector2(88f, -15f) })
        {
            var q = CaveCentro(c.x, c.y);
            if (q.y < -0.3f) continue;
            bool violeta = c.y < 0f;
            Crystals(g, q, 6f, violeta, true);
            Crystals(g, q + new Vector3(2.4f, 0f, -1.5f), 3.4f, !violeta, false);
            AddLight(g, q + Vector3.up * 4f, violeta ? new Color(0.8f, 0.45f, 1f) : new Color(0.35f, 0.95f, 1f), 22f, 90f);
        }
        for (int i = 0, tries = 0; i < 90 && tries < 1500; tries++)
        {
            float z = R(-28f, 124f), media = CaveMedia(z), lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * R(media - 3f, media + 6f);
            var q = CaveCentro(z, lado);
            if (q.y < -0.3f) continue;
            Penasco(p, q, R(1f, 3.4f));
            i++;
        }
        // columnas de roca que van del piso a lo oscuro, pegadas a las paredes
        for (int i = 0, tries = 0; i < 26 && tries < 800; tries++)
        {
            float z = R(-24f, 122f), media = CaveMedia(z), lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * (media - R(-1f, 5f));
            var q = CaveCentro(z, lado);
            if (Mathf.Abs(z - CaveRioZ) < 7f || q.y < -0.5f || q.y > 6f) continue;
            float r = R(1.4f, 3f);
            var col = MeshObject(p, "Columna", RocaIrregular(r, R(16f, 30f), k => Mathf.Lerp(1f, 0.45f, Mathf.Sin(k * Mathf.PI)) + (k > 0.9f ? 0.3f : 0f), 11, 8), mExpParedAlt, true);
            col.transform.localPosition = q + Vector3.down * 0.2f;
            col.transform.localRotation = Quaternion.Euler(R(-3f, 3f), R(0f, 360f), R(-3f, 3f));
            blocked.Add(new Rect(q.x - r, q.z - r, r * 2f, r * 2f));
            i++;
        }
        // estalagmitas y hongos: siempre lejos del centro, por donde se camina y se pelea
        for (int i = 0, tries = 0; i < 44 && tries < 1500; tries++)
        {
            float z = R(-26f, 120f), media = CaveMedia(z), lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * R(media * 0.62f, media + 2f);
            var q = CaveCentro(z, lado);
            if (q.y < -0.3f || q.y > 7f || !Free(new Vector2(q.x, q.z), 1.2f)) continue;
            Stalagmite(g, q, R(1.5f, 5f));
            i++;
        }
        for (int i = 0, tries = 0; i < 46 && tries < 1500; tries++)
        {
            float z = R(-28f, 118f), media = CaveMedia(z), lado = (rng.NextDouble() < 0.5 ? -1f : 1f) * R(5f, media);
            var q = CaveCentro(z, lado);
            if (q.y < -0.3f || q.y > 5f || !Free(new Vector2(q.x, q.z), 0.5f)) continue;
            Mushrooms(g, q);
            i++;
        }
    }

    void Corazon(Transform g)
    {
        var c = Group("Corazon", g);
        c.localPosition = CaveCentro(CaveCorazonZ + 13f);
        // la geoda enorme, al fondo de la última sala, con columnas de cristal en semicírculo
        Crystals(c, Vector3.zero, 5.5f, true, true);
        Crystals(c, new Vector3(-3.5f, 0f, -1f), 3.6f, false, true);
        Crystals(c, new Vector3(3.5f, 0f, -1f), 3.6f, true, false);
        for (int i = 0; i < 9; i++)
        {
            float a = Mathf.Lerp(190f, 350f, i / 8f) * Mathf.Deg2Rad + Mathf.PI;
            var col = MeshObject(c, "Columna", RocaIrregular(R(0.8f, 1.2f), R(5f, 11f), k => 1f - k * 0.65f, 9, 6), mExpParedAlt, true);
            col.transform.localPosition = new Vector3(Mathf.Cos(a) * 13f, -0.1f, Mathf.Sin(a) * 10f);
            col.transform.localRotation = Quaternion.Euler(R(-5f, 5f), R(0f, 360f), R(-5f, 5f));
            Crystals(c, new Vector3(Mathf.Cos(a) * 14.4f, 0f, Mathf.Sin(a) * 11f), 1.4f, i % 2 == 0, false);
        }
        var lt = AddLight(c, new Vector3(0f, 5f, -2f), new Color(0.75f, 0.5f, 1f), 30f, 60f);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.12f;
    }
}
