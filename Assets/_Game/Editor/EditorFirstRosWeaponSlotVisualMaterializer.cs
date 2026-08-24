using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstRosWeaponSlotVisualMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.92f);

        static EditorFirstRosWeaponSlotVisualMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize ROS Weapon Slot Visuals")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);
            Transform weapons = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE/Canvas/Weapons"
                )
                : null;

            if (weapons == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = false;
            for (int slot = 1; slot <= 5; slot++)
            {
                Transform root = weapons.Find("WeaponSlot_" + slot);
                if (root == null)
                    continue;

                bool firstVisualMigration = root.Find("Icon") == null;
                changed |= EnsureIcon(root);
                changed |= ConfigureSlotTextLayout(root);

                if (slot <= 2)
                    changed |= EnsureFireModePanel(root);

                if (firstVisualMigration)
                    EditorUtility.SetDirty(root.gameObject);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Slots ROS actualizados: icono prioritario, municion abajo a la izquierda y modo de tiro externo en slots 1/2."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool EnsureIcon(Transform slot)
        {
            Transform existing = slot.Find("Icon");
            if (existing != null)
                return false;

            GameObject go = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image)
            );
            go.transform.SetParent(slot, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.38f, 0.12f);
            rect.anchorMax = new Vector2(0.98f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return true;
        }

        private static bool ConfigureSlotTextLayout(Transform slot)
        {
            bool changed = false;

            Text legacyName = slot.Find("WeaponName")?.GetComponent<Text>();
            if (legacyName != null && legacyName.gameObject.activeSelf)
            {
                legacyName.gameObject.SetActive(false);
                changed = true;
            }

            Text number = slot.Find("Slot")?.GetComponent<Text>();
            if (number != null)
            {
                RectTransform rect = number.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(6f, -3f);
                rect.sizeDelta = new Vector2(28f, 19f);
                number.alignment = TextAnchor.UpperLeft;
                number.fontStyle = FontStyle.Bold;
                changed = true;
            }

            Text ammo = slot.Find("Ammo")?.GetComponent<Text>();
            if (ammo != null)
            {
                RectTransform rect = ammo.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(6f, 4f);
                rect.sizeDelta = new Vector2(105f, 22f);
                ammo.alignment = TextAnchor.LowerLeft;
                ammo.fontStyle = FontStyle.Bold;
                changed = true;
            }

            return changed;
        }

        private static bool EnsureFireModePanel(Transform slot)
        {
            if (slot.Find("FireModePanel") != null)
                return false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject panelObject = new GameObject(
                "FireModePanel",
                typeof(RectTransform),
                typeof(Image)
            );
            panelObject.transform.SetParent(slot, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.anchoredPosition = new Vector2(-5f, 0f);
            panel.sizeDelta = new Vector2(56f, 48f);

            Image background = panelObject.GetComponent<Image>();
            background.color = Dark;
            background.raycastTarget = false;

            Text key = CreateText(panel, "Key", "B", font, 10);
            RectTransform keyRect = key.rectTransform;
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(5f, -3f);
            keyRect.sizeDelta = new Vector2(18f, 16f);
            key.alignment = TextAnchor.UpperLeft;

            Text mode = CreateText(panel, "Mode", "AUTO", font, 12);
            RectTransform modeRect = mode.rectTransform;
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.one;
            modeRect.offsetMin = new Vector2(2f, 2f);
            modeRect.offsetMax = new Vector2(-2f, -2f);
            mode.alignment = TextAnchor.MiddleCenter;

            panelObject.SetActive(false);
            return true;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int size
        )
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Text)
            );
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
