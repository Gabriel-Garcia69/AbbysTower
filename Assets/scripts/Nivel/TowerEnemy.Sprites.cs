using UnityEngine;

/// <summary>
/// Apariencia de sprite (pixel art de PixelLab) para los enemigos del GDD: si existen las 8 rotaciones en
/// Resources/EnemigosSprites/&lt;Tipo&gt;/&lt;dirección&gt;.png se usan en lugar de las piezas 3D.
/// El sprite mira siempre a la cámara y elige la dirección según hacia dónde camina el enemigo (como Abby).
/// El tamaño y los pies vienen del .meta (pixels por unidad y pivote), así que aquí no se escala.
/// </summary>
public partial class TowerEnemy
{
    static readonly string[] SpriteDirs = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
    static Material spriteMat;
    float glowEscala = 1f;

    bool SpriteVisual()
    {
        var sprites = new Sprite[8];
        bool any = false;
        // el jefe de cada piso tiene su dibujo (por escena: el patrón de ataque se repite entre pisos)
        string carpeta = kind == Kind.Jefe ? "Jefe_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : kind.ToString();
        for (int i = 0; i < 8; i++)
        {
            sprites[i] = Resources.Load<Sprite>($"EnemigosSprites/{carpeta}/{SpriteDirs[i]}");
            any |= sprites[i] != null;
        }
        if (!any) return false;

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        var go = new GameObject("Cuerpo");
        go.transform.SetParent(visual, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = SpriteMaterial();
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        sr.receiveShadows = true;
        var es = go.AddComponent<EnemySprite>();
        es.enemigo = this; es.sr = sr; es.sprites = sprites;
        sr.sprite = sprites[6] != null ? sprites[6] : System.Array.Find(sprites, s => s != null);
        body = null;
        eyes = new Renderer[0];
        if (IsBoss)
        {
            // el jefe trae su propia luz (late al enfurecerse, ver Update)
            var l = new GameObject("Brillo").AddComponent<Light>();
            l.transform.SetParent(visual, false);
            l.transform.localPosition = new Vector3(0f, 3.4f, -1.2f);
            l.type = LightType.Point; l.color = new Color(0.72f, 0.45f, 1f); l.range = 7f; l.intensity = 2.2f; l.shadows = LightShadows.None;
            glow = l; glowEscala = 0.4f;   // un sprite se quema con la luz fuerte de los jefes 3D
        }
        visual.localScale = Vector3.one * 0.05f;
        return true;
    }

    /// <summary>El mismo material que el sprite de Abby (iluminado por las luces 3D); si no hay Abby, el de sprites.</summary>
    static Material SpriteMaterial()
    {
        if (spriteMat != null) return spriteMat;
        var p = GameObject.FindWithTag("Player");
        var abby = p != null ? p.GetComponentInChildren<AbbySpriteAnimator>(true) : null;
        var sr = abby != null ? abby.GetComponent<SpriteRenderer>() : null;
        if (sr != null && sr.sharedMaterial != null) spriteMat = sr.sharedMaterial;
        else spriteMat = new Material(Shader.Find("Sprites/Default"));
        return spriteMat;
    }

    /// <summary>Elige la rotación según la cámara, parpadea al recibir golpes y brilla morado al avisar un ataque.</summary>
    public class EnemySprite : MonoBehaviour
    {
        public TowerEnemy enemigo;
        public SpriteRenderer sr;
        public Sprite[] sprites;
        int dir = 6;
        Vector3 ultima, vel;
        float paso, sacudida, fase = -1f;
        bool listo;

        /// <summary>
        /// Movimiento del sprite (sin cuadros de animación): respira quieto, rebota y se inclina al caminar, se agacha al
        /// avisar el ataque y se estira al lanzarse, tiembla al recibir un golpe. Escala y giro sobre el eje de la vista.
        /// </summary>
        void Mover(Vector3 f, Vector3 r, out Vector3 escala, out Vector3 offset, out float giro)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 p = enemigo.transform.position;
            if (!listo) { ultima = p; listo = true; fase = Random.Range(0f, 10f); }
            Vector3 v = (p - ultima) / dt; v.y = 0f; ultima = p;
            vel = Vector3.Lerp(vel, v, 1f - Mathf.Exp(-12f * dt));
            float rapidez = vel.magnitude;
            escala = Vector3.one; offset = Vector3.zero; giro = 0f;
            float t = Time.time + fase;   // cada uno a su ritmo

            switch (enemigo.state)
            {
                case State.Telegraph:
                {
                    float k = Mathf.Clamp01(enemigo.timer / Mathf.Max(0.1f, enemigo.telegraph));
                    escala = new Vector3(1f + 0.12f * k, 1f - 0.14f * k, 1f);   // se agacha, cargando
                    giro = -Vector3.Dot(enemigo.facing, r) * 10f * k;            // se echa para atrás
                    offset = new Vector3(Mathf.Sin(t * 60f) * 0.02f * k, 0f, 0f);
                    break;
                }
                case State.Lunge:
                    escala = new Vector3(0.88f, 1.12f, 1f);                       // se estira al lanzarse
                    giro = Vector3.Dot(enemigo.facing, r) * 14f;
                    break;
                case State.Recover:
                    escala = new Vector3(1.04f, 0.97f, 1f);
                    break;
                default:
                    if (rapidez > 0.3f)
                    {
                        // camina: rebote a cada paso y se inclina hacia donde va
                        paso += dt * Mathf.Lerp(6f, 11f, Mathf.Clamp01(rapidez / 6f));
                        float b = Mathf.Abs(Mathf.Sin(paso));
                        escala = new Vector3(1f + 0.04f * (1f - b), 1f - 0.04f * (1f - b) + 0.03f * b, 1f);
                        offset = Vector3.up * b * 0.06f;
                        giro = -Vector3.Dot(vel.normalized, r) * Mathf.Clamp01(rapidez / 4f) * 7f + Mathf.Sin(paso) * 2.5f;
                    }
                    else
                    {
                        float s2 = Mathf.Sin(t * 2.2f);
                        escala = new Vector3(1f - 0.015f * s2, 1f + 0.025f * s2, 1f);   // respira
                    }
                    break;
            }
            // golpe recibido: tiembla un instante
            if (enemigo.flash > 0f) sacudida = 0.18f;
            if (sacudida > 0f)
            {
                sacudida -= dt;
                offset += new Vector3(Mathf.Sin(Time.time * 90f) * 0.06f * (sacudida / 0.18f), 0f, 0f);
                escala = Vector3.Scale(escala, new Vector3(1.06f, 0.94f, 1f));
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || enemigo == null || sr == null) return;
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) return;
            f.Normalize();
            Vector3 rr = cam.transform.right; rr.y = 0f; rr.Normalize();
            Mover(f, rr, out var escala, out var offset, out float giro);
            transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0f, 0f, giro);   // de frente a la cámara
            transform.localScale = escala;
            transform.localPosition = Quaternion.Inverse(transform.parent.rotation) * (rr * offset.x + Vector3.up * offset.y);

