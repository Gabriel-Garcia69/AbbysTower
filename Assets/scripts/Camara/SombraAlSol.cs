using UnityEngine;

/// <summary>
/// Sombra de un personaje sprite (enemigos, Dante) con el sol: el sprite visible mira a la cámara, así que su sombra era la de un
/// plano de canto (una rayita con el sol de lado) que además giraba con la cámara. Esta copia invisible solo proyecta sombra y se
/// queda de frente al sol, con el mismo dibujo, tamaño y posición que el sprite en cada cuadro.
/// (Abby usa SpriteShadowCasters, que además maneja faroles; aquí basta el sol y no toca las capas de las luces.)
/// Va en el mismo objeto que el SpriteRenderer.
/// </summary>
[DefaultExecutionOrder(120)]   // después de que el animador del sprite eligió dibujo, escala y giro
public class SombraAlSol : MonoBehaviour
{
    SpriteRenderer fuente, copia;

    void Awake()
    {
        fuente = GetComponent<SpriteRenderer>();
        if (fuente == null) { enabled = false; return; }
        var go = new GameObject("SombraSprite");
        go.transform.SetParent(transform.parent, false);
        copia = go.AddComponent<SpriteRenderer>();
        copia.sharedMaterial = fuente.sharedMaterial;
        copia.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        copia.receiveShadows = false;
        fuente.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void OnDisable() { if (copia != null) copia.enabled = false; }
    void OnEnable() { if (copia != null) copia.enabled = true; }
    void OnDestroy() { if (copia != null) Destroy(copia.gameObject); }

    void LateUpdate()
    {
        if (copia == null) return;
        copia.enabled = fuente.enabled && fuente.sprite != null;
        if (!copia.enabled) return;
        copia.sprite = fuente.sprite;
        copia.color = fuente.color;
        var t = copia.transform;
        t.position = transform.position;
        t.localScale = transform.localScale;
        var sol = RenderSettings.sun;
        Vector3 hacia = sol != null ? -sol.transform.forward : Vector3.up;
        hacia.y = 0f;
        // de frente al sol (de pie); sin sol o con el sol justo arriba, como el sprite
        t.rotation = hacia.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-hacia.normalized, Vector3.up) : transform.rotation;
    }
}
