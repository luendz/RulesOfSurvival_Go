using System.Collections.Generic;
using ROS.Game.Character;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstPlayerEquipmentVisualMaterializer
    {
        private const string ScenePath = "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstPlayerEquipmentVisualMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize Player Equipment Visuals")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || !System.IO.File.Exists(ScenePath))
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool temporary = !scene.IsValid() || !scene.isLoaded;
            if (temporary) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (!scene.IsValid() || !scene.isLoaded) return;

            PlayerInputReader input = FindPlayer(scene);
            if (input == null)
            {
                if (temporary) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = false;
            GameObject player = input.gameObject;
            PlayerEquipmentVisualPresenter presenter = player.GetComponent<PlayerEquipmentVisualPresenter>();
            if (presenter == null)
            {
                presenter = player.AddComponent<PlayerEquipmentVisualPresenter>();
                changed = true;
            }

            Transform root = Child(player.transform, "EquipmentVisuals", ref changed);
            Transform helmet = Child(root, "HelmetSocket", ref changed);
            Transform vest = Child(root, "VestSocket", ref changed);
            Transform backpack = Child(root, "BackpackSocket", ref changed);

            Animator animator = FindAnimator(player);
            if (animator != null)
            {
                changed |= Follow(helmet, animator.GetBoneTransform(HumanBodyBones.Head));
                Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
                changed |= Follow(vest, chest);
                changed |= Follow(backpack, chest);
            }

            Defs d = FindDefinitions();
            changed |= Visual(helmet, "Helmet_Lv1", d.h1, PrimitiveType.Sphere);
            changed |= Visual(helmet, "Helmet_Lv2", d.h2, PrimitiveType.Sphere);
            changed |= Visual(helmet, "Helmet_Lv3", d.h3, PrimitiveType.Sphere);
            changed |= Visual(vest, "Vest_Lv1", d.v1, PrimitiveType.Cube);
            changed |= Visual(vest, "Vest_Lv2", d.v2, PrimitiveType.Cube);
            changed |= Visual(vest, "Vest_Lv3", d.v3, PrimitiveType.Cube);
            changed |= Visual(backpack, "Backpack_Lv1", d.b1, PrimitiveType.Cube);
            changed |= Visual(backpack, "Backpack_Lv2", d.b2, PrimitiveType.Cube);
            changed |= Visual(backpack, "Backpack_Lv3", d.b3, PrimitiveType.Cube);

            presenter.BindViewFromHierarchy();
            presenter.ConfigureBackpackDefinitions(d.b1, d.b2, d.b3);
            EditorUtility.SetDirty(presenter);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("[Editor First] Equipamiento visual fisico materializado en Player_Prototype.");
            }
            if (temporary) EditorSceneManager.CloseScene(scene, true);
        }

        private static Transform Child(Transform parent, string name, ref bool changed)
        {
            Transform found = parent.Find(name);
            if (found != null) return found;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            changed = true;
            return go.transform;
        }

        private static bool Follow(Transform socket, Transform bone)
        {
            if (socket == null || bone == null) return false;
            BoneSocketFollower f = socket.GetComponent<BoneSocketFollower>();
            bool changed = false;
            if (f == null)
            {
                f = socket.gameObject.AddComponent<BoneSocketFollower>();
                changed = true;
            }
            if (f.TargetBone != bone)
            {
                f.Bind(bone);
                EditorUtility.SetDirty(f);
                changed = true;
            }
            return changed;
        }

        private static bool Visual(Transform socket, string name, InventoryItemDefinition item, PrimitiveType fallback)
        {
            if (socket.Find(name) != null) return false;

            GameObject root = new GameObject(name);
            root.transform.SetParent(socket, false);

            GameObject model = null;
            if (item != null && item.worldModel != null)
            {
                model = PrefabUtility.InstantiatePrefab(item.worldModel, root.transform) as GameObject;
                if (model != null && PrefabUtility.IsPartOfPrefabInstance(model))
                    PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            if (model == null)
            {
                model = GameObject.CreatePrimitive(fallback);
                model.name = "Placeholder_ReplaceMe";
                model.transform.SetParent(root.transform, false);
                Collider c = model.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
                if (fallback == PrimitiveType.Sphere) model.transform.localScale = new Vector3(.28f, .20f, .30f);
                else model.transform.localScale = new Vector3(.40f, .50f, .22f);
            }
            else
            {
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                if (item.worldScale.sqrMagnitude > .0001f) model.transform.localScale = item.worldScale;
            }

            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            root.SetActive(false);
            return true;
        }

        private static Defs FindDefinitions()
        {
            Defs d = new Defs();
            List<InventoryItemDefinition> bags = new List<InventoryItemDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:InventoryItemDefinition"))
            {
                InventoryItemDefinition i = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (i == null) continue;
                if (i.itemType == ItemType.Helmet)
                {
                    if (i.protectionLevel == ProtectionLevel.Level1 && d.h1 == null) d.h1 = i;
                    else if (i.protectionLevel == ProtectionLevel.Level2 && d.h2 == null) d.h2 = i;
                    else if (i.protectionLevel == ProtectionLevel.Level3 && d.h3 == null) d.h3 = i;
                }
                else if (i.itemType == ItemType.Armor)
                {
                    if (i.protectionLevel == ProtectionLevel.Level1 && d.v1 == null) d.v1 = i;
                    else if (i.protectionLevel == ProtectionLevel.Level2 && d.v2 == null) d.v2 = i;
                    else if (i.protectionLevel == ProtectionLevel.Level3 && d.v3 == null) d.v3 = i;
                }
                else if (i.itemType == ItemType.Backpack) bags.Add(i);
            }
            bags.Sort((a, b) => a.backpackCapacity.CompareTo(b.backpackCapacity));
            if (bags.Count > 0) d.b1 = bags[0];
            if (bags.Count > 1) d.b2 = bags[1];
            if (bags.Count > 2) d.b3 = bags[2];
            return d;
        }

        private static Animator FindAnimator(GameObject player)
        {
            foreach (Animator a in player.GetComponentsInChildren<Animator>(true))
                if (a != null && a.isHuman) return a;
            return null;
        }

        private static PlayerInputReader FindPlayer(Scene scene)
        {
            PlayerInputReader fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (PlayerInputReader p in root.GetComponentsInChildren<PlayerInputReader>(true))
            {
                if (p == null || p.gameObject.name.StartsWith("Bot_")) continue;
                if (p.gameObject.name == "Player_Prototype" || p.gameObject.name.StartsWith("Player_")) return p;
                if (fallback == null) fallback = p;
            }
            return fallback;
        }

        private sealed class Defs
        {
            public InventoryItemDefinition h1, h2, h3, v1, v2, v3, b1, b2, b3;
        }
    }
}
