using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Loot
{
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class DeathLootContainer :
        MonoBehaviour,
        IInteractable,
        IPrioritizedInteractable
    {
        private const string VisualResourcePath =
            "DeathLootContainerVisual";

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

        /// <summary>
        /// Cantidad general de objetos/tipos presentes en la caja.
        /// Ejemplo: Rifle x1 + Munición Rifle x120 = 2 objetos de loot.
        /// No suma cada bala como un objeto independiente ni duplica una misma
        /// definición si internamente ocupa más de una pila por maxStack.
        /// </summary>
        public int ItemCount
        {
            get
            {
                EnsureInventory();

                HashSet<string> uniqueItems =
                    new HashSet<string>();

                foreach (
                    InventoryStack stack
                    in inventory.Stacks
                )
                {
                    if (stack == null ||
                        stack.item == null ||
                        stack.amount <= 0)
                    {
                        continue;
                    }

                    uniqueItems.Add(
                        GetGeneralItemKey(stack.item)
                    );
                }

                return uniqueItems.Count;
            }
        }

        /// <summary>
        /// Total físico de unidades sumadas dentro de todas las pilas.
        /// Se mantiene separado por si alguna lógica futura necesita ese dato.
        /// </summary>
        public int TotalUnitCount
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
                    if (stack != null &&
                        stack.item != null &&
                        stack.amount > 0)
                    {
                        count += stack.amount;
                    }
                }

                return count;
            }
        }

        public string InteractionLabel =>
            $"Abrir {DisplayName} ({ItemCount})";

        public int InteractionPriority => 10;

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

            container.CreateVisual();
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
            if (interactor == null)
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

            RulesOfSurvivalHUDNearbyLootPresenter.OpenOrCreate(
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

        private static string GetGeneralItemKey(
            InventoryItemDefinition item
        )
        {
            if (item == null)
            {
                return "null";
            }

            if (item.itemType == ItemType.Weapon &&
                item.weaponDefinition != null)
            {
                string weaponId =
                    item.weaponDefinition.weaponId;

                return !string.IsNullOrWhiteSpace(weaponId)
                    ? $"weapon:{weaponId}"
                    : $"weapon:{item.weaponDefinition.GetInstanceID()}";
            }

            if (item.itemType == ItemType.Ammo)
            {
                return $"ammo:{item.ammoType}";
            }

            if (!string.IsNullOrWhiteSpace(item.itemId))
            {
                return $"item:{item.itemId}";
            }

            return $"{item.itemType}:{item.displayName}";
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

        private void CreateVisual()
        {
            DeathLootVisualDefinition definition =
                Resources.Load<DeathLootVisualDefinition>(
                    VisualResourcePath
                );

            if (
                definition == null ||
                definition.visualPrefab == null
            )
            {
                CreatePrototypeVisual();
                return;
            }

            GameObject visual = Instantiate(
                definition.visualPrefab,
                transform
            );

            visual.name = "CajaLoot_Visual3D";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            Transform modelRoot =
                FitModelToContainer(visual);

            DeathLootHalo halo =
                visual.GetComponent<DeathLootHalo>();

            if (halo != null)
            {
                halo.ConfigureFloatingModel(modelRoot);
            }
        }

        private static Transform FitModelToContainer(
            GameObject visual
        )
        {
            Transform modelRoot =
                visual.transform.Find("Modelo_Caja_Muerte");

            if (modelRoot == null)
            {
                return null;
            }

            Renderer[] modelRenderers =
                modelRoot.GetComponentsInChildren<Renderer>(true);

            if (modelRenderers.Length == 0)
            {
                return modelRoot;
            }

            Bounds bounds = modelRenderers[0].bounds;

            for (int i = 1; i < modelRenderers.Length; i++)
            {
                bounds.Encapsulate(modelRenderers[i].bounds);
            }

            if (
                bounds.size.x <= 0.001f ||
                bounds.size.y <= 0.001f ||
                bounds.size.z <= 0.001f
            )
            {
                return modelRoot;
            }

            Vector3 targetSize =
                new Vector3(0.425f, 0.275f, 0.5f);

            float scaleFactor = Mathf.Min(
                targetSize.x / bounds.size.x,
                targetSize.y / bounds.size.y,
                targetSize.z / bounds.size.z
            );

            modelRoot.localScale *= scaleFactor;

            bounds = modelRenderers[0].bounds;

            for (int i = 1; i < modelRenderers.Length; i++)
            {
                bounds.Encapsulate(modelRenderers[i].bounds);
            }

            Vector3 targetCenter = visual.transform.position;
            float targetBottom = visual.transform.position.y - 0.3f;

            modelRoot.position += new Vector3(
                targetCenter.x - bounds.center.x,
                targetBottom - bounds.min.y,
                targetCenter.z - bounds.center.z
            );

            return modelRoot;
        }
    }
}
