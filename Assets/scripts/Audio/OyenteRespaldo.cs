using UnityEngine;

/// <summary>
/// Oído de respaldo del objeto "Sonido" (que no se destruye al cambiar de escena): solo se enciende cuando no hay otro
/// AudioListener activo, para que los efectos se oigan entre escenas. Antes se quedaba encendido siempre y, con la
/// cámara de la escena, Unity avisaba sin parar "There are 2 audio listeners in the scene".
/// </summary>
public class OyenteRespaldo : MonoBehaviour
{
    AudioListener propio;
    float revisar;

    void Awake()
    {
        propio = gameObject.AddComponent<AudioListener>();
        propio.enabled = FindObjectsByType<AudioListener>().Length <= 1;
    }

    void LateUpdate()
    {
        revisar -= Time.unscaledDeltaTime;
        if (revisar > 0f) return;
        revisar = 0.2f;
        bool otro = false;
        foreach (var l in FindObjectsByType<AudioListener>())
            if (l != propio && l.isActiveAndEnabled) { otro = true; break; }
        if (propio.enabled == otro) propio.enabled = !otro;
    }
}
