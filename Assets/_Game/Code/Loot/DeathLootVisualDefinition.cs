using UnityEngine;

namespace ROS.Game.Loot
{
    [CreateAssetMenu(
        menuName = "ROS/Loot/Death Loot Visual",
        fileName = "DeathLootContainerVisual"
    )]
    public sealed class DeathLootVisualDefinition :
        ScriptableObject
    {
        public GameObject visualPrefab;
    }
}
