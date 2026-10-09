using System;

/// <summary>
/// Estado de la partida que sobrevive entre escenas (exterior ↔ pisos): fragmentos de torre (la moneda que sueltan
/// los enemigos) y mejoras compradas al mercader. Es estático: se pierde al cerrar el juego (todavía no hay guardado).
/// Al morir se pierde la mitad de los fragmentos; las mejoras se conservan.
/// </summary>
public static class RunState
{
    public static int Fragments { get; private set; }
    public static int DamageLevel { get; private set; }   // cada nivel: +25% de daño
    public static int HeartLevel { get; private set; }    // cada nivel: +25 de vida máxima

    public static float DamageMultiplier => 1f + 0.25f * DamageLevel;
    public static float MaxHealthBonus => 25f * HeartLevel;

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

    /// <summary>Castigo al morir: se pierde la mitad de los fragmentos.</summary>
    public static void OnDeath()
    {
        Fragments /= 2;
        Changed?.Invoke();
    }
}
