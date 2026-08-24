using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Tipo de compatibilidad. El layout del estado del jugador vive ahora en
    /// el prefab editable y no se recalcula desde codigo.
    /// </summary>
    [DefaultExecutionOrder(1700)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDPlayerStatusLayout : MonoBehaviour
    {
    }
}
