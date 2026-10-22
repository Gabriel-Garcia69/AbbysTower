using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 3 — El Sendero del Acantilado (atardecer). Acantilado sobre el mar en tres repisas (meseta, repisa media y repisa
/// baja) unidas por escalinatas; desde la repisa baja, puentes colgantes saltan entre farallones hasta el faro roto,
/// donde espera el jefe. Una cascada cae del risco al mar. El sol se pone enfrente, sobre el agua.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect CliffArea = new Rect(-90f, -70f, 180f, 130f);
    static readonly Rect CliffWalk = new Rect(-48f, -40f, 100f, 76f);
    static readonly Vector3 CliffSpawn = new Vector3(-38f, 9.6f, -32f);
    const float CliffTop = 9.5f;   // poco más alto que la repisa media: desde arriba la cámara ve por encima
    static readonly Vector4[] Stacks = { new Vector4(-14f, 11f, 6.5f, 2.5f), new Vector4(10f, 15f, 6f, 4f), new Vector4(36f, 11f, 9.5f, 5.5f) };   // x, z, radio, alto
    const float CliffFallX = 24f;

    float CliffHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        foreach (var s in Stacks)
            if (Vector2.Distance(new Vector2(x, z), new Vector2(s.x, s.y)) < s.z + e * 0.4f) return s.w;
        // pasillos de las escalinatas
        if (Mathf.Abs(x + 36f) < 3.4f && z > -23.4f && z < -13f) return 7f;
        if (Mathf.Abs(x + 20f) < 3.4f && z > -12.2f && z < -4f) return 2.5f;
        float zTop = -20f + e * 1.2f, zMid = -8f + e * 1.2f, zLow = 0.5f + e * 0.6f;   // repisa baja honda: al pie de la escalinata queda un descanso amplio
        float h;
        if (z < zTop)
        {
            // el arroyo baja en un cauce hundido desde el manantial hasta el borde (antes era agua pegada encima del pasto)
            float zs = -20f + EdgeNoise(CliffFallX, -20f) * 1.2f;
            bool cauce = Mathf.Abs(x - CliffFallX) < 2.1f + e * 0.25f && z > zs - 16f;
            bool poza = Vector2.Distance(new Vector2(x, z), new Vector2(CliffFallX, zs - 18f)) < 3.6f + e * 0.3f;
            h = cauce || poza ? CliffTop - 0.8f : CliffTop;
        }
        else if (z < zMid) h = Mathf.Abs(x - CliffFallX) < 1.9f + e * 0.2f ? 7f - 0.6f : 7f;
        else if (z < zLow) h = Mathf.Abs(x - CliffFallX) < 3f ? 1.6f : 2.5f;   // la repisa baja, con la poza donde cae la cascada
        else h = z - zLow < 6f ? -6f : -8f;
        // al oeste el risco termina en el mar abierto, en escalones de roca
        float dW = x + 60f + e * 2f;
        if (dW < 0f) return -8f;
        if (dW < 3f) return Mathf.Min(h, 2.5f);
        if (dW < 6f) return Mathf.Min(h, 7f);
        return h;
    }

    /// <summary>Repisas de un risco según la distancia a la orilla (d mayor que 0 = tierra): mar, repisa baja, media y lo alto.</summary>
    static float Terraza(float d, float a, float b)
    {
        return -8f + 10.5f * Mathf.SmoothStep(0f, 1f, d / 1.4f) + 4.5f * Mathf.SmoothStep(0f, 1f, (d - a + 1.4f) / 1.4f)
             + (CliffTop - 7f) * Mathf.SmoothStep(0f, 1f, (d - b + 1.4f) / 1.4f);
    }

    /// <summary>
    /// Lo de afuera del mapa: al este, la costa de acantilados en tres repisas que se curva hacia el horizonte; al sur,
    /// montes con árboles; al oeste, mar abierto. Coincide con CliffHeight en la orilla del mapa.
    /// </summary>
    float CostaH(float x, float z)
    {
        float e = EdgeNoise(x, z);
        float bend = x > 90f ? (x - 90f) * (x - 90f) * 0.0035f : 0f;
        float dN = 0.5f + bend + e * 1.5f - z, dW = x + 60f + e * 2f;
        float h = Mathf.Min(Terraza(dN, 8.5f, 20.5f), Terraza(dW, 3f, 6f));
        if (h < CliffTop - 0.01f) return h;
        float adentro = Mathf.SmoothStep(0f, 1f, (dN - 20.5f) / 30f) * Mathf.SmoothStep(0f, 1f, (dW - 6f) / 25f);
        float sur = Mathf.SmoothStep(0f, 1f, (-70f - z) / 130f), este = Mathf.SmoothStep(0f, 1f, (x - 90f) / 150f);
        float n = Noise(x * 0.012f + 7f, z * 0.012f), n2 = Noise(x * 0.04f, z * 0.04f + 3f);
        return h + (Mathf.Max(sur * 34f, este * 18f) * (0.45f + 0.55f * n) + n2 * 2.5f * Mathf.Max(sur, este)) * adentro;
    }

    void Costa(Transform parent)
    {
        const float cell = 6f;
        float x0 = -108f, x1 = 402f, z0 = -342f, z1 = 438f;
        int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
        var hh = new float[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++) hh[i, j] = CostaH(x0 + i * cell, z0 + j * cell);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var pasto = new List<int>(); var roca = new List<int>();
        var dentro = new Rect(CliffArea.xMin + 1f, CliffArea.yMin + 1f, CliffArea.width - 2f, CliffArea.height - 2f);
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            bool plano = n.y > 0.8f;
            int k = v.Count;
            foreach (var p in new[] { a, b, c })
            {
                v.Add(p);
                uv.Add(plano ? new Vector2(p.x, p.z) * 0.25f : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) * 0.25f : new Vector2(p.x, p.y) * 0.25f);
            }
            var lista = plano ? pasto : roca;
            lista.Add(k); lista.Add(k + 1); lista.Add(k + 2);
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float xa = x0 + i * cell, za = z0 + j * cell;
                if (dentro.Contains(new Vector2(xa, za)) && dentro.Contains(new Vector2(xa + cell, za + cell))) continue;   // ahí va el terreno del mapa
                float h00 = hh[i, j], h10 = hh[i + 1, j], h01 = hh[i, j + 1], h11 = hh[i + 1, j + 1];
                if (Mathf.Max(Mathf.Max(h00, h10), Mathf.Max(h01, h11)) < -7.5f) continue;   // fondo del mar
                var a = new Vector3(xa, h00, za); var b = new Vector3(xa + cell, h10, za); var c = new Vector3(xa, h01, za + cell); var d = new Vector3(xa + cell, h11, za + cell);
                Tri(a, c, d); Tri(a, d, b);
            }
        var m = new Mesh { name = "Costa", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv);
        m.subMeshCount = 2; m.SetTriangles(pasto, 0); m.SetTriangles(roca, 1);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        var go = MeshObject(parent, "Costa", m, TopMat, true);
        go.GetComponent<MeshRenderer>().sharedMaterials = new[] { TopMat, WallMat };
        // árboles arriba de los riscos y en los montes (no en lo empinado ni pegados a la orilla)
        for (int i = 0, tries = 0; i < 300 && tries < 6000; tries++)
        {
            var q = new Vector2(R(-60f, 330f), R(-300f, 260f));
            if (CliffArea.Contains(q) || Vector2.Distance(q, new Vector2(15f, -20f)) > 260f) continue;
            float h = CostaH(q.x, q.y);
            if (h < CliffTop - 0.01f || Mathf.Abs(CostaH(q.x + 2f, q.y) - h) > 1.2f || Mathf.Abs(CostaH(q.x, q.y + 2f) - h) > 1.2f) continue;
            float dN = 0.5f + (q.x > 90f ? (q.x - 90f) * (q.x - 90f) * 0.0035f : 0f) - q.y;
            if (dN < 26f || q.x + 60f < 10f) continue;
            if (rng.NextDouble() < 0.55) Pine(parent, q, h - 0.1f, R(0.9f, 1.5f), false); else Tree(parent, q, h - 0.1f, R(0.9f, 1.3f), false);
            i++;
        }
        // farallones sueltos frente a la costa
        for (int i = 0; i < 14; i++)
        {
            float x = R(100f, 330f), z = 0.5f + (x - 90f) * (x - 90f) * 0.0035f + R(6f, 30f), s = R(3f, 7f);
            var r = Prim(parent, PrimitiveType.Sphere, "Farallon", new Vector3(x, SeaY + s * 0.4f, z), new Vector3(s * R(0.8f, 1.2f), s * R(1.6f, 3f), s * R(0.8f, 1.2f)), mRock);
            r.transform.localRotation = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), R(-8f, 8f));
            Foam(parent, new Vector3(x, SeaY + 0.05f, z), s * 0.9f, 6f);
        }
        // mar abierto también al suroeste (el mar general no llega tan al sur)
        var mar = Flat(parent, "MarOeste", new Vector3(-480f, SeaY, -470f), 840f, 440f, mWater, 0.05f);
        mar.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void BuildCliffs()
    {
        BuildTerrain();
        BuildSea();
        // sendero por lo alto (sin árboles encima) desde el campamento hasta la escalinata
        blocked.Add(new Rect(-46f, -38f, 14f, 12f));
        blocked.Add(new Rect(-40f, -27f, 8f, 16f));
        blocked.Add(new Rect(-46f, -20f, 96f, 15f));   // la repisa media es camino
        blocked.Add(new Rect(-26f, -5f, 18f, 6f));     // descanso al pie de la escalinata baja y entrada al puente
        var g = Group("Acantilado", root);
        var lvl = Group("Nivel", root);

        // escalinatas entre repisas y puentes a los farallones
        Stairs(lvl, "EscalinataAlta", new Vector3(-36f, 7f, -17.2f), new Vector3(-36f, CliffTop, -23.5f), 5.6f);
        Stairs(lvl, "EscalinataBaja", new Vector3(-20f, 2.5f, -4.5f), new Vector3(-20f, 7f, -12.2f), 5.6f);
        RopeBridge(lvl, "Puente1", new Vector3(-12f, 2.5f, -0.6f), new Vector3(-13f, 2.5f, 11f - 5.6f));
        RopeBridge(lvl, "Puente2", new Vector3(-8.6f, 2.5f, 12.2f), new Vector3(5.2f, 4f, 14.2f));
        RopeBridge(lvl, "Puente3", new Vector3(15.2f, 4f, 15.2f), new Vector3(27.8f, 5.5f, 12.6f));

        // el arroyo: nace en un manantial entre rocas, baja por su cauce y cae del risco a la repisa media
        float zt = -20f + EdgeNoise(CliffFallX, -20f) * 1.2f, zm = -8f + EdgeNoise(CliffFallX, -8f) * 1.2f;
        float aguaAlta = CliffTop - 0.35f, zSrc = zt - 18f;
        Prim(g, PrimitiveType.Cylinder, "Manantial", new Vector3(CliffFallX, aguaAlta, zSrc), new Vector3(7.4f, 0.005f, 7.4f), mRiver);
        FlowFlat(g, "ArroyoAlto", new Vector3(CliffFallX, aguaAlta + 0.005f, (zSrc + 3.6f + zt) / 2f), 4.6f, zt - zSrc - 3.6f + 0.3f, mRiver, 0.2f, new Vector2(0f, 0.5f));
        var pena = Group("Manantial", g);   // el peñasco de donde brota el agua, con una caída chica
        for (int i = 0; i < 9; i++)
        {
            float ang = Mathf.Lerp(200f, 340f, i / 8f) * Mathf.Deg2Rad, rr = R(4.6f, 6f), sz = R(2f, 3.6f);
            var r = Prim(pena, PrimitiveType.Sphere, "Roca", new Vector3(CliffFallX + Mathf.Cos(ang) * rr, CliffTop + sz * 0.25f, zSrc + Mathf.Sin(ang) * rr), new Vector3(sz * 1.3f, sz * R(0.9f, 1.4f), sz), mRock);
            r.transform.localRotation = Quaternion.Euler(R(-10f, 10f), R(0f, 360f), R(-10f, 10f));
        }
        for (int i = 0; i < 6; i++)   // el peñasco: rocas apiladas, la de arriba más chica
        {
            float k = i / 5f, sz = Mathf.Lerp(4.2f, 2f, k);
            var r = Prim(pena, PrimitiveType.Sphere, "Pico", new Vector3(CliffFallX + R(-2.2f, 2.2f) * (1f - k), CliffTop + 0.8f + k * 3.6f, zSrc - 5.6f + R(-0.8f, 0.8f)), new Vector3(sz * R(1f, 1.4f), sz * R(0.6f, 0.85f), sz * R(0.8f, 1.1f)), mRock);
            r.transform.localRotation = Quaternion.Euler(R(-14f, 14f), R(0f, 360f), R(-14f, 14f));
        }
        Waterfall(g, new Vector3(CliffFallX, (CliffTop + 3.4f + aguaAlta) / 2f, zSrc - 3.4f), Vector3.forward, 1.6f, CliffTop + 3.4f - aguaAlta);
        for (int i = 0; i < 8; i++)   // piedras en las orillas del cauce (no dentro del agua)
        {
            float sx = i % 2 == 0 ? -1f : 1f, z = Mathf.Lerp(zSrc + 4f, zt - 1.5f, i / 7f);
            Prim(g, PrimitiveType.Sphere, "Pena", new Vector3(CliffFallX + sx * R(2.9f, 3.4f), CliffTop + 0.1f, z), new Vector3(R(0.9f, 1.5f), R(0.5f, 0.9f), R(0.9f, 1.4f)), mRock);
        }
        blocked.Add(new Rect(CliffFallX - 7f, zSrc - 9f, 14f, zt - zSrc + 9f));
        Waterfall(g, new Vector3(CliffFallX, (aguaAlta + 7f - 0.3f) / 2f, zt + 0.05f), Vector3.forward, 3f, aguaAlta - (7f - 0.3f) + 0.1f);
        FlowFlat(g, "Arroyo", new Vector3(CliffFallX, 7f - 0.3f, (zt + zm) / 2f), 4f, zm - zt, mRiver, 0.2f, new Vector2(0f, 0.6f));   // cruza la repisa media en su cauce
        // cae a una poza en la repisa baja y de ahí, en una segunda cascada, al mar
        float zl = 0.5f + EdgeNoise(CliffFallX, 0.5f) * 0.6f;
        Waterfall(g, new Vector3(CliffFallX, (7f + 2.2f) / 2f, zm + 0.05f), Vector3.forward, 3f, 7f - 2.2f + 0.1f);
        FlowFlat(g, "PozaBaja", new Vector3(CliffFallX, 2.2f, (zm + zl) / 2f), 6.1f, zl - zm + 0.2f, mRiver, 0.2f, new Vector2(0f, 0.25f));   // se va despacio hacia el mar
        Foam(g, new Vector3(CliffFallX, 2.25f, zm + 1f), 2f, 18f);
        Waterfall(g, new Vector3(CliffFallX, (2.2f + SeaY) / 2f, zl + 0.05f), Vector3.forward, 5f, 2.2f - SeaY + 0.2f);
        for (int i = 0; i < 6; i++)
            Prim(g, PrimitiveType.Sphere, "Pena", new Vector3(CliffFallX + (i % 2 == 0 ? -3.3f : 3.3f), 2.5f, Mathf.Lerp(zm + 1f, zl - 1f, i / 5f)), new Vector3(R(1f, 1.6f), R(0.7f, 1.1f), R(1f, 1.5f)), mRock);
        BankGuard(g, new List<Vector3> { new Vector3(CliffFallX - 3.2f, 2.5f, zm), new Vector3(CliffFallX - 3.2f, 2.5f, zl) }, p => false);
        BankGuard(g, new List<Vector3> { new Vector3(CliffFallX + 3.2f, 2.5f, zm), new Vector3(CliffFallX + 3.2f, 2.5f, zl) }, p => false);
        Foam(g, new Vector3(CliffFallX, 7f - 0.25f, zt + 0.8f), 1.6f, 14f);
        Foam(g, new Vector3(CliffFallX, SeaY + 0.1f, zl + 1.4f), 3.2f, 30f);

        // enredaderas que cuelgan de las orillas de las repisas
        for (float x = -46f; x < 50f; x += R(2.2f, 4f))
        {
            if (Mathf.Abs(x - CliffFallX) < 4f) continue;
            float z1 = -20f + EdgeNoise(x, -20f) * 1.2f, z2 = -8f + EdgeNoise(x, -8f) * 1.2f;
            if (rng.NextDouble() < 0.6) Vine(g, new Vector3(x, CliffTop, z1 + 0.3f), R(1f, 2.4f));
            if (rng.NextDouble() < 0.6) Vine(g, new Vector3(x, 7f, z2 + 0.3f), R(1.5f, 4f));
        }
        // faroles a lo largo de la repisa media y en los farallones
        for (float x = -42f; x < 46f; x += 9f)
            if (Mathf.Abs(x - CliffFallX) > 4f && Mathf.Abs(x + 36f) > 3f && Mathf.Abs(x + 20f) > 3f)
                PortLamp(lvl, new Vector3(x, 7f, -9.8f + EdgeNoise(x, -8f) * 1.2f - 1.2f));
        foreach (var s in Stacks) for (int k = 0; k < 2; k++)
            {
                float a = k * 2.4f + 0.8f;
                PortLamp(lvl, new Vector3(s.x + Mathf.Cos(a) * (s.z - 1.6f), s.w, s.y + Mathf.Sin(a) * (s.z - 1.6f)));
            }

        // el faro roto en el último farallón
        var c = Stacks[2];
        var tower = Group("FaroRoto", g);
        tower.localPosition = new Vector3(c.x + 4f, c.w, c.y + 4f);
        Box(tower, "Zocalo", new Vector3(0f, 0.4f, 0f), new Vector3(5f, 0.8f, 5f), mStoneBrick, true);
        float y = 0.8f;
        for (int i = 0; i < 3; i++)
        {
            Prim(tower, PrimitiveType.Cylinder, "Tramo", new Vector3(0f, y + 1.3f, 0f), new Vector3(3.6f - i * 0.3f, 1.3f, 3.6f - i * 0.3f), i % 2 == 0 ? mWhite : mRedPaint, true);
            y += 2.6f;
        }
        // borde roto: un muro curvo hecho de dovelas que se asientan sobre el último tramo, con alturas desiguales
        float rTop = (3.6f - 2 * 0.3f) / 2f - 0.15f;
        for (int i = 0; i < 6; i++)
        {
            R(0.2f, 1.2f);   // (azar que gastaba la versión anterior)
            float alto = R(0.6f, 2f), inc = R(-10f, 10f);
            for (int k = 0; k < 3; k++)
            {
                float a = (i * 3 + k) / 18f * Mathf.PI * 2f;
                float hk = alto * (k == 1 ? 1f : 0.65f);
                var b = Box(tower, "Diente", new Vector3(Mathf.Cos(a) * rTop, y + hk / 2f, Mathf.Sin(a) * rTop), new Vector3(0.6f, hk, 0.35f), mWhite, false);
                b.transform.localRotation = Quaternion.Euler(0f, -90f - a * Mathf.Rad2Deg, k == 1 ? inc * 0.3f : 0f);
            }
        }
        // la linterna de hierro caída al pie del faro y una escalera de caracol que asoma
        MaterialesFuego();
        var linterna = Group("LinternaCaida", tower);
        linterna.localPosition = new Vector3(3.2f, 0.9f, -1.2f);
        linterna.localRotation = Quaternion.Euler(0f, 30f, 68f);
        MeshObject(linterna, "Aro", Ring(1.1f, 1f, 0.12f, 16), mHierro, false);
        for (int k = 0; k < 6; k++)
        {
            float a = k / 6f * Mathf.PI * 2f;
            Box(linterna, "Barra", new Vector3(Mathf.Cos(a) * 1.05f, 0.7f, Mathf.Sin(a) * 1.05f), new Vector3(0.08f, 1.4f, 0.08f), mHierro, false);
        }
        Prim(linterna, PrimitiveType.Sphere, "Cupula", new Vector3(0f, 1.45f, 0f), new Vector3(2.2f, 0.9f, 2.2f), mRedPaint);
        Box(tower, "Puerta", new Vector3(0f, 1.9f, 1.62f), new Vector3(1f, 1.9f, 0.2f), mDoor, false);
        foreach (float yy in new[] { 3.6f, 6.2f })
            Box(tower, "Ventanuco", new Vector3(0f, yy, 1.55f - (yy - 3.6f) * 0.06f), new Vector3(0.35f, 0.7f, 0.1f), mWindow, false);
        for (int i = 0; i < 8; i++)
        {
            var r = Box(tower, "Escombro", new Vector3(R(-5f, 2f), 0.25f, R(-5f, 2f)), new Vector3(R(0.5f, 1.2f), R(0.3f, 0.7f), R(0.5f, 1f)), i % 2 == 0 ? mWhite : mStoneBrick, true);
            r.transform.localRotation = Quaternion.Euler(R(-15f, 15f), R(0f, 90f), R(-15f, 15f));
        }
        Brazier(tower, new Vector3(-2.6f, 0f, -2.6f));
        blocked.Add(new Rect(c.x + 1f, c.y + 1f, 7f, 7f));

        // campamento en el primer farallón: mercader, santuario, fogata
        var a0 = Stacks[0];
        MerchantPost(lvl, new Vector3(a0.x - 2f, a0.w, a0.y + 2.6f), 160f);
        HealShrine(lvl, new Vector3(a0.x + 3f, a0.w, a0.y - 1f));
        Campfire(lvl, new Vector3(a0.x + 1f, a0.w, a0.y + 0.5f));
        Tent(lvl, new Vector3(a0.x - 3.8f, a0.w, a0.y - 2.4f), 30f, mCloth);
        // caserío de pescadores en la repisa media y molino arriba
        House(g, new Vector3(38f, 7f, -16.5f), 180f, 6f, 4.5f, 3.4f, true);
        House(g, new Vector3(-44f, 7f, -16.5f), 180f, 5.5f, 4.2f, 3.2f, true);
        House(g, new Vector3(44f, CliffTop, -33f), -90f, 7f, 5f, 3.6f, true);
        Clutter(g, new Vector3(34f, 7f, -14f));
        Clutter(g, new Vector3(40f, CliffTop, -28f));
        // cerca en la orilla de lo alto
        for (float x = -46f; x < 50f; x += 8f)
        {
            float z1 = -20f + EdgeNoise(x, -20f) * 1.2f;
            if (Mathf.Abs(x + 36f) < 4f || Mathf.Abs(x - CliffFallX) < 3f) continue;
            Fence(g, new Vector2(x, z1 - 1.4f), new Vector2(x + 6.5f, -20f + EdgeNoise(x + 6.5f, -20f) * 1.2f - 1.4f));
        }
        // campamento de salida arriba
        Tent(lvl, new Vector3(-43f, CliffTop, -35f), 70f, mClothB);
        Campfire(lvl, new Vector3(-40f, CliffTop, -36.5f));

        // combate: emboscada en la repisa media, el segundo farallón y el jefe en el faro roto
        Arena(lvl, "Emboscada en la repisa", new Vector3(2f, 7f, -14.2f), new Vector2(20f, 7f), 5);
        var b2 = Stacks[1];
        Arena(lvl, "El farallón del viento", new Vector3(b2.x, b2.w, b2.y), new Vector2(8.5f, 8.5f), 4);
        var boss = Arena(lvl, "Jefe", new Vector3(c.x - 1.5f, c.w, c.y - 1f), new Vector2(13f, 12f), 1, true);
        Exit(lvl, new Vector3(c.x - 3.5f, c.w, c.y + 2.5f), boss);
        Chest(lvl, new Vector3(30f, CliffTop, -26f), 26, 180f);
        Chest(lvl, new Vector3(b2.x + 3f, b2.w, b2.y + 3.2f), 22, 200f);
        Note(lvl, new Vector3(-34f, CliffTop, -34f), 180f, LoreNote.Kind.Diario, "dio3_diario", "Diario de la expedición — día 6",
             "En este piso el sol nunca termina de ponerse. Cruzamos los puentes colgantes de uno en uno.\nLa criatura del faro salta desde lo alto: cuando su sombra cae al suelo, hay que moverse.", "— " + Historia.Hermano);
        Note(lvl, new Vector3(c.x + 1f, c.w, c.y - 5.5f), 0f, LoreNote.Kind.Inscripcion, "dio3_faro", "Placa del faro",
             "Este faro alumbra un mar que no existe afuera de la torre.\nNadie sabe quién lo encendió. Se apaga cuando una criatura fuerte anda cerca.");

        WalkBoundary(CliffWalk);
        var bg = Group("Fondo", root);
        Costa(bg);   // costa de riscos al este, montes al sur, mar abierto al oeste (antes: dos muros gigantes y llano)
        Ship(bg, new Vector3(-60f, SeaY, 150f), 110f, 24f, 6f, false);
        // gaviotas sobre los farallones
        foreach (var s in Stacks)
        {
            var bird = Group("Gaviota", bg);
            Prim(bird, PrimitiveType.Sphere, "Cuerpo", Vector3.zero, new Vector3(0.22f, 0.2f, 0.6f), mWhite);
            var wl = Group("AlaI", bird); var wr = Group("AlaD", bird);
            Box(wl, "Ala", new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), mWhite, false);
            Box(wr, "Ala", new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), mWhite, false);
            var gull = bird.gameObject.AddComponent<Gull>();
            gull.center = new Vector3(s.x, s.w, s.y); gull.radius = R(7f, 12f); gull.height = R(6f, 10f); gull.phase = R(0f, 6f); gull.speed = 0.35f;
            gull.wingL = wl; gull.wingR = wr;
        }

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 180f, 22f, 95f, 32f, 4f, new Vector3(0f, 6f, -8f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Puentes", new Vector3(-1.5f, 2.5f, 13.2f), new Vector2(10f, 5f), 100f, 27f, 22f, 36f, 6f, new Vector3(4f, 1f, 0f), 1.6f, 2,
             "LOS PUENTES DEL ACANTILADO", "");
        Shot(shots, "Ocaso", new Vector3(a0.x, a0.w, a0.y), new Vector2(8f, 8f), 15f, 8f, 12f, 44f, 9f, new Vector3(0f, 2f, 5f), 2.2f, 3,
             "EL OCASO", "El sol se hunde en el mar de la torre", still: true);
        Shot(shots, "Cascada", new Vector3(CliffFallX - 5f, 7f, -14f), new Vector2(6f, 7f), 60f, 16f, 16f, 36f, 4f, new Vector3(3f, 0f, 2f), 1.8f, 2,
             "LA CASCADA DEL RISCO", "", still: true);
        FinishWorld();
    }
}
