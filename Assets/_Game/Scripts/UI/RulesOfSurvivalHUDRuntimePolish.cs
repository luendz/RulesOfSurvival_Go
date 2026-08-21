using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ROS.Game.Combat;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Ajuste runtime del HUD de Rules of Survival para BattleRoyaleTest.
    /// Corrige la colocación de los bloques construidos por RulesOfSurvivalHUD,
    /// elimina overlays heredados que duplican información y reutiliza el panel
    /// amarillo para mostrar/recoger el contenido de cajas de jugador eliminado.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class RulesOfSurvivalHUDRuntimePolish : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";

        private PlayerInteractor _interactor;
        private InventoryComponent _inventory;
        private DeathLootContainer _activeDeathContainer;
        private int _selectedDeathLootIndex;
        private float _nextCleanupTime;
        private bool _layoutApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDRuntimePolish>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_Runtime_Polish")
                .AddComponent<RulesOfSurvivalHUDRuntimePolish>();
        }

        private IEnumerator Start()
        {
            // RulesOfSurvivalHUD se construye también AfterSceneLoad. Esperar dos
            // frames evita depender del orden de los RuntimeInitializeOnLoadMethod.
            yield return null;
            yield return null;

            ResolveReferences();
            ApplyLayout();
            CleanupLegacyHud();
            _layoutApplied = true;
        }

        private void Update()
        {
            ResolveReferences();

            if (!_layoutApplied && GameObject.Find("ROS_HUD_Runtime") != null)
            {
                ApplyLayout();
                _layoutApplied = true;
            }

            if (Time.unscaledTime >= _nextCleanupTime)
            {
                _nextCleanupTime = Time.unscaledTime + 0.5f;
                CleanupLegacyHud();
            }
        }

        private void LateUpdate()
        {
            ResolveReferences();
            SuppressLegacyDeathLootPanel();
            UpdateDeathLootIntegration();
        }

        private void ResolveReferences()
        {
            if (_interactor == null)
            {
                _interactor = FindFirstObjectByType<PlayerInteractor>();
            }

            if (_inventory == null && _interactor != null)
            {
                _inventory = _interactor.GetComponent<InventoryComponent>();
            }
        }

        // -----------------------------------------------------------------
        // Layout fiel a una referencia lógica 1600x900.
        // RulesOfSurvivalHUD usa pivote central para sus raíces; por eso las
        // posiciones aquí representan el centro real de cada bloque.
        // -----------------------------------------------------------------

        private static void ApplyLayout()
        {
            SetRect("Canvas/TopRightStats", new Vector2(205f, 39f), new Vector2(-117.5f, -26.5f));
            SetRect("Canvas/Waypoint", new Vector2(110f, 24f), new Vector2(0f, -14f));

            SetRect("Canvas/MinimapFrame", new Vector2(205f, 205f), new Vector2(110.5f, 112.5f));
            SetRect("Canvas/Latency", new Vector2(75f, 24f), new Vector2(145f, 24f));
            SetRect("Canvas/MinimapFrame/MapBadge", new Vector2(28f, 28f), new Vector2(-48f, -58f));

            SetRect("Canvas/Vitals", new Vector2(385f, 68f), new Vector2(0f, 42f));
            SetRect("Canvas/Weapons", new Vector2(205f, 145f), new Vector2(-110.5f, 79.5f));

            // Panel contextual amarillo: alto y estrecho como la referencia.
            SetRect("Canvas/NearbyLoot", new Vector2(190f, 420f), new Vector2(-300f, 0f));
            SetRect("Canvas/NearbyLoot/Title", new Vector2(190f, 34f), new Vector2(0f, 193f));

            for (int i = 0; i < 7; i++)
            {
                SetRect(
                    $"Canvas/NearbyLoot/LootRow_{i}",
                    new Vector2(176f, 45f),
                    new Vector2(3f, 151f - i * 49f)
                );
            }

            RectTransform toggle = FindRect("Canvas/NearbyLoot/ToggleBg");
            if (toggle != null)
            {
                toggle.sizeDelta = new Vector2(145f, 24f);
                toggle.anchoredPosition = new Vector2(-80f, 170f);
            }
        }

        private static void SetRect(string path, Vector2 size, Vector2 position)
        {
            RectTransform rect = FindRect(path);
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static RectTransform FindRect(string path)
        {
            GameObject root = GameObject.Find("ROS_HUD_Runtime");
            if (root == null)
            {
                return null;
            }

            Transform found = root.transform.Find(path);
            return found as RectTransform;
        }

        // -----------------------------------------------------------------
        // Quitar interfaces del prototipo que compiten con el nuevo HUD.
        // No se toca feedback de impactos, números de daño, kill feed ni menús.
        // -----------------------------------------------------------------

        private static void CleanupLegacyHud()
        {
            DisableAll<NearbyLootPresenter>();
            DisableAll<CombatWeaponHud>();
            DisableAll<CompassUI>();
            DisableAll<BattleRoyalePanelUI>();
            DisableAll<VitalsPanelUI>();
            DisableAll<WeaponPanelUI>();
            DisableAll<InteractionPromptUI>();
            DisableAll<SafeZoneWarningUI>();
            DisableAll<ZoneTimerUI>();
            DisableAll<WeaponSlotsPresenter>();
            DisableAll<HudPresenter>();
            DisableAll<MinimapSystem>();
            DisableAll<EquipmentStatusPresenter>();

            SetInactive("CombatCanvas");
            SetInactive("MinimapCanvas");
            SetInactive("MinimapCamera");
            SetInactive("WeaponSlotsCanvas");
            SetInactive("EquipmentStatusCanvas");

            // Conservar F5-F9 para pruebas de daño, ocultando solamente la ayuda.
            FieldInfo helpField = typeof(DamageDebugControls).GetField(
                "showHelp",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (helpField != null)
            {
                DamageDebugControls[] controls = FindObjectsByType<DamageDebugControls>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                foreach (DamageDebugControls control in controls)
                {
                    if (control != null)
                    {
                        helpField.SetValue(control, false);
                    }
                }
            }
        }

        private static void DisableAll<T>() where T : Behaviour
        {
            T[] components = FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (T component in components)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }

        private static void SetInactive(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                target.SetActive(false);
            }
        }

        // -----------------------------------------------------------------
        // Caja de jugador eliminado dentro del panel amarillo del HUD.
        // Evita el modal grande heredado y mantiene selección con rueda + F.
        // -----------------------------------------------------------------

        private void UpdateDeathLootIntegration()
        {
            DeathLootContainer nearbyContainer = FindNearbyDeathContainer();

            if (nearbyContainer != _activeDeathContainer)
            {
                _activeDeathContainer = nearbyContainer;
                _selectedDeathLootIndex = 0;
            }

            if (_activeDeathContainer == null || _inventory == null)
            {
                RestoreNearbyTitle();
                return;
            }

            List<InventoryStack> stacks = SnapshotValidStacks(_activeDeathContainer);
            if (stacks.Count == 0)
            {
                _activeDeathContainer = null;
                RestoreNearbyTitle();
                return;
            }

            _selectedDeathLootIndex = Mathf.Clamp(
                _selectedDeathLootIndex,
                0,
                stacks.Count - 1
            );

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0.01f)
                {
                    _selectedDeathLootIndex = Mathf.Max(0, _selectedDeathLootIndex - 1);
                }
                else if (scroll < -0.01f)
                {
                    _selectedDeathLootIndex = Mathf.Min(stacks.Count - 1, _selectedDeathLootIndex + 1);
                }
            }

            DrawDeathLootRows(stacks);

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                InventoryStack selected = stacks[_selectedDeathLootIndex];
                if (selected != null && selected.item != null)
                {
                    _activeDeathContainer.TryLoot(
                        selected.item,
                        selected.amount,
                        _inventory
                    );
                }
            }
        }

        private DeathLootContainer FindNearbyDeathContainer()
        {
            if (_interactor == null)
            {
                return null;
            }

            if (_interactor.Current is DeathLootContainer current && !current.IsEmpty)
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

        private static List<InventoryStack> SnapshotValidStacks(DeathLootContainer container)
        {
            List<InventoryStack> result = new List<InventoryStack>();
            if (container == null || container.StoredInventory == null)
            {
                return result;
            }

            foreach (InventoryStack stack in container.StoredInventory.Stacks)
            {
                if (stack != null && stack.item != null && stack.amount > 0)
                {
                    result.Add(stack);
                }
            }

            return result;
        }

        private void DrawDeathLootRows(List<InventoryStack> stacks)
        {
            RectTransform panel = FindRect("Canvas/NearbyLoot");
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);

            Text title = FindText("Canvas/NearbyLoot/Title/TitleText");
            if (title != null)
            {
                title.text = _activeDeathContainer != null
                    ? _activeDeathContainer.DisplayName.ToUpperInvariant()
                    : "LOOT";
            }

            for (int i = 0; i < 7; i++)
            {
                Text row = FindText($"Canvas/NearbyLoot/LootRow_{i}");
                if (row == null)
                {
                    continue;
                }

                if (i >= stacks.Count)
                {
                    row.text = string.Empty;
                    continue;
                }

                InventoryStack stack = stacks[i];
                string prefix = i == _selectedDeathLootIndex ? "▶ " : "  ";
                row.text = $"{prefix}{stack.item.displayName}  x{stack.amount}";
                row.color = i == _selectedDeathLootIndex
                    ? new Color(0.15f, 0.08f, 0.20f, 1f)
                    : Color.black;
            }

            Text toggle = FindText("Canvas/NearbyLoot/ToggleBg/ToggleHint");
            if (toggle != null)
            {
                toggle.text = "SCROLL • F RECOGER";
            }

            Text interaction = FindText("Canvas/InteractionHint");
            if (interaction != null)
            {
                interaction.text = "RUEDA: seleccionar   |   [F] recoger";
            }
        }

        private static void RestoreNearbyTitle()
        {
            Text title = FindText("Canvas/NearbyLoot/Title/TitleText");
            if (title != null)
            {
                title.text = "OBJETOS CERCANOS";
            }

            Text toggle = FindText("Canvas/NearbyLoot/ToggleBg/ToggleHint");
            if (toggle != null)
            {
                toggle.text = "SCROLL TO SELECT";
            }
        }

        private static Text FindText(string path)
        {
            GameObject root = GameObject.Find("ROS_HUD_Runtime");
            if (root == null)
            {
                return null;
            }

            Transform found = root.transform.Find(path);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static void SuppressLegacyDeathLootPanel()
        {
            DeathLootPanelPresenter[] presenters = FindObjectsByType<DeathLootPanelPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (DeathLootPanelPresenter presenter in presenters)
            {
                if (presenter == null)
                {
                    continue;
                }

                if (presenter.IsOpen)
                {
                    presenter.Close();
                }

                presenter.enabled = false;
            }
        }
    }
}
