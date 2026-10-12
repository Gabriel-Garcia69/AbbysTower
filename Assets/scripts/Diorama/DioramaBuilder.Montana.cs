using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 9 — La Montaña del Eco (día despejado). Se escala una montaña por un sendero en zigzag: siete rampas talladas
/// en la ladera unen ocho bancales, cada uno 6 m más alto. Abajo hay pinar y prados; a media subida un refugio con el
/// mercader; arriba la nieve, el viento y la cumbre donde espera el Eco de Piedra. Desde los miradores se ve el valle
/// que va quedando abajo.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect MountArea = new Rect(-100f, -82f, 200f, 334f);
    static readonly Rect MountWalk = new Rect(-56f, -64f, 112f, 300f);
    static readonly Vector3 MountSpawn = new Vector3(0f, 0.1f, -56f);
    static readonly float[] MountZ = { -24f, 8f, 40f, 72f, 104f, 136f, 168f };   // bordes de los bancales
    const float MountStep = 6f, MountSnowLine = 29.5f, MountPeakZ = 236f;

    // rampa k: sube por la cara del bancal (a lo largo de X) y termina en un descanso
    static float RampStart(int k) => k % 2 == 0 ? -44f : 44f;
    static float RampEnd(int k) => k % 2 == 0 ? -13.5f : 13.5f;
    static float RampLanding(int k) => k % 2 == 0 ? -8.5f : 8.5f;

    float MountHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        // rampas y descansos (sin ruido)
        for (int k = 0; k < MountZ.Length; k++)
        {
            float zb = MountZ[k], T = k * MountStep;
            if (z > zb - 7.2f && z <= zb)
            {
                if (Mathf.Abs(x - RampLanding(k)) < 5.9f) return T + MountStep;   // el descanso se mete bajo el final de la rampa (sin ranura)
                float lo = Mathf.Min(RampStart(k), RampEnd(k)) - 1.5f, hi = Mathf.Max(RampStart(k), RampEnd(k)) + 1.5f;
                if (x > lo && x < hi) return T;
            }
        }
        bool calm = false;
        foreach (float zb in MountZ) if (Mathf.Abs(z - zb) < 10f && Mathf.Abs(x) < 50f) calm = true;
        float zz = calm ? z : z + e * 1.8f;
        int n = 0; while (n < MountZ.Length && zz > MountZ[n]) n++;
        float t = n * MountStep;
        // la cumbre: picos que siguen subiendo detrás
        if (z > MountPeakZ + e * 2f) return t + (z < MountPeakZ + 10f ? 6f : z < MountPeakZ + 22f ? 13f : 22f) + (Noise(x * 0.07f, z * 0.07f) > 0.55f ? 4f : 0f);
        // hombros de la montaña a los lados
        float w = Mathf.Abs(x) - 58f + e * 2f;
        if (w > 0f) return t + Mathf.Min(5f * Mathf.Ceil(w / 5f), 40f) + (w > 15f && Noise(x * 0.06f, z * 0.06f) > 0.6f ? 3f : 0f);   // hombros en bancales de 5 m
        // al sur, el valle de abajo
        if (z < -66f + e * 2f) return z < -73f + e * 2f ? -9f : -4f;
        return t;
    }

    void BuildMountain()
    {
        BuildTerrain();
        var g = Group("Montana", root);
        var lvl = Group("Nivel", root);

        // las siete rampas de la subida
        for (int k = 0; k < MountZ.Length; k++)
        {
            float zc = MountZ[k] - 3.6f, T = k * MountStep;
            Stairs(lvl, "Rampa" + k, new Vector3(RampStart(k), T, zc), new Vector3(RampEnd(k), T + MountStep, zc), 6f);
            // barandal en el borde del descanso (del lado del vacío)
            var rail = Group("Barandal" + k, g);
            rail.localPosition = new Vector3(0f, T + MountStep, 0f);
            float xl = RampLanding(k);
            Fence(rail, new Vector2(xl - 4.6f, MountZ[k] - 7f), new Vector2(xl + 4.6f, MountZ[k] - 7f));
            Fence(rail, new Vector2(xl - Mathf.Sign(xl) * 4.6f, MountZ[k] - 7f), new Vector2(xl - Mathf.Sign(xl) * 4.6f, MountZ[k]));   // lado del vacío (la rampa llega por el otro)
        }

        // sendero en zigzag: de cada descanso a la siguiente rampa
        var pts = new List<Vector2> { new Vector2(0f, -58f), new Vector2(-10f, -46f), new Vector2(-30f, -40f), new Vector2(-50f, -34f), new Vector2(-50f, -27.6f), new Vector2(-45f, -27.6f) };
        Trail(g, 3.8f, pts.ToArray());
        for (int k = 0; k + 1 < MountZ.Length; k++)
        {
            float z0 = MountZ[k], z1 = MountZ[k + 1], xl = RampLanding(k), xs = RampStart(k + 1), mid = (z0 + z1 - 7f) / 2f;
            float far = Mathf.Sign(xs) * 50f;
            Trail(g, 3.8f, new Vector2(xl, z0 - 4f), new Vector2(xl, z0 + 3f), new Vector2(xl * 0.2f + far * 0.3f, mid), new Vector2(far, z1 - 10f),
                  new Vector2(far, z1 - 3.6f), new Vector2(xs + Mathf.Sign(xs) * -1f, z1 - 3.6f));
        }
        Trail(g, 3.8f, new Vector2(RampLanding(6), MountZ[6] - 4f), new Vector2(RampLanding(6), MountZ[6] + 6f), new Vector2(0f, 195f));

        // las faldas: prado con flores, cabaña de leñador, cercas de pastoreo
        House(g, new Vector3(30f, 0f, -48f), -90f, 7f, 5f, 3.6f, true);
        Garden(g, new Vector3(22f, 0f, -38f), 6f, 3f);
        var pasture = Group("Corral", g);
        Fence(pasture, new Vector2(-48f, -58f), new Vector2(-30f, -58f));
        Fence(pasture, new Vector2(-48f, -58f), new Vector2(-48f, -50f));
        Clutter(g, new Vector3(24f, 0f, -52f));
        for (int i = 0; i < 3; i++) PortLamp(lvl, new Vector3(4f, 0f, -52f + i * 9f));

        // refugio de montaña a media subida (bancal 3): mercader, santuario, fogata
        float hy = 3 * MountStep;
        var hut = new Vector3(-36f, hy, 55f);
        House(g, hut + new Vector3(-6f, 0f, 6f), 180f, 7f, 5.5f, 3.6f, true);
        MerchantPost(lvl, hut + new Vector3(4f, 0f, 2f), 200f);
        HealShrine(lvl, hut + new Vector3(8f, 0f, -4f));
        Campfire(lvl, hut + new Vector3(1f, 0f, -3f));
        Trail(g, 2.6f, new Vector2(-8.5f, MountZ[2] + 6f), new Vector2(-24f, 52f), new Vector2(-32f, 53f));

        // piedras con banderines (señales del camino) en cada descanso
        for (int k = 0; k < MountZ.Length; k++)
        {
            var c = new Vector3(RampLanding(k) + Mathf.Sign(RampLanding(k)) * 3f, (k + 1) * MountStep, MountZ[k] - 5.5f);
            Prim(g, PrimitiveType.Sphere, "Hito", c + Vector3.up * 0.4f, new Vector3(1f, 0.8f, 0.9f), mRock);
            Box(g, "Asta", c + Vector3.up * 1.6f, new Vector3(0.08f, 2.4f, 0.08f), mTimber, false);
            Box(g, "Banderin", c + new Vector3(0.35f, 2.5f, 0f), new Vector3(0.7f, 0.4f, 0.03f), k % 2 == 0 ? mRedPaint : mCloth, false);
        }

        // la nieve: cae arriba de la línea de nieve
        var snow = Group("Clima", root);
        snow.localPosition = new Vector3(0f, 36f, 160f);
        Snowfall(snow);
        // en lo alto: cristales de hielo y una estatua antigua del eco
        for (int i = 0; i < 10; i++)
        {
            float x = R(-50f, 50f), z = R(110f, 230f);
            float h = MountHeight(x, z);
            if (!Free(new Vector2(x, z), 2f) || ShadowsTrail(new Vector2(x, z), 10f)) continue;
            Crystals(g, new Vector3(x, h, z), R(0.5f, 0.9f), false, i % 3 == 0);
        }
        var statue = Group("Estatua", g);
        statue.localPosition = new Vector3(0f, 7 * MountStep, 222f);
        Box(statue, "Pedestal", new Vector3(0f, 0.8f, 0f), new Vector3(4f, 1.6f, 4f), mStoneBrick, true);
        Prim(statue, PrimitiveType.Capsule, "Figura", new Vector3(0f, 4.2f, 0f), new Vector3(1.6f, 2.6f, 1.2f), mWhite, true);
        Prim(statue, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 7.2f, 0f), Vector3.one * 1.3f, mWhite);
        blocked.Add(new Rect(-3f, 219f, 6f, 6f));

        // combate: las faldas, el paso de las cabras, la cornisa, la cresta helada y la cumbre
        Arena(lvl, "Las faldas", new Vector3(-24f, 0f, -44f), new Vector2(20f, 12f), 4);
        Arena(lvl, "El paso de las cabras", new Vector3(-18f, 2 * MountStep, 22f), new Vector2(22f, 12f), 5);
        Arena(lvl, "La cornisa", new Vector3(10f, 4 * MountStep, 86f), new Vector2(22f, 12f), 5);
        Arena(lvl, "La cresta helada", new Vector3(-16f, 6 * MountStep, 150f), new Vector2(22f, 12f), 6);
        var boss = Arena(lvl, "Jefe", new Vector3(0f, 7 * MountStep, 203f), new Vector2(28f, 20f), 1, true);
        Exit(lvl, new Vector3(-8f, 7 * MountStep, 216f), boss);

        Chest(lvl, new Vector3(50f, MountStep, -14f), 20, -90f);
        Chest(lvl, new Vector3(-52f, 4 * MountStep, 80f), 26, 90f);
        Chest(lvl, new Vector3(52f, 5 * MountStep, 112f), 30, -90f);
        Note(lvl, new Vector3(6f, 0f, -54f), 180f, LoreNote.Kind.Inscripcion, "dio_montana_pie", "Al pie de la montaña",
             "Grita tu nombre en la cumbre y la montaña te lo devolverá.\nSi te contesta con otra voz, es el Eco de Piedra. No le contestes: pelea.");
        Note(lvl, hut + new Vector3(-1f, 0f, 4f), 180f, LoreNote.Kind.Diario, "dio_montana_refugio", "Libro del refugio",
             "Firmé el libro del refugio. Mi nombre ya estaba ahí muchas veces, todas con mi letra y todas diciendo lo mismo: \"esta vez sí llego\".\nAsí que lo escribí otra vez. Esta vez sí llego.", "— A.");

        WalkBoundary(MountWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 40f, false, false, true, true, 200, true);
        // el valle de abajo (se ve desde los miradores): prados, un lago y bosque
        Flat(bg, "Valle", new Vector3(0f, -9f, MountArea.yMin - 350f + 1f), 1400f, 702f, mGrass, 0.25f);
        var vlake = Prim(bg, PrimitiveType.Cylinder, "LagoValle", new Vector3(-40f, -8.92f, -175f), new Vector3(100f, 0.01f, 64f), mRiver);
        vlake.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var shoreRing = Prim(bg, PrimitiveType.Cylinder, "OrillaValle", new Vector3(-40f, -8.97f, -175f), new Vector3(108f, 0.01f, 72f), mSand);
        shoreRing.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        EnsurePlantMeshes();
        for (int i = 0; i < 340; i++)
        {
            var q = new Vector2(R(-300f, 300f), R(-460f, -90f));
            float lx = (q.x + 40f) / 56f, lz = (q.y + 175f) / 38f;
            if (lx * lx + lz * lz < 1f) continue;
            if (q.y > -110f && Mathf.Abs(q.x) < 60f) continue;   // no tapar la base de la montaña
            if (rng.NextDouble() < 0.7) Pine(bg, q, -9f, R(1.3f, 2.4f), false); else Tree(bg, q, -9f, R(1.3f, 2f), false);
        }
        // lomas verdes en el valle
        for (int i = 0; i < 12; i++)
        {
            var q = new Vector3(R(-320f, 320f), -9f, R(-520f, -220f));
            var hill = MeshObject(bg, "Loma", blobMeshes[rng.Next(blobMeshes.Length)], mGrass, false);
            hill.transform.localPosition = q;
            hill.transform.localScale = new Vector3(R(60f, 120f), R(14f, 30f), R(50f, 90f));
        }
        House(bg, new Vector3(30f, -9f, -150f), 30f, 7f, 5f, 3.6f, true);
        House(bg, new Vector3(44f, -9f, -170f), -20f, 6f, 5f, 3.4f, false);
        Flat(bg, "Cumbres", new Vector3(0f, 7 * MountStep + 22f, MountArea.yMax + 349f), MountArea.width, 700f, mSnowPix, 0.25f);
        FarPeaks(bg, new Vector3(0f, 30f, 150f), 380f, -95f, 95f, 18, 160f, 260f, true);
        FarPeaks(bg, new Vector3(0f, -9f, -150f), 620f, 115f, 245f, 12, 70f, 140f, false);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 30f, 150f, 34f, 4f, new Vector3(0f, 20f, 90f), 2.6f, 10, intro: 5f);
        // miradores: la cámara voltea al valle que va quedando abajo
        Shot(shots, "Mirador1", new Vector3(RampLanding(1), 2 * MountStep, MountZ[1] - 3.6f), new Vector2(8f, 6f), 180f, 20f, 34f, 44f, 2f, new Vector3(0f, -4f, -30f), 2.4f, 3,
             "EL VALLE", "Lo que dejas abajo", still: true);
        Shot(shots, "Refugio", hut, new Vector2(10f, 8f), 200f, 16f, 26f, 40f, 6f, new Vector3(-4f, 0f, -8f), 2f, 2,
             "EL REFUGIO", "", still: true);
        Shot(shots, "Mirador2", new Vector3(RampLanding(4), 5 * MountStep, MountZ[4] - 3.6f), new Vector2(8f, 6f), 170f, 24f, 40f, 46f, 0f, new Vector3(0f, -10f, -40f), 2.4f, 3,
             "MÁS ALTO QUE LAS NUBES", "", still: true);
        Shot(shots, "Cumbre", new Vector3(0f, 7 * MountStep, 190f), new Vector2(12f, 6f), 0f, 14f, 34f, 42f, 10f, new Vector3(0f, 4f, 14f), 2.4f, 2,
             "LA CUMBRE DEL ECO", "");
        FinishWorld();
    }
}
