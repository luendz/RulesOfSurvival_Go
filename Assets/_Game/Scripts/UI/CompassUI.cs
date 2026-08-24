using ROS.Game.CameraSystem;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Compatibilidad legacy. La brujula visual vive ahora dentro de
    /// HUD_ROS_EDITABLE y RulesOfSurvivalHUD actualiza su contenido.
    /// Este componente conserva Bind para no romper llamadas antiguas,
    /// pero nunca crea Canvas, textos ni GameObjects en runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CompassUI : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ThirdPersonCamera playerCamera;

        public void Bind(
            Transform targetPlayer,
            ThirdPersonCamera camera = null
        )
        {
            playerTransform = targetPlayer;
            playerCamera = camera;
        }
    }
}
