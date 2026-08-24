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
        private const string ViewResourcePath = "EditorFirst/BattleRoyaleStartMenuView";

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
            EnsureEditableView();
            HookButtons();
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

            EnsureEditableView();
            HookButtons();
            MatchRequested = false;
            SetVisible(true);

            if (input != null)
                input.SetUiBlocked(true);

            if (LobbySession.ConsumeLaunchRequest())
                StartMatch();
        }

        public bool StartMatch()
        {
            if (MatchRequested || sequence == null)
                return false;

            MatchRequested = true;

            if (input != null)
                input.SetUiBlocked(false);

            if (!sequence.BeginSequence())
            {
                MatchRequested = false;
                if (input != null)
                    input.SetUiBlocked(true);
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
            SetVisible(false);

            if (input != null)
            {
                input.SetUiBlocked(false);
                input.transform.position = freeroamSpawnPoint;
            }
        }

        private void EnsureEditableView()
        {
            if (viewRoot == null)
            {
                GameObject prefab = Resources.Load<GameObject>(ViewResourcePath);
                if (prefab != null)
                {
                    viewRoot = Instantiate(prefab, transform, false);
                    viewRoot.name = "BattleRoyaleStartMenuView";
                }
                else
                {
                    Debug.LogError(
                        "No existe el prefab editable EditorFirst/BattleRoyaleStartMenuView. " +
                        "Abre el proyecto en Unity para materializar los assets editor-first.",
                        this
                    );
                    return;
                }
            }

            if (startMatchButton == null)
                startMatchButton = FindNamed<Button>(viewRoot.transform, "StartMatchButton");
            if (freeroamButton == null)
                freeroamButton = FindNamed<Button>(viewRoot.transform, "FreeroamButton");
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

        private static T FindNamed<T>(Transform root, string objectName)
            where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
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
