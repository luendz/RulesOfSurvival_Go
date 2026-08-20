using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class SafeZoneWarningUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private Health playerHealth;
        [SerializeField] private BattleRoyaleManager battleRoyale;

        [Header("UI")]
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private Text warningText;

        [Header("Pulse")]
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float minAlpha = 0.55f;
        [SerializeField] private float maxAlpha = 1f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            EnsureReferences();

            if (warningPanel != null)
            {
                _canvasGroup =
                    warningPanel.GetComponent<CanvasGroup>();

                if (_canvasGroup == null)
                {
                    _canvasGroup =
                        warningPanel.AddComponent<CanvasGroup>();
                }
            }

            SetWarningVisible(false);
        }

        private void Update()
        {
            EnsureReferences();

            if (safeZone == null || playerHealth == null)
            {
                SetWarningVisible(false);
                return;
            }

            if (battleRoyale != null &&
                battleRoyale.State != MatchState.Playing &&
                battleRoyale.State != MatchState.FinalCircle)
            {
                SetWarningVisible(false);
                return;
            }

            if (!playerHealth.IsAlive)
            {
                SetWarningVisible(false);
                return;
            }

            bool isOutside =
                safeZone.IsOutside(
                    playerHealth.transform.position
                );

            SetWarningVisible(isOutside);

            if (!isOutside)
                return;

            UpdateWarningText();
            UpdatePulse();
        }

        private void EnsureReferences()
        {
            if (safeZone == null)
            {
                safeZone =
                    FindFirstObjectByType<SafeZoneController>();
            }

            if (playerHealth == null)
            {
                playerHealth =
                    BattleRoyaleBotController.FindLocalPlayerHealth();
            }

            if (battleRoyale == null)
            {
                battleRoyale =
                    FindFirstObjectByType<BattleRoyaleManager>();
            }
        }

        private void UpdateWarningText()
        {
            if (warningText == null)
                return;

            float damage =
                safeZone.CurrentDamagePerSecond;

            if (damage > 0f)
            {
                warningText.text =
                    $"FUERA DE LA ZONA SEGURA\nDAÑO {damage:0.#}/s";
            }
            else
            {
                warningText.text =
                    "FUERA DE LA ZONA SEGURA";
            }
        }

        private void UpdatePulse()
        {
            if (_canvasGroup == null)
                return;

            float t =
                (Mathf.Sin(
                    Time.time * pulseSpeed
                ) + 1f) * 0.5f;

            _canvasGroup.alpha =
                Mathf.Lerp(
                    minAlpha,
                    maxAlpha,
                    t
                );
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningPanel != null &&
                warningPanel.activeSelf != visible)
            {
                warningPanel.SetActive(visible);
            }
        }
    }
}
