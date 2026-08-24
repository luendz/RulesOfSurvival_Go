using System.Collections;
using ROS.Game.Character;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Audio
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class CharacterAudioController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioSource actionSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Footsteps")]
        [SerializeField] private AudioClip[] walkClips;
        [SerializeField] private AudioClip[] runClips;
        [SerializeField] private AudioClip[] sprintClips;
        [SerializeField] private AudioClip[] crouchWalkClips;
        [SerializeField] private AudioClip[] crawlClips;
        [SerializeField] private AudioClip[] metalMoveClips;

        [Header("Footstep Intervals (seconds)")]
        [SerializeField] private float walkInterval    = 0.55f;
        [SerializeField] private float runInterval     = 0.33f;
        [SerializeField] private float sprintInterval  = 0.25f;
        [SerializeField] private float crouchInterval  = 0.65f;
        [SerializeField] private float crawlInterval   = 0.75f;

        [Header("Jump / Land")]
        [SerializeField] private AudioClip[] jumpClips;
        [SerializeField] private AudioClip[] jump3Clips;
        [SerializeField] private AudioClip[] hardLandClips;
        [SerializeField] private AudioClip[] waterEntryClips;

        [Header("Stance - Prone")]
        [SerializeField] private AudioClip[] proneDownClips;
        [SerializeField] private AudioClip[] proneDownFoleyClips;
        [SerializeField] private AudioClip[] proneUpClips;
        [SerializeField] private AudioClip[] proneUpFoleyClips;

        [Header("Stance - Crouch")]
        [SerializeField] private AudioClip[] crouchClips;

        [Header("Death")]
        [SerializeField] private AudioClip[] dieClips;

        [Header("Actions")]
        [SerializeField] private AudioClip[] pickupClips;
        [SerializeField] private AudioClip[] pickupHandClips;
        [SerializeField] private AudioClip[] vaultClips;
        [SerializeField] private AudioClip[] throwGrenadeClips;

        [Header("Parachute / Skydive")]
        [SerializeField] private AudioClip[] parachuteOpenClips;
        [SerializeField] private AudioClip   fallWindIntroClip;
        [SerializeField] private AudioClip   skydiveWindClip;
        [SerializeField] private AudioClip   skydiveWindFastClip;
        [SerializeField] private AudioClip   skydiveLandingClip;

        [Tooltip("Y-velocity threshold to switch to fast wind loop.")]
        [SerializeField] private float fastWindThreshold = 15f;

        private PlayerMotor _motor;
        private PlayerMovementState _prevState;
        private bool _prevCrouching;
        private bool _prevProne;
        private bool _prevGrounded;
        private bool _isDead;
        private Coroutine _footstepRoutine;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
        }

        private void OnEnable()
        {
            if (_motor == null)
                return;

            _prevState    = _motor.MovementState;
            _prevCrouching = _motor.IsCrouching;
            _prevProne    = _motor.IsProne;
            _prevGrounded = _motor.IsGrounded;

            StartFootstepCycle();
        }

        private void OnDisable()
        {
            StopFootstepCycle();
            StopLoop();
        }

        private void Update()
        {
            if (_motor == null || _isDead)
                return;

            PlayerMovementState state  = _motor.MovementState;
            bool crouching = _motor.IsCrouching;
            bool prone     = _motor.IsProne;
            bool grounded  = _motor.IsGrounded;

            HandleDeathTransition(state);
            if (_isDead)
                return;

            HandleProneTransitions(prone);
            HandleCrouchTransitions(crouching, prone);
            HandleJumpTransition(state);
            HandleFallingTransition(state);
            HandleLandTransition(state, grounded);
            HandleParachuteTransition(state);
            HandleSkydiveFallWind(state);

            _prevState    = state;
            _prevCrouching = crouching;
            _prevProne    = prone;
            _prevGrounded = grounded;
        }

        // ------------------------------------------------------------------ public events

        public void PlayPickup()       => RandomAudioPlayer.Play(actionSource, pickupClips);
        public void PlayPickupHand()   => RandomAudioPlayer.Play(actionSource, pickupHandClips);
        public void PlayVault()        => RandomAudioPlayer.Play(actionSource, vaultClips);
        public void PlayThrowGrenade() => RandomAudioPlayer.Play(actionSource, throwGrenadeClips);
        public void PlayDeath()        => TriggerDeath();

        // ------------------------------------------------------------------ transitions

        private void HandleDeathTransition(PlayerMovementState state)
        {
            if (state == PlayerMovementState.Dead && _prevState != PlayerMovementState.Dead)
                TriggerDeath();
        }

        private void TriggerDeath()
        {
            if (_isDead)
                return;

            _isDead = true;
            RandomAudioPlayer.Play(actionSource, dieClips);
            StopFootstepCycle();
            StopLoop();
        }

        private void HandleProneTransitions(bool prone)
        {
            if (prone && !_prevProne)
            {
                RandomAudioPlayer.Play(actionSource,  proneDownClips);
                RandomAudioPlayer.Play(footstepSource, proneDownFoleyClips);
            }
            else if (!prone && _prevProne)
            {
                RandomAudioPlayer.Play(actionSource,  proneUpClips);
                RandomAudioPlayer.Play(footstepSource, proneUpFoleyClips);
            }
        }

        private void HandleCrouchTransitions(bool crouching, bool prone)
        {
            if (prone)
                return;

            if (crouching != _prevCrouching)
                RandomAudioPlayer.Play(actionSource, crouchClips);
        }

        private void HandleJumpTransition(PlayerMovementState state)
        {
            if (state == PlayerMovementState.Jumping &&
                _prevState != PlayerMovementState.Jumping)
            {
                RandomAudioPlayer.Play(actionSource, jumpClips);
            }
        }

        private void HandleFallingTransition(PlayerMovementState state)
        {
            if (state != PlayerMovementState.Falling ||
                _prevState == PlayerMovementState.Falling)
                return;

            PlayOneShot(actionSource, fallWindIntroClip);

            if (skydiveWindClip != null)
                PlayLoop(skydiveWindClip);
        }

        private void HandleLandTransition(PlayerMovementState state, bool grounded)
        {
            bool wasAirborne =
                _prevState == PlayerMovementState.Jumping ||
                _prevState == PlayerMovementState.Falling;

            bool justLanded = grounded && !_prevGrounded && wasAirborne;

            if (!justLanded)
                return;

            RandomAudioPlayer.Play(actionSource, hardLandClips);
        }

        private void HandleParachuteTransition(PlayerMovementState state)
        {
            bool entering = state == PlayerMovementState.Parachuting &&
                            _prevState != PlayerMovementState.Parachuting;

            bool exiting = state != PlayerMovementState.Parachuting &&
                           _prevState == PlayerMovementState.Parachuting;

            if (entering)
            {
                RandomAudioPlayer.Play(actionSource, parachuteOpenClips);
                PlayLoop(skydiveWindClip);
            }
            else if (exiting)
            {
                StopLoop();

                if (state == PlayerMovementState.Idle ||
                    _motor.IsGrounded)
                {
                    PlayOneShot(actionSource, skydiveLandingClip);
                }
            }
        }

        private void HandleSkydiveFallWind(PlayerMovementState state)
        {
            if (state != PlayerMovementState.Falling || loopSource == null)
                return;

            float fallSpeed = Mathf.Abs(_motor.Velocity.y);
            AudioClip target = fallSpeed >= fastWindThreshold
                ? skydiveWindFastClip
                : skydiveWindClip;

            if (target != null && loopSource.clip != target)
                PlayLoop(target);
        }

        // ------------------------------------------------------------------ footstep coroutine

        private void StartFootstepCycle()
        {
            StopFootstepCycle();
            _footstepRoutine = StartCoroutine(FootstepLoop());
        }

        private void StopFootstepCycle()
        {
            if (_footstepRoutine == null)
                return;

            StopCoroutine(_footstepRoutine);
            _footstepRoutine = null;
        }

        private IEnumerator FootstepLoop()
        {
            while (true)
            {
                PlayerMovementState state = _motor.MovementState;
                float interval = GetFootstepInterval(state);

                if (interval > 0f)
                {
                    PlayFootstep(state);
                    yield return new WaitForSeconds(interval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private float GetFootstepInterval(PlayerMovementState state)
        {
            switch (state)
            {
                case PlayerMovementState.Walking:   return walkInterval;
                case PlayerMovementState.Running:   return runInterval;
                case PlayerMovementState.Sprinting: return sprintInterval;
                case PlayerMovementState.Crouching: return crouchInterval;
                case PlayerMovementState.Prone:     return crawlInterval;
                default:                            return 0f;
            }
        }

        private void PlayFootstep(PlayerMovementState state)
        {
            if (footstepSource == null)
                return;

            AudioClip[] clips;

            switch (state)
            {
                case PlayerMovementState.Walking:   clips = walkClips;       break;
                case PlayerMovementState.Running:   clips = runClips;        break;
                case PlayerMovementState.Sprinting: clips = sprintClips;     break;
                case PlayerMovementState.Crouching: clips = crouchWalkClips; break;
                case PlayerMovementState.Prone:     clips = crawlClips;      break;
                default: return;
            }

            RandomAudioPlayer.Play(footstepSource, clips);
        }

        // ------------------------------------------------------------------ helpers

        private void PlayLoop(AudioClip clip)
        {
            if (loopSource == null || clip == null)
                return;

            if (loopSource.clip == clip && loopSource.isPlaying)
                return;

            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.Play();
        }

        private void StopLoop()
        {
            if (loopSource != null && loopSource.isPlaying)
                loopSource.Stop();
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip)
        {
            if (source != null && clip != null)
                source.PlayOneShot(clip);
        }
    }
}
