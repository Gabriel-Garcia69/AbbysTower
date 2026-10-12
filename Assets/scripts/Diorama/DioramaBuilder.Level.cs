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
    public static readonly string[] Route = { "Diorama", "Puerto", "Acantilado", "Cueva", "Templo", "Nevado", "Cima" };

    public struct LevelInfo { public string title, subtitle, boss, next; public float hp; }

    /// <summary>Datos del piso que genera este componente (título, jefe, siguiente escena, dificultad).</summary>
    public LevelInfo Info
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: return new LevelInfo { title = "PISO 2", subtitle = "El Puerto de las Mareas", boss = "Capitán sin Barco", next = "Acantilado", hp = 1.15f };
                case Layout.Acantilado: return new LevelInfo { title = "PISO 3", subtitle = "El Sendero del Acantilado", boss = "La Gárgola del Faro Roto", next = "Cueva", hp = 1.3f };
                case Layout.Cueva: return new LevelInfo { title = "PISO 4", subtitle = "La Gruta de Cristal", boss = "Corazón de Geoda", next = "Templo", hp = 1.5f };
                case Layout.Templo: return new LevelInfo { title = "PISO 5", subtitle = "El Templo de Arena", boss = "El Guardián Dorado", next = "Nevado", hp = 1.7f };
                case Layout.Nevado: return new LevelInfo { title = "PISO 6", subtitle = "La Aldea de la Nieve Eterna", boss = "Escarcha, la Viuda Blanca", next = "Cima", hp = 1.9f };
                case Layout.Cima: return new LevelInfo { title = "LA CIMA", subtitle = "Sobre las nubes", boss = "El Arquitecto", next = "", hp = 2.3f };
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
            switch (layout)
            {
                case Layout.Puerto: return new[] { S, A, E, S, A };
                case Layout.Acantilado: return new[] { J, A, S, T, J };
                case Layout.Cueva: return new[] { H, S, T, A, H };
                case Layout.Templo: return new[] { E, A, J, H, E };
                case Layout.Nevado: return new[] { T, H, E, A, J };
                case Layout.Cima: return new[] { E, H, T, A, J, B };
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
        enc.roster = Roster;
        enc.bossStyle = (int)layout;   // cada piso, su jefe con su propio patrón
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
    void MerchantPost(Transform parent, Vector3 pos, float yaw)
    {
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
    void Stairs(Transform parent, string name, Vector3 from, Vector3 to, float width, Material mat = null)
    {
        if (mat == null) mat = mStone;
        var s = Group(name, parent);
        Vector3 flat = to - from; float rise = flat.y; flat.y = 0f;
        float run = flat.magnitude;
        s.localPosition = from;
        s.localRotation = Quaternion.LookRotation(flat.normalized);
        int steps = Mathf.Max(2, Mathf.RoundToInt(rise / 0.22f));
        float depth = run / steps, h = rise / steps;
        for (int i = 0; i < steps; i++)
        {
            float top = (i + 1) * h;
            Box(s, "Escalon", new Vector3(0f, top / 2f - 0.6f, i * depth + depth / 2f), new Vector3(width, top + 1.2f, depth), i % 2 == 0 ? mat : mCobble, false);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            float L = Mathf.Sqrt(run * run + rise * rise);
            var rail = Box(s, "Pretil", new Vector3(sx * (width / 2f + 0.25f), rise / 2f + 0.35f, run / 2f), new Vector3(0.5f, 0.6f, L), mStone, false);
            rail.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
            Box(s, "Costado", new Vector3(sx * (width / 2f + 0.25f), rise / 2f - 0.6f, run / 2f), new Vector3(0.5f, rise + 1.2f, run), mStone, false);
            SolidBox(s, "Collider_Pretil", new Vector3(sx * (width / 2f + 0.25f), rise / 2f + 1f, run / 2f), new Vector3(0.5f, rise + 2f, run));
        }
        var ramp = new GameObject("Collider_Rampa");
        ramp.transform.SetParent(s, false);
        ramp.transform.localPosition = new Vector3(0f, rise / 2f - 0.08f, run / 2f);
        ramp.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
        ramp.AddComponent<BoxCollider>().size = new Vector3(width, 0.15f, Mathf.Sqrt(run * run + rise * rise) + 0.4f);
        Vector3 c = (from + to) / 2f;
        blocked.Add(new Rect(Mathf.Min(from.x, to.x) - width, Mathf.Min(from.z, to.z) - width, Mathf.Abs(to.x - from.x) + width * 2f, Mathf.Abs(to.z - from.z) + width * 2f));
    }

    /// <summary>Ajusta la escena al piso: título, siguiente escena y altura a la que caer cuenta como caída.</summary>
    public void ConfigureScene(float killY)
    {
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd == null) return;
        var info = Info;
        fd.floorTitle = info.title;
        fd.floorSubtitle = info.subtitle;
        fd.nextScene = info.next;
        fd.returnScene = "Exterior";
        fd.killY = killY;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(fd);
#endif
    }
}
