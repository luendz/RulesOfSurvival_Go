using ROS.Game.Core;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class WeaponSlotsPresenter : MonoBehaviour
    {
        private const float SlotH     = 46f;
        private const float SlotW     = 210f;
        private const float Padding   = 4f;
        private const int   SlotCount = 3;

        private WeaponEquipmentController _equipment;
        private PlayerLootEquipment       _lootEquipment;
        private GameObject                _root;

        private readonly SlotRow[] _rows = new SlotRow[SlotCount];

        private struct SlotRow
        {
            public GameObject Root;
            public Image      BgImage;
            public Image      Border;
            public Image      IconImg;
            public Text       NameLabel;
            public Text       AmmoLabel;
        }

        private static readonly Color BgNormal  = new Color(0.06f, 0.06f, 0.06f, 0.82f);
        private static readonly Color BgActive  = new Color(0.14f, 0.11f, 0.01f, 0.90f);
        private static readonly Color BorderOn  = new Color(1.00f, 0.85f, 0.10f, 1.00f);
        private static readonly Color BorderOff = new Color(0.00f, 0.00f, 0.00f, 0.00f);
        private static readonly Color IconTint  = LootIconHelper.GetIconColor(ItemType.Weapon);

        // ----------------------------------------------------------------

        public void Bind(WeaponEquipmentController equipment, PlayerLootEquipment lootEquipment)
        {
            _equipment     = equipment;
            _lootEquipment = lootEquipment;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root);
        }

        private void Update()
        {
            if (_equipment == null) return;

            WeaponController[] slots =
            {
                _equipment.PrimarySlot1,
                _equipment.PrimarySlot2,
                _equipment.SidearmSlot,
            };

            int active = _equipment.EquippedSlot; // 1-based

            for (int i = 0; i < SlotCount; i++)
            {
                WeaponController w   = slots[i];
                bool             sel = (active == i + 1);
                SlotRow          row = _rows[i];

                if (w != null)
                {
                    string wName = w.Definition != null ? w.Definition.displayName : w.name;
                    row.NameLabel.text  = wName;
                    row.AmmoLabel.text  = $"{w.AmmoInMagazine} / {w.ReserveAmmo}";
                    row.NameLabel.color = sel
                        ? new Color(1f, 0.95f, 0.55f)
                        : new Color(0.9f, 0.9f, 0.9f, 0.9f);

                    Sprite icon = FindWeaponIcon(i + 1);
                    if (icon != null)
                    {
                        row.IconImg.sprite           = icon;
                        row.IconImg.color            = Color.white;
                        row.IconImg.preserveAspect   = true;
                    }
                    else
                    {
                        row.IconImg.sprite = null;
                        row.IconImg.color  = IconTint;
                    }
                    row.IconImg.enabled = true;
                }
                else
                {
                    row.NameLabel.text  = "—";
                    row.AmmoLabel.text  = string.Empty;
                    row.NameLabel.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                    row.IconImg.enabled = false;
                }

                row.BgImage.color = sel ? BgActive  : BgNormal;
                row.Border.color  = sel ? BorderOn  : BorderOff;
            }
        }

        private Sprite FindWeaponIcon(int slot)
        {
            return _lootEquipment?.GetWeaponItem(slot)?.icon;
        }

        // ----------------------------------------------------------------

        private void BuildUI()
        {
            _root = new GameObject("WeaponSlotsCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            float totalH = SlotCount * SlotH + (SlotCount - 1) * Padding;

            GameObject container = new GameObject("Container");
            container.transform.SetParent(_root.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(1f, 0f);
            cr.anchorMax        = new Vector2(1f, 0f);
            cr.pivot            = new Vector2(1f, 0f);
            cr.anchoredPosition = new Vector2(-14f, 110f);
            cr.sizeDelta        = new Vector2(SlotW, totalH);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < SlotCount; i++)
            {
                float y = (SlotCount - 1 - i) * (SlotH + Padding);
                _rows[i] = MakeSlotRow(container.transform, i + 1, y, font);
            }
        }

        private SlotRow MakeSlotRow(Transform parent, int slotNum, float y, Font font)
        {
            GameObject go = new GameObject($"Slot{slotNum}");
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta        = new Vector2(0f, SlotH);

            Image bg = go.AddComponent<Image>();
            bg.color = BgNormal;

            // Borde izquierdo
            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(go.transform, false);
            Image border = borderGo.AddComponent<Image>();
            border.color = BorderOff;
            RectTransform br = borderGo.GetComponent<RectTransform>();
            br.anchorMin        = Vector2.zero;
            br.anchorMax        = new Vector2(0f, 1f);
            br.sizeDelta        = new Vector2(4f, 0f);
            br.anchoredPosition = Vector2.zero;
            br.pivot            = Vector2.zero;

            // Icono del arma (36x36, margen izquierdo tras el borde)
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon = iconGo.AddComponent<Image>();
            icon.color          = IconTint;
            icon.preserveAspect = true;
            icon.enabled        = false;
            RectTransform ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin        = new Vector2(0f, 0.5f);
            ir.anchorMax        = new Vector2(0f, 0.5f);
            ir.pivot            = new Vector2(0f, 0.5f);
            ir.anchoredPosition = new Vector2(8f, 0f);
            ir.sizeDelta        = new Vector2(36f, 36f);

            // Numero de slot (pequeño, encima del icono)
            GameObject numGo = new GameObject("Num");
            numGo.transform.SetParent(go.transform, false);
            Text num = numGo.AddComponent<Text>();
            num.font      = font;
            num.fontSize  = 10;
            num.fontStyle = FontStyle.Bold;
            num.color     = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            num.alignment = TextAnchor.LowerLeft;
            num.text      = slotNum.ToString();
            RectTransform nr = numGo.GetComponent<RectTransform>();
            nr.anchorMin        = new Vector2(0f, 0f);
            nr.anchorMax        = new Vector2(0f, 0f);
            nr.pivot            = Vector2.zero;
            nr.anchoredPosition = new Vector2(8f, 2f);
            nr.sizeDelta        = new Vector2(14f, 14f);

            // Nombre del arma
            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            Text nameLabel = nameGo.AddComponent<Text>();
            nameLabel.font      = font;
            nameLabel.fontSize  = 14;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.color     = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            nameLabel.alignment = TextAnchor.MiddleLeft;
            nameLabel.text      = "—";
            RectTransform wr = nameGo.GetComponent<RectTransform>();
            wr.anchorMin = new Vector2(0f, 0f);
            wr.anchorMax = new Vector2(0.62f, 1f);
            wr.offsetMin = new Vector2(50f, 0f);
            wr.offsetMax = new Vector2(0f, 0f);

            // Municion
            GameObject ammoGo = new GameObject("Ammo");
            ammoGo.transform.SetParent(go.transform, false);
            Text ammoLabel = ammoGo.AddComponent<Text>();
            ammoLabel.font      = font;
            ammoLabel.fontSize  = 13;
            ammoLabel.color     = new Color(0.75f, 0.85f, 1f, 0.92f);
            ammoLabel.alignment = TextAnchor.MiddleRight;
            ammoLabel.text      = string.Empty;
            RectTransform ar = ammoGo.GetComponent<RectTransform>();
            ar.anchorMin = new Vector2(0.58f, 0f);
            ar.anchorMax = new Vector2(1f, 1f);
            ar.offsetMin = Vector2.zero;
            ar.offsetMax = new Vector2(-8f, 0f);

            return new SlotRow
            {
                Root       = go,
                BgImage    = bg,
                Border     = border,
                IconImg    = icon,
                NameLabel  = nameLabel,
                AmmoLabel  = ammoLabel,
            };
        }
    }
}
