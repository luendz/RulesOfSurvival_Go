using ROS.Game.Core;

namespace ROS.Game.Combat
{
    public static class DamageRules
    {
        public const float HeadMultiplier = 2f;
        public const float TorsoMultiplier = 1f;
        public const float ArmMultiplier = 0.75f;
        public const float LegMultiplier = 0.65f;

        public static float GetHitZoneMultiplier(
            DamageType damageType,
            HitZone hitZone)
        {
            if (damageType != DamageType.Firearm)
            {
                return 1f;
            }

            switch (hitZone)
            {
                case HitZone.Head:
                    return HeadMultiplier;
                case HitZone.Arm:
                    return ArmMultiplier;
                case HitZone.Leg:
                    return LegMultiplier;
                case HitZone.None:
                case HitZone.Torso:
                default:
                    return TorsoMultiplier;
            }
        }

        public static bool CanProtectionReduce(
            DamageType damageType)
        {
            return damageType == DamageType.Generic ||
                   damageType == DamageType.Firearm ||
                   damageType == DamageType.Explosion;
        }
    }
}
