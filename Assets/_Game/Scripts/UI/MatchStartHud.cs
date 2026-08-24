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
            ResolvePhysicalView();
            ResolveGameplayReferences();
            SetVisible(false);
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
            ResolvePhysicalView();
        }

        private void Update()
        {
            ResolveGameplayReferences();
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

        private void ResolvePhysicalView()
        {
            if (panelRoot == null)
            {
                Transform root = FindNamedTransform("MatchStatePanel");
                panelRoot = root != null ? root.gameObject : null;
            }

            if (titleText == null)
                titleText = FindNamedComponent<Text>("MatchStateTitle");

            if (detailText == null)
                detailText = FindNamedComponent<Text>("MatchStateDetail");
        }

        private void ResolveGameplayReferences()
        {
            if (sequence == null)
                sequence = FindFirstObjectByType<MatchStartController>();

            if (parachute == null)
            {
                PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
                if (input != null)
                    parachute = input.GetComponent<ParachuteController>();
            }

            Subscribe();
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

        private Transform FindNamedTransform(string objectName)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == objectName) return all[i];
            return null;
        }

        private T FindNamedComponent<T>(string objectName) where T : Component
        {
            Transform target = FindNamedTransform(objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
