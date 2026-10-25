using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fondo del menú principal: va mostrando paisajes de algunos pisos (no todos), uno a la vez. Carga el mapa en segundo
/// plano encima de la escena del menú, le apaga todo lo de juego (Abby, cámara del piso, HUD, peleas), lo recorre con
/// un paneo lento de la cámara del menú y pasa al siguiente con un fundido a negro. MenuPrincipal lo agrega.
/// </summary>
public class FondoMenu : MonoBehaviour
{
    [System.Serializable]
    public struct Vista
    {
        public string escena;
        public Vector3 centro;
        public float radio, altura, alturaMira, angulo0, giro;
    }

    /// <summary>Los paisajes del menú: puerto con faro, valle de las cascadas, acantilado al atardecer y la cima.</summary>
    public Vista[] vistas =
    {
        new Vista { escena = "Puerto", centro = new Vector3(33f, 3f, 17f), radio = 50f, altura = 16f, alturaMira = 6f, angulo0 = -0.6f, giro = 0.035f },
        new Vista { escena = "Cascadas", centro = new Vector3(-5f, 6f, 95f), radio = 55f, altura = 22f, alturaMira = 4f, angulo0 = 0.2f, giro = 0.03f },
        new Vista { escena = "Acantilado", centro = new Vector3(0f, 6f, -5f), radio = 48f, altura = 18f, alturaMira = 3f, angulo0 = -0.4f, giro = 0.035f },
        new Vista { escena = "Cima", centro = new Vector3(-2f, 4f, 0f), radio = 62f, altura = 24f, alturaMira = 4f, angulo0 = 0.6f, giro = 0.03f },
    };
    public float segundosPorVista = 14f, fundido = 1.2f;

    [System.NonSerialized] public Camera cam;
    Scene menu;
    int indice;
    float tVista, negro = 1f;
    Vista actual;
    bool hayVista;

    void Start()
    {
        menu = gameObject.scene;
        StartCoroutine(Ciclo());
    }

    IEnumerator Ciclo()
    {
        while (true)
        {
            var v = vistas[indice % vistas.Length];
            indice++;
            if (!Application.CanStreamedLevelBeLoaded(v.escena)) continue;
            var op = SceneManager.LoadSceneAsync(v.escena, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            var sc = SceneManager.GetSceneByName(v.escena);
            Limpiar(sc);
            SceneManager.SetActiveScene(sc);   // su luz, cielo y niebla
            Musica.Poner(Musica.Pista.Menu);     // el mapa cambiaría a su música
            actual = v; hayVista = true; tVista = 0f;
            for (float t = 0f; t < fundido; t += Time.unscaledDeltaTime) { negro = 1f - t / fundido; yield return null; }
            negro = 0f;
            yield return new WaitForSeconds(segundosPorVista);
            for (float t = 0f; t < fundido; t += Time.unscaledDeltaTime) { negro = t / fundido; yield return null; }
            negro = 1f;
            hayVista = false;
            SceneManager.SetActiveScene(menu);
            var un = SceneManager.UnloadSceneAsync(sc);
            while (un != null && !un.isDone) yield return null;
        }
    }

    /// <summary>Un mapa jugable convertido en paisaje: sin Abby, sin cámaras ni oídos propios, sin HUD ni peleas.</summary>
    static void Limpiar(Scene sc)
    {
        foreach (var raiz in sc.GetRootGameObjects())
        {
            foreach (var fd in raiz.GetComponentsInChildren<FloorDirector>(true)) fd.gameObject.SetActive(false);
            foreach (var c in raiz.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
            foreach (var a in raiz.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
            foreach (var r in raiz.GetComponentsInChildren<RoomEncounter>(true)) r.enabled = false;
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag("Player")) { t.gameObject.SetActive(false); break; }
        }
    }

    void LateUpdate()
    {
        if (cam == null || !hayVista) return;
        tVista += Time.deltaTime;
        float a = actual.angulo0 + tVista * actual.giro;
        cam.transform.position = actual.centro + new Vector3(Mathf.Sin(a) * actual.radio, actual.altura, -Mathf.Cos(a) * actual.radio);
        cam.transform.rotation = Quaternion.LookRotation(actual.centro + Vector3.up * actual.alturaMira - cam.transform.position);
    }

    void OnGUI()
    {
        if (negro <= 0.001f) return;
        GUI.depth = 10;   // detrás del menú (que dibuja con depth 0)
        var prev = GUI.color;
        GUI.color = new Color(0.02f, 0.01f, 0.05f, negro);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
