using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Parachute;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class MatchStartHud : MonoBehaviour
    {
        [SerializeField] private MatchStartController sequence;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;

        private float _landingMessageUntil;

        private void Awake()
        {
            if (sequence == null || parachute == null || panelRoot == null ||
                titleText == null || detailText == null)
            {
                Debug.LogError($"[{nameof(MatchStartHud)}] Referencias incompletas en '{name}'.", this);
                enabled = false;
                return;
            }
            SetVisible(false);
            Subscribe();
        }

        public void Configure(
            MatchStartController startSequence,
            ParachuteController playerParachute
        )
        {
            Unsubscribe();
            sequence = startSequence;
            parachute = playerParachute;
            Subscribe();
        }

        private void Update()
        {
            if (sequence == null || parachute == null)
            {
                SetVisible(false);
                return;
            }

            string title;
            string detail;

            if (!sequence.SequenceRunning && parachute.State == AirDropState.Landed)
            {
                if (Time.unscaledTime > _landingMessageUntil)
                {
                    SetVisible(false);
                    return;
                }

                title = "EN PARTIDA";
                detail = "Aterrizaje completado";
            }
            else if (parachute.State == AirDropState.InPlane)
            {
                if (sequence.WarmupRemaining > 0f)
                {
                    title = "PREPARANDO VUELO";
                    detail = $"Inicio en {Mathf.CeilToInt(sequence.WarmupRemaining)}";
                }
                else
                {
                    title = "RUTA DEL AVIÓN";
                    detail = sequence.CanJumpNow
                        ? "[F / ESPACIO] SALTAR"
                        : $"Vuelo {sequence.FlightProgress * 100f:0}%";
                }
            }
            else if (parachute.State == AirDropState.FreeFall)
            {
                title = "CAÍDA LIBRE";
                detail = $"Altura {parachute.GroundClearance:0} m  ·  [ESPACIO] ABRIR";
            }
            else if (parachute.State == AirDropState.Parachuting)
            {
                title = "PARACAÍDAS";
                detail = $"Altura {parachute.GroundClearance:0} m  ·  WASD PARA PLANEAR";
            }
            else
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (titleText != null) titleText.text = title;
            if (detailText != null) detailText.text = detail;
        }

        private void Subscribe()
        {
            if (parachute == null) return;
            parachute.Landed -= HandleLanding;
            parachute.Landed += HandleLanding;
        }

        private void Unsubscribe()
        {
            if (parachute != null)
                parachute.Landed -= HandleLanding;
        }

        private void HandleLanding()
        {
            _landingMessageUntil = Time.unscaledTime + 2.5f;
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null && panelRoot.activeSelf != visible)
                panelRoot.SetActive(visible);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
