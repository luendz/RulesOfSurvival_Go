using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyHudView : MonoBehaviour
    {
        [Serializable]
        private struct MenuPanelReference
        {
            public LobbyMenuId menu;
            public GameObject panel;
        }

        [Serializable]
        private struct MenuButtonReference
        {
            public LobbyMenuId menu;
            public Button button;
        }

        [Header("Root")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private LobbyCharacterRotator characterRotator;

        [Header("Datos visibles")]
        [SerializeField] private Text profileText;
        [SerializeField] private Text currencyText;
        [SerializeField] private Text mapText;
        [SerializeField] private Text modeText;

        [Header("Acciones")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button soloButton;
        [SerializeField] private Button duoButton;
        [SerializeField] private Button squadButton;
        [SerializeField] private Button[] backButtons = Array.Empty<Button>();

        [Header("Navegación")]
        [SerializeField] private MenuPanelReference[] menuPanels = Array.Empty<MenuPanelReference>();
        [SerializeField] private MenuButtonReference[] menuButtons = Array.Empty<MenuButtonReference>();

        public Canvas Canvas => canvas;
        public LobbyCharacterRotator CharacterRotator => characterRotator;
        public Text ModeText => modeText;

        public void CaptureReferences()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            characterRotator = FindComponentByName<LobbyCharacterRotator>("Character Drag Area");

            Button profileButton = FindButton("Profile");
            Button modeButton = FindButton("Mode");

            profileText = profileButton != null
                ? profileButton.GetComponentInChildren<Text>(true)
                : null;
            currencyText = FindText("Currency");
            mapText = FindText("Map");
            modeText = modeButton != null
                ? modeButton.GetComponentInChildren<Text>(true)
                : null;

            playButton = FindButton("Play");
            soloButton = FindButton("Solo");
            duoButton = FindButton("Duo");
            squadButton = FindButton("Squad");

            List<Button> backs = new List<Button>();
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (Button button in allButtons)
            {
                if (button != null && button.name == "Back")
                {
                    backs.Add(button);
                }
            }
            backButtons = backs.ToArray();

            menuPanels = new[]
            {
                Panel(LobbyMenuId.Character),
                Panel(LobbyMenuId.Inventory),
                Panel(LobbyMenuId.Weapons),
                Panel(LobbyMenuId.Store),
                Panel(LobbyMenuId.Events),
                Panel(LobbyMenuId.Missions),
                Panel(LobbyMenuId.Friends),
                Panel(LobbyMenuId.Settings),
                Panel(LobbyMenuId.PlayMode)
            };

            menuButtons = new[]
            {
                MenuButton(LobbyMenuId.Character, "Profile"),
                MenuButton(LobbyMenuId.Character, "PERSONAJE"),
                MenuButton(LobbyMenuId.Inventory, "INVENTARIO"),
                MenuButton(LobbyMenuId.Weapons, "ARMAS"),
                MenuButton(LobbyMenuId.Store, "TIENDA"),
                MenuButton(LobbyMenuId.Events, "Events"),
                MenuButton(LobbyMenuId.Missions, "Missions"),
                MenuButton(LobbyMenuId.Friends, "Friends"),
                MenuButton(LobbyMenuId.Settings, "Settings"),
                MenuButton(LobbyMenuId.PlayMode, "Mode")
            };
        }

        public void ApplyRuntimeData(
            string playerName,
            int playerLevel,
            int gold,
            int diamonds,
            string mapName,
            LobbyMatchMode selectedMode
        )
        {
            if (profileText != null)
            {
                string safeName = string.IsNullOrWhiteSpace(playerName)
                    ? "JUGADOR"
                    : playerName.ToUpperInvariant();
                profileText.text = $"{safeName}   |   NIVEL {Mathf.Max(1, playerLevel)}";
            }

            if (currencyText != null)
            {
                currencyText.text =
                    $"ORO {Mathf.Max(0, gold)}   |   DIAMANTES {Mathf.Max(0, diamonds)}";
            }

            if (mapText != null)
            {
                string safeMap = string.IsNullOrWhiteSpace(mapName)
                    ? "SIN MAPA"
                    : mapName.ToUpperInvariant();
                mapText.text = $"MAPA: {safeMap}";
            }

            if (modeText != null)
            {
                modeText.text = selectedMode.ToString().ToUpperInvariant();
            }
        }

        public void BindRuntime(
            LobbyNavigationController navigation,
            Action<LobbyMatchMode> selectMode,
            Action play
        )
        {
            if (navigation == null)
            {
                return;
            }

            foreach (MenuPanelReference reference in menuPanels)
            {
                if (reference.panel != null)
                {
                    navigation.RegisterPanel(reference.menu, reference.panel);
                }
            }

            foreach (MenuButtonReference reference in menuButtons)
            {
                if (reference.button == null)
                {
                    continue;
                }

                LobbyMenuId menu = reference.menu;
                reference.button.onClick.AddListener(() => navigation.Open(menu));
            }

            foreach (Button backButton in backButtons)
            {
                if (backButton != null)
                {
                    backButton.onClick.AddListener(navigation.Back);
                }
            }

            if (soloButton != null)
            {
                soloButton.onClick.AddListener(() => selectMode?.Invoke(LobbyMatchMode.Solo));
            }

            if (duoButton != null)
            {
                duoButton.onClick.AddListener(() => selectMode?.Invoke(LobbyMatchMode.Duo));
            }

            if (squadButton != null)
            {
                squadButton.onClick.AddListener(() => selectMode?.Invoke(LobbyMatchMode.Squad));
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(() => play?.Invoke());
            }
        }

        private MenuPanelReference Panel(LobbyMenuId menu)
        {
            Transform panel = FindDescendant($"Menu {menu}");
            return new MenuPanelReference
            {
                menu = menu,
                panel = panel != null ? panel.gameObject : null
            };
        }

        private MenuButtonReference MenuButton(LobbyMenuId menu, string objectName)
        {
            return new MenuButtonReference
            {
                menu = menu,
                button = FindButton(objectName)
            };
        }

        private Button FindButton(string objectName)
        {
            Transform child = FindDescendant(objectName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private Text FindText(string objectName)
        {
            Transform child = FindDescendant(objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private T FindComponentByName<T>(string objectName) where T : Component
        {
            Transform child = FindDescendant(objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private Transform FindDescendant(string objectName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
