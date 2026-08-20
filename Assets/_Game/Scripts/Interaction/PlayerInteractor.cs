using ROS.Game.Input;
using UnityEngine;

namespace ROS.Game.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform origin;

        [Header("Detection")]
        [SerializeField] private float distance = 3f;
        [SerializeField] private float centerHeight = 0.75f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly Collider[] _overlaps =
            new Collider[128];

        private IInteractable _current;

        public IInteractable Current =>
            IsAlive(_current)
                ? _current
                : null;

        private void Awake()
        {
            if (input == null)
            {
                input =
                    GetComponent<PlayerInputReader>();
            }

            if (origin == null)
            {
                origin = transform;
            }
        }

        private void Update()
        {
            _current =
                FindNearestInteractable();

            IInteractable interactable =
                Current;

            if (
                input == null ||
                !input.InteractPressed ||
                interactable == null
            )
            {
                return;
            }

            if (!interactable.CanInteract(gameObject))
            {
                return;
            }

            interactable.Interact(gameObject);

            _current = null;
        }

        private IInteractable FindNearestInteractable()
        {
            if (origin == null)
            {
                return null;
            }

            Vector3 center =
                origin.position +
                Vector3.up * centerHeight;

            int count =
                Physics.OverlapSphereNonAlloc(
                    center,
                    distance,
                    _overlaps,
                    mask,
                    QueryTriggerInteraction.Collide
                );

            IInteractable nearest = null;

            float nearestDistance =
                float.MaxValue;

            int nearestPriority =
                int.MinValue;

            for (int i = 0; i < count; i++)
            {
                Collider collider =
                    _overlaps[i];

                if (collider == null)
                {
                    continue;
                }

                IInteractable interactable =
                    FindInteractable(collider);

                if (!IsAlive(interactable))
                {
                    continue;
                }

                Vector3 closestPoint =
                    collider.ClosestPoint(
                        origin.position
                    );

                float sqrDistance =
                    (
                        closestPoint -
                        origin.position
                    ).sqrMagnitude;

                int priority =
                    interactable is
                        IPrioritizedInteractable
                        prioritized
                        ? prioritized
                            .InteractionPriority
                        : 0;

                if (
                    priority < nearestPriority ||
                    (
                        priority == nearestPriority &&
                        sqrDistance >= nearestDistance
                    )
                )
                {
                    continue;
                }

                nearestPriority = priority;

                nearestDistance =
                    sqrDistance;

                nearest =
                    interactable;
            }

            return nearest;
        }

        private static bool IsAlive(
            IInteractable interactable
        )
        {
            if (interactable == null)
            {
                return false;
            }

            if (
                interactable is Object unityObject
            )
            {
                return unityObject != null;
            }

            return true;
        }

        private static IInteractable
            FindInteractable(
                Collider collider
            )
        {
            if (collider == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours =
                collider
                    .GetComponentsInParent<
                        MonoBehaviour
                    >(true);

            foreach (
                MonoBehaviour behaviour
                in behaviours
            )
            {
                if (
                    behaviour != null &&
                    behaviour is
                        IInteractable interactable
                )
                {
                    return interactable;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Transform reference =
                origin != null
                    ? origin
                    : transform;

            Vector3 center =
                reference.position +
                Vector3.up * centerHeight;

            Gizmos.DrawWireSphere(
                center,
                distance
            );
        }
    }
}
