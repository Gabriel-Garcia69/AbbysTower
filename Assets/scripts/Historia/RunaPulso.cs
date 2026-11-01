using UnityEngine;

/// <summary>
/// Pulso leve de la emisión de las runas del suelo (la barrera del tutorial). Usa una copia del material en cada renderer (nunca el
/// asset compartido), así no modifica Dio_RunaBarrera ni las demás runas.
/// </summary>
public class RunaPulso : MonoBehaviour
{
    public float hz = 0.5f, amplitud = 0.3f;

    Material[] mats;
    Color[] baseEmision;

    void Start()
    {
        var rs = GetComponentsInChildren<Renderer>();
        mats = new Material[rs.Length]; baseEmision = new Color[rs.Length];
        for (int i = 0; i < rs.Length; i++)
        {
            mats[i] = rs[i].material;   // instancia propia
            baseEmision[i] = mats[i].HasProperty("_EmissionColor") ? mats[i].GetColor("_EmissionColor") : Color.white;
        }
    }

    void Update()
    {
        if (mats == null) return;
        float k = 1f - amplitud * (0.5f - 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f));   // entre 1 y 1 - amplitud
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != null && mats[i].HasProperty("_EmissionColor")) mats[i].SetColor("_EmissionColor", baseEmision[i] * k);
    }

    void OnDestroy()
    {
        if (mats != null) foreach (var m in mats) if (m != null) Destroy(m);
    }
}
