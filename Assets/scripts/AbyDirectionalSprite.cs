using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sprite de Aby en 8 direcciones relativo a la cámara (sin Animator).
/// - Elige el sprite según hacia dónde mira (Facing) visto desde la cámara.
/// - Caminar: usa los frames de Walking cuando existen (East; West = East volteado).
///   En las demás direcciones usa el sprite fijo con un pequeño "bob" para que no se vea congelada.
/// Ponlo en Visual (junto al SpriteRenderer). Desactiva SpriteDirectionAnimator y deja el Controller del Animator en None.
/// Click derecho en el título del componente → "Auto-asignar sprites de Aby" para llenar los campos solo.
/// </summary>
public class AbyDirectionalSprite : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController player;
    public SpriteRenderer sr;
    public Transform cam;

    [Header("Idle por dirección (0=E 1=NE 2=N 3=NW 4=W 5=SW 6=S 7=SE)")]
    public Sprite[] idle = new Sprite[8];

    [Header("Caminar (frames hacia el Este)")]
    public Sprite[] walkEast;
    public float walkFps = 12f;
    public float bobHeight = 0.04f;     // rebote para direcciones sin frames de caminar
    public float bobSpeed = 14f;

    [Header("Carpetas (solo para Auto-asignar)")]
    public string rotationsFolder = "Assets/sprites/Abby/Idle/rotations";
    public string walkEastFolder = "Assets/sprites/Abby/Idle/animations/Walking/east";

    static readonly string[] Names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    float baseY, animTime;
    bool baseYSet;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (player != null) player.autoFlip = false;   // los 8 sprites reemplazan el flip
    }

    void LateUpdate()
    {
        if (player == null || sr == null) return;
        if (cam == null) { if (Camera.main == null) return; cam = Camera.main.transform; }
        if (!baseYSet) { baseY = transform.localPosition.y; baseYSet = true; }

        // dirección en pantalla: x = derecha, y = hacia el fondo (arriba en pantalla)
        Vector3 f = player.Facing;
        Vector3 camR = cam.right; camR.y = 0f; camR.Normalize();
        Vector3 camF = cam.forward; camF.y = 0f; camF.Normalize();
        float x = Vector3.Dot(f, camR);
        float y = Vector3.Dot(f, camF);
        float ang = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        int dir = Mathf.RoundToInt(ang / 45f);
        dir = ((dir % 8) + 8) % 8;

        bool walking = player.State == PlayerState.Move && player.Speed > 0.1f;
        bool eastOrWest = dir == 0 || dir == 4;

        sr.flipX = false;
        float bob = 0f;

        if (walking)
        {
            animTime += Time.deltaTime;
            if (eastOrWest && walkEast != null && walkEast.Length > 0)
            {
                int i = Mathf.FloorToInt(animTime * walkFps) % walkEast.Length;
                sr.sprite = walkEast[i];
                sr.flipX = dir == 4;                  // West = East volteado
            }
            else
            {
                sr.sprite = GetIdle(dir);
                bob = Mathf.Abs(Mathf.Sin(animTime * bobSpeed)) * bobHeight;
            }
        }
        else
        {
            animTime = 0f;
            sr.sprite = GetIdle(dir);
        }

        Vector3 p = transform.localPosition;
        p.y = baseY + bob;
        transform.localPosition = p;
    }

    Sprite GetIdle(int dir)
    {
        if (idle != null && dir < idle.Length && idle[dir] != null) return idle[dir];
        return sr.sprite;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-asignar sprites de Aby")]
    void AutoAssign()
    {
        Undo.RecordObject(this, "Auto-asignar sprites de Aby");
        idle = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            idle[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{rotationsFolder}/{Names[i]}.png");
            if (idle[i] == null) Debug.LogWarning($"No encontré {rotationsFolder}/{Names[i]}.png (¿Texture Type = Sprite?)");
        }

        var frames = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < 64; i++)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{walkEastFolder}/frame_{i:000}.png");
            if (s == null) break;
            frames.Add(s);
        }
        walkEast = frames.ToArray();

        player = GetComponentInParent<PlayerController>();
        sr = GetComponent<SpriteRenderer>();
        EditorUtility.SetDirty(this);
        Debug.Log($"Aby: {System.Array.FindAll(idle, s => s != null).Length}/8 idle, {walkEast.Length} frames de caminar.");
    }
#endif
}
