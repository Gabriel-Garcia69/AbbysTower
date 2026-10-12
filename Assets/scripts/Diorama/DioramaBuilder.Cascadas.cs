using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 3 — El Valle de las Cascadas (mañana clara). Un valle largo que sube hacia el norte en cinco bancales; un río
/// serpentea de bancal en bancal y en cada escalón cae en cascada. Se cruza tres veces por puentes de tablones, se sube
/// por escalinatas al lado del agua, hay molino, campamento del mercader a la mitad y, al fondo, las Gemelas: dos
/// cascadas enormes que caen de un risco de 20 m a una poza donde espera el jefe.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect FallsArea = new Rect(-110f, -76f, 220f, 350f);
    static readonly Rect FallsWalk = new Rect(-55f, -62f, 110f, 304f);
    static readonly Vector3 FallsSpawn = new Vector3(14f, 0.1f, -54f);
    static readonly float[] FallsZ = { 40f, 100f, 160f, 210f };   // escalones del valle (cada uno sube 4 m)
    const float FallsStep = 4f, FallsCliffZ = 246f, FallsCliffY = 36f;
    static readonly Vector2 FallsPool = new Vector2(19f, 236f);
    // escalinatas: x de cada una (en el escalón FallsZ[i])
    static readonly float[] FallsStairX = { -32f, 32f, 32f, -30f };

    static float FallsRiverX(float z) => 22f * Mathf.Sin(z * 0.035f);
    const float FallsPondZ = -56f;   // al sur el río termina en un estanque (no corta el terreno hasta la orilla del mapa)
    static Vector2 FallsPond => new Vector2(FallsRiverX(FallsPondZ), FallsPondZ);

    float FallsTier(float z) { int k = 0; while (k < FallsZ.Length && z > FallsZ[k]) k++; return k * FallsStep; }

    /// <summary>¿Cerca de una escalinata o del cauce? (ahí el borde del escalón es recto, sin ruido)</summary>
    bool FallsCalm(float x, float z)
    {
        for (int i = 0; i < FallsZ.Length; i++)
            if (Mathf.Abs(x - FallsStairX[i]) < 7f && Mathf.Abs(z - FallsZ[i]) < 15f) return true;
        return Mathf.Abs(x - FallsRiverX(z)) < 9f;
    }

    bool FallsInRiver(float x, float z)
    {
        if (z > FallsCliffZ - 2f) return false;
        if (Vector2.Distance(new Vector2(x, z), FallsPool) < 14f) return true;
        if (z < FallsPondZ) return Vector2.Distance(new Vector2(x, z), FallsPond) < 9.5f;
        return Mathf.Abs(x - FallsRiverX(z)) < 6.6f;
    }

    float FallsHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        // el gran risco del fondo, con las dos acequias que alimentan a las Gemelas
        if (z > FallsCliffZ + e * 1.5f)
        {
            if (z < FallsCliffZ + 18f && (Mathf.Abs(x - 12f) < 2.6f || Mathf.Abs(x - 26f) < 2.6f)) return FallsCliffY - 0.9f;
            return FallsCliffY + (z > FallsCliffZ + 30f && Noise(x * 0.05f, z * 0.05f) > 0.55f ? 6f : 0f);
        }
        float zz = FallsCalm(x, z) ? z : z + e * 1.6f;
        float T = FallsTier(zz);
        // escalinatas: el pasillo de abajo queda al nivel de abajo, sin ruido
        for (int i = 0; i < FallsZ.Length; i++)
            if (Mathf.Abs(x - FallsStairX[i]) < 4.4f && z > FallsZ[i] - 12f && z <= FallsZ[i]) return i * FallsStep;
        // la poza de las Gemelas
        float dp = Vector2.Distance(new Vector2(x, z), FallsPool);
        if (dp < 12.5f + e * 0.6f) return 16f - 2.8f;
        if (dp < 14f + e * 0.6f) return 16f - 1.1f;
        // el río: cauce, orilla baja y orilla
        float d = Mathf.Abs(x - FallsRiverX(z)) + e * 0.5f;
        if (z < FallsPondZ)
        {
            float dpond = Vector2.Distance(new Vector2(x, z), FallsPond) + e * 0.4f;
            if (dpond < 7.6f) return T - 2f;
            if (dpond < 9.2f) return T - 1.1f;
        }
        else if (z < FallsPool.y - 6f)
        {
            if (d < 4.4f) return T - 2f;
            if (d < 6.2f) return T - 1.1f;
        }
        // las laderas del valle suben a los lados
        float side = Mathf.Abs(x) - 58f + e * 2f;
        if (side > 20f) return 30f + (Noise(x * 0.06f, z * 0.06f) > 0.55f ? 4f : 0f);   // paredes del valle, a la altura de afuera
        if (side > 0f) return T + (side < 6f ? 3f : side < 12f ? 6.5f : 10f) + (side > 12f && Noise(x * 0.08f, z * 0.08f) > 0.6f ? 2.5f : 0f);
        if (z < -64f + e * 2f) return T + 3f;   // al sur, un bancal que cierra el valle (la cámara pasa por encima)
        return T;
    }

    void BuildFalls()
    {
        BuildTerrain();
        var g = Group("Valle", root);
        var lvl = Group("Nivel", root);
        var water = Group("Agua", root);

        // agua: una franja que sigue al río en cada bancal (1.2 m bajo el suelo) + la poza
        float zPrev = FallsPondZ;
        var pondWater = Prim(water, PrimitiveType.Cylinder, "Estanque", new Vector3(FallsPond.x, -1.2f, FallsPond.y), new Vector3(19f, 0.005f, 19f), mRiver);
        pondWater.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i <= FallsZ.Length; i++)
        {
            float z1 = i < FallsZ.Length ? FallsZ[i] : FallsPool.y, y = i * FallsStep - 1.2f;
            MeshObject(water, "Rio_" + i, ZRiverStrip(zPrev, z1 + 0.02f, y, 12.6f), mRiver, false);
            zPrev = z1;
        }
        var poolWater = Flat(water, "Poza", new Vector3(FallsPool.x, 16f - 1.2f, FallsPool.y), 29f, 29f, mRiver, 0.12f);
        poolWater.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // enredaderas que cuelgan del gran risco
        for (float x = -50f; x < 56f; x += R(2.5f, 5f))
            if (Mathf.Abs(x - 12f) > 4f && Mathf.Abs(x - 26f) > 4f) Vine(g, new Vector3(x, FallsCliffY, FallsCliffZ + EdgeNoise(x, FallsCliffZ) * 1.5f - 0.2f), R(3f, 9f));
        // cascadas en cada escalón, con espuma abajo
        for (int i = 0; i < FallsZ.Length; i++)
        {
            float zb = FallsZ[i], xr = FallsRiverX(zb), top = (i + 1) * FallsStep - 1.2f, bottom = i * FallsStep - 1.2f;
            Waterfall(water, new Vector3(xr, (top + bottom) / 2f, zb), Vector3.back, 8.5f, top - bottom + 0.1f);
            Foam(water, new Vector3(xr, bottom + 0.08f, zb - 1.2f), 3f, 26f);
        }
        // las Gemelas: dos cascadas de 20 m al fondo, con sus acequias arriba
        foreach (float fx in new[] { 12f, 26f })
        {
            Waterfall(water, new Vector3(fx, (FallsCliffY - 0.4f + 14.8f) / 2f, FallsCliffZ), Vector3.back, 6f, FallsCliffY - 0.4f - 14.8f);
            Flat(water, "Acequia", new Vector3(fx, FallsCliffY - 0.4f, FallsCliffZ + 9f), 5.2f, 18f, mRiver, 0.2f);
            Foam(water, new Vector3(fx, 14.9f, FallsCliffZ - 2.5f), 5f, 40f);
        }
        var mist = Group("Bruma", water);
        mist.localPosition = new Vector3(19f, 15.5f, FallsCliffZ - 3f);
        Smoke(mist, Vector3.zero, new Color(1f, 1f, 1f, 0.35f), false);

        // rocas con espuma en el río
        for (int i = 0; i < 40; i++)
        {
            float z = R(-70f, 205f), x = FallsRiverX(z) + R(-3f, 3f), y = FallsTier(z) - 1.3f;
            float s = R(0.5f, 1.3f);
            var rock = Prim(water, PrimitiveType.Sphere, "RocaRio", new Vector3(x, y, z), new Vector3(s * R(1f, 1.6f), s * 0.8f, s), mRock);
            rock.transform.localRotation = Quaternion.Euler(R(0f, 20f), R(0f, 360f), 0f);
            if (i % 2 == 0) Foam(water, new Vector3(x, y + 0.15f, z + s * 0.6f), 0.8f, 6f);
        }

        // escalinatas junto al agua
        for (int i = 0; i < FallsZ.Length; i++)
            Stairs(lvl, "Escalinata" + i, new Vector3(FallsStairX[i], i * FallsStep, FallsZ[i] - 11f), new Vector3(FallsStairX[i], (i + 1) * FallsStep, FallsZ[i]), 7f);

        // tres puentes de tablones sobre el río
        PlankBridge(lvl, "PuenteVado", new Vector3(-18.5f, 0f, -10f), new Vector3(3.5f, 0f, -10f), 5f);
        PlankBridge(lvl, "PuenteMolino", new Vector3(-7f, 4f, 85f), new Vector3(15f, 4f, 85f), 5f);
        PlankBridge(lvl, "PuenteAlto", new Vector3(-10.5f, 12f, 180f), new Vector3(11f, 12f, 180f), 5f);
        float[] bridgeZ = { -10f, 85f, 180f };

        // orillas: muro invisible a los dos lados del río (menos en los puentes) y juncos
        foreach (float sx in new[] { -1f, 1f })
        {
            var pts = new List<Vector3>();
            for (float z = FallsPondZ + 6f; z < FallsPool.y - 12f; z += 2f)
            {
                float x = FallsRiverX(z) + sx * 6.3f;
                pts.Add(new Vector3(x, FallsTier(z), z));
                if (rng.NextDouble() < 0.25 && Mathf.Abs(x) < 56f) Reeds(g, new Vector3(x - sx * 0.6f, FallsTier(z) - 1.1f, z));
            }
            BankGuard(lvl, pts, p =>
            {
                foreach (float bz in bridgeZ) if (Mathf.Abs(p.z - bz) < 3.2f) return true;
                return false;
            });
        }
        // el estanque del sur: muro alrededor menos por donde llega el río
        var pondRing = new List<Vector3>();
        for (int k = 0; k <= 36; k++) { float a = k / 36f * Mathf.PI * 2f; pondRing.Add(new Vector3(FallsPond.x + Mathf.Cos(a) * 9.6f, 0f, FallsPond.y + Mathf.Sin(a) * 9.6f)); }
        BankGuard(lvl, pondRing, p => p.z > FallsPondZ + 5f && Mathf.Abs(p.x - FallsRiverX(p.z)) < 7.5f);
        // la poza: muro alrededor menos por donde sale el río
        var pool = new List<Vector3>();
        for (int k = 0; k <= 36; k++)
        {
            float a = k / 36f * Mathf.PI * 2f;
            pool.Add(new Vector3(FallsPool.x + Mathf.Cos(a) * 14.3f, 16f, FallsPool.y + Mathf.Sin(a) * 14.3f));
        }
        BankGuard(lvl, pool, p => p.z > FallsCliffZ - 2f || (p.z < FallsPool.y - 9f && Mathf.Abs(p.x - FallsRiverX(p.z)) < 7f));

        // el sendero: del puente del vado hasta la poza, cruzando el río tres veces
        Trail(g, 3.6f,
            new Vector2(14f, -56f), new Vector2(12f, -30f), new Vector2(6f, -10f), new Vector2(-20f, -10f), new Vector2(-30f, 4f),
            new Vector2(-32f, 26f), new Vector2(-32f, 29f));
        Trail(g, 3.6f,
            new Vector2(-32f, 41f), new Vector2(-30f, 62f), new Vector2(-20f, 80f), new Vector2(-9f, 85f), new Vector2(17f, 85f),
            new Vector2(30f, 86f), new Vector2(32f, 89f));
        Trail(g, 3.6f,
            new Vector2(32f, 101f), new Vector2(28f, 118f), new Vector2(26f, 135f), new Vector2(30f, 146f), new Vector2(32f, 149f));
        Trail(g, 3.6f,
            new Vector2(32f, 161f), new Vector2(26f, 172f), new Vector2(13f, 180f), new Vector2(-12f, 180f), new Vector2(-26f, 188f),
            new Vector2(-30f, 199f));
        Trail(g, 3.6f,
            new Vector2(-30f, 211f), new Vector2(-24f, 222f), new Vector2(-18f, 230f));

        // primer bancal: casitas del valle y el molino junto al río
        House(g, new Vector3(34f, 0f, -38f), -90f, 7f, 5f, 3.6f, true);
        House(g, new Vector3(36f, 0f, -18f), -90f, 6f, 4.6f, 3.4f, false);
        Garden(g, new Vector3(26f, 0f, -26f), 6f, 3f);
        Well(g, new Vector3(24f, 0f, -44f));
        Clutter(g, new Vector3(28f, 0f, -34f));
        Mill(g, new Vector3(FallsRiverX(18f) + 7.6f, 0f, 18f));
        for (int i = 0; i < 4; i++) PortLamp(lvl, new Vector3(9f - i * 0.5f, 0f, -48f + i * 12f));

        // segundo bancal: ruinas de un acueducto sobre el campo
        for (int i = 0; i < 5; i++)
        {
            var arch = Group("Arco", g);
            arch.localPosition = new Vector3(-48f + i * 7f, 4f, 66f);
            Box(arch, "Pilar", new Vector3(-2.6f, 2.5f, 0f), new Vector3(1.2f, 5f, 1.4f), mStoneBrick, true);
            Box(arch, "Pilar", new Vector3(2.6f, 2.5f, 0f), new Vector3(1.2f, 5f, 1.4f), mStoneBrick, true);
            if (i % 2 == 0) Box(arch, "Dintel", new Vector3(0f, 5.4f, 0f), new Vector3(6.6f, 0.8f, 1.4f), mStoneBrick, false);
            Ivy(arch, new Vector3(-2.6f, R(1.5f, 4f), -0.75f), new Vector3(1.1f, R(1f, 2f), 0.3f));
        }
        blocked.Add(new Rect(-53f, 63f, 36f, 6f));

        // tercer bancal: campamento del mercader con vista a la cascada
        var camp = new Vector3(26f, 8f, 124f);
        MerchantPost(lvl, camp + new Vector3(-4f, 0f, 3f), 200f);
        HealShrine(lvl, camp + new Vector3(4.5f, 0f, -1f));
        Campfire(lvl, camp + new Vector3(0f, 0f, -1f));
        Tent(lvl, camp + new Vector3(8f, 0f, 5f), 20f, mCloth);
        Tent(lvl, camp + new Vector3(-9f, 0f, 6f), -30f, mClothB);

        // cuarto bancal: piedras del santuario del agua
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            var st = Box(g, "Menhir", new Vector3(-36f + Mathf.Cos(a) * 7f, 12f + 1.6f, 180f + Mathf.Sin(a) * 7f), new Vector3(1.2f, 3.2f, 0.9f), mStoneBrick, true);
            st.transform.localRotation = Quaternion.Euler(R(-4f, 4f), -a * Mathf.Rad2Deg, R(-4f, 4f));
        }
        Crystals(g, new Vector3(-36f, 12f, 180f), 1.2f, false, true);
        blocked.Add(new Rect(-45f, 171f, 18f, 18f));

        // combate: el vado, el acueducto, la ladera del campamento, el santuario y el Señor del Torrente junto a la poza
        Arena(lvl, "El prado del vado", new Vector3(-32f, 0f, 12f), new Vector2(22f, 18f), 4);
        Arena(lvl, "El acueducto", new Vector3(-30f, 4f, 76f), new Vector2(22f, 16f), 5);
        Arena(lvl, "La ladera", new Vector3(30f, 8f, 140f), new Vector2(22f, 14f), 5);
        Arena(lvl, "Las piedras del agua", new Vector3(-14f, 12f, 192f), new Vector2(22f, 10f), 5);
        var boss = Arena(lvl, "Jefe", new Vector3(-20f, 16f, 228f), new Vector2(26f, 18f), 1, true);
        Exit(lvl, new Vector3(-24f, 16f, 238f), boss);

        Chest(lvl, new Vector3(50f, 0f, 30f), 18, -90f);
        Chest(lvl, new Vector3(-50f, 8f, 150f), 24, 90f);
        Chest(lvl, new Vector3(48f, 12f, 205f), 30, 180f);
        Note(lvl, new Vector3(18f, 0f, -50f), 180f, LoreNote.Kind.Inscripcion, "dio_cascadas_valle", "Piedra del valle",
             "El río nace al fondo del valle, en las Gemelas, y baja cuatro bancales hasta aquí.\nSube contra la corriente: la salida está donde el agua cae más fuerte.");
        Note(lvl, new Vector3(-40f, 12f, 204f), 180f, LoreNote.Kind.Diario, "dio_cascadas_diario", "Ruido de agua",
             "Aquí no se oye la torre, solo el agua.\nLa otra vez llegué a las Gemelas sin descansar y no me fue bien. En la ladera del tercer bancal hay un campamento con mercader y un santuario: úsalos antes de seguir.", "— A.");

        WalkBoundary(FallsWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 30f, false, false, true, true, 260, false);
        Flat(bg, "AfueraSur", new Vector3(0f, 3f, FallsArea.yMin - 1250f + 1f), FallsArea.width, 2500f, mGrass, 0.25f);
        Flat(bg, "AfueraNorte", new Vector3(0f, FallsCliffY, FallsArea.yMax + 1250f - 1f), FallsArea.width, 2500f, mGrass, 0.25f);
        FarPeaks(bg, new Vector3(0f, 30f, 120f), 420f, -70f, 70f, 14, 120f, 200f, true);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 26f, 120f, 32f, 6f, new Vector3(0f, 8f, 60f), 2.6f, 10, intro: 5f);
        Shot(shots, "Vado", new Vector3(-12f, 0f, -10f), new Vector2(6f, 4f), 60f, 18f, 22f, 36f, 6f, new Vector3(4f, 0f, 6f), 1.8f, 2,
             "EL VADO", "", still: true);
        Shot(shots, "Molino", new Vector3(4f, 4f, 85f), new Vector2(8f, 4f), 300f, 18f, 24f, 36f, 6f, new Vector3(-2f, 0f, -8f), 1.8f, 2,
             "LAS CASCADAS DEL VALLE", "El agua baja de bancal en bancal");
        Shot(shots, "Campamento", camp, new Vector2(8f, 8f), 210f, 16f, 26f, 40f, 6f, new Vector3(-6f, 0f, -10f), 2f, 2,
             "EL CAMPAMENTO", "", still: true);
        Shot(shots, "Gemelas", new Vector3(-12f, 16f, 226f), new Vector2(10f, 10f), 40f, 6f, 30f, 46f, 14f, new Vector3(8f, 4f, 6f), 2.4f, 3,
             "LAS GEMELAS", "Dos cascadas, un solo río", still: true);
        FinishWorld();
    }

    /// <summary>Franja de agua que sigue la curva del río (a lo largo de Z) entre z0 y z1.</summary>
    Mesh ZRiverStrip(float z0, float z1, float y, float width)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int n = Mathf.Max(2, Mathf.CeilToInt((z1 - z0) / 1f));
        for (int i = 0; i <= n; i++)
        {
            float z = Mathf.Lerp(z0, z1, i / (float)n), x = FallsRiverX(z);
            v.Add(new Vector3(x - width / 2f, y, z)); v.Add(new Vector3(x + width / 2f, y, z));
            uv.Add(new Vector2(0f, z * 0.15f)); uv.Add(new Vector2(width * 0.15f, z * 0.15f));
            if (i < n) { int k = i * 2; t.AddRange(new[] { k, k + 2, k + 3, k, k + 3, k + 1 }); }
        }
        var m = new Mesh { name = "Rio" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Molino de agua: casa con rueda que gira junto al río.</summary>
    void Mill(Transform parent, Vector3 pos)
    {
        House(parent, pos + new Vector3(4f, 0f, 0f), 90f, 6f, 5f, 4f, true);
        var wheel = Group("Rueda", parent);
        wheel.localPosition = pos + new Vector3(-0.8f, 1.8f, 0f);
        wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);   // el eje Y de la rueda queda a lo largo de X
        for (int i = 0; i < 8; i++)
        {
            var paddle = Box(wheel, "Pala", Vector3.zero, new Vector3(3.6f, 0.6f, 0.12f), mPlank, false);
            paddle.transform.localRotation = Quaternion.Euler(0f, i * 22.5f, 0f);
        }
        Prim(wheel, PrimitiveType.Cylinder, "Eje", Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), mTimber);
        wheel.gameObject.AddComponent<EnemySpin>().speed = 25f;
        blocked.Add(new Rect(pos.x - 3f, pos.z - 5f, 11f, 10f));
    }
}
