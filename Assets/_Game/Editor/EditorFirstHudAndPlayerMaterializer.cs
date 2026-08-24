using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.UI;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstHudAndPlayerMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.90f);

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.96f);

        static EditorFirstHudAndPlayerMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize HUD And Main Player")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(ScenePath))
                return;

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

            bool changed = false;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            Transform hud = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE"
                )
                : null;

            Transform canvas = hud != null
                ? hud.Find("Canvas")
                : null;

            if (canvas != null)
            {
                changed |= EnsureMatchState(canvas);
                changed |= EnsureKillFeed(canvas);
                changed |= EnsureDamageDirection(canvas);
                changed |= EnsureEquipmentStatus(canvas);
                changed |= EnsureQuickConsume(canvas);
                changed |= EnsureCombatFeedback(canvas);

                changed |= EnsureComponent<MatchStartHud>(hud.gameObject);
                changed |= EnsureComponent<KillFeedPresenter>(hud.gameObject);
                changed |= EnsureComponent<DamageDirectionIndicator>(hud.gameObject);
                changed |= EnsureComponent<EquipmentStatusPresenter>(hud.gameObject);
                changed |= EnsureComponent<QuickConsumePresenter>(hud.gameObject);
            }

            PlayerInputReader player = FindInScene<PlayerInputReader>(scene);
            if (player != null)
            {
                changed |= EnsureComponent<ParachuteController>(player.gameObject);
                changed |= EnsureComponent<DamageNumberSpawner>(player.gameObject);
                changed |= EnsureComponent<WeaponAmmoConnector>(player.gameObject);
                changed |= EnsureComponent<ConsumableController>(player.gameObject);
                changed |= EnsureComponent<PlayerLootEquipment>(player.gameObject);
                changed |= EnsureComponent<CombatFeedbackPresenter>(player.gameObject);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] HUD y jugador principal materializados fisicamente en escena 08."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool EnsureMatchState(Transform canvas)
        {
            Transform root = canvas.Find("MatchStatePanel");
            if (root != null)
                return false;

            GameObject panel = CreateUiObject("MatchStatePanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -55f);
            rt.sizeDelta = new Vector2(540f, 78f);
            Image bg = panel.AddComponent<Image>();
            bg.color = Dark;
            bg.raycastTarget = false;

            CreateText(panel.transform, "MatchStateTitle", "", 19,
                TextAnchor.MiddleCenter, new Vector2(0f, -7f),
                new Vector2(520f, 30f), true);
            CreateText(panel.transform, "MatchStateDetail", "", 14,
                TextAnchor.MiddleCenter, new Vector2(0f, -40f),
                new Vector2(520f, 26f), false);

            panel.SetActive(false);
            return true;
        }

        private static bool EnsureKillFeed(Transform canvas)
        {
            Transform root = canvas.Find("KillFeedRoot");
            if (root != null)
                return false;

            GameObject container = CreateUiObject("KillFeedRoot", canvas);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -120f);
            rt.sizeDelta = new Vector2(360f, 120f);

            for (int i = 0; i < 5; i++)
            {
                Text row = CreateText(container.transform, "KillFeedRow_" + i,
                    string.Empty, 15, TextAnchor.MiddleRight,
                    new Vector2(0f, -i * 24f), new Vector2(340f, 22f), false);
                row.gameObject.SetActive(false);
            }
            return true;
        }

        private static bool EnsureDamageDirection(Transform canvas)
        {
            Transform root = canvas.Find("DamageDirectionRoot");
            if (root != null)
                return false;

            GameObject center = CreateUiObject("DamageDirectionRoot", canvas);
            RectTransform cr = center.GetComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = Vector2.zero;

            CreateArrow(center.transform, "DamageArrow_Front", new Vector2(0f, 120f), 0f);
            CreateArrow(center.transform, "DamageArrow_Right", new Vector2(120f, 0f), -90f);
            CreateArrow(center.transform, "DamageArrow_Back", new Vector2(0f, -120f), 180f);
            CreateArrow(center.transform, "DamageArrow_Left", new Vector2(-120f, 0f), 90f);
            return true;
        }

        private static bool EnsureEquipmentStatus(Transform canvas)
        {
            Transform root = canvas.Find("EquipmentStatusRoot");
            if (root != null)
                return false;

            GameObject container = CreateUiObject("EquipmentStatusRoot", canvas);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-18f, 18f);
            rt.sizeDelta = new Vector2(170f, 60f);

            CreateText(container.transform, "HelmetStatus", "CASCO —", 13,
                TextAnchor.MiddleRight, new Vector2(0f, 36f), new Vector2(160f, 18f), false);
            CreateText(container.transform, "VestStatus", "CHALECO —", 13,
                TextAnchor.MiddleRight, new Vector2(0f, 18f), new Vector2(160f, 18f), false);
            CreateText(container.transform, "BackpackStatus", "MOCHILA —", 13,
                TextAnchor.MiddleRight, Vector2.zero, new Vector2(160f, 18f), false);
            return true;
        }

        private static bool EnsureQuickConsume(Transform canvas)
        {
            Transform root = canvas.Find("QuickConsumeRoot");
            if (root != null)
                return false;

            GameObject container = CreateUiObject("QuickConsumeRoot", canvas);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-14f, 270f);
            rt.sizeDelta = new Vector2(216f, 52f);

            for (int i = 0; i < 3; i++)
            {
                GameObject slot = CreateUiObject("QuickConsumeSlot_" + i, container.transform);
                RectTransform sr = slot.GetComponent<RectTransform>();
                sr.anchorMin = sr.anchorMax = new Vector2(0f, 0f);
                sr.pivot = Vector2.zero;
                sr.anchoredPosition = new Vector2(i * 74f, 0f);
                sr.sizeDelta = new Vector2(68f, 52f);
                Image bg = slot.AddComponent<Image>();
                bg.color = Dark;

                GameObject iconObj = CreateUiObject("Icon", slot.transform);
                RectTransform ir = iconObj.GetComponent<RectTransform>();
                ir.anchorMin = new Vector2(0.1f, 0.35f);
                ir.anchorMax = new Vector2(0.55f, 0.95f);
                ir.offsetMin = ir.offsetMax = Vector2.zero;
                iconObj.AddComponent<Image>().preserveAspect = true;

                CreateText(slot.transform, "Name", string.Empty, 10,
                    TextAnchor.LowerCenter, new Vector2(0f, 1f),
                    new Vector2(64f, 19f), false);
                CreateText(slot.transform, "Count", "0", 14,
                    TextAnchor.MiddleRight, new Vector2(16f, 12f),
                    new Vector2(45f, 28f), true);
                slot.SetActive(false);
            }

            CreateText(container.transform, "QuickConsumeKeyHint", "H", 11,
                TextAnchor.UpperLeft, new Vector2(4f, 54f), new Vector2(20f, 16f), false);
            return true;
        }

        private static bool EnsureCombatFeedback(Transform canvas)
        {
            Transform root = canvas.Find("CombatFeedbackRoot");
            if (root != null)
                return false;

            GameObject feedback = CreateUiObject("CombatFeedbackRoot", canvas);
            RectTransform fr = feedback.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;

            GameObject hit = CreateUiObject("HitmarkerRoot", feedback.transform);
            RectTransform hr = hit.GetComponent<RectTransform>();
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
            hr.sizeDelta = new Vector2(50f, 50f);

            CreateBar(hit.transform, "Hitmarker_TL", new Vector2(-10f, 10f), new Vector2(9f, 2f));
            CreateBar(hit.transform, "Hitmarker_TR", new Vector2(10f, 10f), new Vector2(9f, 2f));
            CreateBar(hit.transform, "Hitmarker_BL", new Vector2(-10f, -10f), new Vector2(9f, 2f));
            CreateBar(hit.transform, "Hitmarker_BR", new Vector2(10f, -10f), new Vector2(9f, 2f));
            CreateText(hit.transform, "HeadshotLabel", "HEADSHOT", 12,
                TextAnchor.MiddleCenter, new Vector2(0f, -32f), new Vector2(110f, 24f), true)
                .gameObject.SetActive(false);
            hit.SetActive(false);

            CreateScreenDamageBar(feedback.transform, "DamageFeedback_Front",
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(110f, 8f));
            CreateScreenDamageBar(feedback.transform, "DamageFeedback_Right",
                new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(8f, 110f));
            CreateScreenDamageBar(feedback.transform, "DamageFeedback_Back",
                new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(110f, 8f));
            CreateScreenDamageBar(feedback.transform, "DamageFeedback_Left",
                new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(8f, 110f));
            return true;
        }

        private static void CreateArrow(Transform parent, string name, Vector2 pos, float rotation)
        {
            GameObject go = CreateUiObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 0.15f, 0.1f, 0f);
            image.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24f, 60f);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void CreateBar(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateUiObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void CreateScreenDamageBar(
            Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateUiObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 0.08f, 0.03f, 0f);
            image.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 pos,
            Vector2 size,
            bool bold)
        {
            GameObject go = CreateUiObject(name, parent);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = Color.white;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return text;
        }

        private static bool EnsureComponent<T>(GameObject target)
            where T : Component
        {
            if (target.GetComponent<T>() != null)
                return false;

            target.AddComponent<T>();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] found = roots[i].GetComponentsInChildren<T>(true);
                if (found.Length > 0)
                    return found[0];
            }
            return null;
        }
    }
}
