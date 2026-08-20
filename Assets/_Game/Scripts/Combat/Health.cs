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
        public float CurrentArmor
        {
            get
            {
                EnsureProtectionReference();

                return _currentArmor +
                       (_protection != null
                           ? _protection.CurrentTotalDurability
                           : 0f);
            }
        }
        public float MaxHealth => maxHealth;
        public float MaxArmor
        {
            get
            {
                EnsureProtectionReference();

                return maxArmor +
                       (_protection != null
                           ? _protection.MaxTotalDurability
                           : 0f);
            }
        }
        public PlayerLifeState LifeState { get; private set; }
        public bool IsAlive => LifeState == PlayerLifeState.Alive;
        public DamageInfo LastDamage { get; private set; }
        public DamageResult LastDamageResult { get; private set; }
        public ProtectiveEquipment Protection
        {
            get
            {
                EnsureProtectionReference();
                return _protection;
            }
        }

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ArmorChanged;
        public event Action<DamageResult> Damaged;
        public event Action<DamageInfo> Died;

        private float _currentArmor;
        private ProtectiveEquipment _protection;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _currentArmor = 0f;
            LifeState = PlayerLifeState.Alive;
            LastDamage = default;
            LastDamageResult = default;
            EnsureProtectionReference();
        }

        private void OnDestroy()
        {
            if (_protection != null)
            {
                _protection.Changed -= OnProtectionChanged;
            }
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f) return;

            LastDamage = damage;

            float multiplier =
                DamageRules.GetHitZoneMultiplier(
                    damage.Type,
                    damage.HitZone
                );

            float incoming = damage.Amount * multiplier;
            float absorbed = 0f;

            EnsureProtectionReference();

            if (_protection != null)
            {
                ProtectionResolution resolution =
                    _protection.Resolve(
                        damage,
                        incoming
                    );

                absorbed += resolution.AbsorbedDamage;
                incoming = resolution.RemainingDamage;
            }

            if (_currentArmor > 0f &&
                DamageRules.CanProtectionReduce(damage.Type))
            {
                float shieldAbsorbed = Mathf.Min(
                    _currentArmor,
                    incoming * armorAbsorption
                );

                _currentArmor -= shieldAbsorbed;
                incoming -= shieldAbsorbed;
                absorbed += shieldAbsorbed;

                ArmorChanged?.Invoke(
                    CurrentArmor,
                    MaxArmor
                );
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - incoming);

            LastDamageResult = new DamageResult(
                damage,
                multiplier,
                damage.Amount * multiplier,
                absorbed,
                previousHealth - CurrentHealth,
                CurrentHealth <= 0f
            );

            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            Damaged?.Invoke(LastDamageResult);

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

        public void OverrideMaxHealth(float newMax)
        {
            if (newMax <= 0f) return;
            float ratio = maxHealth > 0f ? CurrentHealth / maxHealth : 1f;
            maxHealth = newMax;
            CurrentHealth = maxHealth * ratio;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void AddArmor(float amount)
        {
            _currentArmor = Mathf.Clamp(
                _currentArmor + amount,
                0f,
                maxArmor
            );

            ArmorChanged?.Invoke(CurrentArmor, MaxArmor);
        }

        private void EnsureProtectionReference()
        {
            ProtectiveEquipment found =
                GetComponent<ProtectiveEquipment>();

            if (found == _protection)
            {
                return;
            }

            if (_protection != null)
            {
                _protection.Changed -= OnProtectionChanged;
            }

            _protection = found;

            if (_protection != null)
            {
                _protection.Changed += OnProtectionChanged;
            }
        }

        private void OnProtectionChanged()
        {
            ArmorChanged?.Invoke(CurrentArmor, MaxArmor);
        }
    }
}
