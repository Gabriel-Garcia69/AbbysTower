using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GDD: cada piso tiene 4 hordas; no hay jefes de piso (el único jefe es Dante, en el último). Después de construir un
/// piso de la ruta: los guardianes de los pisos 1-4 se vuelven la última horda, se quitan las arenas que sobran y se
/// agregan las que faltan (en un llano del camino entre el inicio y la salida). La salida se abre al limpiar las 4 hordas
/// y, en el último piso, Dante no aparece hasta entonces.
/// </summary>
public partial class DioramaBuilder
{
    const int HordasPorPiso = 4;

    void Hordas()
    {
        if (!EnRutaGDD || root == null) return;
        Vector3 s = SpawnPoint;
        RoomEncounter jefeFinal = null;
        var hordas = new List<RoomEncounter>();
        foreach (var r in root.GetComponentsInChildren<RoomEncounter>(true))
        {
            if (r.isBoss && layout == Layout.Cima) { jefeFinal = r; continue; }
            hordas.Add(r);
        }
        int normales = 0;
        foreach (var r in hordas) if (!r.isBoss) normales++;
        // los guardianes de piso: si ya hay 4 hordas, se quitan; si no, son una horda más
        for (int i = hordas.Count - 1; i >= 0; i--)
        {
            var r = hordas[i];
            if (!r.isBoss) continue;
            if (normales >= HordasPorPiso) { hordas.RemoveAt(i); DestroyImmediate(r.gameObject); }
            else { ConvertirEnHorda(r); normales++; }
        }
        hordas.Sort((a, b) => Plano(a.transform.position - s).CompareTo(Plano(b.transform.position - s)));
        while (hordas.Count > HordasPorPiso) { var x = hordas[hordas.Count - 2]; hordas.Remove(x); DestroyImmediate(x.gameObject); }
        var salida = BuscarSalida();
        Vector3 fin = salida != null ? salida.transform.position : jefeFinal != null ? jefeFinal.transform.position : s + Vector3.forward * 80f;
        // si el mapa tiene caminos, las 4 hordas van SOBRE el camino, repartidas de la entrada a la salida
        // (las que estaban a mano a veces caían junto al río o dentro de una cerca y el piso no se podía terminar)
        var enCamino = HordasEnCamino(s, fin);
        if (enCamino != null)
        {
            foreach (var h in hordas) if (h != null) DestroyImmediate(h.gameObject);
            hordas = enCamino;
        }
        else
        // primero, lugares escogidos a mano en cada mapa (x, z, ancho, fondo); si aún faltan, se buscan solos
        foreach (var e in HordasExtra)
        {
            if (enCamino != null) break;
            if (hordas.Count >= HordasPorPiso) break;
            var lvl0 = root.Find("Nivel");
            hordas.Add(Arena(lvl0 != null ? lvl0 : root, "Horda", new Vector3(e.x, Height(e.x, e.y), e.y), new Vector2(e.z, e.w), 5));
        }
        hordas.Sort((a, b) => Plano(a.transform.position - s).CompareTo(Plano(b.transform.position - s)));
        for (int intento = 0; hordas.Count < HordasPorPiso && intento < 6; intento++)
        {
            var nueva = ArenaAutomatica(hordas, s, fin);
            if (nueva == null) break;
            hordas.Add(nueva);
            hordas.Sort((a, b) => Plano(a.transform.position - s).CompareTo(Plano(b.transform.position - s)));
        }
        if (hordas.Count < HordasPorPiso) Debug.LogWarning($"[Diorama] {layout}: solo {hordas.Count} hordas (no hubo dónde poner más).");
        // numeradas en el orden del recorrido (de la entrada hacia la salida), no por distancia en línea recta
        Vector3 rumbo = fin - s; rumbo.y = 0f; rumbo = rumbo.sqrMagnitude > 1f ? rumbo.normalized : Vector3.forward;
        hordas.Sort((a, b) => Vector3.Dot(a.transform.position - s, rumbo).CompareTo(Vector3.Dot(b.transform.position - s, rumbo)));
        for (int i = 0; i < hordas.Count; i++)
        {
            hordas[i].displayName = $"Horda {i + 1} de {hordas.Count}";
            hordas[i].roster = Roster;
        }
        if (salida != null) { salida.unlockedBy = null; salida.requiere = hordas.ToArray(); }
        if (jefeFinal != null) jefeFinal.esperaA = hordas.ToArray();
        Debug.Log($"[Diorama] {layout}: {hordas.Count} hordas" + (jefeFinal != null ? " + Dante." : "."));
    }

