using System;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    public readonly struct ProtectionResolution
    {
        public readonly float AbsorbedDamage;
        public readonly float RemainingDamage;
        public readonly bool UsedHelmet;
        public readonly bool UsedVest;

        public ProtectionResolution(
            float absorbedDamage,
            float remainingDamage,
            bool usedHelmet,
            bool usedVest)
        {
            AbsorbedDamage = absorbedDamage;
            RemainingDamage = remainingDamage;
            UsedHelmet = usedHelmet;
            UsedVest = usedVest;
        }
    }

    public sealed class ProtectiveEquipment : MonoBehaviour
    {
        [Header("Helmet")]
        [SerializeField] private ProtectionLevel helmetLevel;
        [SerializeField] private float helmetDurability;

        [Header("Vest")]
        [SerializeField] private ProtectionLevel vestLevel;
        [SerializeField] private float vestDurability;

        public ProtectionLevel HelmetLevel => helmetLevel;
        public ProtectionLevel VestLevel => vestLevel;
        public float HelmetDurability => helmetDurability;
        public float VestDurability => vestDurability;
        public float MaxHelmetDurability => GetHelmetDurability(helmetLevel);
        public float MaxVestDurability => GetVestDurability(vestLevel);
        public float CurrentTotalDurability => helmetDurability + vestDurability;
        public float MaxTotalDurability => MaxHelmetDurability + MaxVestDurability;
        public bool HasProtection =>
            helmetLevel != ProtectionLevel.None ||
            vestLevel != ProtectionLevel.None;

        public event Action Changed;

        private void Awake()
        {
            helmetDurability = NormalizeDurability(
                helmetLevel,
                helmetDurability,
                true
            );

            vestDurability = NormalizeDurability(
                vestLevel,
                vestDurability,
                false
            );
        }

        public void EquipHelmet(ProtectionLevel level)
        {
            helmetLevel = level;
            helmetDurability = GetHelmetDurability(level);
            Changed?.Invoke();
        }

        public void EquipVest(ProtectionLevel level)
        {
            vestLevel = level;
            vestDurability = GetVestDurability(level);
            Changed?.Invoke();
        }

        public ProtectionResolution Resolve(
            DamageInfo damage,
            float incomingDamage)
        {
            float remaining = Mathf.Max(0f, incomingDamage);

            if (remaining <= 0f ||
                !DamageRules.CanProtectionReduce(damage.Type))
            {
                return new ProtectionResolution(
                    0f,
                    remaining,
                    false,
                    false
                );
            }

            bool useHelmet =
                damage.HitZone == HitZone.Head &&
                helmetLevel != ProtectionLevel.None &&
                helmetDurability > 0f;

            bool useVest =
                !useHelmet &&
                vestLevel != ProtectionLevel.None &&
                vestDurability > 0f &&
                (damage.Type == DamageType.Explosion ||
                 damage.HitZone == HitZone.Torso ||
                 damage.HitZone == HitZone.Arm);

            if (!useHelmet && !useVest)
            {
                return new ProtectionResolution(
                    0f,
                    remaining,
                    false,
                    false
                );
            }

            ProtectionLevel level =
                useHelmet ? helmetLevel : vestLevel;

            float availableDurability =
                useHelmet ? helmetDurability : vestDurability;

            float effectiveReduction = GetReduction(level) *
                (1f - Mathf.Clamp01(damage.ArmorPenetration));

            float absorbed = Mathf.Min(
                availableDurability,
                remaining * effectiveReduction
            );

            remaining -= absorbed;

            if (useHelmet)
            {
                helmetDurability = Mathf.Max(
                    0f,
                    helmetDurability - absorbed
                );

                if (helmetDurability <= 0f)
                {
                    helmetLevel = ProtectionLevel.None;
                }
            }
            else
            {
                vestDurability = Mathf.Max(
                    0f,
                    vestDurability - absorbed
                );

                if (vestDurability <= 0f)
                {
                    vestLevel = ProtectionLevel.None;
                }
            }

            Changed?.Invoke();

            return new ProtectionResolution(
                absorbed,
                remaining,
                useHelmet,
                useVest
            );
        }

        public static float GetReduction(ProtectionLevel level)
        {
            switch (level)
            {
                case ProtectionLevel.Level1:
                    return 0.30f;
                case ProtectionLevel.Level2:
                    return 0.40f;
                case ProtectionLevel.Level3:
                    return 0.55f;
                case ProtectionLevel.None:
                default:
                    return 0f;
            }
        }

        public static float GetHelmetDurability(ProtectionLevel level)
        {
            switch (level)
            {
                case ProtectionLevel.Level1:
                    return 80f;
                case ProtectionLevel.Level2:
                    return 150f;
                case ProtectionLevel.Level3:
                    return 230f;
                case ProtectionLevel.None:
                default:
                    return 0f;
            }
        }

        public static float GetVestDurability(ProtectionLevel level)
        {
            switch (level)
            {
                case ProtectionLevel.Level1:
                    return 100f;
                case ProtectionLevel.Level2:
                    return 180f;
                case ProtectionLevel.Level3:
                    return 260f;
                case ProtectionLevel.None:
                default:
                    return 0f;
            }
        }

        private static float NormalizeDurability(
            ProtectionLevel level,
            float durability,
            bool helmet)
        {
            if (level == ProtectionLevel.None)
            {
                return 0f;
            }

            float maximum = helmet
                ? GetHelmetDurability(level)
                : GetVestDurability(level);

            return durability <= 0f
                ? maximum
                : Mathf.Min(durability, maximum);
        }
    }
}
