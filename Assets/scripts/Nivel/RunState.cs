using System;

/// <summary>
/// Estado de la partida que sobrevive entre escenas (exterior ↔ pisos): fragmentos de torre (la moneda que sueltan
/// los enemigos) y mejoras compradas al mercader. Es estático: se pierde al cerrar el juego (todavía no hay guardado).
/// Al morir se pierde una parte de los fragmentos según la dificultad (ver Difficulty).
/// </summary>
public static class RunState
{
    public static int Fragments { get; private set; }
    public static int DamageLevel { get; private set; }   // cada nivel: +25% de daño
    public static int HeartLevel { get; private set; }    // cada nivel: +25 de vida máxima

    public static float DamageMultiplier => 1f + 0.25f * DamageLevel;
    public static float MaxHealthBonus => 25f * HeartLevel;

    /// <summary>Vida con la que Abby sale de una escena (se restaura al entrar a la siguiente); -1 = nada guardado (se empieza con la vida completa).</summary>
    public static float SavedHealth { get; private set; } = -1f;
    public static bool HasSavedHealth => SavedHealth > 0f;
    public static void SaveHealth(float health) { SavedHealth = Math.Max(1f, health); }
    /// <summary>Se borra al empezar la partida y al morir: el reintento arranca con la vida completa.</summary>
    public static void ClearHealth() { SavedHealth = -1f; }

    public static event Action Changed;

    public static void AddFragments(int amount)
    {
        Fragments = Math.Max(0, Fragments + amount);
        Changed?.Invoke();
    }

    public static bool Spend(int cost)
    {
        if (Fragments < cost) return false;
        Fragments -= cost;
        Changed?.Invoke();
        return true;
    }

    public static void BuyDamage() { DamageLevel++; Changed?.Invoke(); }
    public static void BuyHeart() { HeartLevel++; Changed?.Invoke(); }

    /// <summary>Castigo al morir según la dificultad: una parte de los fragmentos (en Hardcore todo, mejoras incluidas).</summary>
    public static void OnDeath()
    {
        var diff = Difficulty.Active;
        Fragments -= (int)Math.Ceiling(Fragments * diff.fragmentsLost);
        if (diff.loseUpgrades) { DamageLevel = 0; HeartLevel = 0; }
        Changed?.Invoke();
    }
}
