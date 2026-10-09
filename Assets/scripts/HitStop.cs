using System.Collections;
using UnityEngine;

/// <summary>Congela el juego unos milisegundos al conectar un golpe. HitStop.Do(0.06f);</summary>
public class HitStop : MonoBehaviour
{
    static HitStop instance;

    public static void Do(float duration, float timeScale = 0.02f)
    {
        if (duration <= 0f) return;
        if (instance == null)
        {
            var go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStop>();
        }
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.Run(duration, timeScale));
    }

    IEnumerator Run(float duration, float scale)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    void OnDestroy() { if (instance == this) Time.timeScale = 1f; }
}
