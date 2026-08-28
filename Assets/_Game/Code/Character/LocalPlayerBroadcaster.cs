using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Character
{
    /// <summary>
    /// Coloca este componente en el prefab del jugador local.
    /// En Start() notifica a HUDSessionConnector para conectar el HUD al jugador.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerBroadcaster : MonoBehaviour
    {
        private void Start()
        {
            if (HUDSessionConnector.Instance == null)
                return;

            HUDSessionConnector.Instance.BindLocalPlayer(gameObject);
        }
    }
}
