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
            ResolvePhysicalView();
            ResolveGameplayReferences();
        }

        private void OnEnable()
        {
            ResolveGameplayReferences();
            Subscribe();
        }

        public void Bind(Health playerHealth, Transform player)
        {
            Unsubscribe();
            health = playerHealth;
            playerTransform = player;
            ResolvePhysicalView();
            Subscribe();
        }

        private void ResolveGameplayReferences()
        {
            if (playerTransform == null)
            {
                PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
                if (input != null)
                {
                    playerTransform = input.transform;
                    if (health == null)
                        health = input.GetComponent<Health>();
                }
            }
        }

        private void ResolvePhysicalView()
        {
            if (arrows == null || arrows.Length != 4)
                arrows = new Image[4];

            arrows[0] ??= FindNamed<Image>("DamageArrow_Front");
            arrows[1] ??= FindNamed<Image>("DamageArrow_Right");
            arrows[2] ??= FindNamed<Image>("DamageArrow_Back");
            arrows[3] ??= FindNamed<Image>("DamageArrow_Left");
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

        private T FindNamed<T>(string objectName) where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == objectName) return all[i];
            return null;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
    }
}
