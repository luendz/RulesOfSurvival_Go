using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Transform directionReference;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip receivedDamageClip;
        [SerializeField] private AudioClip hitConfirmedClip;
        [SerializeField] private float hitmarkerDuration = 0.16f;
        [SerializeField] private float damageIndicatorDuration = 0.65f;

        private readonly HashSet<WeaponController> _weapons =
            new HashSet<WeaponController>();

        private Texture2D _whiteTexture;
        private float _hitmarkerUntil;
        private float _damageIndicatorUntil;
        private float _incomingAngle;
        private bool _lastHitWasHeadshot;
        private bool _lastHitWasFatal;
        private float _nextWeaponRefresh;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (directionReference == null && Camera.main != null)
            {
                directionReference = Camera.main.transform;
            }

            _whiteTexture = Texture2D.whiteTexture;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
            }

            RefreshWeaponSubscriptions();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            foreach (WeaponController weapon in _weapons)
            {
                if (weapon != null)
                {
                    weapon.HitConfirmed -= OnHitConfirmed;
                }
            }

            _weapons.Clear();
        }

        private void Update()
        {
            if (directionReference == null && Camera.main != null)
            {
                directionReference = Camera.main.transform;
            }

            if (Time.unscaledTime >= _nextWeaponRefresh)
            {
                _nextWeaponRefresh = Time.unscaledTime + 0.5f;
                RefreshWeaponSubscriptions();
            }
        }

        private void RefreshWeaponSubscriptions()
        {
            WeaponController[] found =
                GetComponentsInChildren<WeaponController>(true);

            foreach (WeaponController weapon in found)
            {
                if (weapon != null && _weapons.Add(weapon))
                {
                    weapon.HitConfirmed += OnHitConfirmed;
                }
            }
        }

        private void OnHitConfirmed(DamageResult result)
        {
            _lastHitWasHeadshot = result.IsHeadshot;
            _lastHitWasFatal = result.WasFatal;
            _hitmarkerUntil = Time.unscaledTime + hitmarkerDuration;

            if (audioSource != null && hitConfirmedClip != null)
            {
                audioSource.PlayOneShot(hitConfirmedClip);
            }
        }

        private void OnDamaged(DamageResult result)
        {
            Vector3 sourceDirection = -result.Damage.Direction;

            if (sourceDirection.sqrMagnitude <= 0.001f &&
                result.Damage.Instigator != null)
            {
                sourceDirection =
                    result.Damage.Instigator.transform.position -
                    transform.position;
            }

            Transform reference = directionReference != null
                ? directionReference
                : transform;

            Vector3 local = reference.InverseTransformDirection(
                sourceDirection.sqrMagnitude > 0.001f
                    ? sourceDirection.normalized
                    : reference.forward
            );

            _incomingAngle = Mathf.Atan2(local.x, local.z) *
                             Mathf.Rad2Deg;
            _damageIndicatorUntil =
                Time.unscaledTime + damageIndicatorDuration;

            if (audioSource != null && receivedDamageClip != null)
            {
                audioSource.PlayOneShot(receivedDamageClip);
            }
        }

        private void OnGUI()
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = Texture2D.whiteTexture;
            }

            if (Time.unscaledTime < _hitmarkerUntil)
            {
                DrawHitmarker();
            }

            if (Time.unscaledTime < _damageIndicatorUntil)
            {
                DrawDamageDirection();
            }
        }

        private void DrawHitmarker()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            const float gap = 6f;
            const float length = 9f;
            const float thickness = 2f;

            Color previous = GUI.color;
            GUI.color = _lastHitWasFatal
                ? new Color(1f, 0.2f, 0.2f, 1f)
                : _lastHitWasHeadshot
                    ? new Color(1f, 0.82f, 0.2f, 1f)
                    : Color.white;

            DrawRect(centerX - gap - length, centerY - gap - length, length, thickness);
            DrawRect(centerX + gap, centerY - gap - length, length, thickness);
            DrawRect(centerX - gap - length, centerY + gap + length, length, thickness);
            DrawRect(centerX + gap, centerY + gap + length, length, thickness);

            if (_lastHitWasHeadshot)
            {
                GUI.Label(
                    new Rect(centerX - 55f, centerY + 28f, 110f, 24f),
                    "HEADSHOT"
                );
            }

            GUI.color = previous;
        }

        private void DrawDamageDirection()
        {
            float normalized = Mathf.Repeat(
                _incomingAngle + 180f,
                360f
            ) - 180f;

            const float thickness = 8f;
            const float length = 110f;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.08f, 0.03f, 0.72f);

            if (normalized >= -45f && normalized <= 45f)
            {
                DrawRect(Screen.width * 0.5f - length * 0.5f, 18f, length, thickness);
            }
            else if (normalized > 45f && normalized < 135f)
            {
                DrawRect(Screen.width - 26f, Screen.height * 0.5f - length * 0.5f, thickness, length);
            }
            else if (normalized < -45f && normalized > -135f)
            {
                DrawRect(18f, Screen.height * 0.5f - length * 0.5f, thickness, length);
            }
            else
            {
                DrawRect(Screen.width * 0.5f - length * 0.5f, Screen.height - 26f, length, thickness);
            }

            GUI.color = previous;
        }

        private void DrawRect(float x, float y, float width, float height)
        {
            GUI.DrawTexture(
                new Rect(x, y, width, height),
                _whiteTexture
            );
        }
    }
}
