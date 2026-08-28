using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class DamageDirectionIndicator : MonoBehaviour
    {
        private const float FlashTime = 0.9f;

        [SerializeField] private Health health;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Image[] arrows = new Image[4];

        private void Awake()
        {
            if (health == null || playerTransform == null || arrows == null || arrows.Length != 4 ||
                System.Array.Exists(arrows, arrow => arrow == null))
            {
                Debug.LogError($"[{nameof(DamageDirectionIndicator)}] Referencias incompletas en '{name}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        public void Bind(Health playerHealth, Transform player)
        {
            Unsubscribe();
            health = playerHealth;
            playerTransform = player;
            Subscribe();
        }

        private void Subscribe()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Damaged += OnDamaged;
        }

        private void Unsubscribe()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageResult result)
        {
            if (playerTransform == null)
                return;

            Vector3 hitPoint = result.Damage.Point;
            Vector3 direction = hitPoint != Vector3.zero
                ? hitPoint - playerTransform.position
                : -result.Damage.Direction;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Vector3 forward = playerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            float angle = Vector3.SignedAngle(
                forward.normalized,
                direction.normalized,
                Vector3.up
            );

            int index = angle > -45f && angle <= 45f
                ? 0
                : angle > 45f && angle <= 135f
                    ? 1
                    : angle < -45f && angle >= -135f
                        ? 3
                        : 2;

            StartCoroutine(FlashArrow(index));
        }

        private IEnumerator FlashArrow(int index)
        {
            if (arrows == null || index < 0 || index >= arrows.Length)
                yield break;

            Image arrow = arrows[index];
            if (arrow == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < FlashTime)
            {
                elapsed += Time.deltaTime;
                Color color = arrow.color;
                color.a = Mathf.SmoothStep(1f, 0f, elapsed / FlashTime);
                arrow.color = color;
                yield return null;
            }

            Color finalColor = arrow.color;
            finalColor.a = 0f;
            arrow.color = finalColor;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
    }
}
