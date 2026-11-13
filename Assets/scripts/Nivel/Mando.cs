using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

/// <summary>
/// Control (gamepad) y teclado juntos: sabe qué se usó por última vez (para mostrar los botones del control o las
/// teclas), navega los menús con cruceta o stick (con repetición al mantener) y da el nombre de cada botón
/// (Xbox: A B X Y · PlayStation: Cruz Círculo Cuadrado Triángulo).
/// </summary>
public static class Mando
{
    static bool usando, ps;
    static int frame = -1, v, h;
    static int dirAntes; static float repetirEn;
    static Gamepad padCal; static bool lsOk, rsOk, ltAntes, bloqueoPulsado;
    static float gatI, gatD, gatDAntes, stIAntes, stDAntes, baseI, baseD; static Vector2 stI, stD;

    /// <summary>Gatillo izquierdo (LT) sostenido para bloquear, ya calibrado y con histéresis.</summary>
    public static bool BloqueoSostenido { get { Actualizar(); return ltAntes; } }
    /// <summary>LT recién apretado (este cuadro).</summary>
    public static bool BloqueoPulsado { get { Actualizar(); return bloqueoPulsado; } }
    /// <summary>Stick derecho calibrado (cero si en reposo no marcaba cero).</summary>
    public static Vector2 StickDerecho { get { Actualizar(); return stD; } }

    /// <summary>true si lo último que se tocó fue el control (los textos muestran sus botones).</summary>
    public static bool Usando { get { Actualizar(); return usando; } }

    /// <summary>Menús: -1 arriba, 1 abajo (teclado, cruceta o stick izquierdo; al mantener se repite).</summary>
    public static int Vertical { get { Actualizar(); return v; } }
    /// <summary>Menús: -1 izquierda, 1 derecha.</summary>
    public static int Horizontal { get { Actualizar(); return h; } }

