using System.Collections.Generic;
using ROS.Game.Audio;
using ROS.Game.Combat;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Transform directionReference;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] receivedDamageClips;
        [SerializeField] private AudioClip[] hitConfirmedClips;
        [SerializeField] private float hitmarkerDuration = 0.16f;
        [SerializeField] private float damageIndicatorDuration = 0.65f;

        [Header("Physical HUD References")]
        [SerializeField] private GameObject hitmarkerRoot;
        [SerializeField] private Image[] hitmarkerParts = new Image[4];
        [SerializeField] private Text headshotLabel;
        [SerializeField] private Image[] damageBars = new Image[4];

        private readonly HashSet<WeaponController> _weapons =
            new HashSet<WeaponController>();

        private float _hitmarkerUntil;
        private float _damageIndicatorUntil;
        private float _incomingAngle;
        private bool _lastHitWasHeadshot;
        private bool _lastHitWasFatal;
        private float _nextWeaponRefresh;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    $"[{nameof(CombatFeedbackPresenter)}] Referencias incompletas en '{name}'.",
                    this);
                enabled = false;
                return;
            }

            SetHitmarkerVisible(false);
            SetDamageBarsAlpha(0f);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Damaged += OnDamaged;
            }

            RefreshWeaponSubscriptions();
        }

        private void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;

            foreach (WeaponController weapon in _weapons)
            {
                if (weapon != null)
                    weapon.HitConfirmed -= OnHitConfirmed;
            }

            _weapons.Clear();
            SetHitmarkerVisible(false);
            SetDamageBarsAlpha(0f);
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextWeaponRefresh)
            {
                _nextWeaponRefresh = Time.unscaledTime + 0.5f;
                RefreshWeaponSubscriptions();
            }

            UpdatePhysicalFeedback();
        }

        private bool HasRequiredReferences()
        {
            if (health == null || directionReference == null || hitmarkerRoot == null ||
                headshotLabel == null || hitmarkerParts == null || hitmarkerParts.Length != 4 ||
                damageBars == null || damageBars.Length != 4)
                return false;

            for (int i = 0; i < 4; i++)
                if (hitmarkerParts[i] == null || damageBars[i] == null)
                    return false;

            return true;
        }

        private void RefreshWeaponSubscriptions()
        {
            WeaponController[] found =
                GetComponentsInChildren<WeaponController>(true);

            foreach (WeaponController weapon in found)
            {
                if (weapon != null && _weapons.Add(weapon))
                    weapon.HitConfirmed += OnHitConfirmed;
            }
        }

        private void OnHitConfirmed(DamageResult result)
        {
            _lastHitWasHeadshot = result.IsHeadshot;
            _lastHitWasFatal = result.WasFatal;
            _hitmarkerUntil = Time.unscaledTime + hitmarkerDuration;
            RandomAudioPlayer.Play(audioSource, hitConfirmedClips);
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

            _incomingAngle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            _damageIndicatorUntil =
                Time.unscaledTime + damageIndicatorDuration;

            RandomAudioPlayer.Play(audioSource, receivedDamageClips);
        }

        private void UpdatePhysicalFeedback()
        {
            bool showHitmarker = Time.unscaledTime < _hitmarkerUntil;
            SetHitmarkerVisible(showHitmarker);

            if (showHitmarker)
            {
                Color color = _lastHitWasFatal
                    ? new Color(1f, 0.2f, 0.2f, 1f)
                    : _lastHitWasHeadshot
                        ? new Color(1f, 0.82f, 0.2f, 1f)
                        : Color.white;

                for (int i = 0; i < hitmarkerParts.Length; i++)
                    if (hitmarkerParts[i] != null) hitmarkerParts[i].color = color;

                if (headshotLabel != null)
                    headshotLabel.gameObject.SetActive(_lastHitWasHeadshot);
            }

            float remaining = _damageIndicatorUntil - Time.unscaledTime;
            if (remaining <= 0f)
            {
                SetDamageBarsAlpha(0f);
                return;
            }

            float alpha = Mathf.Clamp01(remaining / Mathf.Max(0.01f, damageIndicatorDuration)) * 0.72f;
            int activeIndex = DirectionIndex(_incomingAngle);

            for (int i = 0; i < damageBars.Length; i++)
            {
                Image bar = damageBars[i];
                if (bar == null) continue;
                Color color = bar.color;
                color.a = i == activeIndex ? alpha : 0f;
                bar.color = color;
            }
        }

        private static int DirectionIndex(float angle)
        {
            float normalized = Mathf.Repeat(angle + 180f, 360f) - 180f;
            if (normalized >= -45f && normalized <= 45f) return 0;
            if (normalized > 45f && normalized < 135f) return 1;
            if (normalized < -45f && normalized > -135f) return 3;
            return 2;
        }

        private void SetHitmarkerVisible(bool visible)
        {
            if (hitmarkerRoot != null && hitmarkerRoot.activeSelf != visible)
                hitmarkerRoot.SetActive(visible);

            if (!visible && headshotLabel != null)
                headshotLabel.gameObject.SetActive(false);
        }

        private void SetDamageBarsAlpha(float alpha)
        {
            if (damageBars == null) return;
            for (int i = 0; i < damageBars.Length; i++)
            {
                if (damageBars[i] == null) continue;
                Color color = damageBars[i].color;
                color.a = alpha;
                damageBars[i].color = color;
            }
        }

    }
}
