using ROS.Game.AI;
using ROS.Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class VitalsPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;

        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthText;

        [Header("Armor")]
        [SerializeField] private Image armorFill;
        [SerializeField] private Text armorText;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
                health.ArmorChanged += OnArmorChanged;
            }
        }

        private void Start()
        {
            RefreshUI();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.ArmorChanged -= OnArmorChanged;
            }
        }

        private void EnsureReferences()
        {
            if (health == null)
            {
                health = BattleRoyaleBotController.FindLocalPlayerHealth();
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            UpdateHealth(current, max);
        }

        private void OnArmorChanged(float current, float max)
        {
            UpdateArmor(current, max);
        }

        private void RefreshUI()
        {
            if (health == null)
                return;

            UpdateHealth(
                health.CurrentHealth,
                health.MaxHealth
            );

            UpdateArmor(
                health.CurrentArmor,
                health.MaxArmor
            );
        }

        private void UpdateHealth(float current, float max)
        {
            float normalized =
                max > 0f
                    ? Mathf.Clamp01(current / max)
                    : 0f;

            UpdateBar(
                healthFill,
                normalized
            );

            if (healthText != null)
            {
                healthText.text =
                    $"VIDA {Mathf.CeilToInt(current)}";
            }
        }

        private void UpdateArmor(float current, float max)
        {
            float normalized =
                max > 0f
                    ? Mathf.Clamp01(current / max)
                    : 0f;

            UpdateBar(
                armorFill,
                normalized
            );

            if (armorText != null)
            {
                armorText.text =
                    $"ARMADURA {Mathf.CeilToInt(current)}";
            }
        }

        private void UpdateBar(
            Image fillImage,
            float normalized
        )
        {
            if (fillImage == null)
                return;

            RectTransform rect =
                fillImage.rectTransform;

            Vector2 anchorMax =
                rect.anchorMax;

            anchorMax.x = normalized;

            rect.anchorMax = anchorMax;

            fillImage.enabled =
                normalized > 0.001f;
        }
    }
}
