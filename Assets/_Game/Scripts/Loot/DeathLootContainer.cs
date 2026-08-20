using ROS.Game.Combat;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class DeathLootContainer :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField]
        private InventoryComponent inventory;

        public InventoryComponent StoredInventory =>
            inventory;

        public int ItemCount
        {
            get
            {
                EnsureInventory();

                int count = 0;

                foreach (
                    InventoryStack stack
                    in inventory.Stacks
                )
                {
                    if (stack != null)
                    {
                        count += stack.amount;
                    }
                }

                return count;
            }
        }

        public string InteractionLabel =>
            $"Saquear caja ({ItemCount})";

        private void Awake()
        {
            EnsureInventory();
        }

        public static DeathLootContainer Create(
            Vector3 position,
            InventoryComponent source
        )
        {
            GameObject containerObject =
                new GameObject("Caja_Loot_Jugador");

            containerObject.transform.position =
                position + Vector3.up * 0.3f;

            BoxCollider collider =
                containerObject.AddComponent<BoxCollider>();

            collider.size =
                new Vector3(0.9f, 0.6f, 1.1f);

            DeathLootContainer container =
                containerObject.AddComponent<DeathLootContainer>();

            container.CreatePrototypeVisual();
            container.InitializeFrom(source);

            return container;
        }

        public bool InitializeFrom(
            InventoryComponent source
        )
        {
            EnsureInventory();
            inventory.SetCapacity(float.MaxValue);

            return
                source == null ||
                source.Stacks.Count == 0 ||
                source.TransferAllTo(inventory);
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null || ItemCount <= 0)
            {
                return false;
            }

            Health health =
                interactor.GetComponent<Health>();

            if (health != null && !health.IsAlive)
            {
                return false;
            }

            InventoryComponent target =
                interactor.GetComponent<InventoryComponent>();

            return
                target != null &&
                target.CanReceiveAll(inventory);
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            InventoryComponent target =
                interactor.GetComponent<InventoryComponent>();

            if (!inventory.TransferAllTo(target))
            {
                return;
            }

            Destroy(gameObject);
        }

        private void EnsureInventory()
        {
            if (inventory == null)
            {
                inventory =
                    GetComponent<InventoryComponent>();
            }
        }

        private void CreatePrototypeVisual()
        {
            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            visual.name =
                "CajaLoot_VisualProvisional";

            visual.transform.SetParent(
                transform,
                false
            );

            visual.transform.localScale =
                new Vector3(0.9f, 0.55f, 1.1f);

            Collider visualCollider =
                visual.GetComponent<Collider>();

            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }
        }
    }
}
