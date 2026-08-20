using ROS.Game.Interaction;
using ROS.Game.Loot;
using UnityEngine;

namespace ROS.Game.UI
{
    public sealed class NearbyLootPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] [Min(1)] private int maximumRows = 6;

        private GUIStyle _titleStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _currentStyle;

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = GetComponent<PlayerInteractor>();
            }
        }

        private void OnGUI()
        {
            if (interactor == null || interactor.Nearby.Count == 0)
            {
                return;
            }

            EnsureStyles();

            int rows = Mathf.Min(maximumRows, interactor.Nearby.Count);
            float width = 330f;
            float height = 44f + rows * 31f;
            Rect panel = new Rect(
                Screen.width - width - 20f,
                92f,
                width,
                height
            );

            GUI.Box(panel, GUIContent.none);
            GUI.Label(
                new Rect(panel.x + 14f, panel.y + 8f, width - 28f, 28f),
                "OBJETOS CERCANOS",
                _titleStyle
            );

            for (int i = 0; i < rows; i++)
            {
                IInteractable candidate = interactor.Nearby[i];
                if (candidate == null)
                {
                    continue;
                }

                bool current = ReferenceEquals(candidate, interactor.Current);
                bool canInteract =
                    candidate.CanInteract(interactor.gameObject);
                string prefix =
                    current && canInteract
                        ? "[F] "
                        : "    ";
                string label = candidate.InteractionLabel;

                if (candidate is LootPickup pickup &&
                    pickup.IsBlockedByInventoryCapacity(gameObject))
                {
                    label += " (sin espacio)";
                }

                if (candidate is LootPickup levelPickup &&
                    levelPickup.IsBlockedByEquipmentLevel(
                        interactor.gameObject
                    ))
                {
                    label += " (nivel igual o inferior)";
                }

                GUI.Label(
                    new Rect(
                        panel.x + 14f,
                        panel.y + 39f + i * 31f,
                        width - 28f,
                        27f
                    ),
                    prefix + label,
                    current ? _currentStyle : _rowStyle
                );
            }
        }

        private void EnsureStyles()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            _rowStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };

            _currentStyle ??= new GUIStyle(_rowStyle)
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color(1f, 0.85f, 0.25f)
                }
            };
        }
    }
}