            Vector3 mira = enemigo.facing.sqrMagnitude > 0.01f ? enemigo.facing : -f;
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            float ang = Mathf.Atan2(Vector3.Dot(mira, f), Vector3.Dot(mira, r)) * Mathf.Rad2Deg;
            int d = ((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8;
            if (sprites[d] != null) dir = d;
            if (sprites[dir] != null) sr.sprite = sprites[dir];

            Color c = Color.white;
            if (enemigo.state == State.Spawning)
            {
                // sale como silueta de sombra y toma su color al terminar de salir
                float k = Mathf.Clamp01((enemigo.timer - GrietaAbre) / GrietaSube);
                c = Color.Lerp(new Color(0.12f, 0.04f, 0.2f), Color.white, k * k);
            }
            else if (enemigo.state == State.Dying) c = Color.Lerp(new Color(1f, 0.6f, 0.6f), new Color(0.15f, 0.05f, 0.25f), Mathf.Clamp01(enemigo.timer / 0.3f));   // se hace sombra
            else if (enemigo.flash > 0f) c = new Color(1f, 0.45f, 0.45f);
            else if (enemigo.state == State.Telegraph || enemigo.state == State.Slam)
                c = Color.Lerp(Color.white, new Color(0.85f, 0.55f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 30f));
            else if (enemigo.Enraged) c = new Color(1f, 0.8f, 0.85f);
            sr.color = c;
        }
    }
}
