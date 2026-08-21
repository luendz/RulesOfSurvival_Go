using UnityEngine;

namespace ROS.Game.Inventory
{
    [CreateAssetMenu(
        menuName = "ROS/Inventory/Consumable Definition",
        fileName  = "Consumable_"
    )]
    public sealed class ConsumableDefinition : ScriptableObject
    {
        [Header("Efecto")]
        [Min(0f)]
        public float healAmount   = 0f;   // puntos de vida restaurados

        [Min(0f)]
        public float energyAmount = 0f;   // energía / boost (reservado)

        [Header("Duración")]
        [Min(0.1f)]
        public float useDuration  = 4f;   // segundos para completar el uso

        [Header("Cancelación")]
        public bool cancelOnDamage = true;
        public bool cancelOnMove   = false;

        [Header("Límite de curación")]
        [Tooltip("Si está activo, el ítem no cura por encima de este porcentaje de vida.")]
        public bool  respectMaxFraction    = false;
        [Range(0f, 1f)]
        public float maxHealthFraction     = 0.75f;
    }
}
