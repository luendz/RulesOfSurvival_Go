using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstConsumableHudMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstConsumableHudMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize Consumable HUD")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);
            Transform canvas = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE/Canvas")
                : null;

            if (canvas != null && canvas.Find("ConsumableProgressBar") == null)
            {
                GameObject root = new GameObject(
                    "ConsumableProgressBar",
                    typeof(RectTransform)
                );
                root.transform.SetParent(canvas, false);
                RectTransform rr = root.GetComponent<RectTransform>();
                rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.18f);
                rr.sizeDelta = new Vector2(320f, 22f);

                GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(root.transform, false);
                RectTransform bgr = bg.GetComponent<RectTransform>();
                bgr.anchorMin = Vector2.zero;
                bgr.anchorMax = Vector2.one;
                bgr.offsetMin = bgr.offsetMax = Vector2.zero;
                Image bgImage = bg.GetComponent<Image>();
                bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
                bgImage.raycastTarget = false;

                GameObject fill = new GameObject("ConsumableProgressFill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(root.transform, false);
                RectTransform fr = fill.GetComponent<RectTransform>();
                fr.anchorMin = Vector2.zero;
                fr.anchorMax = Vector2.one;
                fr.pivot = new Vector2(0f, 0.5f);
                fr.offsetMin = fr.offsetMax = Vector2.zero;
                Image fillImage = fill.GetComponent<Image>();
                fillImage.color = new Color(0.25f, 0.92f, 0.35f, 1f);
                fillImage.raycastTarget = false;

                GameObject label = new GameObject("ConsumableProgressLabel", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(root.transform, false);
                RectTransform lr = label.GetComponent<RectTransform>();
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                Text text = label.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 13;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;

                root.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
