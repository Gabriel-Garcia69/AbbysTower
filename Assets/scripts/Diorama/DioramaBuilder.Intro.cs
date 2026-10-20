using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sets de la cinemática inicial (layout Intro), cada uno lejos de los otros: la ciudad donde aparece la torre, el
/// interior con las escaleras que bajan al abismo, el combate de los canalizadores, el funeral (2030) y la plaza del
/// discurso del capitán y la despedida (2035). La cámara y los textos los maneja Cinematica.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Vector3 SetCiudad = Vector3.zero, SetAbismo = new Vector3(2000f, 0f, 0f), SetBatalla = new Vector3(-2000f, 0f, 0f),
                            SetFuneral = new Vector3(0f, 0f, 2000f), SetPlaza = new Vector3(0f, 0f, -2000f);
    Material mEdificio, mAsfalto, mConcreto, mTorre, mRuna;

    void IntroMaterials()
    {
        mEdificio = Saved("Dio_Edificio", () =>
        {
            var m = new Material(LitShader);
#if UNITY_EDITOR
            foreach (var p in new[] { "Assets/Textures/Diorama/pix_edificio.png", "Assets/Textures/Diorama/pix_edificio_e.png" })
            {
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp != null) { imp.filterMode = FilterMode.Point; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Diorama/pix_edificio.png"));
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Diorama/pix_edificio_e.png"));
#endif
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white * 1.4f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetFloat("_Smoothness", 0.2f);
            return m;
        });
        mAsfalto = Plain("Dio_Asfalto", new Color(0.16f, 0.16f, 0.18f), 0.25f);
        mConcreto = Plain("Dio_Concreto", new Color(0.55f, 0.54f, 0.52f), 0.1f);
        mTorre = Plain("Dio_TorreAbismo", new Color(0.12f, 0.1f, 0.16f), 0.35f);
        mRuna = Glow("Dio_RunaAbismo", new Color(0.6f, 0.3f, 1f), 2.4f);
    }

    /// <summary>La torre del abismo: piedra oscura en gradas que se angostan, franjas y rendijas de luz violeta, aguja y un orbe.</summary>
    Transform TorreAbismo(Transform parent, string name, Vector3 pos, float s)
    {
        var t = Group(name, parent);
        t.localPosition = pos;
        float y = 0f;
        float[] radios = { 24f, 19f, 15f, 11f, 7.5f };
        float[] altos = { 55f, 50f, 46f, 42f, 38f };
        for (int i = 0; i < radios.Length; i++)
        {
            float r = radios[i] * s, h = altos[i] * s;
            Prim(t, PrimitiveType.Cylinder, "Grada", new Vector3(0f, y + h / 2f, 0f), new Vector3(r * 2f, h / 2f, r * 2f), mTorre);
            Prim(t, PrimitiveType.Cylinder, "Franja", new Vector3(0f, y + h - 1f * s, 0f), new Vector3(r * 2f + 0.6f, 0.5f * s, r * 2f + 0.6f), mRuna);
            for (int k = 0; k < 8; k++)
            {
                float a = (k / 8f + i * 0.0625f) * Mathf.PI * 2f;
                var slit = Box(t, "Rendija", new Vector3(Mathf.Cos(a) * (r + 0.05f), y + h * 0.5f, Mathf.Sin(a) * (r + 0.05f)), new Vector3(0.6f * s, h * 0.55f, 0.3f), mRuna, false);
                slit.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                slit.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            y += h;
        }
        var spire = MeshObject(t, "Aguja", Cone(1f, 1f, 8), mTorre, true);
        spire.transform.localPosition = new Vector3(0f, y, 0f);
        spire.transform.localScale = new Vector3(6f * s, 45f * s, 6f * s);
        var orb = Prim(t, PrimitiveType.Sphere, "Orbe", new Vector3(0f, y + 48f * s, 0f), Vector3.one * 5f * s, mRuna);
        orb.AddComponent<FloatingBob>().amplitude = 1.2f * s;
        AddLight(t, new Vector3(0f, y + 48f * s, 0f), new Color(0.6f, 0.3f, 1f), 120f * s, 6f);
        // la puerta abajo
        Box(t, "Puerta", new Vector3(0f, 9f * s, -radios[0] * s - 0.1f), new Vector3(10f * s, 18f * s, 0.6f), Glow("Dio_PuertaAbismo", new Color(0.25f, 0.08f, 0.45f), 1.6f), false);
        return t;
    }

    void Edificio(Transform parent, Vector3 pos, float w, float d, float h)
    {
        var b = Box(parent, "Edificio", pos + Vector3.up * h / 2f, new Vector3(w, h, d), mEdificio, false, 1f / 28f);
        b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        Box(parent, "Azotea", pos + Vector3.up * (h + 0.25f), new Vector3(w + 0.4f, 0.5f, d + 0.4f), mConcreto, false);
        if (rng.NextDouble() < 0.6) Box(parent, "Maquinas", pos + new Vector3(R(-w, w) * 0.25f, h + 1.2f, R(-d, d) * 0.25f), new Vector3(R(2f, 4f), 1.6f, R(2f, 4f)), mConcreto, false);
        if (h > 50f) AddLight(parent, pos + Vector3.up * (h + 2f), new Color(1f, 0.2f, 0.15f), 6f, 2f);   // luz de aviso en lo alto
    }

    void BuildIntro()
    {
        IntroMaterials();
        EnsurePlantMeshes();

        // ------------------------------------------------ la ciudad (2030) y la torre que aparece
        var c = Group("Set_Ciudad", root);
        c.localPosition = SetCiudad;
        Flat(c, "Suelo", new Vector3(0f, 0f, 0f), 2400f, 2400f, mConcreto, 0.05f);
        for (int k = -8; k <= 8; k++)
        {
            Flat(c, "Calle", new Vector3(0f, 0.02f, 30f + k * 60f), 1200f, 14f, mAsfalto, 0.1f);
            Flat(c, "Calle", new Vector3(30f + k * 60f, 0.021f, 0f), 14f, 1200f, mAsfalto, 0.1f);
        }
        Prim(c, PrimitiveType.Cylinder, "Plaza", new Vector3(0f, 0.03f, 0f), new Vector3(48f, 0.03f, 48f), Plain("Dio_PlazaClara", new Color(0.72f, 0.7f, 0.66f), 0.1f));
        for (int i = -7; i <= 7; i++)
            for (int j = -7; j <= 7; j++)
            {
                if (i == 0 && j == 0) continue;
                int ring = Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));
                var blk = new Vector3(i * 60f, 0f, j * 60f);
                int n = rng.Next(1, 4);
                for (int b = 0; b < n; b++)
                {
                    float w = n == 1 ? R(30f, 40f) : R(14f, 20f), d = n == 1 ? R(30f, 40f) : R(14f, 20f);
                    var off = n == 1 ? Vector3.zero : new Vector3(b % 2 == 0 ? -10f : 10f, 0f, b < 2 ? -10f : 10f);
                    float h = ring <= 2 ? R(30f, 85f) : ring <= 4 ? R(18f, 50f) : R(10f, 30f);
                    Edificio(c, blk + off, w, d, h);
                }
            }
        for (int k = 0; k < 16; k++)
        {
            float a = k / 16f * Mathf.PI * 2f;
            PortLamp(c, new Vector3(Mathf.Cos(a) * 26f, 0f, Mathf.Sin(a) * 26f));
        }
        for (int k = 0; k < 26; k++)   // coches en las calles cercanas
        {
            bool alongX = k % 2 == 0;
            float along = R(-150f, 150f), lane = (rng.Next(-2, 3)) * 60f + 30f + (rng.NextDouble() < 0.5 ? -3f : 3f);
            var pos = alongX ? new Vector3(along, 0f, lane) : new Vector3(lane, 0f, along);
            if (pos.magnitude < 40f) continue;
            var car = Group("Coche", c);
            car.localPosition = pos;
            car.localRotation = Quaternion.Euler(0f, alongX ? 90f : 0f, 0f);
            var paint = Plain("Dio_Coche" + (k % 4), new[] { new Color(0.7f, 0.12f, 0.1f), new Color(0.15f, 0.3f, 0.6f), new Color(0.85f, 0.85f, 0.82f), new Color(0.12f, 0.12f, 0.13f) }[k % 4], 0.6f);
            Box(car, "Carroceria", new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.7f, 4.2f), paint, false);
            Box(car, "Cabina", new Vector3(0f, 1.15f, 0.2f), new Vector3(1.6f, 0.5f, 2.2f), Plain("Dio_Vidrio", new Color(0.2f, 0.25f, 0.32f), 0.9f), false);
        }
        // la gente que se asoma a la plaza
        for (int k = 0; k < 14; k++)
        {
            float a = R(0f, Mathf.PI * 2f), r = R(38f, 48f);
            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            float yaw = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + 180f;
            Figura(c, "Persona", p, yaw, Persona.Civil(new[] { new Color(0.3f, 0.35f, 0.5f), new Color(0.6f, 0.25f, 0.2f), new Color(0.75f, 0.72f, 0.6f), new Color(0.2f, 0.2f, 0.22f) }[k % 4], new Color(0.12f, 0.08f, 0.05f)));
        }
        // la torre empieza bajo tierra: la cinemática la hace emerger
        TorreAbismo(c, "Torre", new Vector3(0f, -260f, 0f), 1f);
        var polvo = Group("Polvo", c);
        for (int k = 0; k < 6; k++)
        {
            float a = k / 6f * Mathf.PI * 2f;
            var sm = Group("Nube", polvo);
            sm.localPosition = new Vector3(Mathf.Cos(a) * 26f, 0.5f, Mathf.Sin(a) * 26f);
            Smoke(sm, Vector3.zero, new Color(0.6f, 0.55f, 0.5f, 0.6f), false);
        }
        foreach (var ps in polvo.GetComponentsInChildren<ParticleSystem>()) { var mn = ps.main; mn.playOnAwake = false; ps.Stop(); }
        for (int k = 0; k < 24; k++)   // escombros alrededor del cráter
        {
            float a = R(0f, Mathf.PI * 2f), r = R(22f, 34f);
            var e = Box(c, "Escombro", new Vector3(Mathf.Cos(a) * r, 0.3f, Mathf.Sin(a) * r), new Vector3(R(1f, 3f), R(0.4f, 1.2f), R(1f, 3f)), mConcreto, false);
            e.transform.localRotation = Quaternion.Euler(R(-20f, 20f), R(0f, 180f), R(-20f, 20f));
        }

        // ------------------------------------------------ adentro: las escaleras que bajan al abismo
        var ab = Group("Set_Abismo", root);
        ab.localPosition = SetAbismo;
        Box(ab, "Rellano", new Vector3(0f, -1f, -10f), new Vector3(70f, 2f, 40f), mTorre, false);
        Box(ab, "MuroO", new Vector3(-36f, 20f, 90f), new Vector3(2f, 300f, 240f), mTorre, false);
        Box(ab, "MuroE", new Vector3(36f, 20f, 90f), new Vector3(2f, 300f, 240f), mTorre, false);
        Box(ab, "MuroS", new Vector3(0f, 20f, -30f), new Vector3(74f, 300f, 2f), mTorre, false);
        Box(ab, "Techo", new Vector3(0f, 60f, 90f), new Vector3(74f, 2f, 240f), mTorre, false);
        for (int i = 0; i < 90; i++)   // la escalinata enorme que baja
        {
            float z = 10f + i * 2.2f, top = -i * 1.3f;
            Box(ab, "Escalon", new Vector3(0f, top - 2f, z), new Vector3(30f, 4f, 2.2f), i % 2 == 0 ? mTorre : mStoneBrick, false);
        }
        for (int i = 0; i < 14; i++)   // runas en las paredes que se pierden hacia abajo
            foreach (float sx in new[] { -1f, 1f })
                Box(ab, "Runa", new Vector3(sx * 34.9f, 8f - i * 9f, 14f + i * 12f), new Vector3(0.2f, 3f, 0.8f), mRuna, false);
        for (int i = 0; i < 16; i++)   // el ejército en el borde, mirando hacia abajo
        {
            float x = -14f + (i % 8) * 4f + R(-0.6f, 0.6f), z = 2f - (i / 8) * 3.5f + R(-0.5f, 0.5f);
            Figura(ab, "Soldado", new Vector3(x, 0f, z), 180f + R(-12f, 12f), Persona.Soldado(i % 3 == 0));
        }
        foreach (float sx in new[] { -1f, 1f })   // reflectores sobre trípode
        {
            var tri = Group("Reflector", ab);
            tri.localPosition = new Vector3(sx * 20f, 0f, 4f);
            Box(tri, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), mMetal, false);
            Box(tri, "Foco", new Vector3(0f, 3.1f, 0.2f), new Vector3(0.7f, 0.5f, 0.6f), mMetal, false);
            var l = new GameObject("Haz").AddComponent<Light>();
            l.transform.SetParent(tri, false);
            l.transform.localPosition = new Vector3(0f, 3.1f, 0.6f);
            l.transform.localRotation = Quaternion.Euler(30f, -sx * 12f, 0f);
            l.type = LightType.Spot; l.spotAngle = 45f; l.range = 90f; l.intensity = 30f; l.color = new Color(1f, 0.95f, 0.85f); l.shadows = LightShadows.Soft;
        }

        // ------------------------------------------------ los canalizadores contra las criaturas
        var bt = Group("Set_Batalla", root);
        bt.localPosition = SetBatalla;
        Flat(bt, "Suelo", Vector3.zero, 400f, 400f, mCaveRock, 0.25f);
        for (int i = 0; i < 26; i++)
        {
            float a = R(0f, Mathf.PI * 2f), r = R(14f, 60f);
            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (rng.NextDouble() < 0.5) Crystals(bt, p, R(0.8f, 2f), rng.NextDouble() < 0.4, i % 5 == 0);
            else Stalagmite(bt, p, R(3f, 9f));
        }
        string[] kinds = { "Bruto", "Sombra", "Sombra", "Saltador", "Torbellino", "Hechicero" };
        Vector3[] cpos = { new Vector3(4f, 0f, 5f), new Vector3(-5f, 0f, 6f), new Vector3(1f, 0f, 9f), new Vector3(7f, 0f, -2f), new Vector3(-8f, 0f, 1f), new Vector3(-2f, 0f, 12f) };
        for (int i = 0; i < kinds.Length; i++)
        {
            var pf = Resources.Load<GameObject>("Enemigos/" + kinds[i]);
            if (pf == null) continue;
            var cr = Group("Criatura", bt);
            cr.localPosition = cpos[i];
            cr.localRotation = Quaternion.Euler(0f, 180f + R(-30f, 30f), 0f);
            var v = (GameObject)Instantiate(pf, cr);
            v.transform.localScale = Vector3.one * (kinds[i] == "Bruto" ? 1.6f : 1.1f);
            cr.gameObject.AddComponent<CriaturaProp>();
        }
        // los canalizadores: armas de energía, uno con bastón lanzando un hechizo
        Color[] energias = { new Color(0.35f, 0.8f, 1f), new Color(1f, 0.55f, 0.2f), new Color(0.5f, 1f, 0.6f), new Color(0.35f, 0.8f, 1f) };
        Vector3[] hpos = { new Vector3(2f, 0f, -3f), new Vector3(-3f, 0f, -2f), new Vector3(6f, 0f, -6f), new Vector3(-7f, 0f, -5f) };
        for (int i = 0; i < hpos.Length; i++)
        {
            var hp = Persona.Cazador(energias[i]);
            if (i == 3) hp.arma = 3;
            var f = Figura(bt, "Canalizador", hpos[i], 0f, hp);
            var arm = f.Find("BrazoD");
            if (arm != null) arm.localRotation = Quaternion.Euler(i == 3 ? -150f : -80f, 0f, i % 2 == 0 ? 20f : -10f);
            Smoke(f, new Vector3(0f, 0.2f, 0f), new Color(energias[i].r, energias[i].g, energias[i].b, 0.35f), true);   // el aura de energía
        }
        Prim(bt, PrimitiveType.Sphere, "Hechizo", new Vector3(-4f, 2.4f, 3f), Vector3.one * 0.9f, Glow("Dio_Hechizo", new Color(0.5f, 1f, 0.6f), 3f));
        AddLight(bt, new Vector3(-4f, 2.4f, 3f), new Color(0.5f, 1f, 0.6f), 10f, 4f);
        // una criatura caída deja minerales que brillan
        var min = Group("Minerales", bt);
        min.localPosition = new Vector3(10f, 0f, 2f);
        Crystals(min, Vector3.zero, 0.5f, false, true);
        Crystals(min, new Vector3(0.8f, 0f, 0.4f), 0.35f, true, false);
        Crystals(min, new Vector3(-0.6f, 0f, 0.5f), 0.3f, false, false);
        var hunter = Figura(bt, "Recolector", new Vector3(11.5f, 0f, -0.6f), -40f, Persona.Cazador(new Color(1f, 0.55f, 0.2f)));
        hunter.localScale = new Vector3(1f, 0.82f, 1f);   // agachado

        // ------------------------------------------------ el funeral (2030)
        var fu = Group("Set_Funeral", root);
        fu.localPosition = SetFuneral;
        Flat(fu, "Pasto", Vector3.zero, 600f, 600f, mGrass, 0.2f);
        for (int i = 0; i < 70; i++)   // lápidas en hileras
        {
            float x = -36f + (i % 14) * 5.5f + R(-0.4f, 0.4f), z = 12f + (i / 14) * 6f;
            if (Mathf.Abs(x) < 4f && z < 14f) continue;
            var st = Box(fu, "Lapida", new Vector3(x, 0.55f, z), new Vector3(0.7f, 1.1f, 0.2f), mStoneBrick, false);
            st.transform.localRotation = Quaternion.Euler(R(-3f, 3f), R(-6f, 6f), R(-3f, 3f));
        }
        for (int i = 0; i < 18; i++) Pine(fu, new Vector2(R(-60f, 60f), R(30f, 70f)), 0f, R(1.2f, 1.8f), false);
        for (int i = 0; i < 10; i++) Pine(fu, new Vector2(i < 5 ? R(-45f, -25f) : R(25f, 45f), R(-20f, 10f)), 0f, R(1.1f, 1.6f), false);
        House(fu, new Vector3(0f, 0f, 70f), 180f, 9f, 7f, 5f, false);   // la capilla al fondo
        // el ataúd con flores y la foto del padre
        var coffin = Group("Ataud", fu);
        coffin.localPosition = new Vector3(0f, 0f, 4f);
        Box(coffin, "Base", new Vector3(0f, 0.4f, 0f), new Vector3(2.4f, 0.8f, 1f), mTimber, false);
        Box(coffin, "Caja", new Vector3(0f, 1f, 0f), new Vector3(2.2f, 0.5f, 0.75f), Plain("Dio_Ataud", new Color(0.25f, 0.13f, 0.08f), 0.5f), false);
        for (int i = 0; i < 14; i++)
            Prim(coffin, PrimitiveType.Sphere, "Flor", new Vector3(R(-0.8f, 0.8f), 1.3f, R(-0.25f, 0.25f)), Vector3.one * R(0.12f, 0.2f), mFlowers[rng.Next(mFlowers.Length)]);
        var foto = Group("Foto", fu);
        foto.localPosition = new Vector3(2.2f, 0f, 4.4f);
        Box(foto, "Caballete", new Vector3(0f, 0.7f, 0.1f), new Vector3(0.08f, 1.4f, 0.08f), mTimber, false);
        Box(foto, "Marco", new Vector3(0f, 1.35f, 0f), new Vector3(0.65f, 0.85f, 0.06f), Plain("Dio_MarcoOro", new Color(0.7f, 0.55f, 0.25f), 0.6f), false);
        Box(foto, "Retrato", new Vector3(0f, 1.35f, -0.035f), new Vector3(0.5f, 0.7f, 0.02f), Plain("Dio_Retrato", new Color(0.45f, 0.42f, 0.38f), 0.2f), false);
        // los invitados de luto (con paraguas)
        var luto = new Color(0.07f, 0.07f, 0.08f);
        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.Lerp(-1.1f, 1.1f, i / 7f);
            var p = new Vector3(Mathf.Sin(a) * 6.5f, 0f, 4f - Mathf.Cos(a) * 6.5f);
            if (Mathf.Abs(p.x) < 2.2f) continue;
            var pe = Persona.Civil(luto, i % 3 == 0 ? new Color(0.5f, 0.45f, 0.4f) : new Color(0.1f, 0.07f, 0.05f));
            pe.abrigo = true; pe.paraguas = i % 2 == 0; pe.peloLargo = i % 2 == 1;
            Figura(fu, "Invitado", p, Mathf.Atan2(-p.x, 4f - p.z) * Mathf.Rad2Deg + 180f, pe);
        }
        var invitadas = Group("Invitadas", fu);   // las dos que platican (adelante, a la izquierda)
        invitadas.localPosition = new Vector3(-6f, 0f, -4f);
        var i1 = Persona.Civil(luto, new Color(0.35f, 0.25f, 0.18f)); i1.abrigo = true; i1.peloLargo = true; i1.paraguas = true;
        var i2 = Persona.Civil(luto, new Color(0.55f, 0.5f, 0.45f)); i2.abrigo = true; i2.peloLargo = true;
        Figura(invitadas, "Invitada1", new Vector3(-0.6f, 0f, 0f), 120f, i1);
        Figura(invitadas, "Invitada2", new Vector3(0.6f, 0f, 0.2f), -120f, i2);
        // Abis (12 años) y su hermano (17), frente al ataúd
        var herm = Group("Hermanos", fu);
        herm.localPosition = new Vector3(0f, 0f, 0.5f);
        var hermano = Persona.Civil(luto, new Color(0.1f, 0.07f, 0.05f), 0.98f); hermano.abrigo = true;
        var hf = Figura(herm, "Hermano", new Vector3(0.6f, 0f, 0f), 180f, hermano);
        var brazo = hf.Find("BrazoI"); if (brazo != null) brazo.localRotation = Quaternion.Euler(0f, 0f, -35f);   // la mano en el hombro de Abis
