using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lo que Abby va pensando mientras sube: unas líneas al entrar al piso, otras a la mitad de las hordas y otras cuando se
/// abre la salida (textos en Historia.Relato). No detiene el juego. Se crea sola al cargar cada piso.
/// </summary>
public class RelatoPiso : MonoBehaviour
{
    readonly Queue<string> cola = new Queue<string>();
    string texto;
    float t, alfa;
    FloorExit salida;
    readonly List<RoomEncounter> hordas = new List<RoomEncounter>();
    bool dijoMitad, dijoFin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded += (s, m) => { if (m == LoadSceneMode.Single) Crear(s.name); };
        Crear(SceneManager.GetActiveScene().name);
    }

    static void Crear(string escena)
    {
        if (!Historia.Relato.ContainsKey(escena)) return;
        new GameObject("RelatoPiso").AddComponent<RelatoPiso>();
    }

    IEnumerator Start()
    {
        string escena = SceneManager.GetActiveScene().name;
        var lineas = Historia.Relato[escena];
        // que pase antes lo que tapa la pantalla: la cinemática del exterior y el título del piso
        yield return new WaitForSeconds(4.5f);
        while (Ocupado()) yield return null;
        Agregar(lineas.inicio);
        foreach (var e in FindObjectsByType<FloorExit>()) { salida = e; break; }
        foreach (var r in FindObjectsByType<RoomEncounter>()) if (!r.isBoss) hordas.Add(r);
        while (!dijoFin)
        {
            int limpias = 0;
            foreach (var r in hordas) if (r != null && r.IsCleared) limpias++;
            if (!dijoMitad && hordas.Count > 0 && limpias * 2 >= hordas.Count) { dijoMitad = true; Agregar(lineas.mitad); }
            if (salida != null && salida.EstaAbierta && hordas.Count > 0 && limpias >= hordas.Count) { dijoFin = true; Agregar(lineas.fin); }
            yield return new WaitForSeconds(0.25f);
        }
    }

    static bool Ocupado()
    {
        foreach (var i in FindObjectsByType<ExteriorIntro>()) if (i.enabled) return true;
        return DanteFinal.Activo;
    }

    void Agregar(string[] l) { if (l != null) foreach (var s in l) cola.Enqueue(s); }

    void Update()
    {
        bool oculto = DanteFinal.Activo || PauseMenu.IsPaused || LoreNote.Open != null;
        alfa = Mathf.MoveTowards(alfa, !oculto && texto != null ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (oculto) return;
        if (texto == null)
        {
            if (cola.Count > 0) { texto = cola.Dequeue(); t = 0f; }
            return;
        }
        t += Time.unscaledDeltaTime;
        var kb = Keyboard.current; var gp = Gamepad.current;
        // da tiempo de leer sin detener la pelea; Enter (o Select en el control: A es saltar) pasa a la siguiente
        bool pasar = (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.selectButton.wasPressedThisFrame);
        if (t > 1.8f + texto.Length * 0.055f || (t > 0.4f && pasar)) texto = null;
    }

    void OnGUI()
    {
        if (texto == null || alfa <= 0.01f) return;
        GUI.depth = -900;
        CuadroNarracion.Dialogo(Historia.Protagonista, texto, CuadroNarracion.Letras(texto, t), alfa * 0.96f);
    }
}
