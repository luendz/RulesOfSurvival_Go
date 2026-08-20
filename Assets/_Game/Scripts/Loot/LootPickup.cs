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

        private GameObject _fallbackVisual;

        private GameObject _temporarilyBlockedCollector;

        private float _collectableAt;

        public InventoryItemDefinition Item => item;
        public int Amount => amount;
        public bool IsConsumed => _consumed;
        public bool IsUsingFallbackVisual => _fallbackVisual != null;
        public GameObject RuntimeVisual => _runtimeVisual;

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
            int quantity,
            GameObject temporarilyBlockedCollector = null,
            float pickupDelay = 0f
        )
        {
            item = definition;

            amount =
                Mathf.Max(
                    1,
                    quantity
                );

            _consumed = false;
            _temporarilyBlockedCollector =
                temporarilyBlockedCollector;
            _collectableAt =
                Time.time + Mathf.Max(0f, pickupDelay);

            RefreshVisual();
        }

        public static LootPickup SpawnRuntime(
            InventoryItemDefinition definition,
            int quantity,
            Vector3 position,
            GameObject temporarilyBlockedCollector = null,
            float pickupDelay = 0.75f)
        {
            if (definition == null || quantity <= 0)
            {
                return null;
            }

            GameObject pickupObject =
                new GameObject();

            pickupObject.name = $"Loot_{definition.displayName}";
            pickupObject.transform.position = position;

            BoxCollider interactionCollider =
                pickupObject.AddComponent<BoxCollider>();

            interactionCollider.center = Vector3.up * 0.25f;
            interactionCollider.size =
                new Vector3(0.8f, 0.6f, 0.8f);

            LootPickup pickup =
                pickupObject.AddComponent<LootPickup>();

            pickup.Configure(
                definition,
                quantity,
                temporarilyBlockedCollector,
                pickupDelay
            );

            return pickup;
        }

        public bool CanInteract(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                item == null ||
                interactor == null ||
                IsTemporarilyBlocked(interactor)
            )
            {
                return false;
            }

            if (item.IsEquippable)
            {
                PlayerLootEquipment equipment =
                    interactor.GetComponent<PlayerLootEquipment>();

                return equipment != null &&
                       equipment.CanEquip(item);
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            return inventory.GetMaxAddableAmount(item) > 0;
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

            return inventory.GetMaxAddableAmount(item) <= 0;
        }

        public void Interact(
            GameObject interactor
        )
        {
            TryCollect(interactor);
        }

        public bool TryCollect(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return false;
            }

            if (item.IsEquippable)
            {
                PlayerLootEquipment equipment =
                    interactor.GetComponent<PlayerLootEquipment>();

                if (equipment == null ||
                    !equipment.TryEquip(item, out InventoryItemDefinition replaced))
                {
                    return false;
                }

                if (replaced != null && replaced != item)
                {
                    Vector3 dropPosition =
                        interactor.transform.position +
                        interactor.transform.forward * 1.1f +
                        Vector3.up * 0.25f;

                    SpawnRuntime(
                        replaced,
                        1,
                        dropPosition,
                        interactor
                    );
                }

                Consume();
                return true;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            int collectedAmount =
                Mathf.Min(
                    amount,
                    inventory.GetMaxAddableAmount(item)
                );

            if (collectedAmount <= 0 ||
                !inventory.Add(item, collectedAmount))
            {
                return false;
            }

            amount -= collectedAmount;

            if (amount <= 0)
            {
                Consume();
            }

            return true;
        }

        private bool IsTemporarilyBlocked(GameObject interactor)
        {
            if (_temporarilyBlockedCollector == null ||
                Time.time >= _collectableAt)
            {
                _temporarilyBlockedCollector = null;
                return false;
            }

            return interactor == _temporarilyBlockedCollector;
        }

        private void Consume()
        {
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

            DestroyVisual(ref _runtimeVisual);
            DestroyVisual(ref _fallbackVisual);

            if (
                item == null ||
                visualRoot == null
            )
            {
                return;
            }

            if (item.worldModel == null)
            {
                CreateFallbackVisual();
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

        private void CreateFallbackVisual()
        {
            _fallbackVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            _fallbackVisual.name =
                "Visual_Provisional_Sin_Modelo";

            Transform fallbackTransform =
                _fallbackVisual.transform;

            fallbackTransform.SetParent(
                visualRoot,
                false
            );

            fallbackTransform.localPosition =
                Vector3.up * 0.18f;
            fallbackTransform.localScale =
                Vector3.one * 0.35f;

            DisableVisualPhysics(
                _fallbackVisual
            );
        }

        private static void DestroyVisual(
            ref GameObject visual
        )
        {
            if (visual == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(visual);
            }
            else
            {
                DestroyImmediate(visual);
            }

            visual = null;
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
