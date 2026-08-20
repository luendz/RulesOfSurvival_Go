using ROS.Game.Core;

namespace ROS.Game.Combat
{
    public readonly struct DamageResult
    {
        public readonly DamageInfo Damage;
        public readonly float HitZoneMultiplier;
        public readonly float DamageAfterMultiplier;
        public readonly float AbsorbedDamage;
        public readonly float HealthDamage;
        public readonly bool WasFatal;

        public DamageResult(
            DamageInfo damage,
            float hitZoneMultiplier,
            float damageAfterMultiplier,
            float absorbedDamage,
            float healthDamage,
            bool wasFatal)
        {
            Damage = damage;
            HitZoneMultiplier = hitZoneMultiplier;
            DamageAfterMultiplier = damageAfterMultiplier;
            AbsorbedDamage = absorbedDamage;
            HealthDamage = healthDamage;
            WasFatal = wasFatal;
        }

        public bool IsHeadshot =>
            Damage.Type == DamageType.Firearm &&
            Damage.HitZone == HitZone.Head;
    }
}
