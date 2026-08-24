using ROS.Game.UI;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstHudBehaviorMaterializer
    {
        private const string HudPath =
            "Assets/_Game/Resources/EditorFirst/ROS_HUD_Editable.prefab";

        static EditorFirstHudBehaviorMaterializer()
        {
            EditorApplication.delayCall += EnsureHudBehaviors;
        }

        [MenuItem("Rules Of Survival/Editor First/Ensure HUD Behaviors On Prefab")]
        public static void EnsureHudBehaviors()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            if (prefab == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
            bool changed = false;

            if (root.GetComponent<RulesOfSurvivalHUDRuntimePolish>() == null)
            {
                root.AddComponent<RulesOfSurvivalHUDRuntimePolish>();
                changed = true;
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, HudPath);

            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
        }
    }
}
