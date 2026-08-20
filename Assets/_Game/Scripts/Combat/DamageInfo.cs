using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly GameObject Instigator;
        public readonly DamageType Type;
        public readonly HitZone HitZone;

        public DamageInfo(
            float amount,
            Vector3 point,
            Vector3 direction,
            GameObject instigator,
            DamageType type = DamageType.Generic,
            HitZone hitZone = HitZone.Torso)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Instigator = instigator;
            Type = type;
            HitZone = hitZone;
        }

        public DamageInfo WithHitZone(HitZone hitZone)
        {
            return new DamageInfo(
                Amount,
                Point,
                Direction,
                Instigator,
                Type,
                hitZone
            );
        }
    }
}
