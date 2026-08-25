using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    public static class EditorFirstReloadUpperBodyRepair
    {
        private const string ControllerPath = "Assets/_Game/Animations/AC_Player_Prototype.controller";
        private const string StandingPath = "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadStandingHip.fbx";
        private const string CrouchPath = "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadCrouchHip.fbx";

        public static bool Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return false;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimationClip standing = LoadClip(StandingPath);
            AnimationClip crouch = LoadClip(CrouchPath);
            if (controller == null || standing == null)
                return false;

            int layerIndex = FindLayer(controller, "UpperBodyActions");
            if (layerIndex < 0)
                return false;

            AnimatorStateMachine machine = controller.layers[layerIndex].stateMachine;
            AnimatorState standingState = FindState(machine, "ReloadStanding");
            AnimatorState crouchState = FindState(machine, "ReloadCrouch");
            bool changed = false;

            if (standingState != null && standingState.motion != standing)
            {
                standingState.motion = standing;
                EditorUtility.SetDirty(standingState);
                changed = true;
            }

            AnimationClip crouchClip = crouch != null ? crouch : standing;
            if (crouchState != null && crouchState.motion != crouchClip)
            {
                crouchState.motion = crouchClip;
                EditorUtility.SetDirty(crouchState);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Editor First] Reload Upper Body verificado: standing + crouch.");
            return changed;
        }

        private static int FindLayer(AnimatorController controller, string name)
        {
            for (int i = 0; i < controller.layers.Length; i++)
                if (controller.layers[i].name == name) return i;
            return -1;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            if (machine == null) return null;
            foreach (ChildAnimatorState child in machine.states)
                if (child.state != null && child.state.name == name) return child.state;
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            {
                AnimatorState found = FindState(child.stateMachine, name);
                if (found != null) return found;
            }
            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            return null;
        }
    }
}
