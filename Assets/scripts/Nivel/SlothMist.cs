using UnityEngine;

/// <summary>
/// PEREZA — Niebla de sopor: dentro, Abby camina a la mitad de velocidad y le pesan los párpados (aviso en pantalla).
/// El impulso (dash) no se frena: es la forma de cruzarla rápido. Va con partículas de bruma azul.
/// </summary>
public class SlothMist : MonoBehaviour
{
    public Vector3 size = new Vector3(6f, 2f, 6f);
    [Range(0.1f, 1f)] public float slow = 0.5f;

    float notice;

    void Update()
    {
        var motor = Hazards.Motor;
        if (motor == null) return;
        Vector3 lp = transform.InverseTransformPoint(motor.transform.position);
        bool inside = Mathf.Abs(lp.x) < size.x / 2f && Mathf.Abs(lp.z) < size.z / 2f && lp.y > -1f && lp.y < size.y;
        if (!inside) return;
        motor.Slow(slow, 0.15f);
        notice -= Time.deltaTime;
        if (notice <= 0f)
        {
            notice = 3f;
            FloorDirector.Popup(motor.transform.position + Vector3.up * 1.9f, "...qué sueño...", new Color(0.7f, 0.85f, 1f), 1.6f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.5f);
        Gizmos.DrawWireCube(Vector3.up * size.y / 2f, size);
    }
}
