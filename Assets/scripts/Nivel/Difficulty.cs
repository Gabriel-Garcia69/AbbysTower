using System;
using UnityEngine;

/// <summary>
/// Modo de juego elegido en Opciones (menú de pausa). Se guarda en PlayerPrefs.
/// Afecta: daño que recibe Abby, vida/velocidad/ritmo de ataque de los enemigos, curaciones y lo que se pierde al morir.
/// La vida de los enemigos se fija al aparecer; lo demás cambia al instante.
/// </summary>
public static class Difficulty
{
    public enum Level { Facil, Normal, Dificil, Hardcore }

    public struct Preset
    {
        public string name, description;
        public Color color;
        public float damageTaken, enemyHealth, enemySpeed, attackCooldown, healing, fragmentsLost;
        public bool loseUpgrades;
    }

    public static readonly Preset[] Presets =
    {
        new Preset { name = "FÁCIL", description = "Para disfrutar la historia. Recibes la mitad de daño.",
            color = new Color(0.45f, 0.95f, 0.6f), damageTaken = 0.5f, enemyHealth = 0.7f, enemySpeed = 0.9f, attackCooldown = 1.3f, healing = 1.5f, fragmentsLost = 0.25f },
        new Preset { name = "NORMAL", description = "La experiencia pensada para el juego.",
            color = new Color(0.55f, 0.75f, 1f), damageTaken = 1f, enemyHealth = 1f, enemySpeed = 1f, attackCooldown = 1f, healing = 1f, fragmentsLost = 0.5f },
        new Preset { name = "DIFÍCIL", description = "Enemigos más duros y agresivos. Curarte rinde menos.",
            color = new Color(1f, 0.6f, 0.25f), damageTaken = 1.5f, enemyHealth = 1.35f, enemySpeed = 1.12f, attackCooldown = 0.8f, healing = 0.75f, fragmentsLost = 0.75f },
        new Preset { name = "PESADILLA", description = "Triple de daño y enemigos implacables. Solo para quien ya conoce la torre.",
            color = new Color(1f, 0.15f, 0.2f), damageTaken = 3f, enemyHealth = 1.8f, enemySpeed = 1.25f, attackCooldown = 0.6f, healing = 0.5f, fragmentsLost = 1f, loseUpgrades = true },
    };

    const string PrefKey = "abby.difficulty";
    static Level? current;

    public static event Action Changed;

    public static Level Current
    {
        get
        {
            if (current == null) current = (Level)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)Level.Normal), 0, Presets.Length - 1);
            return current.Value;
        }
        set
        {
            if (current == value) return;
            current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static Preset Active => Presets[(int)Current];
}
