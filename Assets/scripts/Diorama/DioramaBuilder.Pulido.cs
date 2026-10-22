using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pulido al final de cada mapa: faldones de roca bajo las orillas de los planos de afuera (sin ellos, desde abajo se
/// veía pasto y árboles "levitando") y limpieza de plantas mal puestas (dentro del agua o flotando sobre el suelo).
/// </summary>
public partial class DioramaBuilder
{
    static readonly string[] Afuera = { "Afuera", "Meseta", "Escalon", "Cumbres", "Valle" };

    void Pulir()
    {
        CaminosMalla();   // todos los caminos del mapa, unidos en una malla por material
        if (root == null || layout == Layout.Intro) return;
        Faldones();
        PodarPlantas();
        Hordas();
        OrdenarFaroles();
        if (layout == Layout.Pueblo || layout == Layout.Puerto) LimiteVisible(Group("LimitesVisibles", root), Walk);   // tienen sus propios bordes
        Entorno();
    }

    /// <summary>
    /// Alrededor del inicio, de todos lados: al girar la cámara al empezar siempre hay algo bonito que ver (flores y
    /// arbustos; palmeras y columnas en el desierto; pinos en la nieve). Nunca sobre el camino, el agua o lo ocupado.
    /// </summary>
    void Entorno()
    {
        if (layout == Layout.Intro || layout == Layout.Expedicion || layout == Layout.Cueva) return;
        var agua = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") agua.Add(mr.bounds);
        }
        var g = Group("Entorno", root);
        var flores = Group("Flores", g);   // solo las flores (primitivas) se funden; árboles y arbustos comparten malla y así pesan menos
        Vector3 s = SpawnPoint;
        Transform salida = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "Salida") { salida = t; break; }
        Vector2 a = new Vector2(s.x, s.z), b = salida != null ? new Vector2(salida.position.x, salida.position.z) : a + Vector2.up * 60f;
        float y0 = Height(s.x, s.z);
        int puestos = 0;
        for (int tries = 0; puestos < 44 && tries < 1500; tries++)
        {
            float ang = R(0f, 360f) * Mathf.Deg2Rad, r = R(6.5f, 30f);
            var q = a + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            if (!Free(q, 1.2f) || !TerrainArea.Contains(q)) continue;
            // lejos del camino principal (inicio → salida) y del punto de aparición
            Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
            if (Vector2.Distance(q, a + ab * t) < 5f) continue;
            float h = Height(q.x, q.y);
            if (Mathf.Abs(h - y0) > 4f || Mathf.Abs(Height(q.x + 1f, q.y) - h) > 0.3f || Mathf.Abs(Height(q.x, q.y + 1f) - h) > 0.3f) continue;   // solo en plano
            bool mojado = false;
            foreach (var w in agua) if (q.x > w.min.x - 1f && q.x < w.max.x + 1f && q.y > w.min.z - 1f && q.y < w.max.z + 1f && w.max.y > h - 0.5f) { mojado = true; break; }
            if (mojado) continue;
            var p = new Vector3(q.x, h, q.y);
            float k = (float)rng.NextDouble();
            if (r < 14f) k = layout == Layout.Nevado ? Mathf.Lerp(0.6f, 1f, k) : layout == Layout.Templo ? Mathf.Lerp(0.7f, 1f, k) : k * 0.85f;   // cerca: solo cosas bajas (la cámara gira a ~10 m)
            switch (layout)
            {
                case Layout.Templo:
                    if (k < 0.4f) Palm(g, p, R(0.8f, 1.2f));
                    else if (k < 0.7f) TempleColumn(g, p, R(2f, 4.5f), true);
                    else Bush(g, p, R(0.6f, 1f));
                    break;
                case Layout.Nevado:
                    if (k < 0.6f) Pine(g, q, h, R(0.8f, 1.3f), false);
                    else if (k < 0.68f) Snowman(g, p);
                    else Bush(g, p, R(0.6f, 1f));
                    break;
                default:
                    if (k < 0.55f) Ramo(flores, p);
                    else if (k < 0.85f) Bush(g, p, R(0.6f, 1.1f));
                    else Tree(g, q, h, R(0.8f, 1.15f), false);
                    break;
            }
            blocked.Add(new Rect(q.x - 1f, q.y - 1f, 2f, 2f));
            puestos++;
        }
        Fundir(flores);   // cientos de flores en pocas mallas
    }

    /// <summary>Un manchón de flores de colores.</summary>
    void Ramo(Transform parent, Vector3 pos)
    {
        int n = rng.Next(5, 10);
        for (int i = 0; i < n; i++)
        {
            var o = new Vector3(R(-0.9f, 0.9f), 0f, R(-0.9f, 0.9f));
            float sz = R(0.16f, 0.3f);
            // cubitos (24 vértices) en vez de esferas (515): se ven como flores de pixel y la escena pesa mucho menos
            Prim(parent, PrimitiveType.Cube, "Tallo", pos + o + Vector3.up * sz * 0.6f, new Vector3(0.05f, sz * 1.2f, 0.05f), mLeaves[0])
                .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var f = Prim(parent, PrimitiveType.Cube, "Flor", pos + o + Vector3.up * sz * 1.25f, Vector3.one * sz * 0.8f, mFlowers[rng.Next(mFlowers.Length)]);
            f.transform.localRotation = Quaternion.Euler(45f, R(0f, 90f), 45f);
            f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    void Faldones()
    {
        var planos = new List<MeshRenderer>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool afuera = false;
            foreach (var a in Afuera) if (mr.name.StartsWith(a)) { afuera = true; break; }
            if (afuera && mr.bounds.size.y < 0.5f && mr.bounds.size.x > 40f) planos.Add(mr);
        }
        var g = Group("Faldones", root);
        const float h = 120f;
        foreach (var mr in planos)
        {
            var b = mr.bounds; float y = b.center.y;
            // una pared que baja desde cada orilla, mirando hacia afuera del plano
            Wall(g, "Falda", new Vector3(b.center.x, y - h / 2f, b.min.z), Vector3.back, b.size.x, h, WallMat, 0.25f);
            Wall(g, "Falda", new Vector3(b.center.x, y - h / 2f, b.max.z), Vector3.forward, b.size.x, h, WallMat, 0.25f);
            Wall(g, "Falda", new Vector3(b.min.x, y - h / 2f, b.center.z), Vector3.left, b.size.z, h, WallMat, 0.25f);
            Wall(g, "Falda", new Vector3(b.max.x, y - h / 2f, b.center.z), Vector3.right, b.size.z, h, WallMat, 0.25f);
        }
    }

    /// <summary>
    /// Los faroles van donde tiene sentido: no frente a una escalera ni encima de ella, no flotando, hundidos o en el agua,
    /// no en medio de una arena ni encimados con otro farol. Los que estorban se quitan, y cada escalera queda con un par de
    /// faroles a los lados de su entrada de abajo (como una escalinata de verdad).
    /// </summary>
    void OrdenarFaroles()
    {
        var escaleras = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.Find("Collider_Rampa") != null) escaleras.Add(t);
        var agua = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") agua.Add(mr.bounds);
        }
        var arenas = root.GetComponentsInChildren<RoomEncounter>(true);
        var faroles = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "Farol") faroles.Add(t);
        var quedan = new List<Vector3>();
        int quitados = 0;
        foreach (var f in faroles)
        {
            Vector3 p = f.position;
            bool mal = false;
            foreach (var e in escaleras)
            {
                var rampa = e.Find("Collider_Rampa").GetComponent<BoxCollider>();
                Vector3 l = rampa.transform.InverseTransformPoint(p);   // en el espacio de la rampa (a lo largo de la escalera)
                if (Mathf.Abs(l.x) < rampa.size.x / 2f + 1.3f && Mathf.Abs(l.z) < rampa.size.z / 2f + 4f) { mal = true; break; }
            }
            if (!mal)
                foreach (var b in agua)
                    if (p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z && b.max.y > p.y - 0.3f && Height(p.x, p.z) < b.max.y - 0.05f) { mal = true; break; }
            if (!mal && TerrainArea.Contains(new Vector2(p.x, p.z)) && f.parent != null && !f.parent.name.StartsWith("Muelle"))
            {
                float h = Height(p.x, p.z);
                if (h > VoidY && Mathf.Abs(h - p.y) > 0.5f && !Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down, 1.2f)) mal = true;   // flota o se hunde
            }
            if (!mal)
                foreach (var a in arenas)
                {
                    Vector3 l = a.transform.InverseTransformPoint(p);
                    if (Mathf.Abs(l.x) < a.size.x * 0.32f && Mathf.Abs(l.z) < a.size.y * 0.32f) { mal = true; break; }   // en medio de la pelea
                }
            if (!mal) foreach (var q in quedan) if (Vector3.Distance(q, p) < 3.5f) { mal = true; break; }
            if (mal) { DestroyImmediate(f.gameObject); quitados++; }
            else quedan.Add(p);
        }
        // un par de faroles flanqueando la entrada de abajo de cada escalera
        int puestos = 0;
        foreach (var e in escaleras)
        {
            var rampa = e.Find("Collider_Rampa").GetComponent<BoxCollider>();
            Vector3 pie = rampa.transform.TransformPoint(new Vector3(0f, 0f, -rampa.size.z / 2f - 0.8f));
            float hPie = Height(pie.x, pie.z);
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 w = rampa.transform.TransformPoint(new Vector3(sx * (rampa.size.x / 2f + 1f), 0f, -rampa.size.z / 2f - 0.8f));
                float h = Height(w.x, w.z);
                if (Mathf.Abs(h - hPie) > 0.4f || Mathf.Abs(Height(w.x + 0.6f, w.z) - h) > 0.3f) continue;   // al lado no hay suelo plano
                bool cerca = false;
                foreach (var q in quedan) if (Vector3.Distance(q, new Vector3(w.x, h, w.z)) < 2.5f) cerca = true;
                if (cerca) continue;
                PortLamp(e.parent != null ? e.parent : root, e.parent != null ? e.parent.InverseTransformPoint(new Vector3(w.x, h, w.z)) : new Vector3(w.x, h, w.z));
                quedan.Add(new Vector3(w.x, h, w.z));
                puestos++;
            }
        }
        if (quitados + puestos > 0) Debug.Log($"[Diorama] {layout}: faroles: {quitados} quitados (mal puestos), {puestos} a los lados de las escaleras.");
    }

    static readonly string[] Plantas = { "Pino", "Arbol", "Arbusto", "Palmera", "PinoValle" };

    void PodarPlantas()
    {
        var agua = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") agua.Add(mr.bounds);
        }
        var area = TerrainArea;
        var quitar = new List<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (System.Array.IndexOf(Plantas, t.name) < 0) continue;
            Vector3 p = t.position;
            bool mal = false;
            foreach (var b in agua)
                if (p.x > b.min.x - 0.6f && p.x < b.max.x + 0.6f && p.z > b.min.z - 0.6f && p.z < b.max.z + 0.6f && b.max.y > p.y - 0.4f && b.min.y < p.y + 2f)
                {
                    // dentro de la huella del agua y a su altura: ¿de verdad está sobre el agua? (el suelo ahí es más bajo que el agua)
                    if (Height(p.x, p.z) < b.max.y - 0.05f || layout == Layout.Pueblo && InWater(new Vector2(p.x, p.z))) { mal = true; break; }
                }
            // dentro del terreno, si la base no toca el suelo (flota) o quedó hundida
            if (!mal && area.Contains(new Vector2(p.x, p.z)) && layout != Layout.Intro)
            {
                float h = Height(p.x, p.z);
                if (Mathf.Abs(h - p.y) > 0.4f && h > VoidY) mal = true;
            }
            // árboles altos pegados al inicio: la cámara arranca dentro de ellos
            if (!mal && t.name != "Arbusto" && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(SpawnPoint.x, SpawnPoint.z)) < 11f) mal = true;
            if (mal) quitar.Add(t.gameObject);
        }
        foreach (var go in quitar) DestroyImmediate(go);
        if (quitar.Count > 0) Debug.Log($"[Diorama] {layout}: {quitar.Count} plantas quitadas (en el agua o flotando).");
    }
}
