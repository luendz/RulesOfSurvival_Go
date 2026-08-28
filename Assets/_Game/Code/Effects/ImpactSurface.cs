using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Effects
{
    [DisallowMultipleComponent]
    public sealed class ImpactSurface : MonoBehaviour
    {
        [SerializeField] private ImpactSurfaceType surfaceType = ImpactSurfaceType.Default;

        public ImpactSurfaceType SurfaceType => surfaceType;

        public void Configure(ImpactSurfaceType type)
        {
            surfaceType = type;
        }
    }
}
