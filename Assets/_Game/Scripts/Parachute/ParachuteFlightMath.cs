using UnityEngine;

namespace ROS.Game.Parachute
{
    public static class ParachuteFlightMath
    {
        public static bool ShouldAutoDeploy(
            float groundClearance,
            float deployHeight
        )
        {
            return groundClearance >= 0f &&
                   groundClearance <= Mathf.Max(1f, deployHeight);
        }

        public static bool CanJumpFromPlane(
            float flightProgress,
            float minimumProgress,
            float maximumProgress
        )
        {
            float min = Mathf.Clamp01(minimumProgress);
            float max = Mathf.Clamp(maximumProgress, min, 1f);
            return flightProgress >= min && flightProgress <= max;
        }
    }
}
