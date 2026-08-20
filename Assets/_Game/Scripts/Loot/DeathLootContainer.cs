using ROS.Game.Combat;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.UI;
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

        [SerializeField]
        private string sourcePlayerName;

        public InventoryComponent StoredInventory =>
            inventory;

        public string SourcePlayerName =>
            sourcePlayerName;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(
                sourcePlayerName
            )
                ? "Caja de jugador eliminado"
                : $"Caja de {sourcePlayerName}";

        public bool IsEmpty => ItemCount <= 0;

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
            $"Abrir {DisplayName} ({ItemCount})";

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
                new GameObject(
                    source == null
                        ? "Caja_Loot_Jugador"
                        : $"Caja_Loot_{source.gameObject.name}"
                );

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

            sourcePlayerName =
                source != null
                    ? source.gameObject.name
                    : string.Empty;

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

            return
                interactor.GetComponent<
                    InventoryComponent
                >() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            DeathLootPanelPresenter.OpenOrCreate(
                this,
                interactor
            );
        }

        public int TryLoot(
            InventoryItemDefinition item,
            int requestedAmount,
            InventoryComponent destination
        )
        {
            EnsureInventory();

            int transferred =
                inventory.TransferTo(
                    destination,
                    item,
                    requestedAmount
                );

            DestroyIfEmpty();

            return transferred;
        }

        public int LootAllPossible(
            InventoryComponent destination
        )
        {
            EnsureInventory();

            int transferred =
                inventory.TransferAllPossibleTo(
                    destination
                );

            DestroyIfEmpty();

            return transferred;
        }

        private void DestroyIfEmpty()
        {
            if (!IsEmpty)
            {
                return;
            }

            foreach (
                Collider collider
                in GetComponentsInChildren<Collider>(true)
            )
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
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
