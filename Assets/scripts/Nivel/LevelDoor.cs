using UnityEngine;

/// <summary>
/// Puerta de energía entre una sala y su pasillo. Abierta = sin barrera ni collider.
/// RoomEncounter la cierra al empezar el combate y la abre al limpiar la sala.
/// </summary>
public class LevelDoor : MonoBehaviour
{
    public GameObject barrier;
    public Collider blocker;
    public Light glow;   // ilumina el pasillo cuando la barrera está activa

    public bool IsClosed { get; private set; }

    public void SetClosed(bool closed)
    {
        IsClosed = closed;
        if (barrier != null) barrier.SetActive(closed);
        if (blocker != null) blocker.enabled = closed;
        if (glow != null) glow.enabled = closed;
    }
}
