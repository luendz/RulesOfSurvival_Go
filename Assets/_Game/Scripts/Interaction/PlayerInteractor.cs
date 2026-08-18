using ROS.Game.Input;
using UnityEngine;

namespace ROS.Game.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform origin;
        [SerializeField] private float distance = 3f;
        [SerializeField] private LayerMask mask = ~0;

        private IInteractable _current;

        /// <summary>
        /// Current interactable in front of the player. Handles Unity's special
        /// destroyed-object null semantics even though the reference is stored
        /// through an interface.
        /// </summary>
        public IInteractable Current => IsAlive(_current) ? _current : null;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (origin == null) origin = transform;
        }

        private void Update()
        {
            _current = null;

            if (Physics.Raycast(
                    origin.position + Vector3.up * 1.4f,
                    origin.forward,
                    out RaycastHit hit,
                    distance,
                    mask,
                    QueryTriggerInteraction.Collide))
            {
                _current = FindInteractable(hit.collider);
            }

            IInteractable interactable = Current;
            if (input == null || !input.InteractPressed || interactable == null)
                return;

            if (!interactable.CanInteract(gameObject))
                return;

            interactable.Interact(gameObject);

            // The interaction may destroy its GameObject (loot pickups do this).
            // Never retain the reference for the rest of the frame.
            _current = null;
        }

        private static bool IsAlive(IInteractable interactable)
        {
            if (interactable == null)
                return false;

            // Interfaces do not use UnityEngine.Object's overloaded == operator.
            // Explicitly checking the underlying Unity object prevents
            // MissingReferenceException after Destroy().
            if (interactable is Object unityObject)
                return unityObject != null;

            return true;
        }

        private static IInteractable FindInteractable(Collider collider)
        {
            if (collider == null)
                return null;

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour != null && behaviour is IInteractable interactable)
                    return interactable;
            }

            return null;
        }
    }
}
