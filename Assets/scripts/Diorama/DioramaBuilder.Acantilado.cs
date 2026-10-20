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
        if (z < zTop) return z < -32f && Noise(x * 0.09f + 5f, z * 0.09f) > 0.62f ? CliffTop + 2.5f : CliffTop;
        if (z < zMid) return 7f;
        if (z < zLow) return Mathf.Abs(x - CliffFallX) < 3f ? 1.6f : 2.5f;   // la repisa baja, con la poza donde cae la cascada
        return z - zLow < 6f ? -6f : -8f;
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

        // cascada: nace en una poza arriba, cruza la repisa media y cae al mar
        float zt = -20f + EdgeNoise(CliffFallX, -20f) * 1.2f, zm = -8f + EdgeNoise(CliffFallX, -8f) * 1.2f;
        Flat(g, "Poza", new Vector3(CliffFallX, CliffTop + 0.06f, zt - 3f), 5f, 6f, mRiver, 0.2f);
        for (int i = 0; i < 7; i++)
            Prim(g, PrimitiveType.Sphere, "Pena", new Vector3(CliffFallX + R(-3.5f, 3.5f), CliffTop + 0.2f, zt - 3f + R(-3.6f, 3.6f) + (i % 2 == 0 ? 0f : -1.5f)), new Vector3(R(1.2f, 2f), R(0.8f, 1.4f), R(1.2f, 2f)), mRock);
        Waterfall(g, new Vector3(CliffFallX, (CliffTop + 7f) / 2f, zt + 0.05f), Vector3.forward, 3f, CliffTop - 7f + 0.1f);
        Flat(g, "Arroyo", new Vector3(CliffFallX, 7.06f, (zt + zm) / 2f), 3f, zm - zt, mRiver, 0.2f);
        // cae a una poza en la repisa baja y de ahí, en una segunda cascada, al mar
        float zl = 0.5f + EdgeNoise(CliffFallX, 0.5f) * 0.6f;
        Waterfall(g, new Vector3(CliffFallX, (7f + 2.2f) / 2f, zm + 0.05f), Vector3.forward, 3f, 7f - 2.2f + 0.1f);
        Flat(g, "PozaBaja", new Vector3(CliffFallX, 2.2f, (zm + zl) / 2f), 6.1f, zl - zm + 0.2f, mRiver, 0.2f);
        Foam(g, new Vector3(CliffFallX, 2.25f, zm + 1f), 2f, 18f);
        Waterfall(g, new Vector3(CliffFallX, (2.2f + SeaY) / 2f, zl + 0.05f), Vector3.forward, 5f, 2.2f - SeaY + 0.2f);
        for (int i = 0; i < 6; i++)
            Prim(g, PrimitiveType.Sphere, "Pena", new Vector3(CliffFallX + (i % 2 == 0 ? -3.3f : 3.3f), 2.5f, Mathf.Lerp(zm + 1f, zl - 1f, i / 5f)), new Vector3(R(1f, 1.6f), R(0.7f, 1.1f), R(1f, 1.5f)), mRock);
        BankGuard(g, new List<Vector3> { new Vector3(CliffFallX - 3.2f, 2.5f, zm), new Vector3(CliffFallX - 3.2f, 2.5f, zl) }, p => false);
        BankGuard(g, new List<Vector3> { new Vector3(CliffFallX + 3.2f, 2.5f, zm), new Vector3(CliffFallX + 3.2f, 2.5f, zl) }, p => false);
        Foam(g, new Vector3(CliffFallX, 7.1f, zt + 0.8f), 1.6f, 14f);
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
        for (int i = 0; i < 6; i++)   // borde roto
        {
            float a = i / 6f * Mathf.PI * 2f;
            var b = Box(tower, "Diente", new Vector3(Mathf.Cos(a) * 1.4f, y + R(0.2f, 1.2f), Mathf.Sin(a) * 1.4f), new Vector3(0.7f, R(0.6f, 2f), 0.5f), i % 2 == 0 ? mWhite : mRedPaint, false);
            b.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, R(-10f, 10f));
        }
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
        OuterLand(bg, CliffTop, false, true, true, true, 260);
        float far = 2500f, ax = CliffArea.xMin, bb = CliffArea.xMax;
        Wall(bg, "RiscoOeste", new Vector3((-far + ax) / 2f, (CliffTop - 8.3f) / 2f, -20f), Vector3.forward, far + ax, CliffTop + 8.3f, mRock, 0.25f);
        Wall(bg, "RiscoEste", new Vector3((bb + far) / 2f, (CliffTop - 8.3f) / 2f, -20f), Vector3.forward, far - bb, CliffTop + 8.3f, mRock, 0.25f);
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
