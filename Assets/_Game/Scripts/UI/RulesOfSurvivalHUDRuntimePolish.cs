using System.Collections;
using System.Reflection;
using ROS.Game.Combat;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Ajustes visuales y limpieza de HUD heredado.
    /// Este componente ya NO controla el loot de muertos: esa responsabilidad
    /// pertenece exclusivamente a RulesOfSurvivalHUDNearbyLootPresenter.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class RulesOfSurvivalHUDRuntimePolish : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";

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
            yield return null;
            yield return null;

            ApplyLayout();
            CleanupLegacyHud();
            _layoutApplied = true;
        }

        private void Update()
        {
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

        private static void ApplyLayout()
        {
            SetRect("Canvas/TopRightStats", new Vector2(205f, 39f), new Vector2(-117.5f, -26.5f));
            SetRect("Canvas/Waypoint", new Vector2(110f, 24f), new Vector2(0f, -14f));

            SetRect("Canvas/MinimapFrame", new Vector2(205f, 205f), new Vector2(110.5f, 112.5f));
            SetRect("Canvas/Latency", new Vector2(75f, 24f), new Vector2(145f, 24f));
            SetRect("Canvas/MinimapFrame/MapBadge", new Vector2(28f, 28f), new Vector2(-48f, -58f));

            SetRect("Canvas/Vitals", new Vector2(385f, 68f), new Vector2(0f, 42f));
            SetRect("Canvas/Weapons", new Vector2(205f, 145f), new Vector2(-110.5f, 79.5f));

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

            return root.transform.Find(path) as RectTransform;
        }

        private static void CleanupLegacyHud()
        {
            DisableAll<NearbyLootPresenter>();
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

            SuppressLegacyDeathLootPanel();

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
