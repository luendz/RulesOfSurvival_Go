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

        public bool IsOpen =>
            _container != null &&
            _interactor != null &&
            _playerInventory != null;

        public DeathLootContainer OpenedContainer =>
            _container;

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

            Close();

            _container = container;
            _interactor = interactor;
            _playerInventory = inventory;
            _input =
                interactor.GetComponent<
                    PlayerInputReader
                >();
            _health =
                interactor.GetComponent<Health>();
            _scrollPosition = Vector2.zero;

            if (_input != null)
            {
                _input.SetUiBlocked(true);
            }

            return true;
        }

        public void Close()
        {
            if (_input != null)
            {
                _input.SetUiBlocked(false);
            }

            _container = null;
            _interactor = null;
            _playerInventory = null;
            _input = null;
            _health = null;
            _scrollPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            Close();
        }

        private void Update()
        {
            if (!IsOpen)
            {
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

            if (
                Keyboard.current != null &&
                Keyboard.current.escapeKey
                    .wasPressedThisFrame
            )
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();

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

        private void DrawHeader(Rect contentRect)
        {
            GUI.Label(
                new Rect(
                    contentRect.x,
                    contentRect.y,
                    contentRect.width - 90f,
                    38f
                ),
                _container.DisplayName,
                _titleStyle
            );

            if (
                GUI.Button(
                    new Rect(
                        contentRect.xMax - 80f,
                        contentRect.y,
                        80f,
                        32f
                    ),
                    "Cerrar"
                )
            )
            {
                Close();
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
                $"{_playerInventory.Capacity:0.0}",
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
                    listRect.width - 22f
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
            float width
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

            GUI.Box(
                new Rect(0f, y, width, 50f),
                GUIContent.none
            );

            GUI.Label(
                new Rect(14f, y + 5f, width - 205f, 24f),
                $"{item.displayName}  x{stack.amount}",
                _itemStyle
            );

            GUI.Label(
                new Rect(14f, y + 27f, width - 205f, 18f),
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
                        fontSize = 16
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
        }
    }
}
