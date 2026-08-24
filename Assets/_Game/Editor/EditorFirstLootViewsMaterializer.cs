using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa NearbyLoot como la única lista de loot del HUD.
    /// Elimina NearbyObjectIndicator y crea filas físicas editables con:
    /// icono principal, nombre, información secundaria e icono secundario.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstLootViewsMaterializer
    {
        private const string HudPath =
            "Assets/_Game/Resources/EditorFirst/ROS_HUD_Editable.prefab";
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.98f);
        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.94f);

        static EditorFirstLootViewsMaterializer()
        {
            EditorApplication.delayCall += EnsureLootViews;
        }

        [MenuItem("Rules Of Survival/Editor First/Ensure Unified Nearby Loot")]
        public static void EnsureLootViews()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();
            EnsureItemNearbyMetadata();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
            if (root == null)
                return;

            bool changed = false;
            try
            {
                if (root.GetComponent<EditorFirstHudRuntimeRoot>() == null)
                {
                    root.AddComponent<EditorFirstHudRuntimeRoot>();
                    changed = true;
                }

                Transform canvas = Find(root.transform, "Canvas");
                if (canvas != null)
                    changed |= EnsureUnifiedNearbyLoot(canvas);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        public static void MaterializeFunctionalScene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
                return;

            EnsureItemNearbyMetadata();

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

            Transform canvas = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE/Canvas"
                )
                : null;

            if (canvas != null)
                changed |= EnsureUnifiedNearbyLoot(canvas);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        public static bool EnsureUnifiedNearbyLoot(Transform canvas)
        {
            if (canvas == null)
                return false;

            bool changed = false;

            Transform obsoleteIndicator = canvas.Find("NearbyObjectIndicator");
            if (obsoleteIndicator != null)
            {
                Object.DestroyImmediate(obsoleteIndicator.gameObject);
                changed = true;
            }

            Transform nearby = canvas.Find("NearbyLoot");
            if (nearby == null)
                return changed;

            RectTransform root = nearby as RectTransform;
            bool firstMigration = nearby.Find("Row_0") == null;

            for (int i = 0; i < 7; i++)
            {
                Transform legacy = nearby.Find("LootRow_" + i);
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy.gameObject);
                    changed = true;
                }
            }

            if (firstMigration)
            {
                if (root != null)
                {
                    root.sizeDelta = new Vector2(270f, 422f);
                }

                Image rootImage = nearby.GetComponent<Image>();
                if (rootImage != null)
                    rootImage.color = Color.clear;

                Transform title = nearby.Find("Title");
                RectTransform titleRect = title as RectTransform;
                if (titleRect != null)
                {
                    titleRect.anchorMin = new Vector2(0f, 1f);
                    titleRect.anchorMax = new Vector2(1f, 1f);
                    titleRect.pivot = new Vector2(0.5f, 1f);
                    titleRect.anchoredPosition = Vector2.zero;
                    titleRect.sizeDelta = new Vector2(0f, 34f);
                }

                Text titleText = title != null
                    ? title.Find("TitleText")?.GetComponent<Text>()
                    : null;
                if (titleText != null)
                    titleText.text = "NEARBY";

                Transform toggleBg = nearby.Find("ToggleBg");
                RectTransform toggleRect = toggleBg as RectTransform;
                if (toggleRect != null)
                {
                    toggleRect.anchorMin = new Vector2(0f, 0f);
                    toggleRect.anchorMax = new Vector2(1f, 0f);
                    toggleRect.pivot = new Vector2(0.5f, 0f);
                    toggleRect.anchoredPosition = Vector2.zero;
                    toggleRect.sizeDelta = new Vector2(0f, 24f);
                }

                Text hint = toggleBg != null
                    ? toggleBg.Find("ToggleHint")?.GetComponent<Text>()
                    : null;
                if (hint != null)
                    hint.text = "F RECOGER";

                changed = true;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 7; i++)
            {
                if (nearby.Find("Row_" + i) != null)
                    continue;

                BuildUnifiedRow(nearby, font, i);
                changed = true;
            }

            if (firstMigration)
                nearby.gameObject.SetActive(false);

            return changed;
        }

        private static void BuildUnifiedRow(
            Transform parent,
            Font font,
            int index
        )
        {
            RectTransform row = CreateRect("Row_" + index, parent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -34f - index * 52f);
            row.sizeDelta = new Vector2(0f, 52f);

            Image background = row.gameObject.AddComponent<Image>();
            background.color = Yellow;
            background.raycastTarget = false;

            RectTransform selection = CreateRect("Selection", row);
            Stretch(selection, 0f, 0f, 0f, 0f);
            Image selectionImage = selection.gameObject.AddComponent<Image>();
            selectionImage.color = Color.clear;
            selectionImage.raycastTarget = false;

            RectTransform icon = CreateRect("MainIcon", row);
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(7f, 0f);
            icon.sizeDelta = new Vector2(54f, 46f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            RectTransform nameRect = CreateRect("Name", row);
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(66f, -4f);
            nameRect.sizeDelta = new Vector2(-72f, 24f);
            Text name = nameRect.gameObject.AddComponent<Text>();
            name.font = font;
            name.text = "Objeto";
            name.fontSize = 14;
            name.fontStyle = FontStyle.Bold;
            name.alignment = TextAnchor.UpperLeft;
            name.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;

            RectTransform secondaryTextRect = CreateRect("SecondaryText", row);
            secondaryTextRect.anchorMin = new Vector2(0f, 0f);
            secondaryTextRect.anchorMax = new Vector2(1f, 0f);
            secondaryTextRect.pivot = new Vector2(0f, 0f);
            secondaryTextRect.anchoredPosition = new Vector2(66f, 4f);
            secondaryTextRect.sizeDelta = new Vector2(-72f, 20f);
            Text secondaryText = secondaryTextRect.gameObject.AddComponent<Text>();
            secondaryText.font = font;
            secondaryText.text = string.Empty;
            secondaryText.fontSize = 11;
            secondaryText.fontStyle = FontStyle.Normal;
            secondaryText.alignment = TextAnchor.LowerLeft;
            secondaryText.color = new Color(0.07f, 0.07f, 0.07f, 0.95f);
            secondaryText.raycastTarget = false;

            RectTransform secondaryIcon = CreateRect("SecondaryIcon", row);
            secondaryIcon.anchorMin = new Vector2(0f, 0f);
            secondaryIcon.anchorMax = new Vector2(0f, 0f);
            secondaryIcon.pivot = new Vector2(0f, 0f);
            secondaryIcon.anchoredPosition = new Vector2(66f, 4f);
            secondaryIcon.sizeDelta = new Vector2(82f, 20f);
            Image secondaryImage = secondaryIcon.gameObject.AddComponent<Image>();
            secondaryImage.preserveAspect = true;
            secondaryImage.raycastTarget = false;
            secondaryImage.enabled = false;
            secondaryIcon.gameObject.SetActive(false);

            RectTransform divider = CreateRect("Divider", row);
            divider.anchorMin = Vector2.zero;
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0f);
            divider.anchoredPosition = Vector2.zero;
            divider.sizeDelta = new Vector2(0f, 1f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(0f, 0f, 0f, 0.22f);
            dividerImage.raycastTarget = false;

            row.gameObject.SetActive(false);
        }

        private static void EnsureItemNearbyMetadata()
        {
            string[] guids = AssetDatabase.FindAssets("t:InventoryItemDefinition");
            InventoryItemDefinition[] items =
                new InventoryItemDefinition[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                items[i] = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
            }

            bool anyChanged = false;
            for (int i = 0; i < items.Length; i++)
            {
                InventoryItemDefinition item = items[i];
                if (item == null)
                    continue;

                bool changed = false;

                if (item.itemType != ItemType.Ammo &&
                    string.IsNullOrWhiteSpace(item.nearbySecondaryText))
                {
                    string value = GetDefaultSecondaryText(item);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        item.nearbySecondaryText = value;
                        changed = true;
                    }
                }

                if (item.itemType == ItemType.Ammo &&
                    item.nearbySecondaryIcon == null)
                {
                    Sprite compatible = FindCompatibleWeaponIcon(item, items);
                    if (compatible != null)
                    {
                        item.nearbySecondaryIcon = compatible;
                        changed = true;
                    }
                }

                if (!changed)
                    continue;

                EditorUtility.SetDirty(item);
                anyChanged = true;
            }

            if (anyChanged)
                AssetDatabase.SaveAssets();
        }

        private static Sprite FindCompatibleWeaponIcon(
            InventoryItemDefinition ammo,
            InventoryItemDefinition[] items
        )
        {
            if (ammo == null || ammo.itemType != ItemType.Ammo)
                return null;

            for (int i = 0; i < items.Length; i++)
            {
                InventoryItemDefinition candidate = items[i];
                if (candidate == null ||
                    candidate.itemType != ItemType.Weapon ||
                    candidate.icon == null ||
                    candidate.weaponDefinition == null)
                    continue;

                if (candidate.weaponDefinition.ammoType == ammo.ammoType)
                    return candidate.icon;
            }

            return null;
        }

        private static string GetDefaultSecondaryText(
            InventoryItemDefinition item
        )
        {
            switch (item.itemType)
            {
                case ItemType.Weapon:
                    if (item.weaponDefinition == null)
                        return "Weapon";
                    return item.weaponDefinition.family switch
                    {
                        WeaponFamily.AssaultRifle => "Assault Rifle",
                        WeaponFamily.SubmachineGun => "SMG",
                        WeaponFamily.SniperRifle => "Sniper Rifle",
                        WeaponFamily.Shotgun => "Shotgun",
                        WeaponFamily.Pistol => "Pistol",
                        WeaponFamily.LightMachineGun => "LMG",
                        WeaponFamily.Melee => "Melee",
                        _ => "Weapon"
                    };

                case ItemType.Healing:
                    if (item.consumableDefinition != null)
                    {
                        bool hp = item.consumableDefinition.healAmount > 0f;
                        bool energy = item.consumableDefinition.energyAmount > 0f;
                        if (hp && energy) return "Speed and HP up";
                        if (hp) return "+HP";
                        if (energy) return "Energy up";
                    }
                    return "Healing item";

                case ItemType.Armor:
                    return "Reduces damage";
                case ItemType.Helmet:
                    return "Reduces head damage";
                case ItemType.Backpack:
                    return "+capacity";
                case ItemType.Throwable:
                    return "Throwable";
                case ItemType.Attachment:
                    return "Attachment";
                default:
                    return string.Empty;
            }
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
