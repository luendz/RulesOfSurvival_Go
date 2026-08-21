using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Muestra los items de curacion disponibles en el inventario
    /// con su cantidad, en la esquina inferior derecha encima de los slots
    /// de armas. El primero que usaria la tecla H se resalta.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuickConsumePresenter : MonoBehaviour
    {
        private const int   MaxSlots  = 3;
        private const float SlotW     = 68f;
        private const float SlotH     = 52f;
        private const float SlotGap   = 6f;

        private InventoryComponent   _inventory;
        private ConsumableController _consumable;
        private GameObject           _root;

        private readonly ConsumeSlot[] _slots = new ConsumeSlot[MaxSlots];

        private struct ConsumeSlot
        {
            public GameObject          Root;
            public Image               Bg;
            public Image               IconImg;
            public Text                NameLabel;
            public Text                CountLabel;
        }

        private static readonly Color BgNormal  = new Color(0.06f, 0.06f, 0.06f, 0.82f);
        private static readonly Color BgActive  = new Color(0.10f, 0.22f, 0.06f, 0.90f);

        // ----------------------------------------------------------------

        public void Bind(InventoryComponent inventory, ConsumableController consumable)
        {
            _inventory  = inventory;
            _consumable = consumable;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root);
        }

        private void Update()
        {
            if (_inventory == null) return;

            // Recopilar items de curacion unicos con cantidad total
            var entries = new (InventoryItemDefinition item, int count)[MaxSlots];
            int found = 0;

            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.item == null || stack.amount <= 0) continue;
                if (stack.item.itemType != ItemType.Healing) continue;

                bool merged = false;
                for (int i = 0; i < found; i++)
                {
                    if (entries[i].item == stack.item)
                    {
                        entries[i].count += stack.amount;
                        merged = true;
                        break;
                    }
                }
                if (!merged && found < MaxSlots)
                {
                    entries[found++] = (stack.item, stack.amount);
                }
            }

            bool healActive = _consumable != null && _consumable.IsUsing;

            for (int i = 0; i < MaxSlots; i++)
            {
                ConsumeSlot slot = _slots[i];
                bool hasItem = i < found;

                slot.Root.SetActive(hasItem);
                if (!hasItem) continue;

                InventoryItemDefinition def = entries[i].item;
                int count = entries[i].count;
                bool isFirst = (i == 0);

                slot.NameLabel.text  = ShortName(def.displayName);
                slot.CountLabel.text = count.ToString();

                // Icono: sprite del item o color fallback
                if (def.icon != null)
                {
                    slot.IconImg.sprite = def.icon;
                    slot.IconImg.color  = Color.white;
                }
                else
                {
                    slot.IconImg.sprite = null;
                    slot.IconImg.color  = LootIconHelper.GetIconColor(def.itemType);
                }

                // Resaltado: el primer slot (el que usaria H) mientras no
                // este activo ya se ve mas brillante
                bool highlight = isFirst && !healActive;
                slot.Bg.color = highlight ? BgActive : BgNormal;
                slot.CountLabel.color = isFirst
                    ? new Color(0.55f, 1f, 0.55f, 1f)
                    : new Color(0.85f, 0.85f, 0.85f, 0.9f);
            }
        }

        // ----------------------------------------------------------------

        private static string ShortName(string name)
        {
            // Truncar a 8 caracteres para que quepa en el slot pequeno
            if (name.Length <= 8) return name;
            return name.Substring(0, 7) + ".";
        }

        private void BuildUI()
        {
            _root = new GameObject("QuickConsumeCanvas");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            float totalW = MaxSlots * SlotW + (MaxSlots - 1) * SlotGap;

            // Contenedor anclado a esquina inferior derecha, encima de los slots de arma
            // (los slots de arma estan a 110px del fondo, cada uno 46px -> 3 * 46 + 2 * 4 = 146 -> 110 + 146 + margen)
            GameObject container = new GameObject("Container");
            container.transform.SetParent(_root.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(1f, 0f);
            cr.anchorMax        = new Vector2(1f, 0f);
            cr.pivot            = new Vector2(1f, 0f);
            cr.anchoredPosition = new Vector2(-14f, 270f);
            cr.sizeDelta        = new Vector2(totalW, SlotH);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < MaxSlots; i++)
            {
                float x = i * (SlotW + SlotGap);
                _slots[i] = MakeSlot(container.transform, x, font);
                _slots[i].Root.SetActive(false);
            }

            // Etiqueta de tecla H
            GameObject keyGo = new GameObject("KeyHint");
            keyGo.transform.SetParent(container.transform, false);
            Text keyTxt = keyGo.AddComponent<Text>();
            keyTxt.font      = font;
            keyTxt.fontSize  = 11;
            keyTxt.color     = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            keyTxt.alignment = TextAnchor.UpperLeft;
            keyTxt.text      = "H";
            RectTransform kr = keyGo.GetComponent<RectTransform>();
            kr.anchorMin        = new Vector2(0f, 1f);
            kr.anchorMax        = new Vector2(0f, 1f);
            kr.pivot            = new Vector2(0f, 0f);
            kr.sizeDelta        = new Vector2(20f, 16f);
            kr.anchoredPosition = new Vector2(4f, 2f);
        }

        private ConsumeSlot MakeSlot(Transform parent, float x, Font font)
        {
            GameObject go = new GameObject("ConsumeSlot");
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta        = new Vector2(SlotW, 0f);

            Image bg = go.AddComponent<Image>();
            bg.color = BgNormal;

            // Icono
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            Image icon = iconGo.AddComponent<Image>();
            icon.color  = Color.white;
            RectTransform ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin        = new Vector2(0.1f, 0.35f);
            ir.anchorMax        = new Vector2(0.55f, 0.95f);
            ir.offsetMin        = Vector2.zero;
            ir.offsetMax        = Vector2.zero;

            // Nombre
            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            Text nameLabel = nameGo.AddComponent<Text>();
            nameLabel.font      = font;
            nameLabel.fontSize  = 10;
            nameLabel.color     = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            nameLabel.alignment = TextAnchor.LowerCenter;
            nameLabel.text      = string.Empty;
            RectTransform wr = nameGo.GetComponent<RectTransform>();
            wr.anchorMin        = new Vector2(0f, 0f);
            wr.anchorMax        = new Vector2(1f, 0.38f);
            wr.offsetMin        = new Vector2(2f, 0f);
            wr.offsetMax        = new Vector2(-2f, 0f);

            // Cantidad
            GameObject countGo = new GameObject("Count");
            countGo.transform.SetParent(go.transform, false);
            Text countLabel = countGo.AddComponent<Text>();
            countLabel.font      = font;
            countLabel.fontSize  = 14;
            countLabel.fontStyle = FontStyle.Bold;
            countLabel.color     = new Color(0.55f, 1f, 0.55f, 1f);
            countLabel.alignment = TextAnchor.MiddleRight;
            countLabel.text      = "0";
            RectTransform cr = countGo.GetComponent<RectTransform>();
            cr.anchorMin        = new Vector2(0.5f, 0.3f);
            cr.anchorMax        = new Vector2(1f, 1f);
            cr.offsetMin        = Vector2.zero;
            cr.offsetMax        = new Vector2(-4f, 0f);

            return new ConsumeSlot
            {
                Root       = go,
                Bg         = bg,
                IconImg    = icon,
                NameLabel  = nameLabel,
                CountLabel = countLabel,
            };
        }
    }
}
