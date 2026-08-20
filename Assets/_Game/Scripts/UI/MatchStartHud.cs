using ROS.Game.Core;
using ROS.Game.Parachute;
using UnityEngine;

namespace ROS.Game.UI
{
    public sealed class MatchStartHud : MonoBehaviour
    {
        [SerializeField] private MatchStartController sequence;
        [SerializeField] private ParachuteController parachute;

        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;

        public void Configure(
            MatchStartController startSequence,
            ParachuteController playerParachute
        )
        {
            sequence = startSequence;
            parachute = playerParachute;
        }

        private void OnGUI()
        {
            if (sequence == null || parachute == null)
            {
                return;
            }

            EnsureStyles();
            string title;
            string detail;

            if (!sequence.SequenceRunning &&
                parachute.State == AirDropState.Landed)
            {
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
                return;
            }

            float width = Mathf.Min(540f, Screen.width - 32f);
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                24f,
                width,
                78f
            );

            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.07f, 0.11f, 0.88f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 9f, panel.width - 24f, 28f),
                title,
                _titleStyle
            );
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 39f, panel.width - 24f, 27f),
                detail,
                _detailStyle
            );
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            _detailStyle.normal.textColor =
                new Color(0.45f, 0.78f, 1f);
        }
    }
}
