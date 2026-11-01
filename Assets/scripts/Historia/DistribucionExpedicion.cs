using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dónde se paran el capitán y los miembros de la expedición: repartidos en semicírculo hacia la fogata y los braseros, nunca encima
/// del sendero central, a 1,2 m o más entre sí y a 2 m o más de Dante. Lo usan el DioramaBuilder (posiciones iniciales) y
/// ExpedicionPiso20 (cada vez que la expedición avanza).
/// </summary>
public static class DistribucionExpedicion
{
    public const float DistEntreSi = 1.2f, DistDante = 2f;
    /// <summary>Medio ancho del sendero (1,8 m) más margen: nadie se para a menos de esto del centro del sendero.</summary>
    public const float FueraDelSendero = 3.2f;
    public const float LimiteX = 18.5f;

    /// <summary>Arco (semicírculo) de candidatos alrededor de un centro, de ang0 a ang1 grados (0 = +X, 90 = +Z).</summary>
    public static List<Vector2> Semicirculo(Vector2 centro, float r0, float r1, float ang0, float ang1)
    {
        var l = new List<Vector2>();
        for (float r = r0; r <= r1 + 0.01f; r += 1f)
            for (float a = ang0; a <= ang1 + 0.01f; a += 14f)
                l.Add(centro + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r);
        return l;
    }

    /// <summary>Franjas a los dos lados del sendero, entre él y los braseros (a 7 m), alrededor de la altura z.</summary>
    public static List<Vector2> Franjas(Func<float, float> senderoX, float z, float dz)
    {
        var l = new List<Vector2>();
        for (float zz = z - dz; zz <= z + dz + 0.01f; zz += 1.4f)
            for (float lat = 3.4f; lat <= 6.4f + 0.01f; lat += 0.8f)
                foreach (float lado in new[] { -1f, 1f })
                    l.Add(new Vector2(senderoX(zz) + lado * lat, zz));
        return l;
    }

    /// <summary>
    /// Elige n puestos entre los candidatos (en orden aleatorio) que cumplan las reglas; el resultado va de norte a sur (el capitán toma
    /// el primero). Si no alcanzan, devuelve los que hay.
    /// </summary>
    public static Vector2[] Puestos(List<Vector2> candidatos, int n, Vector2? dante, Func<float, float> senderoX, System.Random rng)
    {
        var c = new List<Vector2>(candidatos);
        for (int i = c.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = c[i]; c[i] = c[j]; c[j] = t; }
        var elegidos = new List<Vector2>();
        foreach (var p in c)
        {
            if (Mathf.Abs(p.x) > LimiteX) continue;
            if (Mathf.Abs(p.x - senderoX(p.y)) < FueraDelSendero) continue;
            if (dante.HasValue && Vector2.Distance(p, dante.Value) < DistDante) continue;
            bool cerca = false;
            foreach (var e in elegidos) if (Vector2.Distance(p, e) < DistEntreSi) { cerca = true; break; }
            if (cerca) continue;
            elegidos.Add(p);
            if (elegidos.Count == n) break;
        }
        elegidos.Sort((a, b) => b.y.CompareTo(a.y));
        return elegidos.ToArray();
    }
}
