using UnityEngine;

namespace ROS.Game.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly GameObject Instigator;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, GameObject instigator)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Instigator = instigator;
        }
    }
}
