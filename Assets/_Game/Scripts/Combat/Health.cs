using System;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxArmor = 100f;
        [SerializeField] private float armorAbsorption = 0.55f;

        public float CurrentHealth { get; private set; }
        public float CurrentArmor { get; private set; }
        public float MaxHealth => maxHealth;
        public float MaxArmor => maxArmor;
        public PlayerLifeState LifeState { get; private set; }
        public bool IsAlive => LifeState == PlayerLifeState.Alive;
        public DamageInfo LastDamage { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ArmorChanged;
        public event Action<DamageInfo> Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentArmor = 0f;
            LifeState = PlayerLifeState.Alive;
            LastDamage = default;
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f) return;

            LastDamage = damage;

            float incoming = damage.Amount;
            if (CurrentArmor > 0f)
            {
                float absorbed = Mathf.Min(CurrentArmor, incoming * armorAbsorption);
                CurrentArmor -= absorbed;
                incoming -= absorbed;
                ArmorChanged?.Invoke(CurrentArmor, maxArmor);
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - incoming);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            if (CurrentHealth <= 0f)
            {
                LifeState = PlayerLifeState.Dead;
                Died?.Invoke(damage);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void AddArmor(float amount)
        {
            CurrentArmor = Mathf.Clamp(CurrentArmor + amount, 0f, maxArmor);
            ArmorChanged?.Invoke(CurrentArmor, maxArmor);
        }
    }
}
