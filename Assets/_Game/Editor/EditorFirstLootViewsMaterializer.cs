using ROS.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstLootViewsMaterializer
    {
        private const string HudPath =
            "Assets/_Game/Resources/EditorFirst/ROS_HUD_Editable.prefab";

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.98f);
        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.94f);

        static EditorFirstLootViewsMaterializer()
        {
            EditorApplication.delayCall += EnsureLootViews;
        }

        [MenuItem("Rules Of Survival/Editor First/Ensure Editable Loot Views")]
        public static void EnsureLootViews()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
            bool changed = false;

            if (root.GetComponent<EditorFirstHudRuntimeRoot>() == null)
            {
                root.AddComponent<EditorFirstHudRuntimeRoot>();
                changed = true;
            }

            Transform canvas = Find(root.transform, "Canvas");
            if (canvas != null)
            {
                if (canvas.Find("NearbyObjectIndicator") == null)
                {
                    BuildNearbyIndicator(canvas);
                    changed = true;
                }

                if (canvas.Find("DeathLootPanelROS") == null)
                {
                    BuildDeathLootPanel(canvas);
                    changed = true;
                }
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, HudPath);

            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
        }

        private static void BuildNearbyIndicator(Transform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = CreatePanel(
                "NearbyObjectIndicator",
                canvas,
                new Vector2(214f, 42f),
                new Vector2(-24f, -58f),
                Vector2.one,
                new Color(0.02f, 0.03f, 0.04f, 0.72f)
            );
            root.pivot = Vector2.one;

            RectTransform icon = CreateRect("Icon", root);
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(6f, 0f);
            icon.sizeDelta = new Vector2(34f, 34f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            RectTransform textRect = CreateRect("Text", root);
            Stretch(textRect, 45f, 3f, 5f, 3f);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            AddOutline(textRect.gameObject, new Color(0f, 0f, 0f, 0.8f));

            root.gameObject.SetActive(false);
        }

        private static void BuildDeathLootPanel(Transform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = CreatePanel(
                "DeathLootPanelROS",
                canvas,
                new Vector2(270f, 430f),
                new Vector2(-22f, 0f),
                new Vector2(1f, 0.5f),
                Yellow
            );
            root.pivot = new Vector2(1f, 0.5f);
            AddOutline(root.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));

            RectTransform title = CreateRect("Title", root);
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.anchoredPosition = Vector2.zero;
            title.sizeDelta = new Vector2(0f, 38f);
            Image titleImage = title.gameObject.AddComponent<Image>();
            titleImage.color = Dark;
            titleImage.raycastTarget = false;

            RectTransform titleTextRect = CreateRect("Text", title);
            Stretch(titleTextRect, 10f, 3f, 8f, 2f);
            Text titleText = titleTextRect.gameObject.AddComponent<Text>();
            titleText.font = font;
            titleText.text = "LOOT";
            titleText.fontSize = 17;
            titleText.fontStyle = FontStyle.BoldAndItalic;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            AddOutline(titleTextRect.gameObject, new Color(0f, 0f, 0f, 0.9f));

            for (int i = 0; i < 7; i++)
                BuildLootRow(root, font, i);

            RectTransform footer = CreateRect("Footer", root);
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.anchoredPosition = Vector2.zero;
            footer.sizeDelta = new Vector2(0f, 30f);
            Image footerImage = footer.gameObject.AddComponent<Image>();
            footerImage.color = Dark;
            footerImage.raycastTarget = false;

            RectTransform footerTextRect = CreateRect("Text", footer);
            Stretch(footerTextRect, 5f, 2f, 5f, 2f);
            Text footerText = footerTextRect.gameObject.AddComponent<Text>();
            footerText.font = font;
            footerText.text = "RUEDA  •  F RECOGER  •  ESC";
            footerText.fontSize = 10;
            footerText.fontStyle = FontStyle.Bold;
            footerText.alignment = TextAnchor.MiddleCenter;
            footerText.color = new Color(1f, 0.90f, 0.12f, 1f);
            footerText.raycastTarget = false;

            root.gameObject.SetActive(false);
        }

        private static void BuildLootRow(RectTransform parent, Font font, int index)
        {
            RectTransform row = CreateRect("Row_" + index, parent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -38f - index * 51f);
            row.sizeDelta = new Vector2(0f, 51f);
            Image background = row.gameObject.AddComponent<Image>();
            background.color = Yellow;
            background.raycastTarget = false;

            RectTransform selection = CreateRect("Selection", row);
            Stretch(selection, 0f, 0f, 0f, 0f);
            Image selectionImage = selection.gameObject.AddComponent<Image>();
            selectionImage.color = Color.clear;
            selectionImage.raycastTarget = false;

            RectTransform icon = CreateRect("Icon", row);
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(7f, 0f);
            icon.sizeDelta = new Vector2(43f, 43f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            RectTransform nameRect = CreateRect("Name", row);
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(56f, 3f);
            nameRect.offsetMax = new Vector2(-42f, -3f);
            Text name = nameRect.gameObject.AddComponent<Text>();
            name.font = font;
            name.text = "Objeto";
            name.fontSize = 13;
            name.fontStyle = FontStyle.Bold;
            name.alignment = TextAnchor.MiddleLeft;
            name.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;

            RectTransform amountRect = CreateRect("Amount", row);
            amountRect.anchorMin = new Vector2(1f, 0f);
            amountRect.anchorMax = Vector2.one;
            amountRect.pivot = new Vector2(1f, 0.5f);
            amountRect.anchoredPosition = new Vector2(-5f, 0f);
            amountRect.sizeDelta = new Vector2(36f, 0f);
            Text amount = amountRect.gameObject.AddComponent<Text>();
            amount.font = font;
            amount.text = string.Empty;
            amount.fontSize = 12;
            amount.fontStyle = FontStyle.Bold;
            amount.alignment = TextAnchor.MiddleRight;
            amount.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            amount.raycastTarget = false;

            RectTransform divider = CreateRect("Divider", row);
            divider.anchorMin = Vector2.zero;
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0f);
            divider.anchoredPosition = Vector2.zero;
            divider.sizeDelta = new Vector2(0f, 1f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(0f, 0f, 0f, 0.22f);
            dividerImage.raycastTarget = false;
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 position,
            Vector2 anchor,
            Color color
        )
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float top,
            float right,
            float bottom
        )
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AddOutline(GameObject target, Color color)
        {
            AddOutline(target, color, new Vector2(1f, -1f));
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Transform Find(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name)
                    return all[i];
            }
            return null;
        }
    }
}
