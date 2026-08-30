using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    /// <summary>
    /// Enlaza el lobby ya materializado en la escena. No crea cámaras, luces,
    /// personaje, EventSystem ni UI durante Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbySceneBootstrap : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private LobbyHudView authoredHud;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private LobbyNavigationController _navigation;
        [SerializeField] private LobbyCharacterRotator _rotator;
        [SerializeField] private LobbyCameraController _cameraController;
        [SerializeField] private GameObject _character;

        [Header("Flow")]
        [SerializeField] private string battleRoyaleSceneName = "08_EchoValley";
        [SerializeField] private string mapName = "Ghillie Island";

        [Header("Profile")]
        [SerializeField] private string playerName = "Jugador";
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private int gold = 1250;
        [SerializeField] private int diamonds = 300;

        private Text _modeText;
        private LobbyMatchMode _selectedMode = LobbyMatchMode.Solo;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    $"[{nameof(LobbySceneBootstrap)}] El lobby no está completamente configurado en la escena.",
                    this);
                enabled = false;
                return;
            }

            LobbySession.CancelLaunchRequest();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _modeText = authoredHud.ModeText;
            _rotator.Configure(_character.transform);
            _cameraController.Configure(_character.transform);

            authoredHud.ApplyRuntimeData(
                playerName,
                playerLevel,
                gold,
                diamonds,
                mapName,
                _selectedMode);
            authoredHud.BindRuntime(_navigation, SelectMode, StartBattleRoyale);

            _navigation.MenuChanged -= HandleMenuChanged;
            _navigation.MenuChanged += HandleMenuChanged;
            _navigation.CloseAll();
            HandleMenuChanged(LobbyMenuId.None);
        }

        private void OnDestroy()
        {
            if (_navigation != null)
                _navigation.MenuChanged -= HandleMenuChanged;
        }

        private bool HasRequiredReferences()
        {
            return authoredHud != null && authoredHud.Canvas != null &&
                   _canvas != null && _navigation != null && _rotator != null &&
                   _cameraController != null && _character != null;
        }

        private void SelectMode(LobbyMatchMode mode)
        {
            _selectedMode = mode;
            if (_modeText != null)
                _modeText.text = mode.ToString().ToUpperInvariant();
            _navigation.CloseAll();
        }

        private void StartBattleRoyale()
        {
            if (string.IsNullOrWhiteSpace(battleRoyaleSceneName) ||
                !Application.CanStreamedLevelBeLoaded(battleRoyaleSceneName))
            {
                Debug.LogError(
                    $"La escena '{battleRoyaleSceneName}' no está disponible en Build Settings.",
                    this);
                return;
            }

            LobbySession.RequestMatch(_selectedMode, mapName);
            SceneManager.LoadScene(battleRoyaleSceneName);
        }

        private void HandleMenuChanged(LobbyMenuId menu)
        {
            LobbyCameraPreset preset = menu == LobbyMenuId.Weapons
                ? LobbyCameraPreset.UpperBody
                : LobbyCameraPreset.FullBody;
            _cameraController.SetPreset(preset);
        }
    }
}
