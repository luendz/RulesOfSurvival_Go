using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.UI
{
    /// <summary>
    /// Ajuste final de anclajes para el bloque inferior del HUD.
    /// Mantiene vida centrada abajo y los cuatro slots en una cuadrícula
    /// 1/4 - 2/3 como en la referencia original de Rules of Survival.
    /// </summary>
    [DefaultExecutionOrder(1500)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDPlayerStatusLayout : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private float _nextApplyTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDPlayerStatusLayout>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_PlayerStatus_Layout")
                .AddComponent<RulesOfSurvivalHUDPlayerStatusLayout>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextApplyTime)
            {
                return;
            }

            _nextApplyTime = Time.unscaledTime + 0.25f;
            ApplyLayout();
        }

        private static void ApplyLayout()
        {
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

            RectTransform vitals = status.Find("PlayerVitals") as RectTransform;
            if (vitals != null)
            {
                vitals.anchorMin = new Vector2(0.5f, 0f);
                vitals.anchorMax = new Vector2(0.5f, 0f);
                vitals.pivot = new Vector2(0.5f, 0f);
                vitals.anchoredPosition = new Vector2(0f, 20f);
                vitals.sizeDelta = new Vector2(410f, 72f);
            }

            SetCentered(
                status.Find("PlayerVitals/PlayerName") as RectTransform,
                new Vector2(175f, 20f),
                new Vector2(-42f, 13f)
            );

            SetCentered(
                status.Find("PlayerVitals/HealthValue") as RectTransform,
                new Vector2(50f, 18f),
                new Vector2(176f, -6f)
            );

            SetCentered(
                status.Find("PlayerVitals/HealthIcon") as RectTransform,
                new Vector2(34f, 34f),
                new Vector2(-172f, -7f)
            );

            RectTransform weapons = status.Find("WeaponSlots") as RectTransform;
            if (weapons != null)
            {
                weapons.anchorMin = new Vector2(1f, 0f);
                weapons.anchorMax = new Vector2(1f, 0f);
                weapons.pivot = new Vector2(1f, 0f);
                weapons.anchoredPosition = new Vector2(-18f, 18f);
                weapons.sizeDelta = new Vector2(222f, 118f);
            }

            ApplySlot(status, 1, new Vector2(0f, 59f));
            ApplySlot(status, 4, new Vector2(111f, 59f));
            ApplySlot(status, 2, new Vector2(0f, 0f));
            ApplySlot(status, 3, new Vector2(111f, 0f));
        }

        private static void ApplySlot(
            Transform status,
            int slot,
            Vector2 position
        )
        {
            RectTransform root = status.Find($"WeaponSlots/Slot_{slot}")
                as RectTransform;

            if (root == null)
            {
                return;
            }

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(108f, 56f);

            SetCentered(
                root.Find("Number") as RectTransform,
                new Vector2(18f, 18f),
                new Vector2(-44f, 18f)
            );

            SetCentered(
                root.Find("Name") as RectTransform,
                new Vector2(78f, 22f),
                new Vector2(2f, 6f)
            );

            SetCentered(
                root.Find("Ammo") as RectTransform,
                new Vector2(74f, 20f),
                new Vector2(20f, -17f)
            );
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 size,
            Vector2 position
        )
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
