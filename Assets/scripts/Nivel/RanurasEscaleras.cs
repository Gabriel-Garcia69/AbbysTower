using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tapa las ranuras junto a las escaleras. El pasillo del terreno donde va una escalera es un poco más ancho que ella y
/// sus celdas son bloques de 0.7 m: entre el muro invisible del costado y el risco quedaba una zanja angosta y honda
/// (en el faro del Acantilado, de 3 m). Abby entraba por el pie de la escalera o caía desde arriba y ya no podía salir.
/// Al cargar el piso se mide el suelo junto a cada escalera y, donde hay zanja, se rellena con un bloque invisible al
/// nivel del risco. No toca las escenas: funciona aunque se regenere un mapa. Lo llama FloorDirector.
/// </summary>
public static class RanurasEscaleras
{
    const float Paso = 0.35f;        // cada cuánto se mide a lo largo de la escalera
    const float Ancho = 1.6f;        // hasta dónde se rellena hacia afuera del costado
    const float Hondo = 0.8f;        // cuánto más abajo que la escalera tiene que estar el suelo para ser zanja

    static readonly RaycastHit[] hits = new RaycastHit[16];

    public static void Revisar()
    {
        int tapas = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.Find("Huella") == null || t.Find("Escalon") == null) continue;   // grupo de una escalera (DioramaBuilder.Stairs)
            if (t.Find("Collider_Ranura") != null) continue;
            tapas += Tapar(t);
        }
        if (tapas > 0) Debug.Log($"[Escaleras] {tapas} ranuras tapadas junto a las escaleras.");
    }

    /// <summary>Ancho, largo y altura de la escalera en su espacio local (z a lo largo, x de lado a lado, y hacia arriba).</summary>
    static bool Medidas(Transform s, out float ancho, out float largo, out float alto)
    {
        ancho = 0f; largo = 0f; alto = 0f;
        foreach (Transform c in s)
        {
            if (c.name != "Huella") continue;
            var mf = c.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            Vector3 tam = Vector3.Scale(mf.sharedMesh.bounds.size, c.localScale);
            ancho = Mathf.Max(ancho, tam.x);
            largo = Mathf.Max(largo, c.localPosition.z + tam.z / 2f);
            alto = Mathf.Max(alto, c.localPosition.y + tam.y / 2f);
        }
        return ancho > 0.5f && largo > 0.5f;
    }

    static int Tapar(Transform s)
    {
        if (!Medidas(s, out float ancho, out float largo, out float alto)) return 0;
        int tapas = 0;
        foreach (float lado in new[] { -1f, 1f })
        {
            // zanja a lo largo del costado: [z0, z1) con el suelo de afuera (risco) a cierta altura
            float z0 = 0f, tope = float.NegativeInfinity, fondo = float.PositiveInfinity;
            bool abierta = false;
            for (float z = -0.6f; z <= largo + 1.8f; z += Paso)
            {
                bool zanja = Zanja(s, lado, ancho, largo, alto, z, out float suelo, out float risco);
                if (zanja)
                {
                    if (!abierta) { abierta = true; z0 = z; tope = float.NegativeInfinity; fondo = float.PositiveInfinity; }
                    tope = Mathf.Max(tope, risco);
                    fondo = Mathf.Min(fondo, suelo);
                }
                else if (abierta)
                {
                    abierta = false;
                    tapas += Bloque(s, lado, ancho, z0, z, fondo, tope);
                }
            }
            if (abierta) tapas += Bloque(s, lado, ancho, z0, largo + 1.8f, fondo, tope);
        }
        return tapas;
    }

    /// <summary>
    /// ¿Hay zanja en ese punto del costado? El suelo justo afuera del muro está bastante más abajo que la escalera y,
    /// un poco más allá, vuelve a subir (risco): es un hueco angosto, no una ladera abierta.
    /// </summary>
    static bool Zanja(Transform s, float lado, float ancho, float largo, float alto, float z, out float suelo, out float risco)
    {
        suelo = risco = 0f;
        float escalon = alto * Mathf.Clamp01(z / largo);   // altura de la escalera (local) en ese punto
        float x = lado * (ancho / 2f + 0.62f);
        if (!Suelo(s, new Vector3(x, alto + 6f, z), out suelo)) return false;
        if (suelo > escalon - Hondo) return false;
        // ¿más allá vuelve a subir? (si no, es una ladera: el muro del costado basta)
        bool cerrada = false;
        for (float k = 0.7f; k <= Ancho + 0.2f && !cerrada; k += 0.35f)
        {
            if (!Suelo(s, new Vector3(lado * (ancho / 2f + 0.62f + k), alto + 6f, z), out float r)) continue;
            if (r > suelo + 1.2f) { cerrada = true; risco = r; }
        }
        return cerrada;
    }

    /// <summary>Altura local del suelo más alto bajo ese punto (sin contar la escalera misma).</summary>
    static bool Suelo(Transform s, Vector3 local, out float y)
    {
        y = 0f;
        Vector3 o = s.TransformPoint(local);
        int n = Physics.RaycastNonAlloc(o, Vector3.down, hits, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float mejor = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c.transform.IsChildOf(s) || c.GetComponentInParent<CharacterController>() != null || c.GetComponentInParent<TowerEnemy>() != null) continue;
            if (hits[i].point.y > mejor) mejor = hits[i].point.y;
        }
        if (float.IsNegativeInfinity(mejor)) return false;
        y = s.InverseTransformPoint(new Vector3(o.x, mejor, o.z)).y;
        return true;
    }

    /// <summary>Bloque invisible que llena la zanja hasta el nivel del risco.</summary>
    static int Bloque(Transform s, float lado, float ancho, float z0, float z1, float fondo, float tope)
    {
        if (z1 - z0 < 0.3f || tope <= fondo) return 0;
        var go = new GameObject("Collider_Ranura");
        go.transform.SetParent(s, false);
        float x0 = ancho / 2f + 0.25f, x1 = ancho / 2f + 0.25f + Ancho;
        go.transform.localPosition = new Vector3(lado * (x0 + x1) / 2f, (fondo - 0.5f + tope) / 2f, (z0 + z1) / 2f);
        go.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, tope - (fondo - 0.5f), z1 - z0 + Paso);
        return 1;
    }
}
