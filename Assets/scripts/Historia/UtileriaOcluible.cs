using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una pieza de utilería (hueso, pilar, estalagmita) que puede atenuarse cuando queda entre la cámara y Dante. Va en el objeto del
/// MeshRenderer. Mientras está opaca usa su material de siempre; al atenuarse cambia a su gemelo transparente (un asset por
/// material) y mueve el alfa con un MaterialPropertyBlock, así que nunca se crean materiales en juego. No se pone en suelo, paredes ni techo.
/// </summary>
public class UtileriaOcluible : MonoBehaviour
{
    public Material original, transparente;
    public float alfaMin = 0.25f;

    public static readonly List<UtileriaOcluible> Todas = new List<UtileriaOcluible>();

    Renderer rend;
    MaterialPropertyBlock bloque;
    Color colorBase = Color.white;
    float alfa = 1f;
    bool translucido;

    void OnEnable()
    {
        Todas.Add(this);
        rend = GetComponent<Renderer>();
        if (original != null && original.HasProperty("_BaseColor")) colorBase = original.GetColor("_BaseColor");
    }

    void OnDisable() { Todas.Remove(this); }

    /// <summary>¿Cruza el segmento cámara→Dante, o la cámara está pegada a la pieza (a menos de margen)?</summary>
    public bool EnMedio(Vector3 camara, Vector3 dante, float margen)
    {
        if (rend == null) return false;
        var b = rend.bounds; b.Expand(0.7f);
        if (b.SqrDistance(camara) < margen * margen) return true;
        Vector3 d = dante - camara; float largo = d.magnitude;
        return largo > 0.01f && b.IntersectRay(new Ray(camara, d / largo), out float t) && t <= largo;
    }

    /// <summary>Acerca el alfa al objetivo; la transición completa (1 → alfaMin) dura 'tiempo' segundos.</summary>
    public void Avanzar(bool tapa, float dt, float tiempo)
    {
        float nuevo = Mathf.MoveTowards(alfa, tapa ? alfaMin : 1f, dt / Mathf.Max(0.01f, tiempo) * (1f - alfaMin));
        if (Mathf.Approximately(nuevo, alfa)) return;
        alfa = nuevo;
        if (alfa >= 0.999f)
        {
            alfa = 1f;
            if (translucido) { rend.sharedMaterial = original; rend.SetPropertyBlock(null); translucido = false; }
            return;
        }
        if (!translucido) { if (transparente == null) return; rend.sharedMaterial = transparente; translucido = true; }
        if (bloque == null) bloque = new MaterialPropertyBlock();
        bloque.SetColor("_BaseColor", new Color(colorBase.r, colorBase.g, colorBase.b, alfa));
        rend.SetPropertyBlock(bloque);
    }
}
