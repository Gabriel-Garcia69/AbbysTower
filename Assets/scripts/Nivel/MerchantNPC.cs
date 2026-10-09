using UnityEngine;

/// <summary>
/// El mercader detrás del mostrador: respira (sube y baja), voltea hacia Abby cuando está cerca, saluda con el brazo
/// y la primera vez que la ve le dice algo. Lo arma FloorLayout con formas (se reemplaza por arte después).
/// </summary>
public class MerchantNPC : MonoBehaviour
{
    public Transform body;
    public Transform arm;
    public float lookRadius = 7f;
    public string greeting = "¡Bienvenida, viajera! Tengo de todo... por unos fragmentos.";

    Transform player;
    bool greeted;
    Quaternion baseRot;
    float t;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        baseRot = transform.localRotation;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (body != null) body.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);

        float dist = 999f;
        if (player != null)
        {
            Vector3 d = player.position - transform.position; d.y = 0f;
            dist = d.magnitude;
            if (dist < lookRadius && d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 4f * Time.deltaTime);
            else
                transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRot, 2f * Time.deltaTime);
        }

        bool waving = dist < 4.5f;
        if (arm != null)
        {
            float wave = waving ? 140f + Mathf.Sin(t * 9f) * 25f : 15f;
            arm.localRotation = Quaternion.Slerp(arm.localRotation, Quaternion.Euler(0f, 0f, wave), 8f * Time.deltaTime);
        }
        if (!greeted && dist < 6f)
        {
            greeted = true;
            FloorDirector.Popup(transform.position + Vector3.up * 2.6f, greeting, new Color(1f, 0.9f, 0.6f), 4f);
        }
    }
}
