using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 6 — El Lago de los Espejos (amanecer). Un pueblo de pescadores al sur, un lago enorme que refleja el cielo
/// rosa, pasarelas de tablones que saltan de isla en isla (el santuario con el mercader y un islote con ruinas), la
/// orilla norte y un risco en dos niveles con una cascada gigante que cae al lago. Arriba, el mirador donde espera la
/// Dama del Lago, con todo el lago a los pies.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect LakeArea = new Rect(-110f, -76f, 220f, 340f);
    static readonly Rect LakeWalk = new Rect(-60f, -62f, 120f, 300f);
    static readonly Vector3 LakeSpawn = new Vector3(0f, 0.1f, -54f);
    static readonly Vector2 LakeC = new Vector2(8f, 80f), LakeR = new Vector2(48f, 55f);
    static readonly Vector2 LakeIsle = new Vector2(10f, 70f), LakeIslet = new Vector2(10f, 106f);
    const float LakeWater = -1f, LakeCliffZ = 160f, LakeTopZ = 180f, LakeFallX = 30f, LakeTopY = 14f, LakeLedgeY = 7f;

    float LakeEll(float x, float z) { float a = (x - LakeC.x) / LakeR.x, b = (z - LakeC.y) / LakeR.y; return Mathf.Sqrt(a * a + b * b); }

    float LakeHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        bool calm = (Mathf.Abs(x + 15f) < 7f && Mathf.Abs(z - LakeCliffZ) < 14f) || (Mathf.Abs(x - 5f) < 7f && Mathf.Abs(z - LakeTopZ) < 14f) || Mathf.Abs(x - LakeFallX) < 9f;
        // pasillos de las dos escalinatas del risco
        if (Mathf.Abs(x + 15f) < 4.4f && z > LakeCliffZ - 13f && z <= LakeCliffZ) return 0f;
        if (Mathf.Abs(x - 5f) < 4.4f && z > LakeTopZ - 13f && z <= LakeTopZ) return LakeLedgeY;
        float sideW = Mathf.Abs(x) - 64f + e * 2f;
        float sideH = sideW <= 0f ? 0f : sideW < 6f ? 2.5f : sideW < 12f ? 5f : sideW < 20f ? 8f : 12f;
        float zc = LakeCliffZ + (calm ? 0f : e * 1.5f);
        if (z > zc)
        {
            if (Mathf.Abs(x - LakeFallX) < 4f) return LakeTopY - 1.4f;   // el río de arriba que alimenta la cascada
            float top = z > 240f + e * 2f ? LakeTopY + 3f : LakeTopY;
            if (x < 15f && z < LakeTopZ + (calm ? 0f : e * 1.2f)) top = LakeLedgeY;   // la cornisa de en medio
            return Mathf.Max(top, sideH);
        }
        if (Mathf.Abs(x - LakeFallX) < 6f + (calm ? 0f : e * 0.5f) && z > 118f) return -2.5f;   // ensenada al pie de la cascada
        float r = LakeEll(x, z) + e * 0.03f;
        if (r < 1f)
        {
            if (Vector2.Distance(new Vector2(x, z), LakeIsle) < 12f + e * 0.5f) return 0f;
            if (Vector2.Distance(new Vector2(x, z), LakeIslet) < 7f + e * 0.3f) return 0f;
            return r < 0.86f ? -3.5f : -2f;
        }
        if (z < -64f + e * 2f) return Mathf.Max(2.5f, sideH);
        return sideH;
    }

    void BuildLake()
    {
        BuildTerrain();
        var g = Group("Lago", root);
        var lvl = Group("Nivel", root);
        var water = Group("Agua", root);

        var lake = Flat(water, "Lago", new Vector3(8f, LakeWater, 90f), 112f, 142f, mRiver, 0.08f);
        lake.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        FlowFlat(water, "RioAlto", new Vector3(LakeFallX, LakeTopY - 0.7f, 212f), 8.2f, 104f, mRiver, 0.15f, new Vector2(0f, -0.6f));   // baja hacia la cascada gigante
        // la cascada gigante y su bruma
        Waterfall(water, new Vector3(LakeFallX, (LakeTopY - 0.7f + LakeWater) / 2f, LakeCliffZ), Vector3.back, 8.5f, LakeTopY - 0.7f - LakeWater);
        Foam(water, new Vector3(LakeFallX, LakeWater + 0.1f, LakeCliffZ - 2.5f), 6f, 50f);
        var mist = Group("Bruma", water);
        mist.localPosition = new Vector3(LakeFallX, 0f, LakeCliffZ - 4f);
        Smoke(mist, Vector3.zero, new Color(1f, 0.95f, 0.95f, 0.35f), false);
        // nenúfares y destellos en el agua
        for (int i = 0, tries = 0; i < 60 && tries < 2000; tries++)
        {
            var q = new Vector2(R(-40f, 56f), R(26f, 134f));
            float r = LakeEll(q.x, q.y);
            if (r < 0.75f || r > 0.97f) continue;
            var lily = Prim(water, PrimitiveType.Cylinder, "Lirio", new Vector3(q.x, LakeWater + 0.02f, q.y), new Vector3(R(0.6f, 1.1f), 0.01f, R(0.6f, 1.1f)), mLily);
            if (i % 4 == 0) Prim(lily.transform, PrimitiveType.Sphere, "Flor", new Vector3(0f, 8f, 0f), new Vector3(0.3f, 6f, 0.3f), mLilyFlower);
            i++;
        }

        // pasarelas: orilla oeste -> isla del santuario -> islote -> orilla norte
        PlankBridge(lvl, "PasarelaOeste", new Vector3(-43f, 0f, 60.5f), new Vector3(4.5f, 0f, 60.5f), 5f);
        PlankBridge(lvl, "PasarelaIslote", new Vector3(10f, 0f, 80f), new Vector3(10f, 0f, 101f), 5f);
        PlankBridge(lvl, "PasarelaNorte", new Vector3(10f, 0f, 111f), new Vector3(10f, 0f, 138f), 5f);
        // muelle de pesca en la orilla sur (con cofre en la punta)
        PlankBridge(lvl, "MuellePesca", new Vector3(21f, 0f, 21f), new Vector3(21f, 0f, 37f), 4.2f);
        Boat(g, new Vector3(25.5f, LakeWater + 0.05f, 32f), 4f);
        Boat(g, new Vector3(-12f, LakeWater + 0.05f, 63.5f), 3.6f);
        foreach (Transform b in g) if (b.name == "Barca") { var bob = b.gameObject.AddComponent<FloatingBob>(); bob.amplitude = 0.06f; bob.period = R(3f, 5f); bob.phase = R(0f, 5f); }
        for (int i = 0; i < 6; i++) PortLamp(lvl, new Vector3(-36f + i * 8f, 0.05f, 58.6f));
        for (int i = 0; i < 4; i++) PortLamp(lvl, new Vector3(7.8f, 0.05f, 84f + i * 16f));

        // orillas: muro invisible alrededor del agua, con hueco en cada pasarela
        var shore = new List<Vector3>();
        for (int k = 0; k <= 120; k++)
        {
            float a = k / 120f * Mathf.PI * 2f;
            var p = new Vector3(LakeC.x + Mathf.Cos(a) * LakeR.x * 1.03f, 0f, LakeC.y + Mathf.Sin(a) * LakeR.y * 1.03f);
            shore.Add(p);
            if (k % 3 == 0 && Mathf.Abs(p.z - 60.5f) > 5f) Reeds(g, p + (new Vector3(LakeC.x, 0f, LakeC.y) - p).normalized * 1.2f + Vector3.down * 0.9f);
        }
        BankGuard(lvl, shore, p => (Mathf.Abs(p.z - 60.5f) < 3.4f && p.x < -20f) || (Mathf.Abs(p.x - 10f) < 3.4f && p.z > 120f)
                                   || (Mathf.Abs(p.x - 21f) < 3f && p.z < 40f) || (Mathf.Abs(p.x - LakeFallX) < 7f && p.z > 110f));
        foreach (var isl in new[] { new Vector3(LakeIsle.x, 12.4f, LakeIsle.y), new Vector3(LakeIslet.x, 7.4f, LakeIslet.y) })
        {
            var ring = new List<Vector3>();
            for (int k = 0; k <= 40; k++) { float a = k / 40f * Mathf.PI * 2f; ring.Add(new Vector3(isl.x + Mathf.Cos(a) * isl.y, 0f, isl.z + Mathf.Sin(a) * isl.y)); }
            Vector3 c = isl;
            BankGuard(lvl, ring, p => (Mathf.Abs(p.z - 60.5f) < 3.4f && p.x < c.x) || (Mathf.Abs(p.x - 10f) < 3.4f));
        }
        // ensenada y río de arriba
        foreach (float sx in new[] { -1f, 1f })
        {
            BankGuard(lvl, new List<Vector3> { new Vector3(LakeFallX + sx * 6.4f, 0f, 124f), new Vector3(LakeFallX + sx * 6.4f, 0f, LakeCliffZ) }, p => false);
            float gx = LakeFallX + sx * 4.5f;
            BankGuard(lvl, new List<Vector3> { new Vector3(gx, LakeTopY, LakeCliffZ + 0.5f), new Vector3(gx, LakeTopY, 197.2f) }, p => false);
            BankGuard(lvl, new List<Vector3> { new Vector3(gx, LakeTopY, 202.8f), new Vector3(gx, LakeTopY, 238f) }, p => false);
        }
        // orilla del risco arriba de la ensenada (que nadie caiga al agua desde 15 m)
        BankGuard(lvl, new List<Vector3> { new Vector3(15f, LakeTopY, LakeCliffZ + 0.6f), new Vector3(25.5f, LakeTopY, LakeCliffZ + 0.6f) }, p => false);
        BankGuard(lvl, new List<Vector3> { new Vector3(34.5f, LakeTopY, LakeCliffZ + 0.6f), new Vector3(60f, LakeTopY, LakeCliffZ + 0.6f) }, p => false);

        // escalinatas del risco
        Stairs(lvl, "EscalinataOrilla", new Vector3(-15f, 0f, LakeCliffZ - 11f), new Vector3(-15f, LakeLedgeY, LakeCliffZ), 7f);
        Stairs(lvl, "EscalinataCornisa", new Vector3(5f, LakeLedgeY, LakeTopZ - 11f), new Vector3(5f, LakeTopY, LakeTopZ), 7f);

        // sendero
        Trail(g, 3.8f, new Vector2(0f, -56f), new Vector2(0f, -20f), new Vector2(0f, 8f), new Vector2(-20f, 18f), new Vector2(-46f, 36f),
              new Vector2(-50f, 54f), new Vector2(-44f, 60.5f));
        Trail(g, 3.6f, new Vector2(4f, 64f), new Vector2(10f, 70f), new Vector2(10f, 79f));
        Trail(g, 3.6f, new Vector2(10f, 100f), new Vector2(10f, 110f));
        Trail(g, 3.8f, new Vector2(10f, 139f), new Vector2(0f, 144f), new Vector2(-12f, 146f), new Vector2(-15f, 148f));
        Trail(g, 3.8f, new Vector2(-15f, 161f), new Vector2(-8f, 165f), new Vector2(5f, 166f), new Vector2(5f, 168f));
        Trail(g, 3.8f, new Vector2(5f, 181f), new Vector2(0f, 190f), new Vector2(-8f, 198f));
        Trail(g, 2.6f, new Vector2(0f, -4f), new Vector2(14f, 12f), new Vector2(21f, 20f));   // al muelle de pesca

        // el pueblo de pescadores
        House(g, new Vector3(-28f, 0f, -40f), 90f, 7f, 5f, 3.6f, true);
        House(g, new Vector3(-30f, 0f, -24f), 90f, 6f, 4.6f, 3.4f, false);
        House(g, new Vector3(27f, 0f, -38f), -90f, 7.5f, 5.5f, 3.8f, true);
        House(g, new Vector3(29f, 0f, -20f), -90f, 6f, 4.6f, 3.2f, true);
        Stall(g, new Vector3(-12f, 0f, -30f), 90f);
        Stall(g, new Vector3(12f, 0f, -26f), -90f);
        Well(g, new Vector3(9f, 0f, -44f));
        Garden(g, new Vector3(-16f, 0f, -48f), 7f, 3f);
        Clutter(g, new Vector3(-22f, 0f, -32f));
        Clutter(g, new Vector3(20f, 0f, -30f));
        foreach (var lp in new[] { new Vector3(-3f, 0f, -44f), new Vector3(3f, 0f, -30f), new Vector3(-3f, 0f, -16f), new Vector3(3f, 0f, 0f) }) PortLamp(lvl, lp);
        // redes tendidas y canoas en la orilla sur
        for (int i = 0; i < 3; i++) Boat(g, new Vector3(-17f + i * 4.2f, 0.05f, 26.2f - i * 0.4f), 3.4f);

        // isla del santuario: mercader, santuario, fogata y un cenador
        var isle = new Vector3(LakeIsle.x, 0f, LakeIsle.y);
        MerchantPost(lvl, isle + new Vector3(-5f, 0f, 4f), 120f);
        HealShrine(lvl, isle + new Vector3(5f, 0f, 3f));
        Campfire(lvl, isle + new Vector3(0f, 0f, -2f));
        // islote: ruinas de un cenador (columnas en círculo)
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 0.3f;
            if (i == 1 || i == 4) continue;   // por donde entran las pasarelas
            var p = new Vector3(LakeIslet.x + Mathf.Cos(a) * 5.6f, 0f, LakeIslet.y + Mathf.Sin(a) * 5.6f);
            Prim(lvl, PrimitiveType.Cylinder, "Columna", p + Vector3.up * (i % 2 == 0 ? 2f : 1.2f), new Vector3(0.9f, i % 2 == 0 ? 2f : 1.2f, 0.9f), mWhite, true);
        }
        Crystals(g, new Vector3(LakeIslet.x, 0f, LakeIslet.y), 0.8f, false, true);

        // la cornisa: un mirador con banca y farol
        var look = new Vector3(-30f, LakeLedgeY, 166f);
        Box(g, "Banca", look + new Vector3(0f, 0.4f, -2f), new Vector3(2.4f, 0.15f, 0.6f), mPlank, true);
        PortLamp(lvl, look + new Vector3(2f, 0f, -3f));
        var ledge = Group("Cornisa", g);
        ledge.localPosition = new Vector3(0f, LakeLedgeY, 0f);
        Fence(ledge, new Vector2(-58f, LakeCliffZ + 0.5f), new Vector2(-19.5f, LakeCliffZ + 0.5f));
        Fence(ledge, new Vector2(-10.5f, LakeCliffZ + 0.5f), new Vector2(14f, LakeCliffZ + 0.5f));
        // arriba: piedras antiguas al borde del mirador
        for (int i = 0; i < 5; i++)
            Box(g, "Menhir", new Vector3(-30f + (i % 2) * 1.5f, LakeTopY + 1.5f, 197f + i * 4.5f), new Vector3(1.1f, 3f, 0.8f), mStoneBrick, true);
        blocked.Add(new Rect(-48f, LakeTopZ - 1f, 40f, 22f));   // nada alto detrás del mirador (la toma mira al lago desde ahí)
        // puente sobre el río de arriba (al cofre del este)
        PlankBridge(lvl, "PuenteRioAlto", new Vector3(LakeFallX - 6.5f, LakeTopY, 200f), new Vector3(LakeFallX + 6.5f, LakeTopY, 200f), 5f);

        // combate
        Arena(lvl, "La pradera", new Vector3(0f, 0f, -2f), new Vector2(24f, 14f), 4);
        Arena(lvl, "La orilla oeste", new Vector3(-48f, 0f, 42f), new Vector2(18f, 18f), 5);
        Arena(lvl, "El islote", new Vector3(LakeIslet.x, 0f, LakeIslet.y), new Vector2(13f, 13f), 4);
        Arena(lvl, "La orilla norte", new Vector3(0f, 0f, 144f), new Vector2(18f, 10f), 5);
        var boss = Arena(lvl, "Jefe", new Vector3(-8f, LakeTopY, 205f), new Vector2(26f, 18f), 1, true);
        Exit(lvl, new Vector3(-8f, LakeTopY, 217f), boss);

        Chest(lvl, new Vector3(21f, 0.05f, 36f), 20, 0f);
        Chest(lvl, new Vector3(-52f, LakeLedgeY, 172f), 26, 90f);
        Chest(lvl, new Vector3(48f, LakeTopY, 200f), 30, 180f);
        Note(lvl, new Vector3(4f, 0f, -50f), 180f, LoreNote.Kind.Inscripcion, "dio_lago_pueblo", "Piedra de los pescadores",
             "Piso 6. El lago refleja un cielo que no es el de afuera.\nLos que se asoman dicen ver su casa. Mejor no quedarse mucho tiempo mirando.");
        Note(lvl, look + new Vector3(-2f, 0f, -1f), 180f, LoreNote.Kind.Diario, "dio_lago_diario", "Diario de la expedición — día 12",
             "Me asomé al lago y vi nuestra casa, Abby. La ventana de tu cuarto, con la luz prendida.\nEl capitán nos sacó de ahí a gritos. Dice que la torre te muestra lo que extrañas para que te quedes.", "— " + Historia.Hermano);

        WalkBoundary(LakeWalk);
        var bg = Group("Fondo", root);
        Sierra(bg, LakeArea, 12f, LakeTopY + 3f, 2.5f, 300, false);   // sierra continua (antes: llanos con cortes rectos)
        FarPeaks(bg, new Vector3(0f, 10f, 120f), 430f, -80f, 80f, 16, 110f, 190f, true);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 24f, 140f, 34f, 6f, new Vector3(0f, 6f, 70f), 2.6f, 10, intro: 5f);
        Shot(shots, "Muelle", new Vector3(21f, 0f, 33f), new Vector2(4f, 6f), 20f, 10f, 22f, 42f, 8f, new Vector3(0f, 1f, 12f), 2f, 2,
             "EL LAGO DE LOS ESPEJOS", "", still: true);
        Shot(shots, "Isla", isle, new Vector2(10f, 10f), 160f, 16f, 30f, 42f, 6f, new Vector3(0f, 0f, -6f), 2.2f, 2,
             "LA ISLA DEL SANTUARIO", "", still: true);
        Shot(shots, "Cascada", new Vector3(12f, 0f, 145f), new Vector2(10f, 8f), 55f, 8f, 30f, 44f, 12f, new Vector3(8f, 4f, 6f), 2.2f, 3,
             "LA CASCADA DEL ESPEJO", "", still: true);
        Shot(shots, "Mirador", look, new Vector2(8f, 6f), 180f, 28f, 32f, 46f, 2f, new Vector3(0f, -2f, -24f), 2.4f, 3,
             "EL MIRADOR", "Todo el lago a tus pies", still: true);
        FinishWorld();
    }
}
