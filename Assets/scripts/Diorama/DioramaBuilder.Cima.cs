using UnityEngine;

/// <summary>
/// La Cima — sobre las nubes (crepúsculo con estrellas). Islas de pasto sobre pilares de roca que se hunden en un mar de
/// nubes, unidas por puentes colgantes. En la isla más alta, la azotea de la torre: almenas, columnas de mármol y el altar
/// donde espera el Guardián del Abismo. Rocas que flotan, el sol rozando el horizonte rosa.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect SummitArea = new Rect(-90f, -85f, 180f, 175f);
    static readonly Rect SummitWalk = new Rect(-70f, -72f, 140f, 148f);
    static readonly Vector3 SummitSpawn = new Vector3(0f, 0.1f, -60f);
    // islas: x, z, radio, alto
    static readonly Vector4[] Isles =
    {
        new Vector4(0f, -56f, 11f, 0f), new Vector4(-30f, -26f, 14f, 2f), new Vector4(24f, -14f, 10f, 3.5f),
        new Vector4(-12f, 16f, 14f, 5f), new Vector4(14f, 52f, 18f, 7f),
    };
    const float CloudY = -16f;

    float SummitHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        foreach (var s in Isles)
            if (Vector2.Distance(new Vector2(x, z), new Vector2(s.x, s.y)) < s.z + e * 0.8f) return s.w;
        return VoidY - 10f;
    }

    /// <summary>Puente colgante entre dos islas (de orilla a orilla, sobre la línea que las une).</summary>
    void IsleBridge(Transform parent, int i, int j)
    {
        Vector4 a = Isles[i], b = Isles[j];
        var dir = new Vector2(b.x - a.x, b.y - a.y).normalized;
        var pa = new Vector2(a.x, a.y) + dir * (a.z - 1.6f);
        var pb = new Vector2(b.x, b.y) - dir * (b.z - 1.6f);
        RopeBridge(parent, "Puente" + i + j, new Vector3(pa.x, a.w, pa.y), new Vector3(pb.x, b.w, pb.y), 4.2f);   // puentes anchos: se pelea y se camina cómodo
    }

    void BuildSummit()
    {
        BuildTerrain();
        var g = Group("Cima", root);
        var lvl = Group("Nivel", root);

        // mar de nubes
        Flat(g, "Nubes", new Vector3(0f, CloudY, 0f), 2400f, 2400f, mCloud, 0.01f);
        var ps = new GameObject("Bruma").AddComponent<ParticleSystem>();
        ps.transform.SetParent(g, false);
        ps.transform.localPosition = new Vector3(0f, CloudY + 2f, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 30f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(12f, 26f); main.startColor = new Color(1f, 0.85f, 0.88f, 0.35f);
        main.maxParticles = 600; main.prewarm = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 18f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(300f, 4f, 300f);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSmoke;
        ps.Play();

        // puentes
        IsleBridge(lvl, 0, 1); IsleBridge(lvl, 1, 2); IsleBridge(lvl, 1, 3); IsleBridge(lvl, 3, 4);

        // rocas que flotan y lejanas islas
        for (int i = 0; i < 30; i++)
        {
            var q = new Vector2(R(-120f, 120f), R(-110f, 140f));
            bool near = false;
            foreach (var s in Isles) if (Vector2.Distance(q, new Vector2(s.x, s.y)) < s.z + 8f) near = true;
            if (near) continue;
            float sz = R(1.5f, 6f);
            var rock = Prim(g, PrimitiveType.Sphere, "RocaFlotante", new Vector3(q.x, R(-8f, 14f), q.y), new Vector3(sz * 1.3f, sz, sz), mRock);
            rock.transform.localRotation = Quaternion.Euler(R(0f, 40f), R(0f, 360f), R(0f, 40f));
            var bob = rock.AddComponent<FloatingBob>(); bob.amplitude = R(0.4f, 1.2f); bob.period = R(6f, 12f); bob.phase = R(0f, 10f); bob.spin = R(-4f, 4f);
        }
        EnsurePlantMeshes();
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * R(180f, 300f), R(-6f, 10f), Mathf.Sin(a) * R(180f, 300f) + 60f);
            var isl = MeshObject(g, "IslaLejana", blobMeshes[rng.Next(blobMeshes.Length)], mRock, false);
            isl.transform.localPosition = p;
            isl.transform.localScale = new Vector3(R(18f, 36f), R(24f, 40f), R(18f, 36f));
            var top = Prim(g, PrimitiveType.Sphere, "Pasto", p + Vector3.up * 8f, new Vector3(isl.transform.localScale.x * 0.9f, 4f, isl.transform.localScale.z * 0.9f), mGrass);
            for (int k = 0; k < 4; k++) Pine(g, new Vector2(p.x + R(-8f, 8f), p.z + R(-8f, 8f)), p.y + 9f, R(2f, 3f), false);
        }

        // la azotea de la torre en la isla más alta
        var top0 = Isles[4];
        var roof = Group("Azotea", g);
        roof.localPosition = new Vector3(top0.x, top0.w, top0.y);
        Flat(roof, "Losas", new Vector3(0f, 0.02f, 2f), 28f, 28f, mStoneBrick, 0.3f);
        for (int i = 0; i < 30; i++)
        {
            float a = i / 30f * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * 15.5f, 0f, Mathf.Sin(a) * 15.5f + 2f);
            if (Vector3.Angle(p.normalized, new Vector3(-0.1f, 0f, -1f)) < 22f) continue;   // entrada desde el puente
            Box(roof, "Almena", p + Vector3.up * 0.6f, new Vector3(1.4f, 1.2f, 1f), mStoneBrick, true);
            Box(roof, "Diente", p + Vector3.up * 1.5f, new Vector3(0.7f, 0.6f, 0.7f), mStoneBrick, false);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 0.5f;
            var p = new Vector3(Mathf.Cos(a) * 9.5f, 0f, Mathf.Sin(a) * 9.5f + 4f);
            Prim(roof, PrimitiveType.Cylinder, "Columna", p + Vector3.up * 3f, new Vector3(1.1f, 3f, 1.1f), mWhite, true);
            Box(roof, "Capitel", p + Vector3.up * 6.1f, new Vector3(1.6f, 0.3f, 1.6f), mWhite, false);
            if (i % 2 == 0) Crystals(roof, p + Vector3.up * 6.3f, 0.6f, true, false);
        }
        var altar = Group("Altar", roof);
        altar.localPosition = new Vector3(0f, 0f, 8f);
        Box(altar, "Grada", new Vector3(0f, 0.25f, 0f), new Vector3(5f, 0.5f, 4f), mWhite, true);
        Box(altar, "Mesa", new Vector3(0f, 1f, 0f), new Vector3(2.6f, 1f, 1.6f), mWhite, true);
        Crystals(altar, new Vector3(0f, 1.5f, 0f), 1.4f, false, true);
        var crown = Prim(altar, PrimitiveType.Cylinder, "Corona", new Vector3(0f, 3.4f, 0f), new Vector3(1.2f, 0.12f, 1.2f), mGlow);
        crown.AddComponent<FloatingBob>().amplitude = 0.2f;
        blocked.Add(new Rect(top0.x - 14f, top0.y - 12f, 28f, 28f));

        // campamento en la isla del mercader
        var m0 = Isles[2];
        MerchantPost(lvl, new Vector3(m0.x + 1f, m0.w, m0.y + 2.5f), 200f);
        HealShrine(lvl, new Vector3(m0.x - 3f, m0.w, m0.y - 2f));
        Campfire(lvl, new Vector3(m0.x + 2.5f, m0.w, m0.y - 2.5f));
        foreach (var s in Isles) PortLamp(lvl, new Vector3(s.x + s.z * 0.55f, s.w, s.y - s.z * 0.45f));

        Arena(lvl, "La isla de los vientos", new Vector3(Isles[1].x, Isles[1].w, Isles[1].y), new Vector2(18f, 18f), 6);
        Arena(lvl, "El jardín colgante", new Vector3(Isles[3].x, Isles[3].w, Isles[3].y), new Vector2(18f, 18f), 6);
        var boss = Arena(lvl, "Jefe", new Vector3(top0.x, top0.w, top0.y + 1f), new Vector2(20f, 18f), 1, true);
        Exit(lvl, new Vector3(top0.x, top0.w, top0.y + 12f), boss);
        Chest(lvl, new Vector3(Isles[0].x + 5f, 0f, Isles[0].y - 3f), 30, 0f);
        Chest(lvl, new Vector3(Isles[3].x - 6f, Isles[3].w, Isles[3].y + 4f), 40, 90f);
        Note(lvl, new Vector3(Isles[0].x - 4f, 0f, Isles[0].y - 2f), 160f, LoreNote.Kind.Diario, "dio7_diario", "Diario de la expedición — última página",
             "Abis: llegamos al piso 15 y seguimos. El capitán dice que mañana bajamos hasta el piso 20.\nNadie sabe qué hay más abajo. Si esta es la última hoja que encuentras, no me busques.\nPero si me buscas... sigue las hojas.", "— " + Historia.Hermano);
        Note(lvl, new Vector3(top0.x + 4f, top0.w, top0.y - 6f), 200f, LoreNote.Kind.Inscripcion, "dio7_arquitecto", "Grabado en la piedra",
             "Nadie sabe qué es esta torre, de dónde proviene o por qué apareció.\nCada piso que bajas es más hondo que el anterior. Y el abismo sigue.");

        WalkBoundary(SummitWalk);
        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 20f, 22f, 115f, 32f, 6f, new Vector3(0f, 0f, 5f), 2.6f, 10, intro: 5f);
        Shot(shots, "Nubes", new Vector3(m0.x, m0.w, m0.y), new Vector2(8f, 8f), 10f, 6f, 12f, 46f, 10f, new Vector3(0f, 2f, 5f), 2.2f, 3,
             "SOBRE LAS NUBES", "Un cielo en el fondo del abismo", still: true);
        var mid = (new Vector2(Isles[3].x, Isles[3].y) + new Vector2(Isles[4].x, Isles[4].y)) / 2f;
        Shot(shots, "Ascenso", new Vector3(mid.x, 5f, mid.y), new Vector2(6f, 8f), 100f, 14f, 24f, 38f, 6f, new Vector3(0f, 1f, 3f), 1.8f, 2,
             "LA ÚLTIMA SUBIDA", "");
        // unos árboles en el lado norte de cada isla (al sur taparían la cámara)
        foreach (var s in Isles)
            for (int k = 0; k < 3; k++)
            {
                float a = Mathf.Lerp(0.35f, 2.8f, k / 2f);
                var q = new Vector2(s.x + Mathf.Cos(a) * (s.z - 2.2f), s.y + Mathf.Sin(a) * (s.z - 2.2f));
                if (!Free(q, 1f)) continue;
                if (k == 1) Pine(g, q, s.w, R(1f, 1.3f), true); else Tree(g, q, s.w, R(0.9f, 1.2f), true);
            }
        FinishWorld(false, true);
    }
}
