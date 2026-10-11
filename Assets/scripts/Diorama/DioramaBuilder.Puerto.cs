using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mapa "Muelle de puerto" (estilo DQ3 HD-2D / Octopath): bahía entre dos cabos con un malecón de sillar, un muelle de
/// piedra largo con pretiles almenados, dos veleros amarrados, embarcadero de tablones con barcas, mercado del puerto,
/// almacenes, faro con haz giratorio y gaviotas. El mar llega hasta el horizonte (no hay borde a la vista).
/// Se genera con DioramaBuilder (layout = Puerto).
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect PortArea = new Rect(-100f, -70f, 200f, 150f);
    static readonly Rect PortWalk = new Rect(-30f, -38f, 64f, 84f);
    static readonly Vector3 PortSpawn = new Vector3(0f, 0.1f, -22f);
    const float SeaY = -1.4f, PierX = 3f, PierW = 6f, PierEnd = 44f;
    const float LighthouseX = 44f, LighthouseZ = 27f;

    Material mSand, mWhite, mRedPaint, mSail, mHull;

    // ------------------------------------------------------------------ forma de la bahía

    /// <summary>Altura del puerto: tierra con bancales al sur y a los lados, dos cabos y el fondo del mar.</summary>
    float PortHeight(float x, float z)
    {
        float e = (Noise(x * 0.18f, z * 0.18f) * 2f - 1f) + (Noise(x * 0.5f + 40f, z * 0.5f) * 2f - 1f) * 0.35f
                + (Noise(x * 1.3f - 17f, z * 1.3f + 9f) * 2f - 1f) * 0.25f;
        // línea de costa: malecón recto en el pueblo, cabos irregulares a los lados
        float coast = Mathf.Abs(x) < 30f ? 0f : x < 0f ? 14f + e * 3f : x < 58f ? 34f + e * 2.5f - Mathf.Max(0f, x - 50f) * 2f : 6f + e * 2f;
        if (z > coast)
        {
            float dz = z - coast;
            return dz < 7f ? -4f : dz < 30f ? -6f : -8f;
        }
        float h = 0f;
        float ex = Mathf.Abs(x) - 34f + e * 2f;
        if (ex > 0f) h = Mathf.Max(h, ex < 5f ? 1.5f : ex < 10f ? 3f : ex < 16f ? 4.8f : 7f);
        float es = -z - 36f + e * 2f;
        if (es > 0f) h = Mathf.Max(h, es < 6f ? 1.5f : es < 12f ? 3f : es < 18f ? 4.8f : 7f);
        // los cabos que cierran la bahía
        if (z > 0f && x > 30f) h = Mathf.Max(h, Vector2.Distance(new Vector2(x, z), new Vector2(LighthouseX, LighthouseZ)) < 9f + e ? 4.5f : 2.5f);
        if (z > 0f && x < -30f) h = Mathf.Max(h, 3f);
        // lomas arriba (no en los bordes: ahí empatan con las mesetas de afuera)
        if (h >= 4.8f && Mathf.Abs(x) < 85f && z > -62f && Noise(x * 0.09f + 5f, z * 0.09f) > 0.62f) h += 2.5f;
        return h;
    }

    // ------------------------------------------------------------------ generación

    void BuildPort()
    {
        mSand = Plain("Dio_Arena", new Color(0.86f, 0.78f, 0.58f), 0.1f);
        mWhite = Plain("Dio_Blanco", new Color(0.95f, 0.93f, 0.88f), 0.15f);
        mRedPaint = Plain("Dio_RojoFaro", new Color(0.72f, 0.16f, 0.14f), 0.2f);
        mSail = Saved("Dio_Vela", () =>
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", new Color(0.96f, 0.92f, 0.82f));
            m.SetFloat("_Cull", 0f);   // se ve de los dos lados
            return m;
        });
        mHull = Plain("Dio_Casco", new Color(0.32f, 0.2f, 0.12f), 0.15f);

        BuildTerrain();
        BuildSea();
        BuildQuay();
        BuildStonePier();
        BuildBoardwalk();
        Ship(root, new Vector3(PierX + 11f, SeaY, 21f), 0f, 24f, 6f, true);
        Ship(root, new Vector3(PierX - 11.5f, SeaY, 27f), 180f, 19f, 5f, true);
        BuildPortTown();
        BuildLighthouse();
        BuildTrees();
        BuildGrass();
        BuildPortBackdrop();
        BuildPortBoundary();
        BuildAtmosphere();
        BuildGulls();
        BuildPortShots();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    // ------------------------------------------------------------------ mar

    void BuildSea()
    {
        var g = Group("Mar", root);
        var water = Flat(g, "Agua", new Vector3(0f, SeaY, 300f), 1800f, 1100f, mWater, 0.05f);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Flat(g, "FondoLejano", new Vector3(0f, -8.3f, 300f), 1800f, 1100f, mSand, 0.05f);
        // peñas y farallones en el agua
        for (int i = 0; i < 18; i++)
        {
            Vector2 q = i < 10 ? new Vector2(R(30f, 62f), R(30f, 44f)) : new Vector2(R(-70f, -32f), R(12f, 30f));
            if (i >= 14) q = new Vector2(R(-120f, 140f), R(90f, 220f));
            float s = i >= 14 ? R(6f, 12f) : R(1.2f, 3.2f);
            var rock = Prim(g, PrimitiveType.Sphere, "Pena", new Vector3(q.x, SeaY - s * 0.15f, q.y), new Vector3(s * R(1f, 1.5f), s * R(0.8f, 1.6f), s), mRock);
            rock.transform.localRotation = Quaternion.Euler(R(0f, 25f), R(0f, 360f), R(0f, 25f));
            Foam(g, new Vector3(q.x, SeaY + 0.05f, q.y), s * 0.9f, 8f);
        }
    }

    /// <summary>Destellos del sol sobre la bahía y mar adentro.</summary>
    void SeaSparkles(Transform parent)
    {
        for (int ix = 0; ix < 4; ix++)
            for (int iz = 0; iz < 3; iz++)
            {
                var ps = new GameObject("Destellos").AddComponent<ParticleSystem>();
                ps.transform.SetParent(parent, false);
                ps.transform.localPosition = new Vector3(-60f + (ix + 0.5f) * 30f, SeaY + 0.06f, 4f + (iz + 0.5f) * 28f);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
                main.maxParticles = 300; main.prewarm = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 180f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30f, 0.02f, 28f);
                var sz = ps.sizeOverLifetime; sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSparkle;
                ps.Play();
            }
    }

    // ------------------------------------------------------------------ malecón y muelles

    /// <summary>Malecón de sillar a lo largo de la orilla del pueblo, con remate, bolardos y argollas.</summary>
    void BuildQuay()
    {
        var g = Group("Malecon", root);
        Box(g, "Muro", new Vector3(0f, -2.15f, 0.25f), new Vector3(60f, 4.5f, 0.5f), mBrick, false);
        Box(g, "Remate", new Vector3(0f, 0.08f, 0.15f), new Vector3(60.4f, 0.16f, 1.1f), mStone, false);
        for (float x = -27f; x <= 27f; x += 6f)
        {
            if (Mathf.Abs(x - PierX) < 4.5f || Mathf.Abs(x + 22f) < 3f) continue;
            var b = Prim(g, PrimitiveType.Cylinder, "Bolardo", new Vector3(x, 0.4f, 0.25f), new Vector3(0.38f, 0.32f, 0.38f), mMetal);
            Prim(b.transform, PrimitiveType.Cylinder, "Tapa", new Vector3(0f, 1f, 0f), new Vector3(1.3f, 0.12f, 1.3f), mMetal);
        }
        // algas y musgo al pie del malecón
        for (int i = 0; i < 26; i++)
            Ivy(g, new Vector3(R(-29f, 29f), SeaY + R(-0.1f, 0.5f), 0.55f), new Vector3(R(0.6f, 1.4f), R(0.3f, 0.6f), 0.3f));
        // borde: no se cae una al mar por accidente (huecos en el muelle y el embarcadero)
        float[] cuts = { -23.7f, -20.3f, PierX - PierW / 2f, PierX + PierW / 2f };
        float x0 = -30f;
        for (int i = 0; i <= cuts.Length; i += 2)
        {
            float x1 = i < cuts.Length ? cuts[i] : 30f;
            if (x1 - x0 > 0.2f) SolidBox(g, "Collider_Orilla", new Vector3((x0 + x1) / 2f, 1f, 0.7f), new Vector3(x1 - x0, 2f, 0.4f));
            if (i + 1 < cuts.Length) x0 = cuts[i + 1];
        }
        blocked.Add(new Rect(-31f, -1.5f, 62f, 3f));
    }

    /// <summary>Muelle de piedra que entra en la bahía: cuerpo de sillar, empedrado, pretiles almenados y una plaza al final con farol.</summary>
    void BuildStonePier()
    {
        var g = Group("MuelleDePiedra", root);
        float len = PierEnd - 8f;
        Box(g, "Cuerpo", new Vector3(PierX, -2.4f, len / 2f), new Vector3(PierW, 5f, len), mBrick, true);
        Flat(g, "Empedrado", new Vector3(PierX, 0.11f, len / 2f), PierW - 0.2f, len, mCobble, 0.5f);
        // plaza del final, más ancha
        float pz = PierEnd - 4f;
        Box(g, "Plaza", new Vector3(PierX, -2.4f, pz), new Vector3(12f, 5f, 8f), mBrick, true);
        Flat(g, "EmpedradoPlaza", new Vector3(PierX, 0.115f, pz), 11.8f, 7.8f, mCobble, 0.5f);
        // pretiles
        foreach (float sx in new[] { -1f, 1f })
        {
            PierWall(g, new Vector3(PierX + sx * (PierW / 2f - 0.25f), 0f, (1f + len) / 2f), new Vector3(0.5f, 0f, len - 1f));
            PierWall(g, new Vector3(PierX + sx * 5.75f, 0f, pz), new Vector3(0.5f, 0f, 8f));
            PierWall(g, new Vector3(PierX + sx * 4.4f, 0f, pz - 3.75f), new Vector3(2.7f, 0f, 0.5f));
        }
        PierWall(g, new Vector3(PierX, 0f, PierEnd - 0.25f), new Vector3(12f, 0f, 0.5f));
        // bolardos con cuerdas a los barcos, cajas y barriles, farol del final
        for (float z = 6f; z < len; z += 7f)
            foreach (float sx in new[] { -1f, 1f })
                Prim(g, PrimitiveType.Cylinder, "Bolardo", new Vector3(PierX + sx * 2.1f, 0.4f, z), new Vector3(0.36f, 0.3f, 0.36f), mMetal);
        Clutter(g, new Vector3(PierX - 4f, 0.1f, pz + 1.5f), true);
        Clutter(g, new Vector3(PierX + 4f, 0.1f, pz - 1f), true);
        Clutter(g, new Vector3(PierX + 1.5f, 0.1f, 9f), true);
        foreach (float sx in new[] { -1f, 1f }) PortLamp(g, new Vector3(PierX + sx * 4.8f, 0.1f, PierEnd - 1.2f));
        PortLamp(g, new Vector3(PierX - 2.2f, 0.1f, 16f));
        PortLamp(g, new Vector3(PierX + 2.2f, 0.1f, 28f));
        blocked.Add(new Rect(PierX - 7f, 0f, 14f, PierEnd + 1f));
    }

    /// <summary>Pretil bajo de sillar con almenas (con colisión).</summary>
    void PierWall(Transform parent, Vector3 center, Vector3 size)
    {
        float top = 0.1f;
        Box(parent, "Pretil", new Vector3(center.x, top + 0.35f, center.z), new Vector3(size.x, 0.7f, size.z), mStone, false);
        SolidBox(parent, "Collider_Pretil", new Vector3(center.x, top + 1f, center.z), new Vector3(size.x, 2f, size.z));
        bool alongX = size.x > size.z;
        float len = alongX ? size.x : size.z;
        for (float d = -len / 2f + 0.4f; d < len / 2f - 0.2f; d += 1.4f)
        {
            var a = Box(parent, "Almena", center + (alongX ? new Vector3(d, top + 0.85f, 0f) : new Vector3(0f, top + 0.85f, d)),
                        new Vector3(alongX ? 0.6f : 0.55f, 0.3f, alongX ? 0.55f : 0.6f), mStone, false);
            if (rng.NextDouble() < 0.25) Ivy(parent, a.transform.localPosition + Vector3.up * 0.15f, new Vector3(0.6f, 0.35f, 0.6f));
        }
    }

    /// <summary>Embarcadero de tablones sobre pilotes (en L) con barandal de cuerda y barcas amarradas.</summary>
    void BuildBoardwalk()
    {
        var g = Group("Embarcadero", root);
        Deck(g, -23.7f, -20.3f, 0f, 18f);
        Deck(g, -23.7f, -12f, 15.6f, 19.4f);
        Rail(g, new Vector2(-23.7f, 0.3f), new Vector2(-23.7f, 19.4f));
        Rail(g, new Vector2(-20.3f, 0.3f), new Vector2(-20.3f, 15.6f));
        Rail(g, new Vector2(-23.7f, 19.4f), new Vector2(-12f, 19.4f));
        Rail(g, new Vector2(-20.3f, 15.6f), new Vector2(-12f, 15.6f));
        Rail(g, new Vector2(-12f, 15.6f), new Vector2(-12f, 19.4f));
        Boat(g, new Vector3(-17.5f, SeaY + 0.05f, 11f), 4f);
        Boat(g, new Vector3(-26.2f, SeaY + 0.05f, 9f), 3.6f);
        Boat(g, new Vector3(-15f, SeaY + 0.05f, 22.3f), 4.2f);
        foreach (Transform b in g) if (b.name == "Barca") { var bob = b.gameObject.AddComponent<FloatingBob>(); bob.amplitude = 0.06f; bob.period = R(3f, 5f); bob.phase = R(0f, 5f); }
        // canastas de pescado y redes
        for (int i = 0; i < 5; i++)
            Prim(g, PrimitiveType.Cylinder, "Canasta", new Vector3(R(-23f, -13f), 0.32f, R(16.2f, 18.8f)), new Vector3(0.6f, 0.22f, 0.6f), mPlank);
        PortLamp(g, new Vector3(-23.2f, 0.1f, 18.8f));
        blocked.Add(new Rect(-25f, 0f, 14f, 21f));
    }

    void Deck(Transform parent, float x0, float x1, float z0, float z1)
    {
        bool alongZ = z1 - z0 > x1 - x0;
        if (alongZ)
            for (float z = z0; z < z1 - 0.05f; z += 0.42f)
            {
                var p = Box(parent, "Tablon", new Vector3((x0 + x1) / 2f + R(-0.04f, 0.04f), 0.05f, z + 0.2f), new Vector3(x1 - x0, 0.1f, 0.38f), mPlank, false);
                p.transform.localRotation = Quaternion.Euler(R(-1f, 1f), R(-1.5f, 1.5f), 0f);
            }
        else
            for (float x = x0; x < x1 - 0.05f; x += 0.42f)
            {
                var p = Box(parent, "Tablon", new Vector3(x + 0.2f, 0.05f, (z0 + z1) / 2f + R(-0.04f, 0.04f)), new Vector3(0.38f, 0.1f, z1 - z0), mPlank, false);
                p.transform.localRotation = Quaternion.Euler(0f, R(-1.5f, 1.5f), R(-1f, 1f));
            }
        SolidBox(parent, "Collider_Tablero", new Vector3((x0 + x1) / 2f, -0.1f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.3f, z1 - z0));
        // pilotes hasta el fondo
        for (float x = x0 + 0.2f; x <= x1; x += Mathf.Max(1.6f, (x1 - x0 - 0.4f) / Mathf.Max(1, Mathf.Round((x1 - x0) / 1.6f))))
            for (float z = z0 + 0.8f; z <= z1; z += 1.6f)
                Prim(parent, PrimitiveType.Cylinder, "Pilote", new Vector3(x, -2.1f, z), new Vector3(0.28f, 2.2f, 0.28f), mTimber);
    }

    /// <summary>Barandal de postes con cuerda y colisión baja.</summary>
    void Rail(Transform parent, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a; float len = d.magnitude; Vector2 dir = d / len;
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 1.6f));
        for (int i = 0; i <= n; i++)
        {
            Vector2 p = a + dir * (len * i / n);
            Box(parent, "Poste", new Vector3(p.x, 0.55f, p.y), new Vector3(0.14f, 1.1f, 0.14f), mTimber, false);
        }
        Vector2 c = (a + b) / 2f;
        float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        foreach (float y in new[] { 0.55f, 0.95f })
        {
            var r = Box(parent, "Cuerda", new Vector3(c.x, y, c.y), new Vector3(0.05f, 0.05f, len), mPlank, false);
            r.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        var col = new GameObject("Collider_Barandal");
        col.transform.SetParent(parent, false);
        col.transform.localPosition = new Vector3(c.x, 1f, c.y);
        col.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(0.25f, 2f, len);
    }

    void PortLamp(Transform parent, Vector3 pos)
    {
        var t = Group("Farol", parent);
        t.localPosition = pos;
        Prim(t, PrimitiveType.Cylinder, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.14f, 1.5f, 0.14f), mMetal);
        Box(t, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(0.4f, 0.3f, 0.4f), mStone, false);
        Box(t, "Tapa", new Vector3(0f, 3.4f, 0f), new Vector3(0.5f, 0.1f, 0.5f), mMetal, false);
        Box(t, "Vidrio", new Vector3(0f, 3.1f, 0f), new Vector3(0.3f, 0.45f, 0.3f), mWindowLit, false);
    }

    // ------------------------------------------------------------------ veleros

    /// <summary>Velero: casco curvo con regala, cubierta, dos mástiles con velas, bauprés, jarcia, toldilla y bandera. Flota.</summary>
    void Ship(Transform parent, Vector3 pos, float yaw, float len, float beam, bool moored)
    {
        var s = Group("Velero", parent);
        s.localPosition = pos;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var hull = new GameObject("Casco");
        hull.transform.SetParent(s, false);
        hull.AddComponent<MeshFilter>().sharedMesh = HullMesh(len, beam);
        hull.AddComponent<MeshRenderer>().sharedMaterials = new[] { mHull, mPlank };
        // franja pintada en el costado
        foreach (float sx in new[] { -1f, 1f })
        {
            var band = Box(s, "Franja", new Vector3(sx * (beam / 2f * 0.98f), 1.1f, -0.04f * len), new Vector3(0.06f, 0.22f, len * 0.62f), mRedPaint, false);
            band.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        float deck = 1.65f;
        // toldilla (camarote a popa) con ventanas encendidas
        Box(s, "Toldilla", new Vector3(0f, deck + 0.8f, -len * 0.36f), new Vector3(beam * 0.7f, 1.6f, len * 0.18f), mPlank, false);
        Box(s, "TechoToldilla", new Vector3(0f, deck + 1.65f, -len * 0.36f), new Vector3(beam * 0.78f, 0.12f, len * 0.2f), mHull, false);
        for (int i = -1; i <= 1; i++)
            Box(s, "Ventana", new Vector3(i * 0.8f, deck + 0.9f, -len * 0.45f - 0.02f), new Vector3(0.45f, 0.4f, 0.05f), mWindowLit, false);
        // mástiles, vergas y velas
        float[] mastZ = { len * 0.12f, -len * 0.14f };
        float[] mastH = { len * 0.5f, len * 0.42f };
        for (int m = 0; m < 2; m++)
        {
            float h = mastH[m];
            Prim(s, PrimitiveType.Cylinder, "Mastil", new Vector3(0f, deck + h / 2f, mastZ[m]), new Vector3(0.32f, h / 2f, 0.32f), mTimber);
            for (int k = 0; k < 2; k++)
            {
                float y = deck + h * (0.42f + k * 0.38f), w = beam * (1.5f - k * 0.45f), sh = h * 0.32f;
                var yard = Prim(s, PrimitiveType.Cylinder, "Verga", new Vector3(0f, y, mastZ[m] + 0.25f), new Vector3(0.14f, w / 2f, 0.14f), mTimber);
                yard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var sail = MeshObject(s, "Vela", SailMesh(w * 0.92f, sh, moored ? 0.25f : 0.9f), mSail, true);
                sail.transform.localPosition = new Vector3(0f, y - 0.08f, mastZ[m] + 0.32f);
            }
            var flag = Box(s, "Bandera", new Vector3(0f, deck + h + 0.35f, mastZ[m] - 0.5f), new Vector3(0.04f, 0.6f, 1f), mCloth, false);
            flag.transform.localRotation = Quaternion.Euler(0f, R(-10f, 10f), 0f);
        }
        // bauprés y jarcia
        var sprit = Prim(s, PrimitiveType.Cylinder, "Baupres", new Vector3(0f, deck + 0.8f, len * 0.56f), new Vector3(0.2f, len * 0.14f, 0.2f), mTimber);
        sprit.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Line(s, new Vector3(0f, deck + mastH[0], mastZ[0]), new Vector3(0f, deck + 1.6f, len * 0.68f));
        Line(s, new Vector3(0f, deck + mastH[0], mastZ[0]), new Vector3(0f, deck + mastH[1] * 0.9f, mastZ[1]));
        Line(s, new Vector3(0f, deck + mastH[1], mastZ[1]), new Vector3(0f, deck + 1.8f, -len * 0.47f));
        foreach (float sx in new[] { -1f, 1f })
            for (int m = 0; m < 2; m++)
                Line(s, new Vector3(0f, deck + mastH[m] * 0.85f, mastZ[m]), new Vector3(sx * beam * 0.48f, deck + 0.3f, mastZ[m] - 1.5f));
        // farol de popa
        Box(s, "FarolPopa", new Vector3(0f, deck + 2.3f, -len * 0.47f), new Vector3(0.35f, 0.5f, 0.35f), mWindowLit, false);
        var bob = s.gameObject.AddComponent<FloatingBob>();
        bob.amplitude = moored ? 0.08f : 0.15f; bob.period = R(4.5f, 6.5f); bob.phase = R(0f, 6f);
        if (moored) blocked.Add(new Rect(pos.x - beam, pos.z - len / 2f - 2f, beam * 2f, len + 4f));
    }

    void Line(Transform parent, Vector3 a, Vector3 b)
    {
        var l = Prim(parent, PrimitiveType.Cylinder, "Cabo", (a + b) / 2f, new Vector3(0.04f, Vector3.Distance(a, b) / 2f, 0.04f), mTimber);
        l.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        l.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Casco: secciones a lo largo de Z (popa en -Z, proa en +Z) con regala que sube en las puntas; submalla 0 = casco, 1 = cubierta.</summary>
    static Mesh HullMesh(float len, float beam)
    {
        const int n = 16;
        var top = new Vector3[n + 1, 2]; var bot = new Vector3[n + 1, 2];
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, z = -len / 2f + t * len;
            float w = beam / 2f * (t < 0.65f ? 0.78f + 0.22f * Mathf.Sin(t / 0.65f * Mathf.PI / 2f) : Mathf.Max(0.04f, Mathf.Cos((t - 0.65f) / 0.35f * Mathf.PI / 2f)));
            float sheer = 1.65f + 0.7f * Mathf.Pow((t - 0.45f) * 2f, 2f);
            float k = w * 0.3f;
            top[i, 0] = new Vector3(-w, sheer, z); top[i, 1] = new Vector3(w, sheer, z);
            bot[i, 0] = new Vector3(-k, -1.3f, z); bot[i, 1] = new Vector3(k, -1.3f, z);
        }
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var hullT = new List<int>(); var deckT = new List<int>();
        void Quad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            // orienta la cara hacia afuera
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var tmp = b; b = d; d = tmp; }
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(new Vector2(a.z * 0.5f, a.y * 0.5f)); uv.Add(new Vector2(b.z * 0.5f, b.y * 0.5f)); uv.Add(new Vector2(c.z * 0.5f, c.y * 0.5f)); uv.Add(new Vector2(d.z * 0.5f, d.y * 0.5f));
            tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }
        for (int i = 0; i < n; i++)
        {
            Quad(hullT, top[i, 0], bot[i, 0], bot[i + 1, 0], top[i + 1, 0], Vector3.left);
            Quad(hullT, top[i, 1], bot[i, 1], bot[i + 1, 1], top[i + 1, 1], Vector3.right);
            Quad(hullT, bot[i, 0], bot[i, 1], bot[i + 1, 1], bot[i + 1, 0], Vector3.down);
            // cubierta un poco abajo de la regala
            Vector3 dy = Vector3.down * 0.25f;
            Quad(deckT, top[i, 0] * 0.94f + dy, top[i, 1] * 0.94f + dy, top[i + 1, 1] * 0.94f + dy, top[i + 1, 0] * 0.94f + dy, Vector3.up);
            // cara interior de la regala (se ve desde arriba)
            Quad(hullT, top[i, 0], top[i, 0] * 0.94f + dy, top[i + 1, 0] * 0.94f + dy, top[i + 1, 0], Vector3.right);
            Quad(hullT, top[i, 1], top[i, 1] * 0.94f + dy, top[i + 1, 1] * 0.94f + dy, top[i + 1, 1], Vector3.left);
        }
        Quad(hullT, top[0, 0], bot[0, 0], bot[0, 1], top[0, 1], Vector3.back);   // espejo de popa
        var m = new Mesh { name = "Casco" };
        m.SetVertices(v); m.SetUVs(0, uv);
        m.subMeshCount = 2; m.SetTriangles(hullT, 0); m.SetTriangles(deckT, 1);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Vela cuadra hinchada por el viento (rejilla curvada hacia +Z), colgando hacia abajo desde y = 0.</summary>
    static Mesh SailMesh(float w, float h, float belly)
    {
        const int nx = 6, ny = 5;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= ny; j++)
            for (int i = 0; i <= nx; i++)
            {
                float u = i / (float)nx, s = j / (float)ny;
                float bulge = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(s * Mathf.PI) * belly;
                v.Add(new Vector3((u - 0.5f) * w * (1f - s * 0.08f), -s * h, bulge));
                uv.Add(new Vector2(u, 1f - s));
            }
        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                t.AddRange(new[] { a, c, d, a, d, b });
            }
        var m = new Mesh { name = "Vela" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ pueblo del puerto

    void BuildPortTown()
    {
        var v = Group("Pueblo", root);
        Flat(v, "Plaza", new Vector3(0f, 0.012f, -12f), 50f, 18f, mCobble, 0.5f);
        blocked.Add(new Rect(-25f, -21f, 50f, 18f));
        // almacenes y casas mirando al mar
        House(v, new Vector3(-17f, 0f, -29.5f), 0f, 9f, 6f, 4.6f, true);
        House(v, new Vector3(0f, 0f, -30.5f), 0f, 10f, 6.5f, 5.2f, false);
        House(v, new Vector3(17f, 0f, -29.5f), 0f, 8.5f, 6f, 4.4f, true);
        House(v, new Vector3(-27.5f, 0f, -14f), 90f, 6f, 5f, 3.6f, true);
        House(v, new Vector3(28f, 0f, -12f), -90f, 6.5f, 5f, 3.8f, false);
        // mercado del muelle
        Stall(v, new Vector3(-9f, 0f, -9f), 0f);
        Stall(v, new Vector3(10f, 0f, -10f), 0f);
        Stall(v, new Vector3(-16f, 0f, -16f), 90f);
        foreach (var p in new[] { new Vector3(-5f, 0f, -2.5f), new Vector3(12.5f, 0f, -3f), new Vector3(21f, 0f, -4f), new Vector3(-13f, 0f, -3f), new Vector3(-26f, 0f, -4f), new Vector3(24f, 0f, -22f) })
            Clutter(v, p);
        foreach (float x in new[] { -26f, -14f, -6f, 10f, 18f, 26f }) PortLamp(v, new Vector3(x, 0f, -1.8f));
        foreach (float x in new[] { -10f, 10f }) PortLamp(v, new Vector3(x, 0f, -21.5f));
        // flores en jardineras y macetas sueltas
        for (int i = 0; i < 60; i++)
        {
            Vector2 q = new Vector2(R(-29f, 30f), R(-36f, -3f));
            if (!Free(q, 0.3f) || Mathf.Abs(PortHeight(q.x, q.y)) > 0.01f) continue;
            var f = Prim(v, PrimitiveType.Sphere, "Flor", new Vector3(q.x, 0.12f, q.y), Vector3.one * R(0.12f, 0.22f), mFlowers[rng.Next(mFlowers.Length)]);
            f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Faro en la punta del cabo: torre a franjas, balcón, linterna encendida y un haz que gira.</summary>
    void BuildLighthouse()
    {
        var g = Group("Faro", root);
        float baseY = PortHeight(LighthouseX, LighthouseZ);
        g.localPosition = new Vector3(LighthouseX, baseY, LighthouseZ);
        Box(g, "Zocalo", new Vector3(0f, 0.4f, 0f), new Vector3(5f, 0.8f, 5f), mStone, true);
        float y = 0.8f;
        for (int i = 0; i < 5; i++)
        {
            float r = Mathf.Lerp(2f, 1.45f, i / 5f), h = 2.6f;
            Prim(g, PrimitiveType.Cylinder, "Tramo", new Vector3(0f, y + h / 2f, 0f), new Vector3(r * 2f, h / 2f, r * 2f), i % 2 == 0 ? mWhite : mRedPaint, true);
            y += h;
        }
        Prim(g, PrimitiveType.Cylinder, "Balcon", new Vector3(0f, y + 0.1f, 0f), new Vector3(4.2f, 0.12f, 4.2f), mMetal);
        Prim(g, PrimitiveType.Cylinder, "Linterna", new Vector3(0f, y + 1.1f, 0f), new Vector3(2.2f, 0.9f, 2.2f), mWindowLit);
        var roof = MeshObject(g, "Cupula", Cone(1f, 1f, 12), mRedPaint, true);
        roof.transform.localPosition = new Vector3(0f, y + 2f, 0f);
        roof.transform.localScale = new Vector3(1.5f, 1.4f, 1.5f);
        var lt = AddLight(g, new Vector3(0f, y + 1.1f, 0f), new Color(1f, 0.85f, 0.55f), 22f, 4f);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.05f;
        // haz que gira (FloatingBob con giro y sin vaivén)
        var pivot = Group("Haz", g);
        pivot.localPosition = new Vector3(0f, y + 1.1f, 0f);
        var spin = pivot.gameObject.AddComponent<FloatingBob>(); spin.amplitude = 0f; spin.spin = 35f;
        foreach (float a in new[] { 0f, 180f })
        {
            var arm = Group("Brazo", pivot);
            arm.localRotation = Quaternion.Euler(4f, a, 0f);
            var beam = MeshObject(arm, "Rayo", Cone(1f, 1f, 10), mRay, false);
            beam.transform.localPosition = new Vector3(0f, 0f, 40f);
            beam.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            beam.transform.localScale = new Vector3(3.5f, 40f, 3.5f);
        }
        blocked.Add(new Rect(LighthouseX - 4f, LighthouseZ - 4f, 8f, 8f));
    }

    // ------------------------------------------------------------------ fondo, límites, gaviotas y tomas

    void BuildPortBackdrop()
    {
        var g = Group("Fondo", root);
        float a = PortArea.xMin, b = PortArea.xMax, s = PortArea.yMin;
        const float far = 700f;
        // tierra de afuera a la altura de los bordes (7 m) y su risco hacia el mar
        Flat(g, "TierraOeste", new Vector3((-far + a + 1f) / 2f, 7f, (-far + 14f) / 2f), a + 1f + far, far + 14f, mGrass, 0.25f);
        Flat(g, "TierraEste", new Vector3((b - 1f + far) / 2f, 7f, (-far + 6f) / 2f), far - b + 1f, far + 6f, mGrass, 0.25f);
        Flat(g, "TierraSur", new Vector3((a + b) / 2f, 7f, (-far + s + 1f) / 2f), b - a, far + s + 1f, mGrass, 0.25f);
        Wall(g, "RiscoOeste", new Vector3((-far + a) / 2f, (7f - 8.3f) / 2f, 14f), Vector3.forward, a + far, 15.3f, mRock, 0.25f);
        Wall(g, "RiscoEste", new Vector3((b + far) / 2f, (7f - 8.3f) / 2f, 6f), Vector3.forward, far - b, 15.3f, mRock, 0.25f);
        // lomas en la tierra de afuera (que no sea una planicie)
        for (int i = 0; i < 16; i++)
        {
            var hill = Prim(g, PrimitiveType.Sphere, "Loma", new Vector3(R(-260f, 260f), 7f - 4f, R(-260f, -90f)), new Vector3(R(40f, 70f), R(16f, 26f), R(30f, 50f)), mGrass);
            hill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        for (int i = 0, tries = 0; i < 480 && tries < 8000; tries++)
        {
            var q = new Vector2(R(-260f, 260f), R(-240f, 12f));
            float y;
            if (q.x < a - 1f && q.y < 12f) y = 7f;
            else if (q.x > b + 1f && q.y < 4f) y = 7f;
            else if (q.y < s - 1f) y = 7f;
            else continue;
            if (rng.NextDouble() < 0.6) Pine(g, q, y, R(1.2f, 2f), false); else Tree(g, q, y, R(1.2f, 2f), false);
            i++;
        }
        // montañas al sur, detrás del pueblo, y la torre asomando entre ellas
        EnsurePlantMeshes();
        for (int i = 0; i < 14; i++)
        {
            float x = -300f + i * 46f + R(-10f, 10f), z = R(-460f, -380f), h = R(110f, 180f), r = R(55f, 85f);
            var m = MeshObject(g, "Montana", blobMeshes[rng.Next(blobMeshes.Length)], mMountain, false);
            m.transform.localPosition = new Vector3(x, 7f - h * 0.1f, z);
            m.transform.localScale = new Vector3(r * 2.4f, h * 0.9f, r * 1.6f);
            m.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        }
        var tower = Group("Torre", g);
        tower.localPosition = new Vector3(-120f, 7f, -330f);
        Prim(tower, PrimitiveType.Cylinder, "Cuerpo", new Vector3(0f, 70f, 0f), new Vector3(16f, 70f, 16f), mBrick);
        for (int i = 1; i <= 6; i++) Prim(tower, PrimitiveType.Cylinder, "Anillo", new Vector3(0f, i * 22f, 0f), new Vector3(17.5f, 0.8f, 17.5f), mStone);
        // islas y un velero a lo lejos
        foreach (var p in new[] { new Vector3(-150f, 0f, 230f), new Vector3(120f, 0f, 300f) })
        {
            var isl = Prim(g, PrimitiveType.Sphere, "Isla", new Vector3(p.x, SeaY - 6f, p.z), new Vector3(70f, 22f, 40f), mGrass);
            isl.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int k = 0; k < 12; k++) Pine(g, new Vector2(p.x + R(-22f, 22f), p.z + R(-10f, 10f)), SeaY + 3f, R(2.5f, 4f), false);
        }
        Ship(g, new Vector3(70f, SeaY, 160f), 70f, 26f, 6.5f, false);
    }

    void BuildPortBoundary()
    {
        var g = Group("Limites", root);
        var r = PortWalk;
        SolidBox(g, "Limite_O", new Vector3(r.xMin - 0.5f, 3f, r.center.y), new Vector3(1f, 8f, r.height));
        SolidBox(g, "Limite_E", new Vector3(r.xMax + 0.5f, 3f, r.center.y), new Vector3(1f, 8f, r.height));
        SolidBox(g, "Limite_S", new Vector3(r.center.x, 3f, r.yMin - 0.5f), new Vector3(r.width, 8f, 1f));
    }

    void BuildGulls()
    {
        var g = Group("Gaviotas", root);
        var white = mWhite;
        foreach (var c in new[] { new Vector3(PierX, 0f, 30f), new Vector3(-12f, 0f, 20f), new Vector3(25f, 0f, 10f), new Vector3(PierX + 6f, 0f, 45f) })
            for (int k = 0; k < 2; k++)
            {
                var b = Group("Gaviota", g);
                Prim(b, PrimitiveType.Sphere, "Cuerpo", Vector3.zero, new Vector3(0.22f, 0.2f, 0.6f), white);
                var wl = Group("AlaI", b); var wr = Group("AlaD", b);
                Box(wl, "Ala", new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), white, false);
                Box(wr, "Ala", new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.28f), white, false);
                var gull = b.gameObject.AddComponent<Gull>();
                gull.center = c; gull.radius = R(6f, 13f); gull.height = R(7f, 12f); gull.phase = R(0f, 6.3f);
                gull.speed = R(0.25f, 0.45f) * (rng.NextDouble() < 0.5 ? 1f : -1f);
                gull.wingL = wl; gull.wingR = wr;
            }
    }

    void BuildPortShots()
    {
        var g = Group("Tomas", root);
        // apertura: desde el mar, la bahía con los barcos, el pueblo y los cerros; luego baja hasta Abby
        Shot(g, "Apertura", Vector3.zero, Vector2.one, 195f, 24f, 95f, 32f, 5f, new Vector3(4f, 0f, 14f), 2.6f, 10, intro: 4.5f);
        // al caminar por el muelle: de lado, entre los dos veleros
        Shot(g, "Barcos", new Vector3(PierX, 0f, 20f), new Vector2(PierW, 22f), 90f, 16f, 22f, 36f, 5f, new Vector3(4f, 1f, 0f), 1.6f, 2,
             "LOS VELEROS DEL PUERTO", "");
        // la punta del muelle: hacia mar abierto, el horizonte y el velero lejano
        Shot(g, "Punta", new Vector3(PierX, 0f, PierEnd - 4f), new Vector2(12f, 8f), 10f, 9f, 12f, 44f, 7f, new Vector3(0f, 1.5f, 5f), 2.2f, 3,
             "LA PUNTA DEL MUELLE", "El mar abierto hasta donde alcanza la vista", still: true);
        // el embarcadero: la bahía, las barcas y el faro al fondo
        Shot(g, "Embarcadero", new Vector3(-18f, 0f, 17.5f), new Vector2(11f, 3.8f), 35f, 20f, 18f, 36f, 5f, new Vector3(5f, 0f, 6f), 1.8f, 2,
             "EL EMBARCADERO", "", still: true);
        // el extremo del malecón: el faro en su cabo
        Shot(g, "Faro", new Vector3(28f, 0f, -3f), new Vector2(6f, 6f), 45f, 14f, 16f, 38f, 8f, new Vector3(6f, 2f, 10f), 1.8f, 2,
             "EL FARO DEL CABO", "", still: true);
    }
}
