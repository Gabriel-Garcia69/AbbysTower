using UnityEngine;

/// <summary>
/// Una persona del tutorial como sprite: elige la rotación (8 direcciones) según hacia dónde mira la figura vista desde la
/// cámara y se pone de frente a ella solo sobre el eje Y (con la misma compensación de inclinación que SpriteBillboard).
/// Va en el hijo "Visual" de la figura; la figura (raíz) es la que ExpedicionPiso20 mueve y gira: su frente es -Z local.
///
/// Vida procedural (sin animación dibujada): respira (la escala Y sube de 1 a 1,025 a 0,7 Hz, con fase propia), se balancea un
/// poco de lado a lado, y parada cambia de mirada de vez en cuando (45° cada 3 a 6 s, al azar por figura); si Dante está a menos
/// de radioMirada se queda mirándolo. Al desplazarse hace un balanceo vertical de bobAltura a bobHz.
///
/// Gancho para animación real: si se llena "caminar" (frames por dirección, mismo orden que rotaciones) o se llama a
/// PonerCaminar, al desplazarse usa esos frames (a un ritmo que sigue la velocidad); sin ellos, usa la rotación quieta.
/// </summary>
public class FiguraBillboard : MonoBehaviour
{
    [System.Serializable]
    public class Direccion
    {
        public Sprite[] frames = new Sprite[0];
        public bool Hay => frames != null && frames.Length > 0;
    }

    [Tooltip("Rotaciones: 0=E 1=NE 2=N 3=NO 4=O 5=SO 6=S 7=SE")]
    public Sprite[] rotaciones = new Sprite[8];
    [Tooltip("Opcional: ciclo de caminar por dirección (mismo orden). Vacío = se usa la rotación quieta.")]
    public Direccion[] caminar = new Direccion[8];
    public float caminarFps = 8f;
    [Tooltip("Velocidad (m/s) a la que el ciclo de caminar corre a caminarFps.")]
    public float caminarVelocidadRef = 4.5f;

    public SpriteRenderer sr;
    public Transform raiz;
    public float bobAltura = 0.05f, bobHz = 2f;
    [Header("Vida procedural")]
    public float respiracion = 0.025f, respiracionHz = 0.7f;
    public float balanceoGrados = 1.5f, balanceoHz = 0.45f;
    public float radioMirada = 6f;
    /// <summary>Giro extra (la caída de la expedición); lo anima ExpedicionPiso20.</summary>
    public Quaternion caida = Quaternion.identity;

    Camera cam;
    Transform dante;
    Vector3 ultima, baseLocal, baseEscala;
    float fase, faseResp, faseBal, tCaminar, tMirada, proximaMirada, yawExtra, tBuscar;

    /// <summary>Carga el ciclo de caminar (un arreglo de frames por dirección, 8 en el orden de rotaciones).</summary>
    public void PonerCaminar(Sprite[][] porDireccion, float fps = 8f)
    {
        caminar = new Direccion[8];
        for (int i = 0; i < 8; i++) caminar[i] = new Direccion { frames = porDireccion != null && i < porDireccion.Length && porDireccion[i] != null ? porDireccion[i] : new Sprite[0] };
        caminarFps = fps;
    }

    void Awake()
    {
        if (raiz == null) raiz = transform.parent;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        baseLocal = transform.localPosition;
        baseEscala = transform.localScale;
        ultima = raiz.position;
        // cada figura a su ritmo: que no respiren ni miren todas a la vez
        faseResp = Random.value * Mathf.PI * 2f;
        faseBal = Random.value * Mathf.PI * 2f;
        proximaMirada = Random.Range(3f, 6f);
        tMirada = Random.value * proximaMirada;
        if (caminar == null || caminar.Length != 8) caminar = new Direccion[8];
    }

    void LateUpdate()
    {
        if (cam == null) { cam = Camera.main; if (cam == null) return; }
        Vector3 f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        // movimiento de la raíz
        Vector3 v = (raiz.position - ultima) / dt; v.y = 0f;
        ultima = raiz.position;
        float rapidez = v.magnitude;
        bool anda = rapidez > 0.3f;

        // a quién mira: caminando, hacia donde va; parada, a Dante si está cerca, y si no, cambia de mirada cada 3 a 6 s
        tBuscar -= dt;
        if (dante == null && tBuscar <= 0f) { tBuscar = 1f; var p = GameObject.FindWithTag("Player"); if (p != null) dante = p.transform; }
        Vector3 mira = -raiz.forward; mira.y = 0f;
        bool haciaDante = false;
        if (anda) { yawExtra = 0f; tMirada = 0f; }
        else
        {
            if (dante != null)
            {
                Vector3 d = dante.position - raiz.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f && d.sqrMagnitude < radioMirada * radioMirada) { mira = d; haciaDante = true; }
            }
            if (!haciaDante)
            {
                tMirada += dt;
                if (tMirada >= proximaMirada)
                {
                    tMirada = 0f; proximaMirada = Random.Range(3f, 6f);
                    yawExtra = Mathf.Clamp(yawExtra + (Random.value < 0.5f ? -45f : 45f), -90f, 90f);   // 45° a un lado u otro, sin dar la vuelta entera
                }
                mira = Quaternion.Euler(0f, yawExtra, 0f) * mira;
            }
        }

        int dir = 6;
        if (mira.sqrMagnitude > 0.0001f)
        {
            mira.Normalize();
            dir = ((Mathf.RoundToInt(Mathf.Atan2(Vector3.Dot(mira, f), Vector3.Dot(mira, r)) * Mathf.Rad2Deg / 45f) % 8) + 8) % 8;
        }

        // sprite: ciclo de caminar si lo hay, si no la rotación quieta
        if (sr != null)
        {
            Direccion c = caminar != null && dir < caminar.Length ? caminar[dir] : null;
            if (anda && c != null && c.Hay)
            {
                tCaminar += dt * caminarFps * Mathf.Clamp(rapidez / Mathf.Max(0.1f, caminarVelocidadRef), 0.4f, 1.8f);
                sr.sprite = c.frames[Mathf.FloorToInt(tCaminar) % c.frames.Length];
            }
            else
            {
                tCaminar = 0f;
                if (rotaciones != null && dir < rotaciones.Length && rotaciones[dir] != null) sr.sprite = rotaciones[dir];
            }
        }

        // billboard sobre el eje Y, con la compensación de inclinación, la respiración y el balanceo lateral
        float roll = Mathf.Sin(Time.time * balanceoHz * Mathf.PI * 2f + faseBal) * balanceoGrados * (anda ? 1.8f : 1f);
        transform.rotation = Quaternion.LookRotation(f, Vector3.up) * caida * Quaternion.AngleAxis(roll, Vector3.forward);
        float pitch = Mathf.Asin(Mathf.Clamp(-cam.transform.forward.y, -1f, 1f));
        float aire = 1f + respiracion * (0.5f - 0.5f * Mathf.Cos(Time.time * respiracionHz * Mathf.PI * 2f + faseResp));
        transform.localScale = new Vector3(baseEscala.x, baseEscala.y * aire / Mathf.Max(Mathf.Cos(pitch), 0.5f), baseEscala.z);

        // balanceo vertical al desplazarse
        if (anda) fase += dt * bobHz * Mathf.PI * 2f; else fase = 0f;
        transform.localPosition = baseLocal + Vector3.up * (anda ? bobAltura * (0.5f - 0.5f * Mathf.Cos(fase)) : 0f);
    }
}
