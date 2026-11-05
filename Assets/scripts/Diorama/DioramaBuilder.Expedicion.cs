using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tutorial — la expedición en el piso 20 (propuesta del guion: "que sea la expedición en el piso 20 y muestre cómo
/// fueron derrotados"). Una gruta oscura: el campamento de la expedición, un escalón para aprender a saltar, la zona de
/// entrenamiento, la barrera de energía que se cruza con impulso, la pelea junto a la expedición y el borde del abismo
/// donde aparece la criatura que los derrota. Lo maneja ExpedicionPiso20.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect ExpArea = new Rect(-70f, -112f, 140f, 308f);
    static readonly Rect ExpWalk = new Rect(-24f, -66f, 48f, 196f);
    static readonly Vector3 ExpSpawn = new Vector3(0f, 0.1f, -58f);
    const float ExpLedgeZ0 = -30f, ExpLedgeZ1 = -14f, ExpVeilZ = 4f, ExpEdgeZ = 128f;

    const float ExpBloque = 2.1f, ExpEscalon = 1.5f;   // lado y alto de los bloques de roca, como las terrazas del Piso 1
    const float ExpFondo = 80f;   // la pared de atrás, lejos de la salida: la cámara queda dentro de la sala

    /// <summary>Medio ancho de la gruta a la altura z: se abre en salas grandes (campamento, entrenamiento y oleada) y se cierra entre ellas.</summary>
    public static float ExpMitad(float z)
    {
        float Sala(float c, float s) { float k = (z - c) / s; return Mathf.Exp(-k * k); }
        return 21f + 5f * Sala(-48f, 13f) + 6f * Sala(30f, 20f) + 9f * Sala(78f, 25f) + Mathf.Sin(z * 0.11f) * 1.2f;
    }

    /// <summary>Número fijo de 0 a 1 por bloque, para que cada uno tenga su propia altura.</summary>
    static float AzarBloque(float bx, float bz)
    {
        float s = Mathf.Sin(bx * 12.9898f + bz * 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    float ExpHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (z > ExpEdgeZ + e * 0.8f) return -40f;   // el abismo
        // todo va por bloques: se toma el centro del bloque, así las orillas quedan rectas y la pared sube en terrazas de roca
        float bx = (Mathf.Floor(x / ExpBloque) + 0.5f) * ExpBloque, bz = (Mathf.Floor(z / ExpBloque) + 0.5f) * ExpBloque;
        float azar = AzarBloque(bx, bz);
        float roca = ExpRoca(bx, bz);
        if (roca > 0f) return Mathf.Min(ExpTecho + 1.5f, (Mathf.Ceil(roca / ExpEscalon) + (azar > 0.72f ? 1f : 0f)) * ExpEscalon);
        // al pie de la pared, dos escalones de roca; decoración: el camino y las salas quedan libres
        float aPared = Mathf.Min(ExpMitad(bz) - Mathf.Abs(bx), bz + ExpFondo) + (azar - 0.5f) * 2.2f;
        float base0 = aPared < 1.8f ? 0.9f : aPared < 3.6f ? 0.45f : 0f;
        bool escalon = z > ExpLedgeZ0 + e * 0.6f && z < ExpLedgeZ1 + e * 0.6f;
        // losas sueltas: el suelo deja de ser plano, sin estorbar (más bajas que lo que Dante sube sin saltar)
        float losa = Mathf.Abs(bx - SenderoX(bz)) > 5f && Noise(bx * 0.09f + 9f, bz * 0.09f + 4f) > 0.6f ? 0.2f : 0f;
        return Mathf.Max(base0, (escalon ? 0.9f : 0f) + losa);
    }

    /// <summary>Altura de la roca de las paredes de la gruta: sube en curva con bultos (0 = suelo).</summary>
    static float ExpRoca(float x, float z)
    {
        float e = EdgeNoise(x, z);
        float d = Mathf.Max(Mathf.Abs(x) - ExpMitad(z) + e * 2.5f, -(z + ExpFondo) + e * 2.5f);
        if (d <= 0f) return 0f;
        return Mathf.Min(ExpTecho + 2f, d * d * 0.22f + d * 0.9f + Noise(x * 0.22f + 3f, z * 0.22f) * 4f * Mathf.Clamp01(d / 4f));
    }

    /// <summary>El sendero serpentea suave (una curva con muchos puntos, sin esquinas).</summary>
    /// <summary>La x del centro del sendero a la altura z (la usan el trazado y la distribución de la expedición).</summary>
    public static float SenderoX(float z) => Mathf.Sin(z * 0.06f) * 2.6f + Mathf.Sin(z * 0.17f + 1f) * 0.8f;

    static Vector2[] Sendero(float za, float zb)
    {
        int n = Mathf.CeilToInt((zb - za) / 2.5f);
        var p = new Vector2[n + 1];
        for (int i = 0; i <= n; i++) { float z = Mathf.Lerp(za, zb, i / (float)n); p[i] = new Vector2(SenderoX(z), z); }
        return p;
    }

    void BuildExpedition()
    {
        MaterialesExpedicion();   // texturas de Assets/Textures/Expedicion; todas las rocas (mCaveRock) pasan a pared_roca
        BuildTerrain();
        // la cámara sube sobre lo que se llame "Terreno"; aquí las paredes son del terreno y la mandarían encima del techo
        foreach (Transform t in root.Find("Terreno")) t.name = "RocaGruta";
        var g = Group("Gruta", root);
        var lvl = Group("Nivel", root);

        // cristales y estalagmitas por las paredes: cada uno se apoya en lo que haya de verdad debajo (suelo o roca de la pared);
        // el cálculo analítico de la altura no coincide con la malla suavizada y los dejaba en el aire. Sin superficie, no se pone.
        Physics.SyncTransforms();
        var cristales = Group("CristalesBase", g);
        for (int i = 0; i < 60; i++)
        {
            float z = R(-64f, 126f), x = (rng.NextDouble() < 0.5 ? -1f : 1f) * (ExpMitad(z) - R(0.5f, 5f));   // al pie de la pared, sea cual sea el ancho de la sala
            bool cristal = rng.NextDouble() < 0.5;
            float h = cristal ? 0f : R(2f, 6f);
            bool violeta = rng.NextDouble() < 0.4;
            if (!SuperficieBajo(x, z, out float y)) continue;
            if (cristal) CristalesFacetados(cristales, new Vector3(x, y, z), violeta, i % 4 == 0);   // racimo pequeño al pie de la pared
            else PilaDeRoca(g, new Vector3(x, y - 0.1f, z), h);
        }
        // el campamento de la expedición
        Tent(g, new Vector3(-10f, 0f, -56f), 30f, mCloth);
        Tent(g, new Vector3(10f, 0f, -54f), -30f, mClothB);
        Campfire(g, new Vector3(0f, 0f, -50f));
        Clutter(g, new Vector3(-6f, 0f, -60f));
        Clutter(g, new Vector3(7f, 0f, -61f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var tri = Group("Reflector", g);
            tri.localPosition = new Vector3(sx * 12f, 0f, -44f);
            Box(tri, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), mMetal, false);
            Box(tri, "Foco", new Vector3(0f, 3.1f, 0.2f), new Vector3(0.7f, 0.5f, 0.6f), mMetal, false);
            var l = new GameObject("Haz").AddComponent<Light>();
            l.transform.SetParent(tri, false);
            l.transform.localPosition = new Vector3(0f, 3.1f, 0.6f);
            l.transform.localRotation = Quaternion.Euler(25f, -sx * 15f, 0f);
            l.type = LightType.Spot; l.spotAngle = 55f; l.range = 34f; l.intensity = 9f; l.color = new Color(1f, 0.82f, 0.62f); l.shadows = LightShadows.None;
        }
        // un letrero de la expedición: "PISO 20"
        var sign = Group("Letrero", g);
        sign.localPosition = new Vector3(5f, 0f, -45f);
        Box(sign, "Poste", new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), mTimber, false);
        Box(sign, "Tabla", new Vector3(0f, 1.7f, -0.05f), new Vector3(1.4f, 0.6f, 0.08f), mPlank, false);
        Box(sign, "Marca", new Vector3(0f, 1.7f, -0.1f), new Vector3(1f, 0.25f, 0.02f), Glow("Dio_MarcaPiso", new Color(1f, 0.8f, 0.4f), 1.2f), false);


        // el borde del abismo y la oscuridad de abajo
        for (int i = 0; i < 9; i++)
        {
            float cx = -16f + i * 4f;
            if (Mathf.Abs(cx) < 10f) continue;   // a los lados: al centro tapaban a la criatura en la cinemática
            if (SuperficieBajo(cx, ExpEdgeZ - 1.2f, out float cy)) CristalesFacetados(cristales, new Vector3(cx, cy, ExpEdgeZ - 1.2f), true, i % 3 == 0);
        }
        Fundir(cristales);   // una malla por material (los racimos con luz conservan su luz)
        Flat(g, "Fondo", new Vector3(0f, -39f, ExpEdgeZ + 60f), 200f, 120f, Plain("Dio_Negro", Color.black, 0f), 0.1f);
        // la criatura que espera abajo (la cinemática la sube)
        var colosoPf = Resources.Load<GameObject>("Enemigos/Jefe");
        var coloso = Group("Coloso", lvl);
        coloso.localPosition = new Vector3(0f, -48f, ExpEdgeZ + 28f);   // mide ~31 m: al subir, los ojos quedan sobre el borde
        if (colosoPf != null)
        {
            var v = (GameObject)Instantiate(colosoPf, coloso);
            v.transform.localScale = Vector3.one * 7f;
            v.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var prop = coloso.gameObject.AddComponent<CriaturaProp>();
            prop.cuerpo = new Color(0.1f, 0.08f, 0.13f); prop.ojos = new Color(1f, 0.15f, 0.1f); prop.bob = 0.6f;
        }
        // el sprite se alumbra por lo que tiene encima: una luz fuerte arriba y un poco al frente, y otra roja que le da el tono
        // (apagadas hasta la cinemática: si no, tiñen el fondo de la sala antes de que aparezca)
        AddLight(coloso, new Vector3(0f, 58f, -24f), new Color(0.92f, 0.84f, 0.88f), 85f, 2600f).enabled = false;
        AddLight(coloso, new Vector3(0f, 40f, -24f), new Color(1f, 0.22f, 0.16f), 55f, 700f).enabled = false;

        // la expedición: el capitán y los cazadores (ExpedicionPiso20 los va moviendo)
        var exp = Group("Expedicion", lvl);
        // TODO: faltan los sprites propios del capitán y los soldados: todos usan el cazador de la expedición (otra escala y tinte).
        // Si se agregan carpetas con las 8 rotaciones en Assets/sprites/Expedicion (p. ej. Capitan, Soldado), se usan solas:
        // "Capitan" para el capitán y las demás se mezclan en 3 miembros.
        var juegos = JuegosDeFiguras();
        juegos.TryGetValue("Cazador", out var cazador);
        juegos.TryGetValue("Capitan", out var capSet);
        var otros = new List<Sprite[]>();
        foreach (var kv in juegos) if (kv.Key != "Cazador" && kv.Key != "Capitan") otros.Add(kv.Value);
        Color[] tintes = { new Color(1f, 1f, 1f), new Color(0.92f, 1f, 1.05f), new Color(1.05f, 0.97f, 0.9f),
                           new Color(0.95f, 0.95f, 1.08f), new Color(1.08f, 1f, 0.95f), new Color(0.9f, 1.04f, 1f) };   // ningún canal baja de 0,85
        // puestos: semicírculo al norte de la fogata (hacia ella y los braseros), fuera del sendero, a 1,2 m entre sí y a 2 m de Dante
        var fogata = new Vector2(0f, -50f);
        var puestos = DistribucionExpedicion.Puestos(DistribucionExpedicion.Semicirculo(fogata, 4.2f, 6.2f, 10f, 170f), 7, new Vector2(ExpSpawn.x, ExpSpawn.z), SenderoX, rng);
        if (puestos.Length < 7) puestos = DistribucionExpedicion.Puestos(DistribucionExpedicion.Franjas(SenderoX, -50f, 6f), 7, new Vector2(ExpSpawn.x, ExpSpawn.z), SenderoX, rng);
        int usados = 0;
        for (int k = 0; k < puestos.Length; k++)
        {
            Vector2 d = fogata - puestos[k];   // de cara a la fogata
            float yaw = Quaternion.LookRotation(-new Vector3(d.x, 0f, d.y)).eulerAngles.y;
            var pos = new Vector3(puestos[k].x, 0f, puestos[k].y);
            if (k == 0) FiguraSprite(exp, "Capitan", pos, yaw, 1.12f, new Color(1f, 0.94f, 0.86f), capSet ?? cazador);
            else
            {
                int m = k - 1;   // miembros 1, 3 y 5 con otro sprite si hay
                var set = m % 2 == 1 && otros.Count > 0 ? otros[usados++ % otros.Count] : cazador;
                FiguraSprite(exp, "Miembro" + m, pos, yaw, R(0.95f, 1.05f), tintes[m % tintes.Length], set);
            }
        }

        // la fuente de curación (GDD): entre el entrenamiento y la barrera; el tutorial enseña a usarla con E
        HealShrine(lvl, new Vector3(7f, 0f, 42f));

        // combate del tutorial: entrenamiento y la oleada con la expedición
        var a1 = Arena(lvl, "Entrenamiento", new Vector3(0f, 0f, 30f), new Vector2(30f, 18f), 3);
        a1.roster = new[] { TowerEnemy.Kind.Sombra };
        a1.leash = 10f;
        var a2 = Arena(lvl, "La oleada", new Vector3(0f, 0f, 78f), new Vector2(34f, 22f), 6);
        a2.roster = new[] { TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Bruto, TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Saltador };
        a2.leash = 12f;

        Trail(g, 3.6f, Sendero(-60f, ExpLedgeZ0 - 1f));
        Trail(g, 3.6f, Sendero(ExpLedgeZ1 + 1f, ExpEdgeZ - 3f));
        for (float z = -40f; z < ExpEdgeZ; z += 16f) foreach (float sx in new[] { -1f, 1f })
        {
            Brazier(lvl, new Vector3(sx * 7f, 0f, z));
            // charco cálido: más rango e intensidad que el brasero normal para que el sendero se lea
            foreach (var bl in lvl.GetChild(lvl.childCount - 1).GetComponentsInChildren<Light>()) { bl.range = 12f; bl.intensity = 4.5f; }
        }
        var charco = AddLight(g, new Vector3(0f, 1.6f, -50f), new Color(1f, 0.6f, 0.3f), 20f, 5f);   // la fogata del campamento
        charco.gameObject.AddComponent<LightFlicker>().amount = 0.2f;
        VolumenExpedicion();
        LuzDeSala(g);

        var oclusion = new GameObject("Oclusion");   // atenúa los huesos, pilares y estalagmitas que tapan a Dante
        oclusion.transform.SetParent(root, false);
        oclusion.AddComponent<OclusionSuave>();
        DecalesSuelo(g);   // grietas y charcos con el reflejo del fuego (después del sendero: se apartan de él)
        GrutaUnica(g);
        WalkBoundary(ExpWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 14f, true, true, true, true, 0);

        var dir = new GameObject("Tutorial");
        dir.transform.SetParent(root, false);
        dir.AddComponent<ExpedicionPiso20>();
        BuildAtmosphere();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    // ------------------------------------------------------------------ la expedición como sprites

    /// <summary>La superficie sólida (suelo o pared) justo debajo del punto (x, z), en coordenadas del mapa; false si no hay nada.</summary>
    bool SuperficieBajo(float x, float z, out float y)
    {
        y = 0f;
        if (!Physics.Raycast(root.TransformPoint(new Vector3(x, 60f, z)), Vector3.down, out var h, 140f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        y = root.InverseTransformPoint(h.point).y;
        return true;
    }

    bool avisoSprites;   // un solo aviso por generación

    static readonly string[] DirNombres = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    /// <summary>
    /// Una persona de la expedición como billboard con las 8 rotaciones del cazador (Assets/sprites/Expedicion/Cazador).
    /// La raíz (que ExpedicionPiso20 mueve; su frente es -Z) sigue siendo un Group con el mismo nombre; el sprite va en "Visual".
    /// </summary>
    /// <summary>Las carpetas de Assets/sprites/Expedicion que traen las 8 rotaciones (east.png, north-east.png...) como Sprite, por nombre de carpeta.</summary>
    Dictionary<string, Sprite[]> JuegosDeFiguras()
    {
        var d = new Dictionary<string, Sprite[]>();
#if UNITY_EDITOR
        foreach (var carpeta in UnityEditor.AssetDatabase.GetSubFolders("Assets/sprites/Expedicion"))
        {
            var set = new Sprite[8];
            string faltan = "";
            for (int i = 0; i < 8; i++)
            {
                set[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/" + DirNombres[i] + ".png");
                if (set[i] == null) faltan += DirNombres[i] + " ";
            }
            if (faltan.Length == 0) d[System.IO.Path.GetFileName(carpeta)] = set;
            else Debug.LogWarning("[Expedicion] " + carpeta + ": faltan o no son Sprite: " + faltan + "(reimporta la carpeta y vuelve a generar).");
        }
#endif
        return d;
    }

    Transform FiguraSprite(Transform parent, string name, Vector3 pos, float yaw, float escala, Color tinte, Sprite[] set)
    {
        var f = Group(name, parent);
        f.localPosition = pos;
        f.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var v = new GameObject("Visual");
        v.transform.SetParent(f, false);
        v.transform.localScale = Vector3.one * escala;
        var sr = v.AddComponent<SpriteRenderer>();
        sr.color = tinte;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        sr.receiveShadows = true;
        var fb = v.AddComponent<FiguraBillboard>();
        fb.raiz = f; fb.sr = sr;
        fb.rotaciones = set != null ? set : new Sprite[8];
        if (set == null && !avisoSprites)
        {
            avisoSprites = true;
            Debug.LogWarning("[Expedicion] No hay sprites para las figuras de la expedición (Assets/sprites/Expedicion/Cazador): saldrán invisibles. Reimporta la carpeta y vuelve a generar.");
        }
#if UNITY_EDITOR
        // el mismo material con luz 3D que el sprite de Abby
        var abby = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Abby.prefab");
        var asr = abby != null ? abby.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (asr != null) sr.sharedMaterial = asr.sharedMaterial;
#endif
        sr.sprite = fb.rotaciones[6] != null ? fb.rotaciones[6] : null;
        return f;
    }

    /// <summary>
    /// Postproceso propio de la gruta del tutorial (prioridad sobre el del Diorama): levanta las sombras y los medios tonos y
    /// la exposición para que el jugador y el sendero se lean, sin perder el tono frío. Perfil: Assets/Settings/ExpedicionProfile.asset.
    /// </summary>
    /// <summary>La gruta como sala grande: luz alta y pareja que recorta las paredes de roca, con haces que bajan de las grietas del techo.</summary>
    /// <summary>Peñasco de bloques apilados, cada uno más chico y un poco corrido (combina con las terrazas de la pared).</summary>
    void PilaDeRoca(Transform parent, Vector3 pos, float alto)
    {
        var pila = Group("PilaDeRoca", parent);
        pila.localPosition = pos;
        float y = 0f, lado = alto * 0.75f + 0.8f;
        while (y < alto && lado > 0.6f)
        {
            float h = Mathf.Min(R(1f, 1.8f), alto - y + 0.3f);
            var b = Box(pila, "Bloque", new Vector3(R(-0.25f, 0.25f), y + h / 2f, R(-0.25f, 0.25f)), new Vector3(lado, h, lado * R(0.8f, 1.1f)), rng.NextDouble() < 0.5 ? mExpPared : mExpParedAlt, false, 0.25f);
            b.transform.localRotation = Quaternion.Euler(0f, R(0f, 90f), 0f);
            y += h * 0.9f; lado *= R(0.62f, 0.78f);
        }
        Fundir(pila);
        foreach (var mr in pila.GetComponentsInChildren<MeshRenderer>()) MarcarOcluible(mr);
    }

    void LuzDeSala(Transform g)
    {
        var luces = Group("LuzDeSala", g);
        var haz = MatDecal("Dio_ExpHaz", "haz", true, new Color(0.52f, 0.5f, 0.72f) * 0.22f);
        foreach (float z in new[] { -46f, -6f, 30f, 78f, 116f })
        {
            AddLight(luces, new Vector3(0f, ExpTecho - 6f, z), new Color(0.74f, 0.71f, 0.84f), 52f, 420f);   // de arriba: el suelo y los sprites
            foreach (float lado in new[] { -1f, 1f })   // a media altura: recorta las terrazas de la pared
                AddLight(luces, new Vector3(lado * (ExpMitad(z) - 9f), 8f, z), new Color(0.66f, 0.64f, 0.78f), 26f, 90f);
            foreach (float sx in new[] { -1f, 1f })
            {
                float x = sx * (ExpMitad(z) - R(7f, 11f)), zz = z + R(-7f, 7f), ancho = R(5f, 8f);
                for (int k = 0; k < 2; k++)   // dos planos cruzados: se ve desde cualquier lado
                {
                    var q = Prim(luces, PrimitiveType.Quad, "Haz", new Vector3(x, ExpTecho / 2f, zz), new Vector3(ancho, ExpTecho, 1f), haz);
                    q.transform.localRotation = Quaternion.Euler(0f, 35f + k * 90f, sx * 7f);
                    q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    q.GetComponent<MeshRenderer>().receiveShadows = false;
                    var c = q.GetComponent<Collider>();
                    if (c != null) DestroyImmediate(c);
                }
            }
        }
    }

    void VolumenExpedicion()
    {
#if UNITY_EDITOR
        const string path = "Assets/Settings/ExpedicionProfile.asset";
        var perfil = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            perfil.name = "ExpedicionProfile";
            UnityEditor.AssetDatabase.CreateAsset(perfil, path);
        }
        else
        {
            foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if (o is UnityEngine.Rendering.VolumeComponent) DestroyImmediate(o, true);
            perfil.components.Clear();
        }
        T Add<T>() where T : UnityEngine.Rendering.VolumeComponent
        {
            var c = perfil.Add<T>(true);
            c.name = typeof(T).Name;
            UnityEditor.AssetDatabase.AddObjectToAsset(c, perfil);
            return c;
        }
        // el mismo tono de Cala Sombría: oscuro y con poco color; lo único saturado es el fuego, los cristales y las runas
        var bloom = Add<UnityEngine.Rendering.Universal.Bloom>();
        bloom.threshold.value = 1.15f; bloom.intensity.value = 0.5f; bloom.scatter.value = 0.6f; bloom.tint.value = new Color(1f, 0.94f, 0.88f);
        var split = Add<UnityEngine.Rendering.Universal.SplitToning>();
        split.shadows.value = new Color(0.34f, 0.31f, 0.37f); split.highlights.value = new Color(0.5f, 0.48f, 0.47f); split.balance.value = 0f;
        var ca = Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
        ca.postExposure.value = 0.85f; ca.contrast.value = 8f; ca.saturation.value = -12f;
        var vig = Add<UnityEngine.Rendering.Universal.Vignette>();
        vig.intensity.value = 0.3f; vig.smoothness.value = 0.55f; vig.color.value = Color.black;
        UnityEditor.EditorUtility.SetDirty(perfil);
        var go = new GameObject("VolumenExpedicion");
        go.transform.SetParent(root, false);
        var vol = go.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true; vol.priority = 5f; vol.weight = 1f; vol.sharedProfile = perfil;
#endif
    }
}