    /// <summary>Aceptar: Enter / Espacio o A.</summary>
    public static bool Aceptar
    {
        get
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        }
    }

    /// <summary>Regresar: Esc o B.</summary>
    public static bool Atras
    {
        get
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
        }
    }

    // nombres de los botones
    public static string A => ps ? "Cruz" : "A";
    public static string B => ps ? "Círculo" : "B";
    public static string X => ps ? "Cuadrado" : "X";
    public static string Y => ps ? "Triángulo" : "Y";
    public static string LT => ps ? "L2" : "LT";
    public static string RB => ps ? "R1" : "RB";
    public static string R3 => "R3";
    public static string Start => ps ? "Options" : "Start";
    public const string StickIzq = "Stick izq.";
    public const string StickDer = "Stick der.";
    public const string Cruceta = "Cruceta";

    /// <summary>El texto que corresponde a lo que se está usando.</summary>
    public static string T(string teclado, string control) => Usando ? control : teclado;

    /// <summary>
    /// Cambia las teclas de un texto de ayuda escrito para teclado (letreros, pistas, consejos) por los botones del
    /// control cuando se juega con control. Así también sirven los textos guardados en las escenas.
    /// </summary>
    public static string Texto(string s)
    {
        if (string.IsNullOrEmpty(s) || !Usando) return s;
        s = s.Replace("WASD / stick izquierdo", "Stick izquierdo").Replace("WASD", StickIzq)
             .Replace("Clic izquierdo", X).Replace("Clic izq.", X)
             .Replace("Clic derecho sostenido", LT + " sostenido").Replace("Clic derecho", LT).Replace("Clic der.", LT)
             .Replace("Tres clics seguidos", "Tres golpes seguidos").Replace("tres clics seguidos", "tres golpes seguidos")
             .Replace("Shift", B).Replace("Espacio", A)
             .Replace("presiona E ", "presiona " + Y + " ").Replace("(E)", "(" + Y + ")")
             .Replace("F3", R3);
        if (s.StartsWith("E ") || s.StartsWith("E:") || s.StartsWith("E·")) s = Y + s.Substring(1);
        return s;
    }

    static void Actualizar()
    {
        if (frame == Time.frameCount) return;
        frame = Time.frameCount;
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;

        // gatillos y sticks "calibrados": solo cuentan después de verlos sueltos. Hay controles (o drivers) cuyo gatillo
        // en reposo no marca 0: Abby se quedaba con el escudo puesto y el juego creía que siempre se usaba el control
        if (gp != padCal)
        {
            padCal = gp; lsOk = rsOk = false; ltAntes = false;
            baseI = gp != null ? gp.leftTrigger.ReadValue() : 0f; baseD = gp != null ? gp.rightTrigger.ReadValue() : 0f;
        }
        if (gp != null)
        {
            ps = gp is DualShockGamepad;
            float lt = gp.leftTrigger.ReadValue(), rt = gp.rightTrigger.ReadValue();
            Vector2 ls = gp.leftStick.ReadValue(), rs = gp.rightStick.ReadValue();
            // el reposo de cada gatillo se aprende solo: baja de inmediato y sube despacio (si se queda a medias, en
            // unos segundos ya no cuenta como apretado; mantener el bloqueo un rato normal no lo apaga)
            float dtR = Time.unscaledDeltaTime;
            baseI = lt < baseI ? lt : Mathf.MoveTowards(baseI, lt, 0.04f * dtR);
            baseD = rt < baseD ? rt : Mathf.MoveTowards(baseD, rt, 0.04f * dtR);
            if (ls.sqrMagnitude < 0.04f) lsOk = true;
            if (rs.sqrMagnitude < 0.04f) rsOk = true;
            gatI = Mathf.Max(0f, lt - baseI); gatD = Mathf.Max(0f, rt - baseD);
            stI = lsOk ? ls : Vector2.zero; stD = rsOk ? rs : Vector2.zero;
        }
        else { gatI = gatD = 0f; stI = stD = Vector2.zero; }
        // con histéresis (se aprieta arriba de 0.55 y se suelta abajo de 0.35) para que el ruido no lo prenda y apague
        bool ltAhora = ltAntes ? gatI > 0.3f : gatI > 0.5f;
        bloqueoPulsado = ltAhora && !ltAntes;
        ltAntes = ltAhora;

        // ¿qué se usó? Solo cambia cuando de verdad se aprieta algo (no por un valor que se quede quieto):
        // el teclado o el mouse regresan a las teclas; un botón, un stick que se mueve o un gatillo, al control
        if (gp != null)
        {
            bool tocado = bloqueoPulsado || (gatD > 0.5f && gatDAntes <= 0.5f)
                       || (stI.sqrMagnitude > 0.3f && stIAntes <= 0.3f) || (stD.sqrMagnitude > 0.3f && stDAntes <= 0.3f);
            if (!tocado)
                foreach (var c in gp.allControls)
                    if (c is ButtonControl b && !b.synthetic && !(b.parent is StickControl)
                        && b != gp.leftTrigger && b != gp.rightTrigger && b.wasPressedThisFrame) { tocado = true; break; }
            if (tocado) usando = true;
            gatDAntes = gatD; stIAntes = stI.sqrMagnitude; stDAntes = stD.sqrMagnitude;
        }
        else usando = false;
        if ((kb != null && kb.anyKey.wasPressedThisFrame)
            || (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame || ms.delta.ReadValue().sqrMagnitude > 25f)))
            usando = false;

        // navegación de menús
        v = h = 0;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) v = -1;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) v = 1;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) h = -1;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) h = 1;
        }
        int dir = 0;   // 1 arriba, 2 abajo, 3 izquierda, 4 derecha (cruceta o stick)
        if (gp != null)
        {
            Vector2 st = stI;
            if (gp.dpad.up.isPressed) dir = 1;
            else if (gp.dpad.down.isPressed) dir = 2;
            else if (gp.dpad.left.isPressed) dir = 3;
            else if (gp.dpad.right.isPressed) dir = 4;
            else if (st.magnitude > 0.55f) dir = Mathf.Abs(st.y) >= Mathf.Abs(st.x) ? (st.y > 0f ? 1 : 2) : (st.x < 0f ? 3 : 4);
        }
        float ahora = Time.unscaledTime;
        bool dispara = false;
        if (dir != 0 && dir != dirAntes) { dispara = true; repetirEn = ahora + 0.4f; }
        else if (dir != 0 && ahora >= repetirEn) { dispara = true; repetirEn = ahora + 0.13f; }
        dirAntes = dir;
        if (dispara)
        {
            if (dir == 1) v = -1; else if (dir == 2) v = 1; else if (dir == 3) h = -1; else if (dir == 4) h = 1;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { usando = false; frame = -1; dirAntes = 0; padCal = null; ltAntes = false; }

    // se actualiza cada cuadro aunque nadie pregunte, para no perder la última tecla o botón
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        var go = new GameObject("Mando") { hideFlags = HideFlags.HideInHierarchy };
        Object.DontDestroyOnLoad(go);
        go.AddComponent<MandoVigia>();
    }

    internal static void Paso() => Actualizar();
}

[DefaultExecutionOrder(-2000)]
public sealed class MandoVigia : MonoBehaviour { void Update() => Mando.Paso(); }
