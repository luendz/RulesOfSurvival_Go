using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Mantiene el nombre historico del HUD durante Play Mode para componentes
    /// de compatibilidad que aun lo localizan por nombre. No crea visuales.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    public sealed class EditorFirstHudRuntimeRoot : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.name = "ROS_HUD_Runtime";
        }
    }
}
