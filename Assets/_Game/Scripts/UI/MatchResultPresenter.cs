using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Input;
using UnityEngine;

namespace ROS.Game.UI
{
    public sealed class MatchResultPresenter :
        MonoBehaviour
    {
        [SerializeField] private BattleRoyaleManager matchManager;
        [SerializeField] private Health localPlayer;

        private BattleRoyaleManager _subscribedManager;
        private bool _showResult;
        private bool _victory;
        private int _placement;
        private string _killerName;
        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;
        private Texture2D _whiteTexture;

        public bool IsShowingResult => _showResult;
        public bool IsVictory => _victory;
        public int Placement => _placement;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(
            BattleRoyaleManager manager,
            Health player
        )
        {
            Unsubscribe();

            matchManager = manager;
            localPlayer = player;

            Subscribe();
        }

        private void EnsureReferences()
        {
            if (matchManager == null)
            {
                matchManager =
                    FindFirstObjectByType<
                        BattleRoyaleManager
                    >();
            }

            if (localPlayer == null)
            {
                PlayerInputReader input =
                    FindFirstObjectByType<
                        PlayerInputReader
                    >();

                if (input != null)
                {
                    localPlayer =
                        input.GetComponent<Health>();
                }
            }
        }

        private void Subscribe()
        {
            if (
                matchManager == null ||
                _subscribedManager == matchManager
            )
            {
                return;
            }

            Unsubscribe();

            matchManager.PlayerEliminated +=
                OnPlayerEliminated;

            matchManager.MatchFinished +=
                OnMatchFinished;

            _subscribedManager = matchManager;
        }

        private void Unsubscribe()
        {
            if (_subscribedManager == null)
            {
                return;
            }

            _subscribedManager.PlayerEliminated -=
                OnPlayerEliminated;

            _subscribedManager.MatchFinished -=
                OnMatchFinished;

            _subscribedManager = null;
        }

        private void OnPlayerEliminated(
            EliminationInfo elimination
        )
        {
            if (elimination.Victim != localPlayer)
            {
                return;
            }

            _showResult = true;
            _victory = false;
            _placement = elimination.Placement;

            _killerName =
                elimination.HasKiller
                    ? elimination.Killer.name
                    : string.Empty;
        }

        private void OnMatchFinished(Health winner)
        {
            if (winner != localPlayer)
            {
                return;
            }

            _showResult = true;
            _victory = true;
            _placement = 1;
            _killerName = string.Empty;
        }

        private void OnGUI()
        {
            if (!_showResult)
            {
                return;
            }

            EnsureStyles();

            Color previousColor = GUI.color;

            GUI.color =
                new Color(0f, 0f, 0f, 0.72f);

            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height
                ),
                _whiteTexture
            );

            GUI.color = previousColor;

            string title =
                _victory
                    ? "VICTORIA"
                    : "HAS SIDO ELIMINADO";

            GUI.Label(
                new Rect(
                    0f,
                    Screen.height * 0.34f,
                    Screen.width,
                    80f
                ),
                title,
                _titleStyle
            );

            GUI.Label(
                new Rect(
                    0f,
                    Screen.height * 0.46f,
                    Screen.width,
                    45f
                ),
                $"POSICIÓN FINAL #{_placement}",
                _detailStyle
            );

            if (!_victory &&
                !string.IsNullOrEmpty(_killerName))
            {
                GUI.Label(
                    new Rect(
                        0f,
                        Screen.height * 0.52f,
                        Screen.width,
                        40f
                    ),
                    $"ELIMINADO POR {_killerName}",
                    _detailStyle
                );
            }
        }

        private void EnsureStyles()
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = Texture2D.whiteTexture;
            }

            if (_titleStyle == null)
            {
                _titleStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 44,
                        fontStyle = FontStyle.Bold,
                        normal =
                        {
                            textColor = Color.white
                        }
                    };
            }

            if (_detailStyle == null)
            {
                _detailStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 24,
                        normal =
                        {
                            textColor = Color.white
                        }
                    };
            }
        }
    }
}
