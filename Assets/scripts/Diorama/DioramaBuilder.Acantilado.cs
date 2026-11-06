using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piso 4 — El Sendero del Acantilado (atardecer carmesí). Costa de bloques, como el piso 1: un prado baja por una escalinata a una playa con aldea de
/// pescadores y el esqueleto de un barco; un puente cruza a la isla del naufragio y otro, largo, llega al promontorio del faro roto.
/// Una cascada cae de la meseta a una laguna que desagua al mar; arcos y farallones de roca recortan el sol.
/// La cámara mira al mar desde tierra: lo alto queda a los lados, nunca entre ella y el camino.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect CliffArea = new Rect(-90f, -70f, 190f, 150f);
    static readonly Rect CliffWalk = new Rect(-66f, -46f, 150f, 112f);
    static readonly Vector3 CliffSpawn = new Vector3(-46f, 3.6f, -30f);
    const float PlayaY = 0.6f, PradoY = 2.6f, MesetaY = 5.5f, IsletaY = 1.4f, RellanoY = 2.2f, LagunaY = 0.1f;
    static readonly Vector2 LagunaC = new Vector2(25f, -13f), IsletaC = new Vector2(4f, 31f), RellanoC = new Vector2(43f, 37f), FaroC = new Vector2(68f, 48f);
    static readonly Vector2 RampaA = new Vector2(46f, 37f), RampaB = new Vector2(60f, 42f);
    static readonly Vector2 Puente1A = new Vector2(1f, 4f), Puente1B = new Vector2(3f, 19.5f), Puente2A = new Vector2(16f, 33.4f), Puente2B = new Vector2(37f, 36.5f);
    static readonly Vector2 EscalaA = new Vector2(1f, -2f), EscalaB = new Vector2(1f, -9f);   // de la playa al prado
    // el camino de tierra
    static readonly Vector2[][] CliffRoad =
    {
        new[] { new Vector2(-46f, -30f), new Vector2(-34f, -24f), new Vector2(-20f, -19f), new Vector2(-6f, -13f), EscalaB },
        new[] { EscalaA, Puente1A },
        new[] { EscalaA, new Vector2(-14f, 0f), new Vector2(-30f, 6f) },
        new[] { Puente1B, new Vector2(9f, 28f), Puente2A },
        new[] { Puente2B, RampaA },
        new[] { RampaB, new Vector2(64f, 44f) },
    };

    static float Liso(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, (x - a) / (b - a));

    static float DistSeg(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    static float CostaZ(float x)
    {
        float c = (x - 4f) / 30f;
        return 24f - 16f * Mathf.Exp(-c * c) + 18f * Liso(50f, 64f, x) + 9f * Liso(40f, 72f, -x);
    }

    static float DistCamino(float x, float z)
    {
        float best = 999f; var p = new Vector2(x, z);
        foreach (var r in CliffRoad)
            for (int i = 0; i + 1 < r.Length; i++) best = Mathf.Min(best, DistSeg(p, r[i], r[i + 1], out _));
        return best;
    }

    /// <summary>Alturas por escalones, igual que el piso 1: cada celda del terreno es un bloque.</summary>
    float CliffHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        var p = new Vector2(x, z);
        // los pasillos de las escaleras van primero: su suelo es recto
        if (Mathf.Abs(x - EscalaA.x) < 4f && z <= EscalaA.y + 1f && z > EscalaB.y) return PlayaY;
        if (Mathf.Abs(x - EscalaA.x) < 6f && z <= EscalaB.y && z > EscalaB.y - 6f) return PradoY;
        if (DistSeg(p, RampaA, RampaB, out float tr) < 3.6f && tr < 0.98f) return RellanoY;
        // islas: la del naufragio, el rellano y el promontorio del faro
        if (Vector2.Distance(p, IsletaC) < 13f + e * 2f) return IsletaY;
        if (Vector2.Distance(p, IsletaC) < 15f + e * 2f) return PlayaY;
        if (Vector2.Distance(p, RellanoC) < 7f + e * 0.8f) return RellanoY;
        if (Vector2.Distance(p, new Vector2(66f, 46f)) < 17f + e * 2f) return MesetaY;
        float d = CostaZ(x) + e * 2.5f - z;
        if (d < 0f) return d > -5f ? -4f : d > -12f ? -6f : -8f;
        // la laguna al pie de la cascada y su desagüe al mar
        if (Vector2.Distance(p, LagunaC) < 8.5f + e) return -0.8f;
        if (Mathf.Abs(x - LagunaC.x) < 2.4f && z > LagunaC.y) return -0.8f;
        // la meseta del este
        if (x > 34f + e * 2f)
        {
            if (Mathf.Abs(z + 13f) < 2.4f) return MesetaY - 1.1f;   // el arroyo de arriba
            return MesetaY + Lomas(x, z);
        }
        if (d < 16f + e) return PlayaY;
        return PradoY + Lomas(x, z);
    }

    /// <summary>Afuera del mapa: lomas al sur y al este, también por escalones.</summary>
    static float Lomas(float x, float z)
    {
        float k = Mathf.Max(Liso(70f, 220f, -z) * 11f, Liso(110f, 260f, x) * 8f);
        return k <= 0f ? 0f : Mathf.Round(k * (0.45f + 0.55f * Noise(x * 0.04f + 3f, z * 0.04f)) / 2f) * 2f;
    }

    Material mArenaCosta;
    Material ArenaCosta()
    {
        if (mArenaCosta == null) mArenaCosta = PixelMat("Dio_ArenaCosta2", "pix_sand", new Color(0.36f, 0.3f, 0.32f), 0.05f);
        return mArenaCosta;
    }

    /// <summary>Peñasco de la costa, con la roca del mapa.</summary>
    void PenascoCosta(Transform g, Vector3 pos, float radio)
    {
        var m = MeshObject(g, "Penasco", RocaIrregular(radio, radio * R(0.8f, 1.4f), k => Mathf.Sin(Mathf.PI * Mathf.Clamp(k, 0.04f, 0.96f)) * 0.85f + 0.15f, 9, 6), mRock, true);
        m.transform.localPosition = pos + Vector3.down * radio * 0.15f;
        m.transform.localRotation = Quaternion.Euler(R(-10f, 10f), R(0f, 360f), R(-10f, 10f));
    }

    bool EnAgua(Vector2 q) => CliffHeight(q.x, q.y) < 0.1f;

    /// <summary>¿Hay lugar para plantar? Lejos del camino, en suelo de pasto y sin mucha pendiente.</summary>
    bool SueloDePasto(Vector2 q, out float h)
    {
        h = CliffHeight(q.x, q.y);
        return h >= PradoY && DistCamino(q.x, q.y) > 4.5f && Free(q, 1f) && CliffHeight(q.x + 1.5f, q.y) == h && CliffHeight(q.x, q.y + 1.5f) == h;
    }

    Vector3 EnSuelo(float x, float z) => new Vector3(x, CliffHeight(x, z), z);

    void BuildCliffs()
    {
        BuildTerrain();
        var bg = Group("Fondo", root);
        // afuera, la misma costa con bloques más grandes
        foreach (var r in new[] { new Rect(-330f, -350f, 240f, 800f), new Rect(100f, -350f, 330f, 800f), new Rect(-90f, -350f, 190f, 280f), new Rect(-90f, 80f, 190f, 370f) })
            BuildOuterTerrain(bg, r, 6f);
        BuildSea();
        var g = Group("Acantilado", root);
        var lvl = Group("Nivel", root);
        foreach (var r in new[] { new Rect(-54f, -38f, 16f, 14f), new Rect(-6f, -10f, 14f, 16f), new Rect(44f, 33f, 18f, 12f) }) blocked.Add(r);
        foreach (var r in CliffRoad) for (int i = 0; i + 1 < r.Length; i++) AnotarTramo(PisoSendero(), r[i], r[i + 1], 4.4f);

        Agua(g);
        Stairs(lvl, "EscalaPrado", new Vector3(EscalaA.x, PlayaY, EscalaA.y), new Vector3(EscalaB.x, PradoY, EscalaB.y), 7f);
        Stairs(lvl, "EscalaFaro", new Vector3(RampaA.x, RellanoY, RampaA.y), new Vector3(RampaB.x, MesetaY, RampaB.y), 6f);
        Puentes(lvl);
        Aldea(g, lvl);
        Naufragio(g, lvl);
        Farallones(g);
        Faro(g, lvl);
        Vegetacion(g);
        Orilla(lvl);

        // combate
        Arena(lvl, "El camino del prado", EnSuelo(-28f, -22f), new Vector2(18f, 12f), 5);
        Arena(lvl, "La playa de los naufragios", EnSuelo(-4f, -2f), new Vector2(18f, 11f), 5);
        Arena(lvl, "La isla del naufragio", EnSuelo(-1f, 30f), new Vector2(13f, 12f), 4);
        var boss = Arena(lvl, "Jefe", EnSuelo(60f, 44f), new Vector2(15f, 13f), 1, true);
        Exit(lvl, EnSuelo(64f, 53f), boss);
        Chest(lvl, EnSuelo(46f, -28f), 26, 180f);
        Chest(lvl, EnSuelo(-4f, 37f), 22, 200f);
        Note(lvl, EnSuelo(-42f, -36f), 180f, LoreNote.Kind.Diario, "dio3_diario", "Diario de la expedición — día 6",
             "En este piso el sol nunca termina de ponerse. Cruzamos los puentes colgantes de uno en uno.\nLa criatura del faro salta desde lo alto: cuando su sombra cae al suelo, hay que moverse.", "— " + Historia.Hermano);
        Note(lvl, EnSuelo(58f, 50f), 0f, LoreNote.Kind.Inscripcion, "dio3_faro", "Placa del faro",
             "Este faro alumbra un mar que no existe afuera de la torre.\nNadie sabe quién lo encendió. Se apaga cuando una criatura fuerte anda cerca.");

        WalkBoundary(CliffWalk);
        ArbolesLejos(bg);
        Ship(bg, new Vector3(-90f, SeaY, 170f), 110f, 24f, 6f, false);
        Gaviotas(bg);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 180f, 22f, 95f, 32f, 4f, new Vector3(0f, 6f, -8f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Playa", new Vector3(-6f, PlayaY, -4f), new Vector2(14f, 8f), 12f, 15f, 24f, 40f, 7f, new Vector3(2f, 1f, 10f), 2.2f, 2,
             "LA PLAYA DE LOS NAUFRAGIOS", "El mar de la torre no devuelve nada", still: true);
        Shot(shots, "Puente", new Vector3((Puente2A.x + Puente2B.x) / 2f, IsletaY, (Puente2A.y + Puente2B.y) / 2f), new Vector2(18f, 6f), 330f, 12f, 22f, 44f, 8f, new Vector3(0f, 2f, 4f), 2f, 3,
             "EL PUENTE LARGO", "");
        Shot(shots, "Ocaso", new Vector3(IsletaC.x, IsletaY, IsletaC.y), new Vector2(10f, 8f), 15f, 8f, 14f, 46f, 9f, new Vector3(0f, 2f, 6f), 2.2f, 2,
             "EL OCASO", "El sol se hunde en el mar de la torre", still: true);
        Shot(shots, "Cascada", new Vector3(LagunaC.x - 9f, PlayaY, LagunaC.y + 2f), new Vector2(8f, 10f), 75f, 12f, 16f, 42f, 5f, new Vector3(4f, 1f, 0f), 2f, 2,
             "LA CASCADA DE LA MESETA", "", still: true);
        Shot(shots, "Faro", new Vector3(62f, MesetaY, 48f), new Vector2(10f, 8f), 35f, 14f, 18f, 44f, 7f, new Vector3(3f, 3f, 2f), 2.2f, 2,
             "EL FARO ROTO", "", still: true);
        FinishWorld(false, true);
    }

    void Agua(Transform g)
    {
        // el arroyo de la meseta, la cascada, la laguna y el desagüe
        float arriba = MesetaY - 0.6f, xFall = 33f;
        var rio = FlowFlat(g, "ArroyoAlto", new Vector3(xFall + 22f, arriba, -13f), 3.8f, 44f, mRiver, 0.2f, new Vector2(0f, 0.5f));
        rio.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        Waterfall(g, new Vector3(xFall, (arriba + LagunaY) / 2f, -13f), Vector3.left, 4.2f, arriba - LagunaY + 0.2f);
        Prim(g, PrimitiveType.Cylinder, "Laguna", new Vector3(LagunaC.x, LagunaY, LagunaC.y), new Vector3(19f, 0.005f, 19f), mRiver);
        float zMar = CostaZ(25f);
        FlowFlat(g, "Desague", new Vector3(25f, LagunaY - 0.02f, (LagunaC.y + zMar - 0.5f) / 2f), 4.8f, zMar - 0.5f - LagunaC.y, mRiver, 0.2f, new Vector2(0f, 0.6f));
        Waterfall(g, new Vector3(25f, (LagunaY + SeaY) / 2f, zMar - 0.5f), Vector3.forward, 4.8f, LagunaY - SeaY + 0.2f);
        Foam(g, new Vector3(xFall - 2f, LagunaY + 0.05f, -13f), 3f, 26f);
        Foam(g, new Vector3(25f, SeaY + 0.1f, zMar + 1f), 2.5f, 18f);
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f + R(-0.15f, 0.15f), rr = R(9f, 11f);
            PenascoCosta(g, EnSuelo(LagunaC.x + Mathf.Cos(a) * rr, LagunaC.y + Mathf.Sin(a) * rr), R(0.6f, 1.5f));
        }
        blocked.Add(new Rect(LagunaC.x - 12f, LagunaC.y - 12f, 24f, 24f));
    }

    void Puentes(Transform lvl)
    {
        RopeBridge(lvl, "Puente1", EnSuelo(Puente1A.x, Puente1A.y), EnSuelo(Puente1B.x, Puente1B.y), 5f);
        RopeBridge(lvl, "Puente2", EnSuelo(Puente2A.x, Puente2A.y), EnSuelo(Puente2B.x, Puente2B.y), 5f);
        foreach (var r in CliffRoad)
            for (int i = 0; i + 1 < r.Length; i++)
            {
                float largo = Vector2.Distance(r[i], r[i + 1]);
                Vector2 dir = (r[i + 1] - r[i]) / largo, lado = new Vector2(-dir.y, dir.x);
                for (float s = 4f; s < largo; s += 12f)
                {
                    float sx = Mathf.Repeat(s, 24f) < 12f ? 1f : -1f;
                    Vector2 q = r[i] + dir * s + lado * sx * 3.2f;
                    if (!EnAgua(q)) PortLamp(lvl, EnSuelo(q.x, q.y));
                }
            }
    }

    void Aldea(Transform g, Transform lvl)
    {
        // caserío de pescadores al oeste de la playa, de espaldas al prado y mirando al mar
        House(g, EnSuelo(-35f, 0f), 180f, 6.5f, 5f, 3.6f, true);
        House(g, EnSuelo(-46f, 5f), 165f, 6f, 4.6f, 3.4f, true);
        House(g, EnSuelo(-24f, -3f), 190f, 5.5f, 4.4f, 3.2f, false);
        House(g, EnSuelo(-58f, 11f), 155f, 6f, 4.6f, 3.4f, true);
        blocked.Add(new Rect(-58f, -4f, 40f, 20f));
        Clutter(g, EnSuelo(-30f, 4f));
        Clutter(g, EnSuelo(-40f, 7f));
        // redes tendidas y barcas en la arena
        foreach (var q in new[] { new Vector2(-15f, 8f), new Vector2(-26f, 11f), new Vector2(-36f, 14f) })
        {
            var b = Group("BarcaVarada", g);
            b.localPosition = EnSuelo(q.x, q.y);
            b.localRotation = Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(6f, 14f));
            Boat(b, Vector3.zero, R(3.4f, 4.4f));
        }
        for (int k = 0; k < 3; k++)
        {
            var t = Group("Secadero", g);
            t.localPosition = EnSuelo(-46f + k * 9f, -8f + k * 2f);
            t.localRotation = Quaternion.Euler(0f, R(-15f, 15f), 0f);
            foreach (float sx in new[] { -2.2f, 2.2f }) Box(t, "Poste", new Vector3(sx, 1.1f, 0f), new Vector3(0.18f, 2.2f, 0.18f), mTimber, false);
            Box(t, "Travesano", new Vector3(0f, 2.1f, 0f), new Vector3(4.6f, 0.12f, 0.12f), mTimber, false);
            Box(t, "Red", new Vector3(0f, 1.3f, 0f), new Vector3(4.2f, 1.5f, 0.05f), mClothB, false);
        }
        MerchantPost(lvl, EnSuelo(-18f, -8f), 160f);
        // el campamento donde empieza Abby
        Tent(lvl, EnSuelo(-52f, -34f), 70f, mClothB);
        Campfire(lvl, EnSuelo(-48f, -36f));
        Tent(lvl, EnSuelo(-43f, -41f), 20f, mCloth);
        HealShrine(lvl, EnSuelo(-40f, -26f));
        HealShrine(lvl, EnSuelo(12f, 27f));
    }

    void Naufragio(Transform g, Transform lvl)
    {
        // el barco encallado en la isla, partido y ladeado
        var w = Group("Naufragio", g);
        w.localPosition = EnSuelo(9f, 38f) + Vector3.down * 0.5f;
        w.localRotation = Quaternion.Euler(-9f, 28f, 17f);
        Ship(w, Vector3.zero, 0f, 18f, 6f, false);
        SolidBox(lvl, "Collider_Naufragio", EnSuelo(9f, 38f) + Vector3.up * 2f, new Vector3(9f, 5f, 15f));
        blocked.Add(new Rect(1f, 28f, 16f, 18f));
        // el esqueleto de otro barco en la playa: costillas y quilla
        var c = Group("Costillar", g);
        c.localPosition = EnSuelo(-13f, 4f);
        c.localRotation = Quaternion.Euler(0f, 62f, 0f);
        Box(c, "Quilla", new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.35f, 15f), mTimber, true);
        for (int k = 0; k < 8; k++)
            foreach (float sx in new[] { -1f, 1f })
            {
                if (R(0f, 1f) < 0.12f) continue;   // le faltan algunas
                float z = -6.3f + k * 1.8f, alto = 1f + Mathf.Sin(k / 7f * Mathf.PI) * 0.4f;
                for (int s = 0; s < 5; s++)
                {
                    float a = s / 4f * 1.35f;
                    var b = Box(c, "Costilla", new Vector3(sx * Mathf.Sin(a) * 2.3f * alto, Mathf.Sin(a * 0.5f + 0.1f) * 3.6f * alto, z), new Vector3(0.26f, 1.35f, 0.3f), mTimber, false);
                    b.transform.localRotation = Quaternion.Euler(0f, 0f, -sx * (a * Mathf.Rad2Deg * 0.75f - 8f));
                }
            }
        SolidBox(lvl, "Collider_Costillar", c.localPosition + Vector3.up, new Vector3(5f, 2f, 5f));
    }

    Mesh RocaMar(float r, float alto) => RocaIrregular(r, alto, k => 1f - k * 0.55f + Mathf.Sin(k * 9f) * 0.06f, 12, 9);

    void Farallones(Transform g)
    {
        var f = Group("Farallones", g);
        foreach (var s in new[] { new Vector4(-50f, 50f, 5.5f, 17f), new Vector4(-12f, 62f, 7f, 24f), new Vector4(30f, 62f, 6f, 19f), new Vector4(-66f, 40f, 4f, 11f),
                                  new Vector4(92f, 58f, 7f, 22f), new Vector4(-82f, 70f, 9f, 28f), new Vector4(48f, 86f, 8f, 25f), new Vector4(18f, 54f, 3f, 8f), new Vector4(-34f, 72f, 5f, 15f) })
        {
            var m = MeshObject(f, "Farallon", RocaMar(s.z, s.w), mRock, true);
            m.transform.localPosition = new Vector3(s.x, SeaY - 1f, s.y);
            m.transform.localRotation = Quaternion.Euler(R(-4f, 4f), R(0f, 360f), R(-4f, 4f));
            Foam(f, new Vector3(s.x, SeaY + 0.05f, s.y), s.z * 0.9f, 8f);
        }
        // el arco: rocas en semicírculo, con la del centro colgando
        Arco(f, new Vector3(-30f, SeaY, 50f), 13f, 15f, 15f);
        Arco(f, new Vector3(70f, SeaY, 84f), 11f, 12f, -20f);
    }

    void Arco(Transform p, Vector3 c, float rx, float ry, float yaw)
    {
        var a = Group("Arco", p);
        a.localPosition = c;
        a.localRotation = Quaternion.Euler(0f, yaw, 0f);
        const int n = 15;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n * Mathf.PI, grueso = Mathf.Lerp(5.4f, 3f, Mathf.Sin(t)) * R(0.9f, 1.15f);
            var m = MeshObject(a, "RocaArco", RocaIrregular(grueso, grueso * 1.7f, k => Mathf.Sin(Mathf.PI * Mathf.Clamp(k, 0.04f, 0.96f)) * 0.9f + 0.1f, 10, 7), mRock, true);
            m.transform.localPosition = new Vector3(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry - 1f, R(-0.6f, 0.6f));
            m.transform.localRotation = Quaternion.Euler(R(-12f, 12f), R(0f, 360f), R(-12f, 12f));
        }
        foreach (float sx in new[] { -1f, 1f }) Foam(a, new Vector3(sx * rx, 0.05f, 0f), 5f, 10f);
    }

    void Faro(Transform g, Transform lvl)
    {
        var tower = Group("FaroRoto", g);
        tower.localPosition = new Vector3(FaroC.x, MesetaY - 0.1f, FaroC.y);
        tower.localScale = Vector3.one * 1.7f;
        Box(tower, "Zocalo", new Vector3(0f, 0.4f, 0f), new Vector3(5f, 0.8f, 5f), mStoneBrick, true);
        float y = 0.8f;
        for (int i = 0; i < 4; i++)
        {
            Prim(tower, PrimitiveType.Cylinder, "Tramo", new Vector3(0f, y + 1.3f, 0f), new Vector3(3.6f - i * 0.28f, 1.3f, 3.6f - i * 0.28f), i % 2 == 0 ? mWhite : mRedPaint, true);
            y += 2.6f;
        }
        // borde roto: dovelas de alturas desiguales sobre el último tramo
        float rTop = (3.6f - 3 * 0.28f) / 2f - 0.15f;
        for (int i = 0; i < 6; i++)
        {
            float alto = R(0.6f, 2f), inc = R(-10f, 10f);
            for (int k = 0; k < 3; k++)
            {
                float a = (i * 3 + k) / 18f * Mathf.PI * 2f, hk = alto * (k == 1 ? 1f : 0.65f);
                var b = Box(tower, "Diente", new Vector3(Mathf.Cos(a) * rTop, y + hk / 2f, Mathf.Sin(a) * rTop), new Vector3(0.6f, hk, 0.35f), mWhite, false);
                b.transform.localRotation = Quaternion.Euler(0f, -90f - a * Mathf.Rad2Deg, k == 1 ? inc * 0.3f : 0f);
            }
        }
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
        Box(tower, "Puerta", new Vector3(0f, 1.9f, -1.62f), new Vector3(1f, 1.9f, 0.2f), mDoor, false);
        foreach (float yy in new[] { 3.6f, 6.2f, 8.8f })
            Box(tower, "Ventanuco", new Vector3(0f, yy, -1.55f + (yy - 3.6f) * 0.05f), new Vector3(0.35f, 0.7f, 0.1f), mWindow, false);
        for (int i = 0; i < 10; i++)
        {
            var r = Box(tower, "Escombro", new Vector3(R(-5f, 3f), 0.25f, R(-5f, 1f)), new Vector3(R(0.5f, 1.2f), R(0.3f, 0.7f), R(0.5f, 1f)), i % 2 == 0 ? mWhite : mStoneBrick, true);
            r.transform.localRotation = Quaternion.Euler(R(-15f, 15f), R(0f, 90f), R(-15f, 15f));
        }
        foreach (var q in new[] { new Vector2(58f, 40f), new Vector2(70f, 40f), new Vector2(56f, 52f) }) Brazier(lvl, EnSuelo(q.x, q.y));
        blocked.Add(new Rect(FaroC.x - 6f, FaroC.y - 6f, 12f, 12f));
    }

    void Vegetacion(Transform g)
    {
        var a = Group("Arboles", g);
        // prado del suroeste y meseta; junto al mar, palmeras
        for (int i = 0, tries = 0; i < 34 && tries < 900; tries++)
        {
            var q = new Vector2(R(-80f, 32f), R(-68f, -14f));
            if (q.y > -30f && q.x > -12f) continue;   // el prado que baja a la playa queda abierto
            if (!SueloDePasto(q, out float h)) continue;
            if (rng.NextDouble() < 0.5) Pine(a, q, h - 0.1f, R(0.9f, 1.5f), true); else Tree(a, q, h - 0.1f, R(0.9f, 1.4f), true);
            blocked.Add(new Rect(q.x - 2f, q.y - 2f, 4f, 4f));
            i++;
        }
        for (int i = 0, tries = 0; i < 26 && tries < 900; tries++)
        {
            var q = new Vector2(R(38f, 90f), R(-66f, 18f));
            if (Mathf.Abs(q.y + 13f) < 6f || !SueloDePasto(q, out float h)) continue;
            if (rng.NextDouble() < 0.7) Pine(a, q, h - 0.1f, R(1f, 1.7f), true); else Tree(a, q, h - 0.1f, R(1f, 1.4f), true);
            blocked.Add(new Rect(q.x - 2f, q.y - 2f, 4f, 4f));
            i++;
        }
        foreach (var q in new[] { new Vector2(10f, -4f), new Vector2(14f, 2f), new Vector2(-6f, 8f), new Vector2(18f, -2f), new Vector2(-2f, 40f), new Vector2(-8f, 28f), new Vector2(14f, 24f), new Vector2(-14f, -2f), new Vector2(38f, 40f), new Vector2(47f, 32f) })
            if (CliffHeight(q.x, q.y) > 0.2f && DistCamino(q.x, q.y) > 3f) Palm(a, EnSuelo(q.x, q.y), R(0.9f, 1.3f));
        for (int i = 0, tries = 0; i < 40 && tries < 900; tries++)
        {
            var q = new Vector2(R(-70f, 90f), R(-60f, 60f));
            if (!SueloDePasto(q, out float h)) continue;
            Bush(a, new Vector3(q.x, h, q.y), R(0.7f, 1.3f));
            i++;
        }
        // peñascos: en las orillas de la playa y en el risco
        for (int i = 0, tries = 0; i < 46 && tries < 1200; tries++)
        {
            var q = new Vector2(R(-66f, 84f), R(-44f, 62f));
            float h = CliffHeight(q.x, q.y);
            if (DistCamino(q.x, q.y) < 4.5f || !Free(q, 1.5f) || h < -1.2f) continue;
            bool cercaAgua = h <= PlayaY && (CliffHeight(q.x + 3f, q.y) < 0f || CliffHeight(q.x, q.y + 3f) < 0f), risco = Mathf.Abs(CliffHeight(q.x + 2f, q.y) - h) > 1f || Mathf.Abs(CliffHeight(q.x, q.y + 2f) - h) > 1f;
            if (!cercaAgua && !risco) continue;
            PenascoCosta(g, new Vector3(q.x, h, q.y), R(0.8f, 2.6f));
            i++;
        }
    }

    /// <summary>Barrera invisible a lo largo de toda la orilla, menos donde están los puentes.</summary>
    void Orilla(Transform lvl)
    {
        var o = Group("Limites", lvl);
        const float c = 2.5f;
        for (float x = CliffWalk.xMin; x < CliffWalk.xMax; x += c)
            for (float z = CliffWalk.yMin; z < CliffWalk.yMax; z += c)
            {
                if (CliffHeight(x, z) > -0.7f) continue;
                bool orilla = CliffHeight(x + c, z) > -0.7f || CliffHeight(x - c, z) > -0.7f || CliffHeight(x, z + c) > -0.7f || CliffHeight(x, z - c) > -0.7f;
                if (!orilla) continue;
                var p = new Vector2(x, z);
                if (DistSeg(p, Puente1A, Puente1B, out _) < 4f || DistSeg(p, Puente2A, Puente2B, out _) < 4f || DistSeg(p, RampaA, RampaB, out _) < 4.6f) continue;
                SolidBox(o, "Limite_Orilla", new Vector3(x, 3f, z), new Vector3(c, 22f, c));
            }
    }

    void ArbolesLejos(Transform parent)
    {
        for (int i = 0, tries = 0; i < 260 && tries < 5000; tries++)
        {
            var q = new Vector2(R(-280f, 400f), R(-330f, 70f));
            if (CliffArea.Contains(q)) continue;
            float h = CliffHeight(q.x, q.y);
            if (h < PradoY || CliffHeight(q.x + 3f, q.y) != h) continue;
            if (rng.NextDouble() < 0.55) Pine(parent, q, h - 0.1f, R(1f, 1.7f), false); else Tree(parent, q, h - 0.1f, R(1f, 1.5f), false);
            i++;
        }
    }

    void Gaviotas(Transform bg)
    {
        foreach (var c in new[] { new Vector3(4f, 2f, 34f), new Vector3(-30f, 2f, 48f), new Vector3(66f, 6f, 48f), new Vector3(-8f, 1f, 10f) })
        {
            var bird = Group("Gaviota", bg);
            Prim(bird, PrimitiveType.Sphere, "Cuerpo", Vector3.zero, new Vector3(0.22f, 0.2f, 0.6f), mWhite);
            var wl = Group("AlaI", bird); var wr = Group("AlaD", bird);
            Box(wl, "Ala", new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), mWhite, false);
            Box(wr, "Ala", new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), mWhite, false);
            var gull = bird.gameObject.AddComponent<Gull>();
            gull.center = c; gull.radius = R(7f, 14f); gull.height = R(7f, 13f); gull.phase = R(0f, 6f); gull.speed = 0.35f;
            gull.wingL = wl; gull.wingR = wr;
        }
    }
}