#if UNITY_EDITOR
        AbisSprite(herm, new Vector3(-0.5f, 0f, 0f), 0.82f, "north");
#endif
        // lluvia
        var rain = new GameObject("Lluvia").AddComponent<ParticleSystem>();
        rain.transform.SetParent(fu, false);
        rain.transform.localPosition = new Vector3(0f, 18f, 0f);
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var rm = rain.main; rm.startLifetime = 1.4f; rm.startSpeed = 16f; rm.startSize = 0.05f; rm.maxParticles = 4000; rm.prewarm = true;
        rm.startColor = new Color(0.8f, 0.85f, 0.95f, 0.5f); rm.simulationSpace = ParticleSystemSimulationSpace.World;
        var re = rain.emission; re.rateOverTime = 2200f;
        var rs = rain.shape; rs.shapeType = ParticleSystemShapeType.Box; rs.scale = new Vector3(70f, 1f, 50f);
        rain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.lengthScale = 6f; rr.sharedMaterial = mDust;
        rain.Play();

        // ------------------------------------------------ la plaza (2035): discurso del capitán y despedida
        var pl = Group("Set_Plaza", root);
        pl.localPosition = SetPlaza;
        Flat(pl, "Plaza", Vector3.zero, 2400f, 2400f, Plain("Dio_PlazaClara", new Color(0.72f, 0.7f, 0.66f), 0.1f), 0.05f);
        TorreAbismo(pl, "Torre", new Vector3(0f, 0f, 150f), 1f);
        for (int i = 0; i < 40; i++)   // barricadas alrededor de la torre
        {
            float a = i / 40f * Mathf.PI * 2f;
            Box(pl, "Barrera", new Vector3(Mathf.Cos(a) * 40f, 0.6f, 150f + Mathf.Sin(a) * 40f), new Vector3(3f, 1.2f, 0.5f), Plain("Dio_Barrera", new Color(0.85f, 0.6f, 0.15f), 0.3f), false)
                .transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
        }
        for (int i = -6; i <= 6; i++) if (Mathf.Abs(i) > 1) Edificio(pl, new Vector3(i * 45f, 0f, 260f + Mathf.Abs(i) * 8f), 30f, 30f, R(30f, 80f));
        for (int i = -5; i <= 5; i++) { Edificio(pl, new Vector3(-170f, 0f, i * 40f), 30f, 30f, R(25f, 60f)); Edificio(pl, new Vector3(170f, 0f, i * 40f), 30f, 30f, R(25f, 60f)); }
        var stage = Group("Escenario", pl);
        stage.localPosition = new Vector3(0f, 0f, 40f);
        Box(stage, "Tarima", new Vector3(0f, 0.7f, 0f), new Vector3(18f, 1.4f, 8f), mTimber, false);
        Box(stage, "Podio", new Vector3(0f, 2f, -1.8f), new Vector3(1.2f, 1.2f, 0.7f), Plain("Dio_Podio", new Color(0.15f, 0.2f, 0.35f), 0.4f), false);
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 2; k++)
            {
                float x = sx * (6f + k * 3.5f);
                Box(stage, "Asta", new Vector3(x, 4f, 2.5f), new Vector3(0.15f, 8f, 0.15f), mMetal, false);
                Box(stage, "Estandarte", new Vector3(x, 5.6f, 2.45f), new Vector3(2.2f, 4f, 0.05f), Plain("Dio_Estandarte" + k, k == 0 ? new Color(0.15f, 0.22f, 0.45f) : new Color(0.6f, 0.12f, 0.1f), 0.2f), false);
                Box(stage, "Emblema", new Vector3(x, 6.2f, 2.42f), new Vector3(0.9f, 0.9f, 0.02f), Glow("Dio_Emblema", new Color(1f, 0.8f, 0.4f), 1.1f), false);
            }
        var cap = new Persona { torso = new Color(0.14f, 0.17f, 0.26f), piernas = new Color(0.1f, 0.11f, 0.16f), piel = new Color(0.72f, 0.55f, 0.43f),
                                pelo = new Color(0.4f, 0.38f, 0.36f), escala = 1.05f, sombrero = 2, medallas = true, arma = 2, energia = new Color(1f, 0.8f, 0.4f) };
        var capF = Figura(stage, "Capitan", new Vector3(0f, 1.4f, -1f), 0f, cap);
        var capArm = capF.Find("BrazoI"); if (capArm != null) capArm.localRotation = Quaternion.Euler(-70f, 0f, 15f);   // el brazo en alto
        for (int row = 0; row < 4; row++)   // la expedición formada
            for (int k = 0; k < 9; k++)
            {
                var p = new Vector3(-12f + k * 3f, 0f, 28f - row * 3f);
                Figura(pl, "Expedicionario", p, 180f, row % 2 == 0 ? Persona.Cazador(energias[k % energias.Length]) : Persona.Soldado(false));
            }
        for (int k = 0; k < 40; k++)   // la gente detrás
        {
            var p = new Vector3(R(-26f, 26f), 0f, R(-6f, 12f));
            Figura(pl, "Publico", p, 180f + R(-15f, 15f), Persona.Civil(new[] { new Color(0.3f, 0.35f, 0.5f), new Color(0.6f, 0.25f, 0.2f), new Color(0.75f, 0.72f, 0.6f), new Color(0.2f, 0.4f, 0.3f) }[k % 4], new Color(0.12f, 0.08f, 0.05f), R(0.9f, 1.05f)));
        }
        foreach (float sx in new[] { -1f, 1f })   // camiones de la expedición
            for (int k = 0; k < 3; k++)
            {
                var tr = Group("Camion", pl);
                tr.localPosition = new Vector3(sx * (36f + k * 6f), 0f, 20f - k * 4f);
                var green = Plain("Dio_Militar", new Color(0.25f, 0.3f, 0.2f), 0.2f);
                Box(tr, "Caja", new Vector3(0f, 2f, 1.2f), new Vector3(2.6f, 2.6f, 5f), green, false);
                Box(tr, "Cabina", new Vector3(0f, 1.5f, -2.6f), new Vector3(2.5f, 2f, 2f), green, false);
                foreach (float wx in new[] { -1.2f, 1.2f }) foreach (float wz in new[] { -2.6f, 0f, 2.6f })
                    Prim(tr, PrimitiveType.Cylinder, "Rueda", new Vector3(wx, 0.55f, wz), new Vector3(1.1f, 0.2f, 1.1f), Plain("Dio_Llanta", new Color(0.06f, 0.06f, 0.06f), 0.2f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        // la despedida: Abis (17) y su hermano listo para la expedición, junto a un camión
        var desp = Group("Despedida", pl);
        desp.localPosition = new Vector3(44f, 0f, 8f);
        var hExp = Persona.Cazador(new Color(0.35f, 0.8f, 1f)); hExp.escala = 1.02f;
        Figura(desp, "Hermano", new Vector3(0.7f, 0f, 0f), 90f, hExp);
#if UNITY_EDITOR
        AbisSprite(desp, new Vector3(-0.7f, 0f, 0f), 1f, "east");
#endif

        BuildAtmosphere();
        var cin = new GameObject("Cinematica");
        cin.transform.SetParent(root, false);
        cin.AddComponent<Cinematica>();
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(gameObject);
#endif
    }
}
