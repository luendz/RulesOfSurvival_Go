using System.Collections;
using System.Reflection;
using ROS.Game.Combat;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Conserva solo la limpieza del HUD heredado. El layout ya no se modifica
    /// por codigo: posiciones, tamanos y jerarquia pertenecen al prefab editable.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class RulesOfSurvivalHUDRuntimePolish : MonoBehaviour
    {
        private float _nextCleanupTime;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            CleanupLegacyHud();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCleanupTime)
                return;

            _nextCleanupTime = Time.unscaledTime + 0.5f;
            CleanupLegacyHud();
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

            if (helpField == null)
                return;

            DamageDebugControls[] controls = FindObjectsByType<DamageDebugControls>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (DamageDebugControls control in controls)
            {
                if (control != null)
                    helpField.SetValue(control, false);
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
                    component.enabled = false;
            }
        }

        private static void SetInactive(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
                target.SetActive(false);
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
                    continue;

                if (presenter.IsOpen)
                    presenter.Close();

                presenter.enabled = false;
            }
        }
    }
}
