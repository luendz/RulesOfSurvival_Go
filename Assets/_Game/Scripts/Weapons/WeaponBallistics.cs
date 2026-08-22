using UnityEngine;

namespace ROS.Game.Weapons
{
    /// <summary>
    /// Funciones deterministas compartidas por daño, debug y futuros proyectiles físicos.
    /// Mantiene los cálculos balísticos fuera de WeaponController.
    /// </summary>
    public static class WeaponBallistics
    {
        public const float Gravity = 9.81f;

        public static float EstimateTravelTime(float distance, float muzzleVelocity)
        {
            if (distance <= 0f)
                return 0f;

            return distance / Mathf.Max(1f, muzzleVelocity);
        }

        public static float EstimateDrop(float distance, float muzzleVelocity, float gravityScale)
        {
            if (distance <= 0f || gravityScale <= 0f)
                return 0f;

            float time = EstimateTravelTime(distance, muzzleVelocity);
            return 0.5f * Gravity * Mathf.Max(0f, gravityScale) * time * time;
        }

        public static Vector3 ApplyDropToAimDirection(
            Vector3 origin,
            Vector3 target,
            float muzzleVelocity,
            float gravityScale)
        {
            Vector3 delta = target - origin;
            float distance = delta.magnitude;

            if (distance <= 0.0001f)
                return Vector3.forward;

            float drop = EstimateDrop(distance, muzzleVelocity, gravityScale);
            Vector3 compensatedTarget = target + Vector3.up * drop;
            return (compensatedTarget - origin).normalized;
        }

        public static Vector3 EvaluatePosition(
            Vector3 origin,
            Vector3 initialVelocity,
            float gravityScale,
            float time)
        {
            Vector3 gravity = Vector3.down * Gravity * Mathf.Max(0f, gravityScale);
            return origin + initialVelocity * time + 0.5f * gravity * time * time;
        }
    }
}
