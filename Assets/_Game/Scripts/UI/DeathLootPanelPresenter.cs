using System;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.UI
{
    public sealed class DeathLootPanelPresenter :
        MonoBehaviour
    {
        [SerializeField]
        private float maximumOpenDistance = 4f;

        private DeathLootContainer _container;
        private GameObject _interactor;
        private InventoryComponent _playerInventory;
        private PlayerInputReader _input;
        private Health _health;
        private Vector2 _scrollPosition;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _mutedStyle;
        private GUIStyle _selectedBgStyle;

        // Navegación teclado
        private int _selectedIndex;
        private bool _minimized;

        // Auto-proximidad
        private GameObject _bindInteractor;
        private float _nextScanTime;
        private DeathLootContainer _suppressedContainer;

        public bool IsOpen =>
            _container != null &&
            _interactor != null &&
            _playerInventory != null;

        public DeathLootContainer OpenedContainer =>
            _container;

        // ---------------------------------------------------------------

        public void Bind(GameObject interactor)
        {
            _bindInteractor = interactor;
        }

        public static DeathLootPanelPresenter
            OpenOrCreate(
                DeathLootContainer container,
                GameObject interactor
            )
        {
            DeathLootPanelPresenter presenter =
                FindFirstObjectByType<
                    DeathLootPanelPresenter
                >();

            if (presenter == null)
            {
                presenter =
                    new GameObject(
                        "DeathLootPanelPresenter"
                    ).AddComponent<
                        DeathLootPanelPresenter
                    >();
            }

            presenter.Open(container, interactor);

            return presenter;
        }

        public bool Open(
            DeathLootContainer container,
            GameObject interactor
        )
        {
            if (
                container == null ||
                interactor == null ||
                !container.CanInteract(interactor)
            )
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            // No reabrir si ya es la misma caja
            if (_container == container)
            {
                _minimized = false;
                return true;
            }

            Close();

            _container = container;
            _interactor = interactor;
            _playerInventory = inventory;
            _input = interactor.GetComponent<PlayerInputReader>();
            _health = interactor.GetComponent<Health>();
            _scrollPosition = Vector2.zero;
            _selectedIndex = 0;
            _minimized = false;

            if (_input != null) _input.WeaponScrollBlocked = true;

            return true;
        }

        public void Close()
        {
            if (_input != null) _input.WeaponScrollBlocked = false;

            _container = null;
            _interactor = null;
            _playerInventory = null;
            _input = null;
            _health = null;
            _scrollPosition = Vector2.zero;
            _selectedIndex = 0;
            _minimized = false;
        }

        private void ManualClose()
        {
            _suppressedContainer = _container;
            Close();
        }

        private void OnDisable()
        {
            Close();
        }

        private void Update()
        {
            // Auto-scan de proximidad cuando el panel está cerrado
            if (!IsOpen)
            {
                AutoScanProximity();
                return;
            }

            if (
                _health != null &&
                !_health.IsAlive
            )
            {
                Close();
                return;
            }

            if (
                _container == null ||
                _container.ItemCount <= 0 ||
                Vector3.Distance(
                    _interactor.transform.position,
                    _container.transform.position
                ) > maximumOpenDistance
            )
            {
                Close();
                return;
            }

            if (Keyboard.current == null) return;

            // ESC cierra completamente
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ManualClose();
                return;
            }

            // Tab minimiza / restaura
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                _minimized = !_minimized;
                return;
            }

            if (_minimized) return;

            InventoryStack[] stacks = SnapshotStacks();
            if (stacks.Length == 0) return;

            // Navegación con rueda del ratón
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0f)
                {
                    _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                    ScrollToSelected(stacks.Length);
                }
                else if (scroll < 0f)
                {
                    _selectedIndex = Mathf.Min(stacks.Length - 1, _selectedIndex + 1);
                    ScrollToSelected(stacks.Length);
                }
            }

            // F recoge el ítem seleccionado
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (_selectedIndex >= 0 && _selectedIndex < stacks.Length)
                {
                    InventoryStack sel = stacks[_selectedIndex];
                    if (sel != null && sel.item != null &&
                        _playerInventory.GetMaxAddableAmount(sel.item) > 0)
                    {
                        _container.TryLoot(sel.item, sel.amount, _playerInventory);
                        stacks = SnapshotStacks();
                        if (_selectedIndex >= stacks.Length)
                            _selectedIndex = Mathf.Max(0, stacks.Length - 1);
                        CloseIfEmpty();
                    }
                }
            }
        }

        private void AutoScanProximity()
        {
            if (_bindInteractor == null) return;
            if (Time.time < _nextScanTime) return;
            _nextScanTime = Time.time + 0.25f;

            // Limpiar supresión si el jugador se alejó suficiente de esa caja
            if (_suppressedContainer != null)
            {
                float suppressDist = Vector3.Distance(
                    _bindInteractor.transform.position,
                    _suppressedContainer.transform.position
                );
                if (suppressDist > maximumOpenDistance * 1.5f)
                    _suppressedContainer = null;
            }

            DeathLootContainer[] containers =
                FindObjectsByType<DeathLootContainer>(FindObjectsSortMode.None);

            float best = float.MaxValue;
            DeathLootContainer nearest = null;

            foreach (DeathLootContainer c in containers)
            {
                if (c == null || c.IsEmpty) continue;
                if (c == _suppressedContainer) continue;
                if (!c.CanInteract(_bindInteractor)) continue;
                float d = Vector3.Distance(
                    _bindInteractor.transform.position,
                    c.transform.position
                );
                if (d <= maximumOpenDistance && d < best)
                {
                    best = d;
                    nearest = c;
                }
            }

            if (nearest != null)
            {
                Open(nearest, _bindInteractor);
            }
        }

        private void ScrollToSelected(int count)
        {
            if (count <= 0) return;
            float rowH = 58f;
            float targetY = _selectedIndex * rowH;
            _scrollPosition.y = Mathf.Clamp(
                targetY - 100f,
                0f,
                Mathf.Max(0f, count * rowH - 200f)
            );
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();

            if (_minimized)
            {
                DrawMinimizedBar();
                return;
            }

            float width =
                Mathf.Min(720f, Screen.width - 40f);

            float height =
                Mathf.Min(620f, Screen.height - 40f);

            Rect panelRect =
                new Rect(
                    (Screen.width - width) * 0.5f,
                    (Screen.height - height) * 0.5f,
                    width,
                    height
                );

            GUI.Box(panelRect, GUIContent.none);

            Rect contentRect =
                new Rect(
                    panelRect.x + 24f,
                    panelRect.y + 18f,
                    panelRect.width - 48f,
                    panelRect.height - 36f
                );

            DrawHeader(contentRect);
            DrawContents(contentRect);
        }

        private void DrawMinimizedBar()
        {
            float barW = 340f;
            float barH = 36f;
            Rect bar = new Rect(
                (Screen.width - barW) * 0.5f,
                Screen.height * 0.5f - barH * 0.5f,
                barW,
                barH
            );
            GUI.Box(bar, GUIContent.none);
            GUI.Label(
                new Rect(bar.x + 14f, bar.y + 6f, bar.width - 120f, 24f),
                _container.DisplayName + $"  ({_container.ItemCount} obj.)",
                _headerStyle
            );
            if (GUI.Button(
                    new Rect(bar.xMax - 110f, bar.y + 4f, 56f, 28f),
                    "Abrir"))
            {
                _minimized = false;
                GUIUtility.ExitGUI();
            }
            if (GUI.Button(
                    new Rect(bar.xMax - 50f, bar.y + 4f, 46f, 28f),
                    "✕"))
            {
                ManualClose();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawHeader(Rect contentRect)
        {
            GUI.Label(
                new Rect(
                    contentRect.x,
                    contentRect.y,
                    contentRect.width - 200f,
                    38f
                ),
                _container.DisplayName,
                _titleStyle
            );

            if (GUI.Button(
                    new Rect(
                        contentRect.xMax - 180f,
                        contentRect.y,
                        86f,
                        32f
                    ),
                    "Minimizar"))
            {
                _minimized = true;
                GUIUtility.ExitGUI();
            }

            if (GUI.Button(
                    new Rect(
                        contentRect.xMax - 88f,
                        contentRect.y,
                        88f,
                        32f
                    ),
                    "Cerrar"))
            {
                ManualClose();
                GUIUtility.ExitGUI();
            }

            GUI.Label(
                new Rect(
                    contentRect.x,
                    contentRect.y + 42f,
                    contentRect.width,
                    28f
                ),
                $"Caja: {_container.ItemCount} objetos   |   " +
                $"Mochila: {_playerInventory.UsedCapacity:0.0} / " +
                $"{_playerInventory.Capacity:0.0}   |   " +
                "↑↓ Navegar · F Recoger · Tab Minimizar · Esc Cerrar",
                _headerStyle
            );
        }

        private void DrawContents(Rect contentRect)
        {
            Rect listRect =
                new Rect(
                    contentRect.x,
                    contentRect.y + 82f,
                    contentRect.width,
                    contentRect.height - 142f
                );

            InventoryStack[] stacks =
                SnapshotStacks();

            float contentHeight =
                Mathf.Max(
                    listRect.height,
                    stacks.Length * 58f
                );

            _scrollPosition =
                GUI.BeginScrollView(
                    listRect,
                    _scrollPosition,
                    new Rect(
                        0f,
                        0f,
                        listRect.width - 18f,
                        contentHeight
                    )
                );

            for (int i = 0; i < stacks.Length; i++)
            {
                DrawStackRow(
                    stacks[i],
                    i * 58f,
                    listRect.width - 22f,
                    i == _selectedIndex
                );
            }

            GUI.EndScrollView();

            bool hasLootableStack =
                HasCurrentlyLootableStack(stacks);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = hasLootableStack;

            if (
                GUI.Button(
                    new Rect(
                        contentRect.x,
                        contentRect.yMax - 46f,
                        contentRect.width,
                        40f
                    ),
                    hasLootableStack
                        ? "Recoger todo lo posible"
                        : "Sin capacidad disponible"
                )
            )
            {
                _container.LootAllPossible(
                    _playerInventory
                );

                CloseIfEmpty();
                GUIUtility.ExitGUI();
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawStackRow(
            InventoryStack stack,
            float y,
            float width,
            bool selected
        )
        {
            if (
                stack == null ||
                stack.item == null ||
                stack.amount <= 0
            )
            {
                return;
            }

            InventoryItemDefinition item =
                stack.item;

            int movable =
                Mathf.Min(
                    stack.amount,
                    _playerInventory
                        .GetMaxAddableAmount(item)
                );

            // Fondo de selección amarillo
            if (selected)
            {
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0.88f, 0.1f, 0.28f);
                GUI.Box(new Rect(0f, y, width, 50f), GUIContent.none, _selectedBgStyle);
                GUI.color = prev;
            }

            GUI.Box(
                new Rect(0f, y, width, 50f),
                GUIContent.none
            );

            // Icono
            float iconSize = 42f;
            float textStartX = 14f;

            if (item.icon != null)
            {
                GUI.DrawTexture(
                    new Rect(8f, y + 4f, iconSize, iconSize),
                    item.icon.texture,
                    ScaleMode.ScaleToFit
                );
                textStartX = iconSize + 16f;
            }

            GUI.Label(
                new Rect(textStartX, y + 5f, width - textStartX - 205f, 24f),
                $"{item.displayName}  x{stack.amount}",
                _itemStyle
            );

            GUI.Label(
                new Rect(textStartX, y + 27f, width - textStartX - 205f, 18f),
                $"{item.itemType} · {item.weight:0.##} por unidad",
                _mutedStyle
            );

            bool previousEnabled = GUI.enabled;
            GUI.enabled = movable > 0;

            if (
                GUI.Button(
                    new Rect(
                        width - 180f,
                        y + 8f,
                        166f,
                        34f
                    ),
                    movable > 0
                        ? $"Recoger {movable}"
                        : "Sin espacio"
                )
            )
            {
                _container.TryLoot(
                    item,
                    stack.amount,
                    _playerInventory
                );

                CloseIfEmpty();
                GUIUtility.ExitGUI();
            }

            GUI.enabled = previousEnabled;
        }

        private InventoryStack[] SnapshotStacks()
        {
            if (
                _container == null ||
                _container.StoredInventory == null
            )
            {
                return Array.Empty<InventoryStack>();
            }

            int count =
                _container.StoredInventory.Stacks.Count;

            InventoryStack[] snapshot =
                new InventoryStack[count];

            for (int i = 0; i < count; i++)
            {
                snapshot[i] =
                    _container.StoredInventory.Stacks[i];
            }

            return snapshot;
        }

        private bool HasCurrentlyLootableStack(
            InventoryStack[] stacks
        )
        {
            foreach (InventoryStack stack in stacks)
            {
                if (
                    stack == null ||
                    stack.item == null ||
                    stack.amount <= 0
                )
                {
                    continue;
                }

                if (
                    _playerInventory
                        .GetMaxAddableAmount(
                            stack.item
                        ) > 0
                )
                {
                    return true;
                }
            }

            return false;
        }

        private void CloseIfEmpty()
        {
            if (
                _container == null ||
                _container.ItemCount <= 0
            )
            {
                Close();
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 25,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft
                    };
            }

            if (_headerStyle == null)
            {
                _headerStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13
                    };
            }

            if (_itemStyle == null)
            {
                _itemStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 17,
                        fontStyle = FontStyle.Bold
                    };
            }

            if (_mutedStyle == null)
            {
                _mutedStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        normal =
                        {
                            textColor =
                                new Color(
                                    0.72f,
                                    0.72f,
                                    0.72f
                                )
                        }
                    };
            }

            if (_selectedBgStyle == null)
            {
                _selectedBgStyle = new GUIStyle(GUI.skin.box);
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(1f, 0.88f, 0.1f, 0.35f));
                tex.Apply();
                _selectedBgStyle.normal.background = tex;
            }
        }
    }
}
