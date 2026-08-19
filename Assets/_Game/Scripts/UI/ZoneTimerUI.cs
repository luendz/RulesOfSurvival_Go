using ROS.Game.BattleRoyale;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class ZoneTimerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private SafeZoneController safeZone;

        [SerializeField]
        private Text timerText;

        private void Awake()
        {
            EnsureReferences();
            RefreshUI();
        }

        private void Update()
        {
            EnsureReferences();
            RefreshUI();
        }

        private void EnsureReferences()
        {
            if (safeZone == null)
            {
                safeZone =
                    FindFirstObjectByType<
                        SafeZoneController
                    >();
            }
        }

        private void RefreshUI()
        {
            if (timerText == null)
                return;

            if (safeZone == null)
            {
                timerText.text =
                    "ZONA --:--";

                return;
            }

            if (
                safeZone.CurrentPhase < 0
            )
            {
                timerText.text =
                    "ZONA --:--";

                return;
            }

            int seconds =
                Mathf.CeilToInt(
                    Mathf.Max(
                        0f,
                        safeZone
                            .PhaseTimeRemaining
                    )
                );

            string formattedTime =
                FormatTime(seconds);

            if (safeZone.IsShrinking)
            {
                timerText.text =
                    $"CERRANDO {formattedTime}";
            }
            else
            {
                timerText.text =
                    $"ZONA EN {formattedTime}";
            }
        }

        private static string FormatTime(
            int totalSeconds
        )
        {
            int minutes =
                totalSeconds / 60;

            int seconds =
                totalSeconds % 60;

            return
                $"{minutes:00}:{seconds:00}";
        }
    }
}