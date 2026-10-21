using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kit de nivel para los dioramas (lo que piden los apuntes del proyecto): arenas de combate por oleadas, zona de descanso
/// y comercio (mercader + santuario), cofres, notas de la historia, jefe de la sección y salida al siguiente piso.
/// Los dioramas son pisos de la torre: cada uno es un mundo distinto con la misma estructura de juego.
/// </summary>
public partial class DioramaBuilder
{
    /// <summary>Orden de la ruta HD-2D (nombre de escena por piso).</summary>
    /// <summary>GDD: la demo tiene 5 pisos; el último es el del jefe final (Dante). Los demás mundos quedan fuera de la ruta.</summary>
    public static readonly string[] Route = { "Diorama", "Puerto", "Cascadas", "Acantilado", "Cima" };
    /// <summary>Pisos de la ruta del GDD (4 hordas por piso).</summary>
    bool EnRutaGDD => layout == Layout.Pueblo || layout == Layout.Puerto || layout == Layout.Cascadas || layout == Layout.Acantilado || layout == Layout.Cima;

    public struct LevelInfo { public string title, subtitle, boss, next; public float hp; }

    /// <summary>Datos del piso que genera este componente (título, jefe, siguiente escena, dificultad).</summary>
    public LevelInfo Info
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: return new LevelInfo { title = "PISO 2", subtitle = "El Puerto de las Mareas", boss = "Capitán sin Barco", next = "Cascadas", hp = 1.15f };
                case Layout.Cascadas: return new LevelInfo { title = "PISO 3", subtitle = "El Valle de las Cascadas", boss = "El Señor del Torrente", next = "Acantilado", hp = 1.3f };
                case Layout.Acantilado: return new LevelInfo { title = "PISO 4", subtitle = "El Sendero del Acantilado", boss = "La Gárgola del Faro Roto", next = "Cima", hp = 1.45f };
                case Layout.Cueva: return new LevelInfo { title = "PISO 5", subtitle = "La Gruta de Cristal", boss = "Corazón de Geoda", next = "Lago", hp = 1.6f };
                case Layout.Lago: return new LevelInfo { title = "PISO 6", subtitle = "El Lago de los Espejos", boss = "La Dama del Lago", next = "Templo", hp = 1.75f };
                case Layout.Templo: return new LevelInfo { title = "PISO 7", subtitle = "El Templo de Arena", boss = "El Guardián Dorado", next = "Nevado", hp = 1.9f };
                case Layout.Nevado: return new LevelInfo { title = "PISO 8", subtitle = "La Aldea de la Nieve Eterna", boss = "Escarcha, la Viuda Blanca", next = "Montana", hp = 2.05f };
                case Layout.Expedicion: return new LevelInfo { title = "PISO 20", subtitle = "La expedición", boss = "", next = "Diorama", hp = 0.8f };
                case Layout.Montana: return new LevelInfo { title = "PISO 9", subtitle = "La Montaña del Eco", boss = "El Eco de Piedra", next = "Cima", hp = 2.2f };
                case Layout.Cima: return new LevelInfo { title = "PISO 5", subtitle = "La cima de la torre", boss = "La Sombra", next = "", hp = 1.6f };
                default: return new LevelInfo { title = "PISO 1", subtitle = "El Pueblo del Risco", boss = "Guardián de las Ruinas", next = "Puerto", hp = 1f };
            }
        }
    }

    // ------------------------------------------------------------------ combate

    /// <summary>Enemigos de cada piso (por orden de aparición): cada mundo mezcla estilos de pelea distintos.</summary>
    TowerEnemy.Kind[] Roster
    {
        get
        {
            var S = TowerEnemy.Kind.Sombra; var B = TowerEnemy.Kind.Bruto; var A = TowerEnemy.Kind.Arquero; var J = TowerEnemy.Kind.Saltador;
            var H = TowerEnemy.Kind.Hechicero; var T = TowerEnemy.Kind.Torbellino; var E = TowerEnemy.Kind.Escudero;
            var Em = TowerEnemy.Kind.Embestidor;
            // GDD: un enemigo nuevo por piso. 1 Larva · 2 Acechador y Bruto (golpes fuertes: bloquear) · 3 Embestidor (sombras
            // alargadas) · 4 Guardián con escudo (rodearlo) · 5 Espectro antes de Dante. Sin enemigos humanos ni a distancia.
            switch (layout)
            {
                case Layout.Pueblo: return new[] { S };
                case Layout.Puerto: return new[] { S, J, S, B };
                case Layout.Cascadas: return new[] { S, Em, J, B };
                case Layout.Acantilado: return new[] { E, S, Em, B, J };
                case Layout.Cima: return new[] { H, E, Em, B, J };
            }
            switch (layout)
            {
                case Layout.Puerto: return new[] { S, A, E, S, A };
                case Layout.Acantilado: return new[] { J, A, S, T, J };
                case Layout.Cueva: return new[] { H, S, T, A, H };
                case Layout.Templo: return new[] { E, A, J, H, E };
                case Layout.Nevado: return new[] { T, H, E, A, J };
                case Layout.Cima: return new[] { E, H, T, A, J, B };
                case Layout.Cascadas: return new[] { S, J, A, B, S };
                case Layout.Lago: return new[] { H, A, S, T, E };
                case Layout.Montana: return new[] { B, J, T, E, A, H };
                default: return new[] { S, S, B, J };
            }
        }
    }

    /// <summary>Arena: al entrar empieza la pelea por oleadas (sombras y brutos) o el jefe. Devuelve el encuentro.</summary>
    RoomEncounter Arena(Transform parent, string name, Vector3 center, Vector2 size, int enemies, bool boss = false)
    {
        var a = Group("Arena_" + name, parent);
        a.localPosition = center;
        var enc = a.gameObject.AddComponent<RoomEncounter>();
        enc.size = size;
        enc.triggerInset = Mathf.Min(2f, Mathf.Min(size.x, size.y) * 0.25f);
        enc.doors = new LevelDoor[0];
        enc.maxAlive = 4;
        enc.isBoss = boss;
        enc.healthMultiplier = Info.hp;
        enc.displayName = boss ? Info.boss : name;
        if (boss) enc.bossName = Info.boss;
        if (boss && layout == Layout.Cima) enc.bossKind = TowerEnemy.Kind.Dante;   // GDD: el jefe final es Dante
        enc.roster = Roster;
        enc.bossStyle = BossStyle;   // cada piso, su jefe con su propio patrón
        enc.leash = boss ? 18f : 20f;   // mundo abierto: persiguen a Abby fuera de la arena (puentes, escaleras)
        var sp = new List<Transform>();
        int count = boss ? 1 : enemies;
        for (int i = 0; i < count; i++)
        {
            var t = Group("Aparicion", a);
            if (boss) t.localPosition = new Vector3(0f, 0.1f, size.y * 0.25f);
            else
            {
                float ang = i / (float)count * Mathf.PI * 2f + 0.4f;
                t.localPosition = new Vector3(Mathf.Cos(ang) * size.x * 0.32f, 0.1f, Mathf.Sin(ang) * size.y * 0.32f);
            }
            sp.Add(t);
        }
        enc.spawnPoints = sp.ToArray();
        if (layout >= Layout.Lago) blocked.Add(new Rect(center.x - size.x / 2f, center.z - size.y / 2f, size.x, size.y));   // sin árboles dentro de la arena
        return enc;
    }

    /// <summary>Salida al siguiente piso: un círculo de runas que se enciende al derrotar al jefe.</summary>
    void Exit(Transform parent, Vector3 pos, RoomEncounter unlockedBy)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Salida";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos + Vector3.up * 0.05f;
        go.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        DestroyImmediate(go.GetComponent<Collider>());
        var exit = go.AddComponent<FloorExit>();
        exit.nextScene = Info.next;
        exit.radius = 1.4f;
        exit.unlockedBy = unlockedBy;
        var fx = Group("Salida_Haz", parent);
        fx.localPosition = pos;
        var beam = Prim(fx, PrimitiveType.Cylinder, "Haz", new Vector3(0f, 6f, 0f), new Vector3(2.2f, 6f, 2.2f), mRay);
        beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lt = AddLight(fx, new Vector3(0f, 1.5f, 0f), new Color(0.4f, 1f, 0.95f), 9f, 6f);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.15f;
        exit.unlockedFx = fx.gameObject;
        fx.gameObject.SetActive(false);
        blocked.Add(new Rect(pos.x - 2f, pos.z - 2f, 4f, 4f));
    }

    // ------------------------------------------------------------------ descanso y comercio

    /// <summary>Puesto del mercader: mostrador con toldo (tecla E para comprar) y el mercader encapuchado detrás.</summary>
    /// <summary>GDD: no hay objetos ni coleccionables (solo la fuente de curación): sin mercaderes ni cofres.</summary>
    static readonly bool SinTiendaNiCofres = true;   // readonly (no const): sin avisos de código inalcanzable

    void MerchantPost(Transform parent, Vector3 pos, float yaw)
    {
        if (SinTiendaNiCofres) return;
        var s = Group("Mercader", parent);
        s.localPosition = pos;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Stall(s, Vector3.zero, 0f);
        foreach (Transform c in s) if (c.name == "Puesto") c.gameObject.AddComponent<MerchantShop>();
        var m = Group("Mercader_Personaje", s);
        m.localPosition = new Vector3(0f, 0f, -0.9f);
        var body = Group("Cuerpo", m);
        var robe = Plain("Dio_TunicaMercader", new Color(0.32f, 0.2f, 0.45f), 0.1f);
        var dark = Plain("Dio_TunicaOscura", new Color(0.18f, 0.11f, 0.26f), 0.1f);
        var scarf = Plain("Dio_Bufanda", new Color(0.85f, 0.55f, 0.2f), 0.1f);
        Prim(body, PrimitiveType.Cylinder, "Tunica", new Vector3(0f, 0.55f, 0f), new Vector3(0.95f, 0.55f, 0.85f), robe, true);
        Prim(body, PrimitiveType.Sphere, "Torso", new Vector3(0f, 1.2f, 0f), new Vector3(0.8f, 0.75f, 0.7f), robe);
        Prim(body, PrimitiveType.Cylinder, "BufandaM", new Vector3(0f, 1.5f, 0f), new Vector3(0.62f, 0.08f, 0.58f), scarf);
        Prim(body, PrimitiveType.Sphere, "Capucha", new Vector3(0f, 1.82f, -0.04f), new Vector3(0.62f, 0.6f, 0.62f), dark);
        foreach (float x in new[] { -0.08f, 0.08f })
            Box(body, "Ojo", new Vector3(x, 1.8f, 0.3f), new Vector3(0.06f, 0.04f, 0.02f), mGlow, false);
        Box(body, "Mochila", new Vector3(0f, 1.35f, -0.48f), new Vector3(0.75f, 0.95f, 0.45f), mDoor, false);
        var arm = Group("Brazo", body);
        arm.localPosition = new Vector3(-0.38f, 1.4f, 0.05f);
        Box(arm, "Manga", new Vector3(-0.22f, 0f, 0f), new Vector3(0.45f, 0.16f, 0.16f), robe, false);
        var npc = m.gameObject.AddComponent<MerchantNPC>();
        npc.body = body; npc.arm = arm;
        blocked.Add(new Rect(pos.x - 3f, pos.z - 3f, 6f, 6f));
    }

    /// <summary>Santuario de sanación: pedestal con un cristal verde que cura todo.</summary>
    void HealShrine(Transform parent, Vector3 pos)
    {
        var s = Group("Santuario", parent);
        s.localPosition = pos;
        Prim(s, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.3f, 1.4f), mStone, true);
        Prim(s, PrimitiveType.Cylinder, "Pedestal2", new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 0.12f, 0.8f), mStone);
        var crystalMat = Glow("Dio_CristalVerde", new Color(0.4f, 1f, 0.6f), 3f);
        var crystal = Box(s, "Cristal", new Vector3(0f, 1.6f, 0f), new Vector3(0.45f, 0.75f, 0.45f), crystalMat, false);
        crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        crystal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lt = AddLight(s, new Vector3(0f, 1.8f, 0f), new Color(0.45f, 1f, 0.65f), 7f, 5f);
        var shrine = s.gameObject.AddComponent<HealingShrine>();
        shrine.crystal = crystal.transform; shrine.glow = lt;
        blocked.Add(new Rect(pos.x - 1.5f, pos.z - 1.5f, 3f, 3f));
    }

    /// <summary>Cofre con fragmentos.</summary>
    void Chest(Transform parent, Vector3 pos, int fragments, float yaw = 0f)
    {
        if (SinTiendaNiCofres) return;
        var c = Group("Cofre", parent);
        c.localPosition = pos;
        c.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(c, "Caja", new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.65f), mDoor, true);
        var lid = Group("TapaPivote", c);
        lid.localPosition = new Vector3(0f, 0.6f, -0.32f);
        Box(lid, "Tapa", new Vector3(0f, 0.1f, 0.32f), new Vector3(1.04f, 0.2f, 0.68f), mDoor, false);
        Box(c, "Herraje", new Vector3(0f, 0.3f, 0f), new Vector3(1.04f, 0.1f, 0.68f), mGlow, false);
        var lt = AddLight(c, new Vector3(0f, 1f, 0f), new Color(1f, 0.8f, 0.4f), 4f, 3f);
        var tc = c.gameObject.AddComponent<TreasureChest>();
        tc.lid = lid; tc.glow = lt; tc.fragments = fragments;
        blocked.Add(new Rect(pos.x - 1f, pos.z - 1f, 2f, 2f));
    }

    /// <summary>Nota, diario de A. o inscripción que se lee con E.</summary>
    void Note(Transform parent, Vector3 pos, float yaw, LoreNote.Kind kind, string id, string title, string text, string sign = "")
    {
        var t = Group("Lectura_" + kind, parent);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (kind == LoreNote.Kind.Inscripcion)
        {
            Box(t, "Estela", new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 1.5f, 0.3f), mStone, true);
            Box(t, "Remate", new Vector3(0f, 1.55f, 0f), new Vector3(1.25f, 0.14f, 0.4f), mStone, false);
            for (int i = 0; i < 4; i++)
                Box(t, "Letras", new Vector3(0f, 1.25f - i * 0.2f, -0.16f), new Vector3(0.7f - (i % 2) * 0.2f, 0.04f, 0.01f), mGlow, false);
        }
        else
        {
            Box(t, "Mesita", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.55f), mDoor, true);
            var paper = Box(t, "Papel", new Vector3(0f, 0.71f, 0f), new Vector3(0.36f, 0.01f, 0.46f), mClothB, false);
            paper.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            if (kind == LoreNote.Kind.Diario)
                Box(t, "Cinta", new Vector3(-0.12f, 0.716f, 0f), new Vector3(0.04f, 0.012f, 0.48f), mFlowers[3], false);
        }
        var sparkle = Group("Brillo", t);
        sparkle.localPosition = new Vector3(0f, kind == LoreNote.Kind.Inscripcion ? 1.1f : 0.85f, 0f);
        var lt = AddLight(sparkle, Vector3.zero, new Color(1f, 0.9f, 0.6f), 2.5f, 1.5f);
        var note = t.gameObject.AddComponent<LoreNote>();
        note.id = id; note.kind = kind; note.title = title; note.text = text; note.signature = sign;
        note.sparkle = sparkle.gameObject;
        blocked.Add(new Rect(pos.x - 1f, pos.z - 1f, 2f, 2f));
    }

    // ------------------------------------------------------------------ piezas comunes

    /// <summary>Escalinata de piedra de 'from' (abajo) a 'to' (arriba), con pretiles y rampa invisible para subir parejo.</summary>
    /// <summary>Cómo son las escaleras de cada piso (material, alto del escalón y barandal), para que no se repitan.</summary>
    struct EstiloEscalera { public Material huella, contra, lado; public float alto; public int baranda; public float ancho; }
    // baranda: 0 pretil de piedra macizo · 1 barandal de madera con postes · 2 sin pretil, peñas a los lados · 3 balaustrada blanca

    EstiloEscalera Estilo(Material mat)
    {
        switch (layout)
        {
            case Layout.Puerto:   // del muelle: tablones de madera y barandal de postes
                return new EstiloEscalera { huella = mPlank, contra = mTimber, lado = mTimber, alto = 0.2f, baranda = 1, ancho = 0.85f };
            case Layout.Cascadas:   // de piedra del río con musgo, peldaños bajos y anchos
                return new EstiloEscalera { huella = mStoneBrick, contra = mCaveRock != null ? mCaveRock : mRock, lado = mRock, alto = 0.17f, baranda = 2, ancho = 1.1f };
            case Layout.Acantilado:   // tallada en la roca del risco: peldaños altos, sin pretil
                return new EstiloEscalera { huella = mRock, contra = mRock, lado = mRock, alto = 0.3f, baranda = 2, ancho = 0.9f };
            case Layout.Cima:   // mármol blanco con balaustrada
                return new EstiloEscalera { huella = mWhite, contra = mStoneBrick, lado = mWhite, alto = 0.2f, baranda = 3, ancho = 1f };
            default:
                return new EstiloEscalera { huella = mat ?? mStone, contra = mCobble, lado = mStone, alto = 0.22f, baranda = 0, ancho = 1f };
        }
    }

    void Stairs(Transform parent, string name, Vector3 from, Vector3 to, float width, Material mat = null)
    {
        var st = Estilo(mat);
        width *= st.ancho;
        var s = Group(name, parent);
        Vector3 flat = to - from; float rise = flat.y; flat.y = 0f;
        float run = flat.magnitude;
        s.localPosition = from;
        s.localRotation = Quaternion.LookRotation(flat.normalized);
        int steps = Mathf.Max(2, Mathf.RoundToInt(rise / st.alto));
        float depth = run / steps, h = rise / steps;
        for (int i = 0; i < steps; i++)
        {
            float top = (i + 1) * h;
            // cada escalón: un bloque (contrahuella) y encima una huella un poco más ancha y que sobresale; así ninguna cara
            // queda en el mismo plano que otra (antes los bloques y los costados compartían caras y parpadeaban)
            Box(s, "Escalon", new Vector3(0f, top / 2f - 0.6f, i * depth + depth / 2f), new Vector3(width - 0.04f, top + 1.2f - 0.06f, depth - 0.02f), st.contra, false);
            Box(s, "Huella", new Vector3(0f, top - 0.04f, i * depth + depth / 2f - 0.03f), new Vector3(width, 0.1f, depth + 0.06f), st.huella, false);
        }
        float L = Mathf.Sqrt(run * run + rise * rise), ang = -Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = sx * (width / 2f + 0.25f);
            if (st.baranda == 0)
            {
                // pretil más ancho que el costado (sus caras no coinciden) y costado escalonado debajo
                var rail = Box(s, "Pretil", new Vector3(x, rise / 2f + 0.38f, run / 2f), new Vector3(0.56f, 0.6f, L), st.lado, false);
                rail.transform.localRotation = Quaternion.Euler(ang, 0f, 0f);
                int per = Mathf.Max(1, steps / 8);
                for (int i = 0; i < steps; i += per)
                {
                    int j = Mathf.Min(steps, i + per);
                    float top = j * h;
                    Box(s, "Costado", new Vector3(x, top / 2f - 0.62f, (i + j) * depth / 2f), new Vector3(0.46f, top + 1.2f, (j - i) * depth - 0.01f), st.lado, false);
                }
            }
            else if (st.baranda == 1 || st.baranda == 3)
            {
                // postes y pasamanos (madera en el puerto, columnitas blancas en la cima)
                bool blanco = st.baranda == 3;
                int postes = Mathf.Max(2, Mathf.CeilToInt(run / (blanco ? 0.9f : 1.6f)));
                for (int i = 0; i <= postes; i++)
                {
                    float z = run * i / postes, y = rise * i / postes;
                    if (blanco) Prim(s, PrimitiveType.Cylinder, "Balaustre", new Vector3(x, y + 0.45f, z), new Vector3(0.18f, 0.45f, 0.18f), st.lado);
                    else Box(s, "Poste", new Vector3(x, y + 0.5f, z), new Vector3(0.16f, 1f, 0.16f), st.lado, false);
                }
                var man = Box(s, "Pasamanos", new Vector3(x, rise / 2f + (blanco ? 0.95f : 1.02f), run / 2f), new Vector3(blanco ? 0.26f : 0.12f, blanco ? 0.12f : 0.1f, L), st.lado, false);
                man.transform.localRotation = Quaternion.Euler(ang, 0f, 0f);
                // zanca debajo de los escalones (no se ven huecos al costado)
                var zanca = Box(s, "Zanca", new Vector3(sx * (width / 2f + 0.06f), rise / 2f - 0.25f, run / 2f), new Vector3(0.1f, 0.7f, L), st.contra, false);
                zanca.transform.localRotation = Quaternion.Euler(ang, 0f, 0f);
            }
            else
            {
                // tallada: peñas y matas a los lados en lugar de pretil
                for (float z = 0.4f; z < run; z += R(1.4f, 2.2f))
                {
                    float y = rise * z / run, sz = R(0.6f, 1.2f);
                    var pena = Prim(s, PrimitiveType.Sphere, "Pena", new Vector3(sx * (width / 2f + R(0.25f, 0.6f)), y + sz * 0.15f, z), new Vector3(sz * 1.3f, sz * 0.8f, sz * 1.1f), mRock);
                    pena.transform.localRotation = Quaternion.Euler(R(-12f, 12f), R(0f, 360f), R(-12f, 12f));
                }
                // costado escalonado al ras de los peldaños (un muro entero se veía como una pared gigante en las rampas)
                for (int i = 0; i < steps; i += 2)
                {
                    int j = Mathf.Min(steps, i + 2);
                    float top = j * h - 0.08f;
                    Box(s, "Costado", new Vector3(sx * (width / 2f + 0.25f), top / 2f - 0.6f, (i + j) * depth / 2f), new Vector3(0.46f, top + 1.2f, (j - i) * depth - 0.01f), st.lado, false);
                }
            }
            SolidBox(s, "Collider_Pretil", new Vector3(x, rise / 2f + 1f, run / 2f), new Vector3(0.5f, rise + 2f, run));
        }
        var ramp = new GameObject("Collider_Rampa");
        ramp.transform.SetParent(s, false);
        ramp.transform.localPosition = new Vector3(0f, rise / 2f - 0.08f, run / 2f);
        ramp.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
        ramp.AddComponent<BoxCollider>().size = new Vector3(width, 0.15f, Mathf.Sqrt(run * run + rise * rise) + 0.4f);
        // descanso arriba: tapa la ranura entre el último escalón y el suelo de arriba (las celdas del terreno miden 0.7 m)
        var landing = new GameObject("Collider_Descanso");
        landing.transform.SetParent(s, false);
        landing.transform.localPosition = new Vector3(0f, rise - 0.1f, run + 0.6f);
        landing.AddComponent<BoxCollider>().size = new Vector3(width, 0.2f, 1.6f);
        Box(s, "Descanso", new Vector3(0f, rise + 0.015f, run + 0.6f), new Vector3(width, 0.1f, 1.6f), st.huella, false);   // apenas arriba del suelo: sin parpadeo
        Vector3 c = (from + to) / 2f;
        blocked.Add(new Rect(Mathf.Min(from.x, to.x) - width, Mathf.Min(from.z, to.z) - width, Mathf.Abs(to.x - from.x) + width * 2f, Mathf.Abs(to.z - from.z) + width * 2f));
    }

    /// <summary>Ajusta la escena al piso: título, siguiente escena y altura a la que caer cuenta como caída.</summary>
    /// <summary>Patrón del jefe (0-6, ver TowerEnemy.ChooseBoss): los pisos nuevos reusan patrones.</summary>
    int BossStyle => layout == Layout.Lago ? 3 : layout == Layout.Cascadas ? 1 : layout == Layout.Montana ? 2 : (int)layout;

    /// <summary>Altura bajo la cual Abby se cayó del mapa.</summary>
    public float KillY
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: return -2.2f;
                case Layout.Acantilado: case Layout.Templo: case Layout.Nevado: return -3f;
                case Layout.Cueva: return -2.5f;
                case Layout.Cima: return -12f;
                case Layout.Lago: return -5f;
                case Layout.Cascadas: return -8f;
                case Layout.Montana: return -16f;
                case Layout.Expedicion: return -30f;
                default: return -6f;
            }
        }
    }

    public void ConfigureScene(float killY)
    {
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd == null) return;
        var info = Info;
        fd.floorTitle = info.title;
        fd.floorSubtitle = info.subtitle;
        fd.nextScene = info.next;
        fd.heroName = Historia.Protagonista.ToUpper();
        fd.returnScene = FloorDirector.MenuScene;   // GDD: al final se vuelve al menú principal
        fd.killY = killY;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(fd);
#endif
    }
}
