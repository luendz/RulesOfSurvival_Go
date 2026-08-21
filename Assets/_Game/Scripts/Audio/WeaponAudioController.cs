using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Audio
{
    [RequireComponent(typeof(WeaponController))]
    public sealed class WeaponAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponController weapon;

        [SerializeField] private AudioSource fireSource;
        [SerializeField] private AudioSource actionSource;

        [Header("Fire")]
        [SerializeField] private AudioClip[] fireClips;

        [Header("Reload")]
        [SerializeField] private AudioClip[] reloadStartClips;
        [SerializeField] private AudioClip[] reloadEndClips;

        [Header("Shell Drop")]
        [SerializeField] private AudioClip[] shellDropClips;

        [Tooltip("Seconds after reload end before shell drop sound plays.")]
        [SerializeField] private float shellDropDelay = 0.15f;

        [Header("Dry Fire")]
        [SerializeField] private AudioClip[] dryFireClips;

        private bool _prevReloading;

        private void Awake()
        {
            if (weapon == null)
                weapon = GetComponent<WeaponController>();
        }

        private void OnEnable()
        {
            if (weapon != null)
                weapon.Fired += OnFired;
        }

        private void OnDisable()
        {
            if (weapon != null)
                weapon.Fired -= OnFired;

            CancelInvoke(nameof(PlayShellDrop));
        }

        private void Update()
        {
            if (weapon == null)
                return;

            bool reloading = weapon.IsReloading;

            if (reloading && !_prevReloading)
            {
                RandomAudioPlayer.Play(actionSource, reloadStartClips);
            }
            else if (!reloading && _prevReloading)
            {
                RandomAudioPlayer.Play(actionSource, reloadEndClips);

                if (shellDropClips != null && shellDropClips.Length > 0)
                    Invoke(nameof(PlayShellDrop), shellDropDelay);
            }

            _prevReloading = reloading;
        }

        private void OnFired()
        {
            RandomAudioPlayer.Play(fireSource, fireClips);
        }

        private void PlayShellDrop()
        {
            RandomAudioPlayer.Play(actionSource, shellDropClips);
        }
    }
}
