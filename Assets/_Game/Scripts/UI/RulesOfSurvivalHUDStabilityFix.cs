using System.Collections.Generic;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Capa de estabilidad para el HUD reconstruido.
    /// Corrige dos conflictos de presentación detectados durante las pruebas:
    /// 1) el panel de loot de una caja de jugador muerto debe usar SIEMPRE el
    ///    PlayerInteractor del jugador local y no el de un bot;
    /// 2) los colores de los slots de armas deben tener un único estado final
    ///    por frame para evitar el parpadeo producido por varios presentadores.
    /// </summary>
    [DefaultExecutionOrder(2200)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDStabilityFix : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";

        private static readonly Color SlotNormal =
            new Color(0.08f, 0.095f, 0.105f, 0.88f);

        private static readonly Color SlotSelected =
            new Color(0.16f, 0.17f, 0.17f, 0.94f);

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.06f, 1f);

        private PlayerInputReader _localInput;
        private PlayerInteractor _interactor;
        private InventoryComponent _inventory;
        private WeaponEquipmentController _weapons;
        private DeathLootContainer _activeDeathContainer;
        private int _selectedLootIndex;
        private float _nextResolveTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDStabilityFix>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_StabilityFix")
                .AddComponent<RulesOfSurvivalHUDStabilityFix>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.25f;
                ResolveLocalPlayer();
            }

            StabilizeWeaponSlots();
            UpdateDeathLootPanel();
        }

        private void ResolveLocalPlayer()
        {
            if (!IsValidLocalInput(_localInput))
            {
                _localInput = FindLocalPlayerInput();
                _interactor = null;
                _inventory = null;
                _weapons = null;
                _activeDeathContainer = null;
                _selectedLootIndex = 0;
            }

            if (_localInput == null)
            {
                return;
            }

            GameObject player = _localInput.gameObject;
            _interactor ??= player.GetComponent<PlayerInteractor>();
            _inventory ??= player.GetComponent<InventoryComponent>();
            _weapons ??= player.GetComponent<WeaponEquipmentController>();
        }

        // -----------------------------------------------------------------
        // Slots de armas
        // -----------------------------------------------------------------

        private void StabilizeWeaponSlots()
        {
            if (_weapons == null)
            {
                return;
            }

            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform status = hud.transform.Find("Canvas/PlayerStatusFidelity");
            if (status == null)
            {
                return;
            }

            for (int slot = 1; slot <= 4; slot++)
            {
                Transform root = status.Find($"WeaponSlots/Slot_{slot}");
                if (root == null)
                {
                    continue;
                }

                bool selected = slot <= 3 &&
                                _weapons.EquippedSlot == slot &&
                                _weapons.GetWeaponForSlot(slot) != null;

                Image background = root.GetComponent<Image>();
                if (background != null)
                {
                    background.color = selected ? SlotSelected : SlotNormal;
                }

                Image selection = root.Find("Selection")?.GetComponent<Image>();
                if (selection != null)
                {
                    selection.color = selected ? Yellow : Color.clear;
                }

                Text number = root.Find("Number")?.GetComponent<Text>();
                if (number != null)
                {
                    number.color = selected
                        ? Yellow
                        : new Color(1f, 1f, 1f, 0.82f);
                }
            }
        }

        // -----------------------------------------------------------------
        // Loot de jugador muerto
        // -----------------------------------------------------------------

        private void UpdateDeathLootPanel()
        {
            if (_interactor == null || _inventory == null)
            {
                return;
            }

            DeathLootContainer nearby = FindNearbyDeathContainer();
            if (nearby != _activeDeathContainer)
            {
                _activeDeathContainer = nearby;
                _selectedLootIndex = 0;
            }

            if (_activeDeathContainer == null || _activeDeathContainer.IsEmpty)
            {
                return;
            }

            List<InventoryStack> stacks = SnapshotValidStacks(_activeDeathContainer);
            if (stacks.Count == 0)
            {
                _activeDeathContainer = null;
                return;
            }

            HandleLootSelection(stacks.Count);
            DrawDeathLoot(stacks);
            HandleLootPickup(stacks);
        }

        private DeathLootContainer FindNearbyDeathContainer()
        {
            if (_interactor.Current is DeathLootContainer current &&
                current != null &&
                !current.IsEmpty)
            {
                return current;
            }

            IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
            for (int i = 0; i < nearby.Count; i++)
            {
                if (nearby[i] is DeathLootContainer container &&
                    container != null &&
                    !container.IsEmpty)
                {
                    return container;
                }
            }

            return null;
        }

        private static List<InventoryStack> SnapshotValidStacks(
            DeathLootContainer container
        )
        {
            List<InventoryStack> result = new List<InventoryStack>();

            if (container == null || container.StoredInventory == null)
            {
                return result;
            }

            IReadOnlyList<InventoryStack> source =
                container.StoredInventory.Stacks;

            for (int i = 0; i < source.Count; i++)
            {
                InventoryStack stack = source[i];
                if (stack != null &&
                    stack.item != null &&
                    stack.amount > 0)
                {
                    result.Add(stack);
                }
            }

            return result;
        }

        private void HandleLootSelection(int count)
        {
            _selectedLootIndex = Mathf.Clamp(
                _selectedLootIndex,
                0,
                Mathf.Max(0, count - 1)
            );

            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.01f)
            {
                _selectedLootIndex = Mathf.Max(0, _selectedLootIndex - 1);
            }
            else if (scroll < -0.01f)
            {
                _selectedLootIndex = Mathf.Min(
                    count - 1,
                    _selectedLootIndex + 1
                );
            }
        }

        private void DrawDeathLoot(List<InventoryStack> stacks)
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform panel = hud.transform.Find("Canvas/NearbyLoot");
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);

            Text title = panel.Find("Title/TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = string.IsNullOrWhiteSpace(
                    _activeDeathContainer.DisplayName
                )
                    ? "LOOT"
                    : _activeDeathContainer.DisplayName.ToUpperInvariant();
            }

            const int visibleRows = 7;
            int firstVisible = Mathf.Clamp(
                _selectedLootIndex - visibleRows + 1,
                0,
                Mathf.Max(0, stacks.Count - visibleRows)
            );

            for (int rowIndex = 0; rowIndex < visibleRows; rowIndex++)
            {
                Text row = panel.Find($"LootRow_{rowIndex}")?.GetComponent<Text>();
                if (row == null)
                {
                    continue;
                }

                int stackIndex = firstVisible + rowIndex;
                if (stackIndex >= stacks.Count)
                {
                    row.text = string.Empty;
                    continue;
                }

                InventoryStack stack = stacks[stackIndex];
                bool selected = stackIndex == _selectedLootIndex;
                row.text = $"{(selected ? "▶ " : "  ")}{stack.item.displayName}  x{stack.amount}";
                row.color = selected
                    ? new Color(0.15f, 0.08f, 0.20f, 1f)
                    : Color.black;
            }

            Text toggle = panel.Find("ToggleBg/ToggleHint")?.GetComponent<Text>();
            if (toggle != null)
            {
                toggle.text = "SCROLL • F RECOGER";
            }

            Text interaction =
                hud.transform.Find("Canvas/InteractionHint")?.GetComponent<Text>();
            if (interaction != null)
            {
                interaction.text = "RUEDA: seleccionar   |   [F] recoger";
            }
        }

        private void HandleLootPickup(List<InventoryStack> stacks)
        {
            if (Keyboard.current == null ||
                !Keyboard.current.fKey.wasPressedThisFrame ||
                _selectedLootIndex < 0 ||
                _selectedLootIndex >= stacks.Count)
            {
                return;
            }

            InventoryStack selected = stacks[_selectedLootIndex];
            if (selected == null || selected.item == null)
            {
                return;
            }

            _activeDeathContainer.TryLoot(
                selected.item,
                selected.amount,
                _inventory
            );
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs =
                Resources.FindObjectsOfTypeAll<PlayerInputReader>();

            Scene activeScene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) ||
                    candidate.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null &&
                    !candidate.gameObject.name.StartsWith("Bot_"))
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static bool IsValidLocalInput(PlayerInputReader input)
        {
            return input != null &&
                   input.gameObject.scene.IsValid() &&
                   !input.UsesExternalControl;
        }
    }
}
