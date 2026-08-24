using ROS.Game.Input;
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
    public static class EditorFirstRosWeaponSlotsMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.90f);

        static EditorFirstRosWeaponSlotsMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize ROS Weapon Slots")]
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

            bool changed = false;

            PlayerInputReader player = FindInScene<PlayerInputReader>(scene);
            if (player != null)
            {
                changed |= EnsurePlayerSlotHierarchy(player.transform);
                changed |= EnsureComponent<PlayerAuxiliaryWeaponSlots>(
                    player.gameObject
                );
            }

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);
            Transform hud = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE"
                )
                : null;
            Transform weaponsRoot = hud != null
                ? hud.Find("Canvas/Weapons")
                : null;

            if (hud != null)
            {
                changed |= EnsureComponent<PlayerWeaponSlotsHudPresenter>(
                    hud.gameObject
                );
            }

            if (weaponsRoot != null)
                changed |= EnsureFiveSlotHud(weaponsRoot);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Estructura ROS de 5 slots materializada en jugador y HUD."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool EnsurePlayerSlotHierarchy(Transform player)
        {
            bool changed = false;
            Transform root = player.Find("PlayerWeaponSlots");
            if (root == null)
            {
                GameObject rootObject = new GameObject("PlayerWeaponSlots");
                rootObject.transform.SetParent(player, false);
                root = rootObject.transform;
                changed = true;
            }

            changed |= EnsureChild(root, "PrimarySlot1");
            changed |= EnsureChild(root, "PrimarySlot2");
            changed |= EnsureChild(root, "PistolSlot");
            changed |= EnsureChild(root, "MeleeSlot");
            changed |= EnsureChild(root, "ThrowableSlot");
            return changed;
        }

        private static bool EnsureChild(Transform parent, string name)
        {
            if (parent.Find(name) != null)
                return false;

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return true;
        }

        private static bool EnsureFiveSlotHud(Transform weaponsRoot)
        {
            bool firstMigration = weaponsRoot.Find("WeaponSlot_4") == null;
            bool changed = false;

            if (firstMigration)
            {
                RectTransform rootRect = weaponsRoot as RectTransform;
                if (rootRect != null)
                {
                    rootRect.sizeDelta = new Vector2(330f, 170f);
                    rootRect.anchoredPosition = new Vector2(-72f, 10f);
                }

                changed |= ConfigureExistingSlot(
                    weaponsRoot.Find("WeaponSlot_1"),
                    new Vector2(-45f, 58f),
                    new Vector2(235f, 56f)
                );
                changed |= ConfigureExistingSlot(
                    weaponsRoot.Find("WeaponSlot_2"),
                    new Vector2(-72f, 0f),
                    new Vector2(180f, 52f)
                );
                changed |= ConfigureExistingSlot(
                    weaponsRoot.Find("WeaponSlot_3"),
                    new Vector2(78f, 0f),
                    new Vector2(110f, 52f)
                );
            }

            if (weaponsRoot.Find("WeaponSlot_4") == null)
            {
                CreateSlot(
                    weaponsRoot,
                    4,
                    "FIST",
                    "∞",
                    new Vector2(130f, 58f),
                    new Vector2(82f, 56f)
                );
                changed = true;
            }

            if (weaponsRoot.Find("WeaponSlot_5") == null)
            {
                CreateSlot(
                    weaponsRoot,
                    5,
                    "THROWABLE",
                    "--",
                    new Vector2(78f, -55f),
                    new Vector2(110f, 46f)
                );
                changed = true;
            }

            return changed;
        }

        private static bool ConfigureExistingSlot(
            Transform slot,
            Vector2 position,
            Vector2 size
        )
        {
            if (slot == null)
                return false;

            RectTransform rect = slot as RectTransform;
            if (rect == null)
                return false;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return true;
        }

        private static void CreateSlot(
            Transform parent,
            int number,
            string defaultName,
            string defaultAmmo,
            Vector2 position,
            Vector2 size
        )
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject root = new GameObject(
                "WeaponSlot_" + number,
                typeof(RectTransform),
                typeof(Image)
            );
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = size;
            Image background = root.GetComponent<Image>();
            background.color = Dark;
            background.raycastTarget = false;

            Text slot = CreateText(root.transform, "Slot", number.ToString(), font, 11);
            RectTransform sr = slot.rectTransform;
            sr.anchorMin = new Vector2(0f, 1f);
            sr.anchorMax = new Vector2(0f, 1f);
            sr.pivot = new Vector2(0f, 1f);
            sr.anchoredPosition = new Vector2(5f, -3f);
            sr.sizeDelta = new Vector2(22f, 18f);

            Text name = CreateText(root.transform, "WeaponName", defaultName, font, 11);
            name.alignment = TextAnchor.MiddleCenter;
            RectTransform nr = name.rectTransform;
            nr.anchorMin = new Vector2(0f, 0f);
            nr.anchorMax = new Vector2(1f, 1f);
            nr.offsetMin = new Vector2(20f, 2f);
            nr.offsetMax = new Vector2(-4f, -2f);

            Text ammo = CreateText(root.transform, "Ammo", defaultAmmo, font, 11);
            ammo.alignment = TextAnchor.LowerRight;
            RectTransform ar = ammo.rectTransform;
            ar.anchorMin = Vector2.zero;
            ar.anchorMax = Vector2.one;
            ar.offsetMin = new Vector2(3f, 2f);
            ar.offsetMax = new Vector2(-4f, -2f);
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
