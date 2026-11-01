using UnityEngine;

/// <summary>
/// Oclusión suave de la gruta del tutorial: cada frame mira, de la cámara a Dante, qué piezas de utilería marcadas
/// (UtileriaOcluible: huesos, pilares, estalagmitas) quedan en medio o pegadas a la cámara y las atenúa a ~25 % de alfa en 0,2 s;
/// al salir de en medio se restauran igual de suave. Usa los límites (bounds) de los renderers, no raycasts: la utilería no tiene colliders.
/// </summary>
[DefaultExecutionOrder(1100)]   // después de la cámara
public class OclusionSuave : MonoBehaviour
{
    public float tiempo = 0.2f;
    [Tooltip("Una pieza a menos de esta distancia de la cámara también se atenúa (queda 'encima' del lente).")]
    public float margenCamara = 3f;

    Camera cam;
    Transform dante;

    void LateUpdate()
    {
        if (cam == null) { cam = Camera.main; if (cam == null) return; }
        if (dante == null) { var p = GameObject.FindWithTag("Player"); if (p == null) return; dante = p.transform; }
        Vector3 a = cam.transform.position, b = dante.position + Vector3.up * 1.2f;
        float dt = Time.unscaledDeltaTime;
        var todas = UtileriaOcluible.Todas;
        for (int i = 0; i < todas.Count; i++) todas[i].Avanzar(todas[i].EnMedio(a, b, margenCamara), dt, tiempo);
    }
}
