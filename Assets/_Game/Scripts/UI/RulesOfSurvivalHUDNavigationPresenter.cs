using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Tipo de compatibilidad. La brujula y la flecha del minimapa son parte
    /// del prefab editable y RulesOfSurvivalHUD actualiza sus datos.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNavigationPresenter : MonoBehaviour
    {
    }
}
