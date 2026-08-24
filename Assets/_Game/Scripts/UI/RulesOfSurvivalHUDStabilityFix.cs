using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Tipo de compatibilidad. Los slots visibles pertenecen al prefab editable
    /// y ya no necesitan una capa runtime que reescriba estilos o colores base.
    /// </summary>
    [DefaultExecutionOrder(2200)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDStabilityFix : MonoBehaviour
    {
    }
}