    /// <summary>
    /// Cuatro arenas sobre puntos del camino: suelo parejo, sin agua, lejos del inicio y de la salida, separadas entre sí
    /// y en el orden del recorrido. null si el mapa no tiene caminos suficientes (se usa lo de antes).
    /// </summary>
    List<RoomEncounter> HordasEnCamino(Vector3 inicio, Vector3 fin)
    {
        if (puntosCamino.Count < 30) return null;
        Vector3 rumbo = fin - inicio; rumbo.y = 0f;
        float largo = rumbo.magnitude; if (largo < 1f) return null;
        rumbo /= largo;
        var agua = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") agua.Add(mr.bounds);
        }
        var buenos = new List<Vector3>();
        foreach (var p in puntosCamino)
        {
            var v = new Vector3(p.x, 0f, p.y);
            if (Plano(v - inicio) < 12f || Plano(v - fin) < 10f) continue;
            float y = Height(p.x, p.y);
            bool ok = y > VoidY + 1f;
            for (float dx = -4f; dx <= 4f && ok; dx += 2f)
                for (float dz = -4f; dz <= 4f && ok; dz += 2f)
                {
                    float h = Height(p.x + dx, p.y + dz);
                    var q = new Vector2(p.x + dx, p.y + dz);
                    if (Mathf.Abs(h - y) > 0.6f || InWater(q) || NewWorldWater(q)) ok = false;
                    foreach (var b in agua)
                        if (ok && q.x > b.min.x && q.x < b.max.x && q.y > b.min.z && q.y < b.max.z && b.max.y > h - 0.6f) ok = false;
                }
            if (ok) buenos.Add(new Vector3(p.x, y, p.y));
        }
        if (buenos.Count < 4) return null;
        buenos.Sort((a, b) => Vector3.Dot(a - inicio, rumbo).CompareTo(Vector3.Dot(b - inicio, rumbo)));
        // 4 puntos repartidos a lo largo del recorrido, a 16 m o más uno del otro
        var lista = new List<RoomEncounter>();
        var usados = new List<Vector3>();
        var lvl = root.Find("Nivel");
        for (int k = 0; k < HordasPorPiso; k++)
        {
            float meta = (k + 0.7f) / (HordasPorPiso + 0.2f);
            Vector3 mejor = Vector3.zero; float mejorD = float.MaxValue;
            foreach (var c in buenos)
            {
                bool lejos = true;
                foreach (var u in usados) if (Plano(c - u) < 16f) { lejos = false; break; }
                if (!lejos) continue;
                float t = Vector3.Dot(c - inicio, rumbo) / largo;
                float d = Mathf.Abs(t - meta);
                if (d < mejorD) { mejorD = d; mejor = c; }
            }
            if (mejorD == float.MaxValue) break;
            usados.Add(mejor);
            lista.Add(Arena(lvl != null ? lvl : root, "Horda", mejor, new Vector2(12f, 9f), 5));
        }
        return lista.Count == HordasPorPiso ? lista : null;
    }

    /// <summary>Llanos para las hordas que faltan en cada piso (vistos desde arriba): x, z, ancho y fondo de la arena.</summary>
    Vector4[] HordasExtra
    {
        get
        {
            switch (layout)
            {
                case Layout.Pueblo: return new[] { new Vector4(14f, -8f, 14f, 7f), new Vector4(28f, 10f, 14f, 8f) };   // antes del río y junto a la plaza
                case Layout.Puerto: return new[] { new Vector4(14f, -10f, 14f, 10f) };                                // lado este de la plaza
                case Layout.Cima: return new[] { new Vector4(0f, -50f, 12f, 8f), new Vector4(27f, -11f, 11f, 8f) };     // isla del inicio y la de la fuente
                default: return new Vector4[0];
            }
        }
    }

    static float Plano(Vector3 v) { v.y = 0f; return v.magnitude; }

    FloorExit BuscarSalida()
    {
        foreach (var e in root.GetComponentsInChildren<FloorExit>(true)) return e;
        return null;
    }

    void ConvertirEnHorda(RoomEncounter r)
    {
        r.isBoss = false;
        r.bossName = "";
        r.maxAlive = 4;
        r.size = new Vector2(Mathf.Max(r.size.x, 12f), Mathf.Max(r.size.y, 10f));
        foreach (Transform t in r.transform) if (t.name == "Aparicion") DestroyImmediate(t.gameObject);
        r.spawnPoints = Apariciones(r.transform, r.size, 5);
    }

    Transform[] Apariciones(Transform arena, Vector2 size, int n)
    {
        var sp = new List<Transform>();
        for (int i = 0; i < n; i++)
        {
            var t = Group("Aparicion", arena);
            float ang = i / (float)n * Mathf.PI * 2f + 0.4f;
            t.localPosition = new Vector3(Mathf.Cos(ang) * size.x * 0.32f, 0.1f, Mathf.Sin(ang) * size.y * 0.32f);
            sp.Add(t);
        }
        return sp.ToArray();
    }

    /// <summary>
    /// Una arena nueva en un llano del camino: busca, en el tramo más largo entre el inicio, las hordas y la salida, el
    /// punto más cercano a su mitad donde el suelo sea plano (14 x 10 m), seco, libre y lejos de las otras arenas.
    /// </summary>
    RoomEncounter ArenaAutomatica(List<RoomEncounter> hordas, Vector3 inicio, Vector3 fin)
    {
        var pts = new List<Vector3> { inicio };
        foreach (var h in hordas) pts.Add(h.transform.position);
        pts.Add(fin);
        // tramos más largos primero
        var tramos = new List<int>();
        for (int i = 0; i + 1 < pts.Count; i++) tramos.Add(i);
        tramos.Sort((a, b) => Plano(pts[b + 1] - pts[b]).CompareTo(Plano(pts[a + 1] - pts[a])));
        var agua = new List<Bounds>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") agua.Add(mr.bounds);
        }
        var w = Walk;
        foreach (int i in tramos)
        {
            Vector3 mid = (pts[i] + pts[i + 1]) / 2f;
            Vector3 mejor = Vector3.zero; float mejorD = float.MaxValue;
            for (float dx = -24f; dx <= 24f; dx += 3f)
                for (float dz = -24f; dz <= 24f; dz += 3f)
                {
                    var c = new Vector2(mid.x + dx, mid.z + dz);
                    float d = dx * dx + dz * dz;
                    if (d >= mejorD || !ArenaCabe(c, w, agua, pts, out float y)) continue;
                    mejorD = d; mejor = new Vector3(c.x, y, c.y);
                }
            if (mejorD == float.MaxValue) continue;
            var lvl = root.Find("Nivel");
            var enc = Arena(lvl != null ? lvl : root, "Horda", mejor, new Vector2(14f, 10f), 5);
            return enc;
        }
        return null;
    }

    bool ArenaCabe(Vector2 c, Rect walk, List<Bounds> agua, List<Vector3> otras, out float y)
    {
        y = Height(c.x, c.y);
        if (!walk.Contains(new Vector2(c.x - 7f, c.y - 5f)) || !walk.Contains(new Vector2(c.x + 7f, c.y + 5f))) return false;
        foreach (var o in otras) if (Plano(new Vector3(c.x, 0f, c.y) - o) < 18f) return false;
        if (!LibreSinCamino(c, 4f)) return false;
        // sobre el camino: el centro de la arena a menos de 4 m de algún punto del camino (si el mapa tiene caminos)
        if (puntosCamino.Count > 0)
        {
            float mejor = float.MaxValue;
            foreach (var p in puntosCamino) { float d = (p - c).sqrMagnitude; if (d < mejor) mejor = d; }
            if (mejor > 16f) return false;
        }
        for (float x = -7f; x <= 7f; x += 1.75f)
            for (float z = -5f; z <= 5f; z += 1.65f)
            {
                float h = Height(c.x + x, c.y + z);
                if (Mathf.Abs(h - y) > 0.45f || h < VoidY + 1f) return false;
                if (layout == Layout.Pueblo && InWater(new Vector2(c.x + x, c.y + z))) return false;
                foreach (var b in agua)
                    if (c.x + x > b.min.x && c.x + x < b.max.x && c.y + z > b.min.z && c.y + z < b.max.z && b.max.y > h - 0.6f) return false;
            }
        return true;
    }
}
