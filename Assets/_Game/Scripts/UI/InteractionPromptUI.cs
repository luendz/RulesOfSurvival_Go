using ROS.Game.Interaction;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class InteractionPromptUI :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerInteractor interactor;

        [Header("UI")]
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Text promptText;

        [SerializeField]
        private Text statusText;

        [Header("Colors")]
        [SerializeField]
        private Color availableColor =
            Color.white;

        [SerializeField]
        private Color unavailableColor =
            new Color(
                1f,
                0.3f,
                0.3f,
                1f
            );

        private void Awake()
        {
            EnsureReferences();

            SetVisible(false);
        }

        private void Update()
        {
            EnsureReferences();
            RefreshUI();
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void EnsureReferences()
        {
            if (interactor == null)
            {
                interactor =
                    FindFirstObjectByType<
                        PlayerInteractor
                    >();
            }
        }

        private void RefreshUI()
        {
            if (interactor == null)
            {
                SetVisible(false);
                return;
            }

            IInteractable current =
                interactor.Current;

            if (current == null)
            {
                SetVisible(false);
                return;
            }

            bool canInteract =
                current.CanInteract(
                    interactor.gameObject
                );

            SetVisible(true);

            if (promptText != null)
            {
                promptText.text =
                    $"[F] {current.InteractionLabel}";

                promptText.color =
                    canInteract
                        ? availableColor
                        : unavailableColor;
            }

            if (statusText != null)
            {
                statusText.text =
                    GetUnavailableMessage(
                        current,
                        canInteract
                    );

                statusText.color =
                    unavailableColor;
            }
        }

        private string GetUnavailableMessage(
            IInteractable current,
            bool canInteract
        )
        {
            if (canInteract)
            {
                return string.Empty;
            }

            if (
                current is LootPickup loot &&
                loot.IsBlockedByInventoryCapacity(
                    interactor.gameObject
                )
            )
            {
                return "INVENTARIO LLENO";
            }

            if (
                current is LootPickup levelLoot &&
                levelLoot.IsBlockedByEquipmentLevel(
                    interactor.gameObject
                )
            )
            {
                return "NIVEL IGUAL O INFERIOR";
            }

            return "NO DISPONIBLE";
        }

        private void SetVisible(
            bool visible
        )
        {
            if (
                panel != null &&
                panel.activeSelf != visible
            )
            {
                panel.SetActive(visible);
            }
        }
    }
}
