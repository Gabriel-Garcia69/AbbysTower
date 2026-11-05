using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// En los mapas del diorama, las casas, árboles y arbustos que se interponen entre la cámara y Abby dejan de dibujarse.
/// Se agrega al cargar la escena para que valga en todos los mapas sin volver a generarlos.
/// </summary>
public static class AutoOcluyentes
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        Aplicar();
    }

    static void AlCargar(Scene s, LoadSceneMode m) => Aplicar();

    static void Aplicar()
    {
        if (Object.FindAnyObjectByType<DioramaBuilder>() == null || Object.FindAnyObjectByType<EscenaFinal>() != null) return;
        foreach (var t in Object.FindObjectsByType<Transform>())
        {
            string n = t.name;
            if (n != "Casa" && n != "Pino" && n != "PinoValle" && n != "Arbol" && n != "Arbusto" && n != "Palmera") continue;
            if (t.GetComponent<OccluderFade>() == null) t.gameObject.AddComponent<OccluderFade>();
        }
    }
}
