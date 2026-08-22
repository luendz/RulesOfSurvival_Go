using ROS.Game.CameraSystem;
using ROS.Game.Input;
using ROS.Game.Lobby;
using ROS.Game.Parachute;
using UnityEngine;

namespace ROS.Game.UI
{
    public sealed class BattleRoyaleStartMenu : MonoBehaviour
    {
        [SerializeField] private MatchStartController sequence;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private ThirdPersonCamera playerCamera;
        [SerializeField] private float airplaneCameraDistance = 5.2f;
        [SerializeField] private Vector3 freeroamSpawnPoint = new Vector3(0f, 1f, 0f);

        public bool IsVisible { get; private set; } = true;
        public bool MatchRequested { get; private set; }

        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _buttonStyle;

        public void Configure(
            MatchStartController startSequence,
            PlayerInputReader playerInput,
            ThirdPersonCamera cameraController
        )
        {
            Unsubscribe();
            sequence = startSequence;
            input = playerInput;
            playerCamera = cameraController;
            Subscribe();

            IsVisible = true;
            MatchRequested = false;

            if (input != null)
            {
                input.SetUiBlocked(true);
            }

            // Si la partida viene desde el lobby, el botón JUGAR ya representa
            // la confirmación del usuario y entramos directamente al flujo BR.
            // Abrir 07_BattleRoyaleTest directamente desde el editor conserva
            // este menú para pruebas y modo libre.
            if (LobbySession.ConsumeLaunchRequest())
            {
                StartMatch();
            }
        }

        public bool StartMatch()
        {
            if (MatchRequested || sequence == null)
            {
                return false;
            }

            MatchRequested = true;

            if (input != null)
            {
                input.SetUiBlocked(false);
            }

            if (!sequence.BeginSequence())
            {
                MatchRequested = false;
                if (input != null)
                {
                    input.SetUiBlocked(true);
                }

                return false;
            }

            IsVisible = false;
            if (playerCamera != null)
            {
                playerCamera.EnterAirplaneView(airplaneCameraDistance);
            }

            return true;
        }

        public void StartFreeroam()
        {
            if (MatchRequested) return;

            MatchRequested = true;
            IsVisible = false;

            if (input != null)
                input.SetUiBlocked(false);

            if (input != null)
                input.transform.position = freeroamSpawnPoint;
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureStyles();
            Color previousColor = GUI.color;
            GUI.color = new Color(0.015f, 0.025f, 0.045f, 0.94f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture
            );
            GUI.color = Color.white;

            float panelWidth = Mathf.Min(620f, Screen.width - 40f);
            float panelHeight = 380f;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight
            );

            GUI.Label(
                new Rect(panel.x, panel.y + 24f, panel.width, 60f),
                "RULES OF SURVIVAL",
                _titleStyle
            );
            GUI.Label(
                new Rect(panel.x + 30f, panel.y + 92f, panel.width - 60f, 48f),
                "Battle Royale · Sobrevive, consigue loot y sé el último en pie",
                _subtitleStyle
            );

            float btnW = 280f;
            float btnX = panel.x + (panel.width - btnW) * 0.5f;

            if (GUI.Button(
                    new Rect(btnX, panel.y + 158f, btnW, 60f),
                    "INICIAR PARTIDA BR",
                    _buttonStyle))
            {
                StartMatch();
            }

            GUI.Label(
                new Rect(panel.x, panel.y + 224f, panel.width, 22f),
                "Avión → paracaídas → loot → último en pie",
                _subtitleStyle
            );

            if (GUI.Button(
                    new Rect(btnX, panel.y + 256f, btnW, 52f),
                    "MODO LIBRE (sin BR)",
                    _buttonStyle))
            {
                StartFreeroam();
            }

            GUI.Label(
                new Rect(panel.x, panel.y + 316f, panel.width, 22f),
                "Explora el mapa sin bots ni zona azul",
                _subtitleStyle
            );

            GUI.color = previousColor;
        }

        private void HandlePlayerJumped()
        {
            if (playerCamera != null)
            {
                playerCamera.EnterAirDropView();
            }
        }

        private void HandleSequenceCompleted()
        {
            if (playerCamera != null)
            {
                playerCamera.ExitAirplaneView();
            }
        }

        private void Subscribe()
        {
            if (sequence == null)
            {
                return;
            }

            sequence.PlayerJumped -= HandlePlayerJumped;
            sequence.PlayerJumped += HandlePlayerJumped;
            sequence.SequenceCompleted -= HandleSequenceCompleted;
            sequence.SequenceCompleted += HandleSequenceCompleted;
        }

        private void Unsubscribe()
        {
            if (sequence == null)
            {
                return;
            }

            sequence.PlayerJumped -= HandlePlayerJumped;
            sequence.SequenceCompleted -= HandleSequenceCompleted;
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
                fontSize = 36,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                wordWrap = true
            };
            _subtitleStyle.normal.textColor =
                new Color(0.64f, 0.76f, 0.9f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
