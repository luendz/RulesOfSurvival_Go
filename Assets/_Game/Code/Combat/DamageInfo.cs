using ROS.Game.Core;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly float BaseAmount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly GameObject Instigator;
        public readonly DamageType Type;
        public readonly HitZone HitZone;
        public readonly string WeaponId;
        public readonly WeaponFamily WeaponFamily;
        public readonly float Distance;
        public readonly float BulletTravelTime;
        public readonly float EstimatedBulletDrop;
        public readonly float ArmorPenetration;
        public readonly float HitZoneMultiplierOverride;

        public DamageInfo(
            float amount,
            Vector3 point,
            Vector3 direction,
            GameObject instigator,
            DamageType type = DamageType.Generic,
            HitZone hitZone = HitZone.Torso)
        {
            BaseAmount = Mathf.Max(0f, amount);
            Amount = BaseAmount;
            Point = point;
            Direction = direction;
            Instigator = instigator;
            Type = type;
            HitZone = hitZone;
            WeaponId = string.Empty;
            WeaponFamily = WeaponFamily.Melee;
            Distance = 0f;
            BulletTravelTime = 0f;
            EstimatedBulletDrop = 0f;
            ArmorPenetration = 0f;
            HitZoneMultiplierOverride = 0f;

            if (type != DamageType.Firearm || instigator == null)
                return;

            WeaponController weapon = instigator.GetComponent<WeaponController>();
            if (weapon == null)
                weapon = instigator.GetComponentInParent<WeaponController>();

            WeaponDefinition definition = weapon != null ? weapon.Definition : null;
            if (definition == null)
                return;

            WeaponMount mount = instigator.GetComponent<WeaponMount>();
            if (mount == null)
                mount = instigator.GetComponentInParent<WeaponMount>();

            Vector3 origin = mount != null
                ? mount.ShotOrigin
                : instigator.transform.position;

            float resolvedDistance = Vector3.Distance(origin, point);
            float distanceMultiplier = definition.GetDamageMultiplierAtDistance(resolvedDistance);

            Amount = BaseAmount * distanceMultiplier;
            WeaponId = definition.weaponId;
            WeaponFamily = definition.family;
            Distance = resolvedDistance;
            BulletTravelTime = WeaponBallistics.EstimateTravelTime(
                resolvedDistance,
                definition.muzzleVelocity
            );
            EstimatedBulletDrop = WeaponBallistics.EstimateDrop(
                resolvedDistance,
                definition.muzzleVelocity,
                definition.gravityScale
            );
            ArmorPenetration = Mathf.Clamp01(definition.armorPenetration);
            HitZoneMultiplierOverride = definition.GetHitZoneMultiplier(hitZone);
        }

        public DamageInfo(
            float amount,
            Vector3 point,
            Vector3 direction,
            GameObject instigator,
            WeaponDefinition definition,
            HitZone hitZone = HitZone.Torso)
        {
            BaseAmount = Mathf.Max(0f, amount);
            Amount = BaseAmount;
            Point = point;
            Direction = direction;
            Instigator = instigator;
            Type = DamageType.Generic;
            HitZone = hitZone;
            WeaponId = definition != null ? definition.weaponId : string.Empty;
            WeaponFamily = definition != null
                ? definition.family
                : WeaponFamily.Melee;
            Distance = instigator != null
                ? Vector3.Distance(instigator.transform.position, point)
                : 0f;
            BulletTravelTime = 0f;
            EstimatedBulletDrop = 0f;
            ArmorPenetration = definition != null
                ? Mathf.Clamp01(definition.armorPenetration)
                : 0f;
            HitZoneMultiplierOverride = definition != null
                ? definition.GetHitZoneMultiplier(hitZone)
                : 0f;
        }

        public DamageInfo WithHitZone(HitZone hitZone)
        {
            return new DamageInfo(
                BaseAmount,
                Point,
                Direction,
                Instigator,
                Type,
                hitZone
            );
        }
    }
}
