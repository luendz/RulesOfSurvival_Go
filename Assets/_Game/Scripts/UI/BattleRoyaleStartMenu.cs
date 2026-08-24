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
            EnterMenuInputState();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            EnsureEditableView();
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
            ResolveRuntimeReferences();
            Subscribe();

            EnsureEditableView();
            HookButtons();
            MatchRequested = false;
            SetVisible(true);
            EnterMenuInputState();

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
            ResolveRuntimeReferences();

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
            ResolveRuntimeReferences();

            if (input != null)
                input.SetUiBlocked(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void MaintainMenuInputState()
        {
            ResolveRuntimeReferences();

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

        private void ResolveRuntimeReferences()
        {
            if (sequence == null)
                sequence = FindFirstObjectByType<MatchStartController>(FindObjectsInactive.Include);

            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);

            // La camara del jugador es la referencia mas fiable en escenas que
            // vienen del Lobby o contienen mas de un PlayerInputReader.
            if (playerCamera != null && playerCamera.Target != null)
            {
                PlayerInputReader cameraInput =
                    playerCamera.Target.GetComponent<PlayerInputReader>();

                if (cameraInput != null && !cameraInput.UsesExternalControl)
                    input = cameraInput;
            }

            if (input == null || input.UsesExternalControl)
                input = FindLocalPlayerInput();
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
                if (reader == null ||
                    !reader.gameObject.scene.IsValid() ||
                    reader.UsesExternalControl)
                {
                    continue;
                }

                if (reader.enabled && reader.gameObject.activeInHierarchy)
                    return reader;
            }

            for (int i = 0; i < readers.Length; i++)
            {
                PlayerInputReader reader = readers[i];
                if (reader != null &&
                    reader.gameObject.scene.IsValid() &&
                    !reader.UsesExternalControl)
                {
                    return reader;
                }
            }

            return null;
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
