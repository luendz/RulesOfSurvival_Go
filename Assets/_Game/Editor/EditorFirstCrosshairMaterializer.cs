using ROS.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstCrosshairMaterializer
    {
        private const string HudPath =
            "Assets/_Game/Resources/EditorFirst/ROS_HUD_Editable.prefab";

        static EditorFirstCrosshairMaterializer()
        {
            EditorApplication.delayCall += EnsureCrosshair;
        }

        [MenuItem("Rules Of Survival/Editor First/Ensure Editable Weapon Crosshair")]
        public static void EnsureCrosshair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            if (prefab == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
            bool changed = false;

            WeaponCrosshairPresenter presenter = root.GetComponent<WeaponCrosshairPresenter>();
            if (presenter == null)
            {
                presenter = root.AddComponent<WeaponCrosshairPresenter>();
                changed = true;
            }

            Transform canvas = Find(root.transform, "Canvas");
            Transform crosshair = Find(root.transform, "WeaponCrosshair");
            if (canvas != null && crosshair == null)
            {
                BuildCrosshair(canvas);
                changed = true;
            }

            presenter.BindViewFromHierarchy();
            EditorUtility.SetDirty(presenter);

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, HudPath);

            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
        }

        private static void BuildCrosshair(Transform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateRect("WeaponCrosshair", canvas);
            SetRect(root, new Vector2(140f, 140f), Vector2.zero);

            RectTransform normalRoot = CreateRect("NormalCrosshairRoot", root);
            SetRect(normalRoot, Vector2.zero, Vector2.zero);

            CreateArm("NormalLeft", normalRoot, new Vector2(8f, 2f), new Vector2(-7f, 0f));
            CreateArm("NormalRight", normalRoot, new Vector2(8f, 2f), new Vector2(7f, 0f));
            CreateArm("NormalUp", normalRoot, new Vector2(2f, 8f), new Vector2(0f, 7f));
            CreateArm("NormalDown", normalRoot, new Vector2(2f, 8f), new Vector2(0f, -7f));

            Text left = CreateText("ShotgunLeft", root, "(", font, 28);
            left.rectTransform.anchoredPosition = new Vector2(-17f, 0f);
            Text right = CreateText("ShotgunRight", root, ")", font, 28);
            right.rectTransform.anchoredPosition = new Vector2(17f, 0f);
            left.gameObject.SetActive(false);
            right.gameObject.SetActive(false);
        }

        private static RectTransform CreateArm(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 position
        )
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.94f);
            image.raycastTarget = false;
            return rect;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            Font font,
            int fontSize
        )
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, new Vector2(42f, 58f), Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.98f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
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
