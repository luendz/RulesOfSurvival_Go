using ROS.Game.World;
using UnityEngine;

namespace ROS.Game.Audio
{
    [RequireComponent(typeof(AirplaneController))]
    public sealed class AirplaneAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   flyLoopClip;
        [SerializeField] private AudioClip   flyDaylightClip;

        private AirplaneController _airplane;

        private void Awake()
        {
            _airplane = GetComponent<AirplaneController>();

            if (audioSource == null)
            {
                Debug.LogError($"[{nameof(AirplaneAudioController)}] AudioSource no está asignado en '{name}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_airplane != null)
            {
                _airplane.FlightStarted  += OnFlightStarted;
                _airplane.FlightFinished += OnFlightFinished;
            }
        }

        private void OnDisable()
        {
            if (_airplane != null)
            {
                _airplane.FlightStarted  -= OnFlightStarted;
                _airplane.FlightFinished -= OnFlightFinished;
            }
        }

        private void OnFlightStarted()
        {
            if (audioSource == null)
                return;

            if (flyDaylightClip != null)
                audioSource.PlayOneShot(flyDaylightClip);

            if (flyLoopClip != null)
            {
                audioSource.clip  = flyLoopClip;
                audioSource.loop  = true;
                audioSource.Play();
            }
        }

        private void OnFlightFinished()
        {
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }
        }
    }
}
