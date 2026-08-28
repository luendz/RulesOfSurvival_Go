using ROS.Game.CameraSystem;
using ROS.Game.Input;
using ROS.Game.Lobby;
using ROS.Game.Parachute;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class BattleRoyaleStartMenu : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private MatchStartController sequence;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private ThirdPersonCamera playerCamera;
        [SerializeField] private float airplaneCameraDistance = 5.2f;
        [SerializeField] private Vector3 freeroamSpawnPoint = new Vector3(0f, 1f, 0f);

        [Header("Editable View")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button freeroamButton;

        public bool IsVisible { get; private set; } = true;
        public bool MatchRequested { get; private set; }

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    $"[{nameof(BattleRoyaleStartMenu)}] Referencias incompletas en '{name}'.",
                    this);
                enabled = false;
                return;
            }
            HookButtons();
            Subscribe();
            EnterMenuInputState();
        }

        private void OnEnable()
        {
            HookButtons();
            Subscribe();

            if (!MatchRequested && IsVisible)
                EnterMenuInputState();
        }

        private void LateUpdate()
        {
            // En un build pueden habilitarse otros componentes del jugador despues
            // del Awake/OnEnable del menu. PlayerInputReader vuelve a aplicar su
            // estado de cursor al habilitarse; por eso, mientras este menu siga
            // visible, el menu es la autoridad del cursor y del bloqueo de gameplay.
            if (!MatchRequested &&
                IsVisible &&
                viewRoot != null &&
                viewRoot.activeInHierarchy)
            {
                MaintainMenuInputState();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !MatchRequested && IsVisible)
                EnterMenuInputState();
        }

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

            HookButtons();
            MatchRequested = false;
            SetVisible(true);
            EnterMenuInputState();

            if (LobbySession.ConsumeLaunchRequest())
                StartMatch();
        }

        public bool StartMatch()
        {
            if (MatchRequested || sequence == null)
            {
                if (sequence == null)
                {
                    Debug.LogError(
                        "[BattleRoyaleStartMenu] No se encontro MatchStartController en la escena.",
                        this
                    );
                }
                return false;
            }

            MatchRequested = true;

            ExitMenuInputState();

            if (!sequence.BeginSequence())
            {
                MatchRequested = false;
                EnterMenuInputState();
                return false;
            }

            SetVisible(false);
            if (playerCamera != null)
                playerCamera.EnterAirplaneView(airplaneCameraDistance);

            return true;
        }

        public void StartFreeroam()
        {
            if (MatchRequested)
                return;

            MatchRequested = true;
            ExitMenuInputState();
            SetVisible(false);

            if (input != null)
                input.transform.position = freeroamSpawnPoint;
        }

        private void EnterMenuInputState()
        {
            if (input != null)
                input.SetUiBlocked(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void MaintainMenuInputState()
        {
            if (input != null && !input.UiBlocked)
                input.SetUiBlocked(true);

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;

            if (!Cursor.visible)
                Cursor.visible = true;
        }

        private void ExitMenuInputState()
        {
            if (input != null)
                input.SetUiBlocked(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private bool HasRequiredReferences()
        {
            return viewRoot != null && startMatchButton != null && freeroamButton != null;
        }

        private void HookButtons()
        {
            if (startMatchButton != null)
            {
                startMatchButton.onClick.RemoveListener(HandleStartMatchClicked);
                startMatchButton.onClick.AddListener(HandleStartMatchClicked);
            }

            if (freeroamButton != null)
            {
                freeroamButton.onClick.RemoveListener(StartFreeroam);
                freeroamButton.onClick.AddListener(StartFreeroam);
            }
        }

        private void UnhookButtons()
        {
            if (startMatchButton != null)
                startMatchButton.onClick.RemoveListener(HandleStartMatchClicked);
            if (freeroamButton != null)
                freeroamButton.onClick.RemoveListener(StartFreeroam);
        }

        private void HandleStartMatchClicked()
        {
            StartMatch();
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (viewRoot != null)
                viewRoot.SetActive(visible);
        }

        private void HandlePlayerJumped()
        {
            if (playerCamera != null)
                playerCamera.EnterAirDropView();
        }

        private void HandleSequenceCompleted()
        {
            if (playerCamera != null)
                playerCamera.ExitAirplaneView();
        }

        private void Subscribe()
        {
            if (sequence == null)
                return;

            sequence.PlayerJumped -= HandlePlayerJumped;
            sequence.PlayerJumped += HandlePlayerJumped;
            sequence.SequenceCompleted -= HandleSequenceCompleted;
            sequence.SequenceCompleted += HandleSequenceCompleted;
        }

        private void Unsubscribe()
        {
            if (sequence == null)
                return;

            sequence.PlayerJumped -= HandlePlayerJumped;
            sequence.SequenceCompleted -= HandleSequenceCompleted;
        }

        private void OnDestroy()
        {
            UnhookButtons();
            Unsubscribe();
        }
    }
}
