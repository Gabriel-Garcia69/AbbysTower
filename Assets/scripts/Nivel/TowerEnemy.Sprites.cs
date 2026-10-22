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

    bool SpriteVisual()
    {
        var sprites = new Sprite[8];
        bool any = false;
        for (int i = 0; i < 8; i++)
        {
            sprites[i] = Resources.Load<Sprite>($"EnemigosSprites/{kind}/{SpriteDirs[i]}");
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

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || enemigo == null || sr == null) return;
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) return;
            f.Normalize();
            transform.rotation = Quaternion.LookRotation(f, Vector3.up);   // de frente a la cámara, derecho

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
            else if (enemigo.flash > 0f) c = new Color(1f, 0.45f, 0.45f);
            else if (enemigo.state == State.Telegraph || enemigo.state == State.Slam)
                c = Color.Lerp(Color.white, new Color(0.85f, 0.55f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 30f));
            else if (enemigo.Enraged) c = new Color(1f, 0.8f, 0.85f);
            sr.color = c;
        }
    }
}
