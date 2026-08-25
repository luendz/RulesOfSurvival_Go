using System;
using UnityEngine;

namespace ROS.Game.Animation
{
    /// <summary>
    /// Puente de compatibilidad para escenas/prefabs antiguos que todavía
    /// contienen PlayerAnimatorDriver. El control continuo del Animator vive
    /// exclusivamente en PlayerAnimationCoordinator.
    /// </summary>
    [Obsolete("PlayerAnimatorDriver es legacy. Use PlayerAnimationCoordinator.")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        private void Awake()
        {
            EnsureCoordinator();
            enabled = false;
        }

        private void OnEnable()
        {
            EnsureCoordinator();
            enabled = false;
        }

        private void Reset()
        {
            EnsureCoordinator();
        }

        private void EnsureCoordinator()
        {
            PlayerAnimationCoordinator coordinator =
                GetComponent<PlayerAnimationCoordinator>() ??
                GetComponentInParent<PlayerAnimationCoordinator>();

            if (coordinator != null)
            {
                if (!coordinator.enabled)
                    coordinator.enabled = true;
                return;
            }

            Transform owner = transform;
            while (owner.parent != null)
                owner = owner.parent;

            GameObject target = owner.gameObject;
            coordinator = target.GetComponent<PlayerAnimationCoordinator>();
            if (coordinator == null)
                coordinator = target.AddComponent<PlayerAnimationCoordinator>();

            coordinator.enabled = true;
        }
    }
}
