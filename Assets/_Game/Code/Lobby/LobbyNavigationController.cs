using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROS.Game.Lobby
{
    public sealed class LobbyNavigationController : MonoBehaviour
    {
        private readonly Dictionary<LobbyMenuId, GameObject> _panels = new();
        private readonly Stack<LobbyMenuId> _history = new();

        public LobbyMenuId CurrentMenu { get; private set; } = LobbyMenuId.None;

        public event Action<LobbyMenuId> MenuChanged;

        public void RegisterPanel(
            LobbyMenuId menu,
            GameObject panel
        )
        {
            if (menu == LobbyMenuId.None || panel == null)
            {
                return;
            }

            _panels[menu] = panel;
            panel.SetActive(false);
        }

        public void Open(LobbyMenuId menu)
        {
            if (menu == LobbyMenuId.None || !_panels.ContainsKey(menu))
            {
                CloseAll();
                return;
            }

            if (CurrentMenu == menu)
            {
                return;
            }

            if (CurrentMenu != LobbyMenuId.None)
            {
                _history.Push(CurrentMenu);
            }

            ShowOnly(menu);
        }

        public void Back()
        {
            while (_history.Count > 0)
            {
                LobbyMenuId previous = _history.Pop();
                if (_panels.ContainsKey(previous))
                {
                    ShowOnly(previous, false);
                    return;
                }
            }

            CloseAll();
        }

        public void CloseAll()
        {
            _history.Clear();

            foreach (GameObject panel in _panels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }

            CurrentMenu = LobbyMenuId.None;
            MenuChanged?.Invoke(CurrentMenu);
        }

        private void ShowOnly(
            LobbyMenuId menu,
            bool keepHistory = true
        )
        {
            foreach (KeyValuePair<LobbyMenuId, GameObject> pair in _panels)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(pair.Key == menu);
                }
            }

            if (!keepHistory && CurrentMenu != LobbyMenuId.None)
            {
                // Back() ya administra el historial. No volver a insertar el menú actual.
            }

            CurrentMenu = menu;
            MenuChanged?.Invoke(CurrentMenu);
        }
    }
}
