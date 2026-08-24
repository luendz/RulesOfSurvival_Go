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
            ResolveRuntimeReferences();
            EnsureEditableView();
            HookButtons();
            Subscribe();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            EnsureEditableView();
            HookButtons();
            Subscribe();
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
            ResolveRuntimeReferences();
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
            ResolveRuntimeReferences();

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
            ResolveRuntimeReferences();

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

        private void ResolveRuntimeReferences()
        {
            if (sequence == null)
                sequence = FindFirstObjectByType<MatchStartController>(FindObjectsInactive.Include);

            if (input == null)
                input = FindLocalPlayerInput();

            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] readers =
                FindObjectsByType<PlayerInputReader>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < readers.Length; i++)
            {
                PlayerInputReader reader = readers[i];
                if (reader == null || !reader.gameObject.scene.IsValid())
                    continue;

                if (reader.enabled && reader.gameObject.activeInHierarchy)
                    return reader;
            }

            return readers.Length > 0 ? readers[0] : null;
        }

        private void EnsureEditableView()
        {
            if (viewRoot == null)
            {
                // Si el controlador esta colocado directamente en el Canvas fisico
                // Editor First, ese mismo objeto ES la vista y no debe clonarse.
                if (GetComponent<Canvas>() != null &&
                    FindNamed<Button>(transform, "StartMatchButton") != null)
                {
                    viewRoot = gameObject;
                }
                else
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
            if (root == null)
                return null;

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
