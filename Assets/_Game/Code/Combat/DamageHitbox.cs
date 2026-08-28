using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageHitbox : MonoBehaviour
    {
        [SerializeField] private HitZone hitZone = HitZone.Torso;
        [SerializeField] private Health owner;

        public HitZone HitZone => hitZone;
        public Health Owner
        {
            get
            {
                if (owner == null)
                {
                    owner = GetComponentInParent<Health>();
                }

                return owner;
            }
        }

        public void Configure(
            Health targetOwner,
            HitZone targetZone)
        {
            owner = targetOwner;
            hitZone = targetZone;
        }
    }
}
