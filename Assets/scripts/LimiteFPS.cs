using UnityEngine;

/// <summary>
/// El juego corre a 60 FPS con sincronía vertical. Sin límite rendía cientos de cuadros por segundo y se comía la CPU
/// y la tarjeta (y calentaba la laptop) sin que se viera mejor. Se aplica solo al arrancar, en cualquier escena.
/// </summary>
public static class LimiteFPS
{
    public const int Objetivo = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = Objetivo;
    }
}
