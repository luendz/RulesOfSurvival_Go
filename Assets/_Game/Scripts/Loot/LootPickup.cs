using ROS.Game.Interaction;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    public sealed class LootPickup :
        MonoBehaviour,
        IInteractable
    {
        [Header("Item")]
        [SerializeField]
        private InventoryItemDefinition item;

        [SerializeField]
        private int amount = 1;

        [Header("Visual")]
        [SerializeField]
        private Transform visualRoot;

        private bool _consumed;

        private GameObject _runtimeVisual;

        public string InteractionLabel =>
            item == null
                ? "Recoger"
                : $"Recoger {item.displayName} x{amount}";

        private void Awake()
        {
            EnsureVisualRoot();
            RefreshVisual();
        }

        public void Configure(
            InventoryItemDefinition definition,
            int quantity
        )
        {
            item = definition;

            amount =
                Mathf.Max(
                    1,
                    quantity
                );

            _consumed = false;

            RefreshVisual();
        }

        public bool CanInteract(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                item == null ||
                interactor == null
            )
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            return inventory.CanAdd(
                item,
                amount
            );
        }

        public bool IsBlockedByInventoryCapacity(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                item == null ||
                interactor == null
            )
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            return !inventory.CanAdd(
                item,
                amount
            );
        }

        public void Interact(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                interactor == null ||
                item == null
            )
            {
                return;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (
                inventory == null ||
                !inventory.Add(
                    item,
                    amount
                )
            )
            {
                return;
            }

            _consumed = true;

            Collider[] colliders =
                GetComponentsInChildren<
                    Collider
                >(true);

            foreach (
                Collider col
                in colliders
            )
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            enabled = false;

            Destroy(gameObject);
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            Transform existing =
                transform.Find(
                    "VisualRoot"
                );

            if (existing != null)
            {
                visualRoot = existing;
                return;
            }

            GameObject root =
                new GameObject(
                    "VisualRoot"
                );

            visualRoot =
                root.transform;

            visualRoot.SetParent(
                transform,
                false
            );
        }

        private void RefreshVisual()
        {
            EnsureVisualRoot();

            if (_runtimeVisual != null)
            {
                Destroy(
                    _runtimeVisual
                );

                _runtimeVisual = null;
            }

            if (
                item == null ||
                item.worldModel == null ||
                visualRoot == null
            )
            {
                return;
            }

            _runtimeVisual =
                Instantiate(
                    item.worldModel,
                    visualRoot
                );

            Transform modelTransform =
                _runtimeVisual.transform;

            modelTransform.localPosition =
                item.worldOffset;

            modelTransform.localRotation =
                Quaternion.Euler(
                    item.worldEulerAngles
                );

            modelTransform.localScale =
                item.worldScale;

            DisableVisualPhysics(
                _runtimeVisual
            );
        }

        private static void DisableVisualPhysics(
            GameObject visual
        )
        {
            Collider[] colliders =
                visual.GetComponentsInChildren<
                    Collider
                >(true);

            foreach (
                Collider collider
                in colliders
            )
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            Rigidbody[] rigidbodies =
                visual.GetComponentsInChildren<
                    Rigidbody
                >(true);

            foreach (
                Rigidbody body
                in rigidbodies
            )
            {
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }
    }
}