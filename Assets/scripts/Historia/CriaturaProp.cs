using UnityEngine;

/// <summary>Criatura de utilería para las cinemáticas: le pone sus colores (cuerpo oscuro, ojos que brillan) y la hace flotar.</summary>
public class CriaturaProp : MonoBehaviour
{
    public Color cuerpo = new Color(0.1f, 0.07f, 0.17f);
    public Color ojos = new Color(0.75f, 0.45f, 1f);
    public float bob = 0.08f;
    float fase;
    Vector3 basePos;

    void Start()
    {
        fase = Random.value * 10f;
        basePos = transform.localPosition;
        var mpb = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(mpb);
            bool ojo = r.name.StartsWith("Ojo") || r.name.StartsWith("Orbe") || r.name.StartsWith("Pua");
            mpb.SetColor("_BaseColor", ojo ? ojos * 4f : r.name.StartsWith("Cuerpo") ? cuerpo : cuerpo * 0.85f);
            r.SetPropertyBlock(mpb);
        }
    }

    void Update() => transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2.4f + fase) * bob;
}
