using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstGestureHudHintMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstGestureHudHintMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

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
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            Transform throwableSlot = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE/Canvas/Weapons/WeaponSlot_5"
                )
                : null;

            if (throwableSlot == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = EnsureHint(throwableSlot);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] GestureHintHUD materializado fisicamente sobre WeaponSlot_5."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool EnsureHint(Transform slot)
        {
            if (slot.Find("GestureHintHUD") != null)
                return false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rootObject = new GameObject(
                "GestureHintHUD",
                typeof(RectTransform)
            );
            rootObject.transform.SetParent(slot, false);

            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 8f);
            root.sizeDelta = new Vector2(88f, 26f);

            GameObject circleObject = new GameObject(
                "KeyCircle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline)
            );
            circleObject.transform.SetParent(root, false);

            RectTransform circleRect = circleObject.GetComponent<RectTransform>();
            circleRect.anchorMin = new Vector2(0f, 0.5f);
            circleRect.anchorMax = new Vector2(0f, 0.5f);
            circleRect.pivot = new Vector2(0f, 0.5f);
            circleRect.anchoredPosition = Vector2.zero;
            circleRect.sizeDelta = new Vector2(24f, 24f);

            Image circle = circleObject.GetComponent<Image>();
            circle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Knob.psd"
            );
            circle.type = Image.Type.Simple;
            circle.preserveAspect = true;
            circle.color = new Color(0.055f, 0.065f, 0.075f, 0.90f);
            circle.raycastTarget = false;

            Outline outline = circleObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.72f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Text key = CreateText(
                circleRect,
                "KeyText",
                "G",
                font,
                12,
                TextAnchor.MiddleCenter
            );
            Stretch(key.rectTransform);

            Text label = CreateText(
                root,
                "Label",
                "GESTO",
                font,
                11,
                TextAnchor.MiddleLeft
            );
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(30f, 0f);
            labelRect.offsetMax = Vector2.zero;

            rootObject.SetActive(true);
            EditorUtility.SetDirty(rootObject);
            return true;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int size,
            TextAnchor alignment
        )
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.text = value;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
