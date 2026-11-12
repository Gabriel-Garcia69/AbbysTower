using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Baja al suelo los cristales de decoración que quedaron flotando (orillas de las islas de bloques de la Gruta, escalones).
/// Mide el suelo bajo toda la piña, no solo bajo el centro, y la hunde hasta el punto más bajo (o hasta el agua).
/// Los del techo cuelgan a propósito y los del carrito van encima de él: no se tocan.
/// El generador (DioramaBuilder.AsentarCristal) ya los pone bien; esto arregla las escenas guardadas sin regenerarlas.
/// </summary>
public static class AsentarCristales
{
    [MenuItem("Abby/Revisión/Asentar cristales flotantes (escena abierta)")]
    static void EscenaAbierta()
    {
        int n = Asentar();
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Cristales] {EditorSceneManager.GetActiveScene().name}: {n} piñas asentadas. Guarda la escena.");
    }

    [MenuItem("Abby/Revisión/Asentar cristales flotantes (todas las escenas de la ruta)")]
    static void TodaLaRuta()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string volver = EditorSceneManager.GetActiveScene().path;
        foreach (var e in EditorBuildSettings.scenes)
        {
            if (!e.enabled) continue;
            var escena = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
            int n = Asentar();
            if (n > 0) { EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena); }
            Debug.Log($"[Cristales] {escena.name}: {n} piñas asentadas.");
        }
        if (!string.IsNullOrEmpty(volver)) EditorSceneManager.OpenScene(volver, OpenSceneMode.Single);
    }

    static readonly RaycastHit[] hits = new RaycastHit[32];

    static int Asentar()
    {
        Physics.SyncTransforms();
        float agua = NivelDelAgua();
        int n = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "CristalesGrandes" && t.name != "Cristales") continue;
            if (Excluido(t)) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float suelo = SueloMasBajo(t, b, agua);
            if (float.IsNaN(suelo)) continue;
            float hueco = b.min.y - suelo;
            if (hueco < 0.05f) continue;                         // ya toca el suelo
            float baja = Mathf.Min(hueco, 2.5f) + b.size.y * 0.06f;  // hundido un poco: de lejos se ve que sale del suelo
            Undo.RecordObject(t, "Asentar cristales");
            t.position += Vector3.down * baja;
            n++;
        }
        return n;
    }

    static bool Excluido(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            if (p.name == "CristalesTecho" || p.name == "Colgante" || p.name == "Carrito") return true;
        return t.up.y < 0.5f;
    }

    /// <summary>
    /// El suelo bajo la piña: se mide en el centro y en 8 puntos alrededor y se toma el tercero más bajo. Así, si
    /// cuelga sobre la orilla de un bloque baja hasta el bloque de abajo, pero una sola punta sobre el agua no la hunde
    /// entera. Nunca más abajo que el agua.
    /// </summary>
    static float SueloMasBajo(Transform t, Bounds b, float agua)
    {
        var c = b.center;
        float rx = b.extents.x * 0.7f, rz = b.extents.z * 0.7f;
        var pts = new List<Vector3> { c };
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            pts.Add(c + new Vector3(Mathf.Cos(a) * rx, 0f, Mathf.Sin(a) * rz));
        }
        var suelos = new List<float>();
        foreach (var p in pts)
        {
            int k = Physics.RaycastNonAlloc(new Vector3(p.x, b.max.y + 2f, p.z), Vector3.down, hits, b.size.y + 14f, ~0, QueryTriggerInteraction.Ignore);
            float alto = float.NegativeInfinity;
            for (int i = 0; i < k; i++)
                if (!hits[i].collider.transform.IsChildOf(t)) alto = Mathf.Max(alto, hits[i].point.y);
            suelos.Add(float.IsNegativeInfinity(alto) ? agua : Mathf.Max(alto, agua - 0.3f));
        }
        suelos.Sort();
        float y = suelos[Mathf.Min(2, suelos.Count - 1)];
        return y < -900f ? float.NaN : y;
    }

    /// <summary>Altura del agua del mapa (superficies con el shader de agua), o muy abajo si no hay.</summary>
    static float NivelDelAgua()
    {
        float y = -999f;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
        {
            var m = r.sharedMaterial;
            if (m == null || m.shader == null || m.shader.name != "Abby/Water") continue;
            if (r.bounds.size.x < 20f) continue;   // solo lagos y mar, no charcos ni fuentes
            y = Mathf.Max(y, r.bounds.max.y);
        }
        return y;
    }
}
