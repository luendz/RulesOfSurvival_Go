using System;
using ROS.Game.BattleRoyale;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.World;
using UnityEngine;

namespace ROS.Game.Parachute
{
    public sealed class MatchStartController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleRoyaleManager matchManager;
        [SerializeField] private AirplaneController airplane;
        [SerializeField] private ParachuteController playerParachute;
        [SerializeField] private PlayerInputReader input;

        [Header("Sequence")]
        [SerializeField] private bool startOnStart = true;
        [Min(0f)]
        [SerializeField] private float warmupDuration = 3f;
        [Min(1f)]
        [SerializeField] private float flightDuration = 24f;
        [Range(0f, 1f)]
        [SerializeField] private float minimumJumpProgress = 0.05f;
        [Range(0f, 1f)]
        [SerializeField] private float automaticJumpProgress = 0.88f;

        [Header("Route")]
        [SerializeField] private Vector3 routeStart =
            new Vector3(-90f, 105f, -35f);
        [SerializeField] private Vector3 routeEnd =
            new Vector3(90f, 105f, 35f);

        public float WarmupRemaining { get; private set; }
        public float FlightProgress => airplane != null
            ? airplane.Progress
            : 0f;
        public bool HasJumped { get; private set; }
        public bool SequenceRunning { get; private set; }
        public bool CanJumpNow => SequenceRunning &&
                                  !HasJumped &&
                                  airplane != null &&
                                  ParachuteFlightMath.CanJumpFromPlane(
                                      airplane.Progress,
                                      minimumJumpProgress,
                                      automaticJumpProgress
                                  );

        public event Action SequenceStarted;
        public event Action PlayerJumped;
        public event Action SequenceCompleted;

        private bool _flightStarted;

        private void Start()
        {
            if (startOnStart)
                BeginSequence();
        }

        private void Update()
        {
            if (!SequenceRunning)
                return;

            if (!_flightStarted)
            {
                WarmupRemaining = Mathf.Max(
                    0f,
                    WarmupRemaining - Time.deltaTime
                );

                if (WarmupRemaining <= 0f)
                    BeginFlight();

                return;
            }

            if (HasJumped)
                return;

            bool jumpPressed = input != null &&
                               (input.InteractPressed ||
                                input.JumpPressed);

            if ((jumpPressed && CanJumpNow) ||
                airplane.Progress >= automaticJumpProgress)
            {
                TryJump();
            }
        }

        public void Configure(
            BattleRoyaleManager manager,
            AirplaneController flight,
            ParachuteController parachute,
            PlayerInputReader playerInput,
            Vector3 start,
            Vector3 end,
            float warmupSeconds,
            float flightSeconds
        )
        {
            Unsubscribe();
            matchManager = manager;
            airplane = flight;
            playerParachute = parachute;
            input = playerInput;
            routeStart = start;
            routeEnd = end;
            warmupDuration = Mathf.Max(0f, warmupSeconds);
            flightDuration = Mathf.Max(1f, flightSeconds);
            startOnStart = false;
            Subscribe();
        }

        public bool BeginSequence()
        {
            if (airplane == null || playerParachute == null)
            {
                Debug.LogWarning(
                    "El inicio de partida requiere avión y jugador con paracaídas.",
                    this
                );
                return false;
            }

            Subscribe();
            HasJumped = false;
            _flightStarted = false;
            SequenceRunning = true;
            WarmupRemaining = warmupDuration;

            airplane.PrepareRoute(routeStart, routeEnd);
            playerParachute.PrepareForPlane();
            AttachPlayerToPlane();

            if (matchManager != null)
                matchManager.BeginWarmup();

            SequenceStarted?.Invoke();
            return true;
        }

        public bool TryJump()
        {
            if (!SequenceRunning ||
                HasJumped ||
                !_flightStarted ||
                airplane == null ||
                playerParachute == null)
            {
                return false;
            }

            HasJumped = true;
            Transform player = playerParachute.transform;
            player.SetParent(null, true);
            player.position += -airplane.transform.forward * 2f +
                               Vector3.down;
            playerParachute.BeginAirDrop(airplane.Velocity);
            PlayerJumped?.Invoke();
            return true;
        }

        private void BeginFlight()
        {
            _flightStarted = true;

            if (matchManager != null)
                matchManager.BeginPlanePhase();

            airplane.BeginFlight(
                routeStart,
                routeEnd,
                flightDuration
            );
        }

        private void AttachPlayerToPlane()
        {
            Transform player = playerParachute.transform;
            player.SetParent(airplane.PassengerAnchor, false);
            player.localPosition = Vector3.zero;
            player.localRotation = Quaternion.identity;
        }

        private void HandleRouteCompleted()
        {
            // Último punto válido para abandonar el avión. Si el jugador no
            // saltó manualmente/automáticamente, se lo expulsa antes del tramo
            // de salida para que nunca quede pegado al avión fuera del mapa.
            if (!HasJumped)
                TryJump();
        }

        private void HandleFlightFinished()
        {
            // Fallback defensivo por si otro flujo termina el vuelo sin haber
            // emitido RouteCompleted.
            if (!HasJumped)
                TryJump();
        }

        private void HandleLanding()
        {
            SequenceRunning = false;

            if (matchManager != null)
                matchManager.BeginGameplay();

            SequenceCompleted?.Invoke();
        }

        private void Subscribe()
        {
            if (airplane != null)
            {
                airplane.RouteCompleted -= HandleRouteCompleted;
                airplane.RouteCompleted += HandleRouteCompleted;
                airplane.FlightFinished -= HandleFlightFinished;
                airplane.FlightFinished += HandleFlightFinished;
            }

            if (playerParachute != null)
            {
                playerParachute.Landed -= HandleLanding;
                playerParachute.Landed += HandleLanding;
            }
        }

        private void Unsubscribe()
        {
            if (airplane != null)
            {
                airplane.RouteCompleted -= HandleRouteCompleted;
                airplane.FlightFinished -= HandleFlightFinished;
            }

            if (playerParachute != null)
                playerParachute.Landed -= HandleLanding;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
