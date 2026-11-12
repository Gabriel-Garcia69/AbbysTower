using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zonas donde la cámara fija gira sola unos grados para ver mejor ese pedazo de mapa, también durante las peleas.
/// Conserva la altura, la distancia y el lente del estilo elegido (F5/F6); los controles no brincan: la cámara ya sabe mantener
/// la dirección que Abby trae hasta que el jugador suelta. Se crean al cargar el mapa, sin regenerarlo.
/// </summary>
public static class ZonasCamara
{
    struct Zona
    {
        public float x, z, ancho, fondo, yaw, inclina, aleja;
        public Zona(float x, float z, float ancho, float fondo, float yaw, float inclina = 0f, float aleja = 0f)
        { this.x = x; this.z = z; this.ancho = ancho; this.fondo = fondo; this.yaw = yaw; this.inclina = inclina; this.aleja = aleja; }
    }

    static readonly Dictionary<string, Zona[]> Tabla = new Dictionary<string, Zona[]>
    {
        { "Diorama", new[] { new Zona(20f, -6f, 24f, 18f, 18f), new Zona(-18f, 10f, 26f, 22f, -16f), new Zona(0f, 36f, 40f, 24f, 0f, 3f, 2f) } },
        { "Puerto", new[] { new Zona(3f, 26f, 12f, 40f, 16f), new Zona(-18f, 16f, 26f, 12f, -20f), new Zona(42f, 22f, 22f, 26f, -28f, 2f, 2f), new Zona(0f, -48f, 40f, 18f, 0f, 3f) } },
        { "Cascadas", new[] { new Zona(0f, 70f, 110f, 56f, 16f), new Zona(0f, 130f, 110f, 56f, -16f), new Zona(0f, 190f, 110f, 40f, 12f), new Zona(10f, 226f, 80f, 26f, 8f, 3f, 3f) } },
        { "Acantilado", new[] { new Zona(4f, 0f, 44f, 18f, 8f, 2f), new Zona(4f, 31f, 26f, 26f, 22f, 2f, 3f), new Zona(27f, 35f, 20f, 8f, 24f), new Zona(62f, 44f, 30f, 28f, 28f, 3f, 3f) } },
        { "Cueva", new[] { new Zona(22f, 30f, 34f, 30f, 16f, 2f), new Zona(-16f, 62f, 40f, 28f, -16f, 2f), new Zona(0f, 104f, 50f, 34f, 0f, 3f, 2f) } },
        { "Lago", new[] { new Zona(-34f, 95f, 36f, 60f, 22f), new Zona(50f, 95f, 34f, 60f, -22f), new Zona(10f, 145f, 100f, 26f, 0f, 3f, 3f) } },
        { "Nevado", new[] { new Zona(0f, 145f, 120f, 90f, 0f, 6f, 3f), new Zona(0f, 228f, 120f, 40f, 0f, 4f, 3f) } },
        { "Cima", new[] { new Zona(27f, -11f, 26f, 22f, -18f, 2f, 2f), new Zona(-27f, 20f, 30f, 30f, 18f, 2f, 2f) } },
    };

    // La entrada a cada mundo, como la del piso 1: vista aérea que baja hasta Abby, un poco más desde arriba.
    // El piso 1 se queda como estaba: x = yaw, y = pitch, z = distancia, w = lookUp; luego el corrimiento del cuadro.
    static readonly Dictionary<string, (Vector4 v, Vector3 corre)> Aperturas = new Dictionary<string, (Vector4, Vector3)>
    {
        { "Puerto", (new Vector4(20f, 34f, 100f, 5f), new Vector3(8f, 0f, 30f)) },
        { "Cascadas", (new Vector4(-20f, 36f, 95f, 6f), new Vector3(-6f, 6f, 55f)) },   // antes 120 m: la vuelta a Abby se hacía eterna
        { "Acantilado", (new Vector4(30f, 34f, 105f, 5f), new Vector3(40f, 0f, 45f)) },
        { "Cueva", (new Vector4(12f, 25f, 54f, 3f), new Vector3(0f, 0f, 20f)) },   // por debajo del techo
        { "Lago", (new Vector4(20f, 34f, 130f, 6f), new Vector3(4f, 4f, 70f)) },
        { "Nevado", (new Vector4(20f, 36f, 100f, 5f), new Vector3(0f, 0f, 30f)) },
        { "Cima", (new Vector4(20f, 32f, 115f, 6f), new Vector3(0f, 0f, 40f)) },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void AlCargar(Scene s, LoadSceneMode m)
    {
        if (Aperturas.TryGetValue(s.name, out var ap))
            foreach (var c in Object.FindObjectsByType<CameraShot>())
                if (c.introSeconds > 0f)
                {
                    c.yaw = ap.v.x; c.pitch = ap.v.y; c.distance = ap.v.z; c.lookUp = ap.v.w; c.offset = ap.corre; c.fov = s.name == "Cueva" ? 42f : 30f; c.blendTime = 2.6f;
                    if (s.name == "Cascadas") c.introSeconds = Mathf.Min(c.introSeconds, 3.5f);   // 5.6 s en vez de 8: se hacía larga
                }
        if (!Tabla.TryGetValue(s.name, out var zonas)) return;
        var padre = new GameObject("ZonasCamara");
        foreach (var z in zonas)
        {
            var go = new GameObject("Zona");
            go.transform.SetParent(padre.transform, false);
            go.transform.position = new Vector3(z.x, 0f, z.z);
            var c = go.AddComponent<CameraShot>();
            c.relativa = true; c.enCombate = true;
            c.size = new Vector2(z.ancho, z.fondo); c.yMin = -30f; c.yMax = 60f;
            c.yaw = z.yaw; c.pitch = z.inclina; c.distance = z.aleja;
            c.blendTime = 2.6f; c.priority = 1;
        }
    }
}
