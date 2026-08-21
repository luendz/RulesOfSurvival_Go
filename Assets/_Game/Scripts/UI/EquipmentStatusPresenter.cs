using ROS.Game.Combat;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Muestra en el HUD el nivel de casco, chaleco y mochila equipados.
    /// Se ancla en la esquina inferior derecha junto a la barra de vida.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentStatusPresenter : MonoBehaviour
    {
        private InventoryComponent  _inventory;
        private ProtectiveEquipment _protection;

        private GameObject _root;
        private Text _helmetLabel;
        private Text _vestLabel;
        private Text _backpackLabel;

        // ---------------------------------------------------------------

        public void Bind(InventoryComponent inventory, ProtectiveEquipment protection)
        {
            _inventory  = inventory;
            _protection = protection;

            if (_inventory  != null) _inventory.Changed         += Refresh;
            if (_protection != null) _protection.Changed        += Refresh;

            BuildUI();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory  != null) _inventory.Changed  -= Refresh;
            if (_protection != null) _protection.Changed -= Refresh;

            if (_root != null) Destroy(_root);
        }

        // ---------------------------------------------------------------

        private void Refresh()
        {
            if (_protection != null)
            {
                _helmetLabel.text  = LevelText("CASCO", _protection.HelmetLevel);
                _vestLabel.text    = LevelText("CHALECO", _protection.VestLevel);
            }

            if (_inventory != null)
            {
                float cap = GetBackpackCapacity();
                _backpackLabel.text = cap > 0f
                    ? $"MOCHILA L{BackpackLevel(cap)}"
                    : "MOCHILA —";
            }
        }

        private static string LevelText(string name, ProtectionLevel level)
        {
            if (level == ProtectionLevel.None) return $"{name} —";
            int lv = level == ProtectionLevel.Level1 ? 1
                   : level == ProtectionLevel.Level2 ? 2
                   : 3;
            return $"{name} L{lv}";
        }

        private float GetBackpackCapacity()
        {
            if (_inventory == null) return 0f;
            foreach (InventoryStack s in _inventory.Stacks)
            {
                if (s.item != null && s.item.itemType == Core.ItemType.Backpack)
                    return s.item.backpackCapacity;
            }
            return 0f;
        }

        private static int BackpackLevel(float cap)
        {
            if (cap >= 150f) return 3;
            if (cap >= 100f) return 2;
            return 1;
        }

        // ---------------------------------------------------------------

        private void BuildUI()
        {
            _root = new GameObject("EquipmentStatusCanvas");

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Contenedor: esquina inferior derecha
            GameObject container = new GameObject("Container");
            container.transform.SetParent(_root.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(1f, 0f);
            cr.anchorMax        = new Vector2(1f, 0f);
            cr.pivot            = new Vector2(1f, 0f);
            cr.anchoredPosition = new Vector2(-18f, 18f);
            cr.sizeDelta        = new Vector2(160f, 56f);

            _helmetLabel    = MakeLabel(container.transform, 0, "CASCO —");
            _vestLabel      = MakeLabel(container.transform, 1, "CHALECO —");
            _backpackLabel  = MakeLabel(container.transform, 2, "MOCHILA —");
        }

        private static Text MakeLabel(Transform parent, int row, string defaultText)
        {
            GameObject go = new GameObject($"Label_{row}");
            go.transform.SetParent(parent, false);

            Text txt = go.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 13;
            txt.color     = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            txt.alignment = TextAnchor.MiddleRight;
            txt.text      = defaultText;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(0f, 18f);
            rt.anchoredPosition = new Vector2(0f, -row * 18f);

            return txt;
        }
    }
}
