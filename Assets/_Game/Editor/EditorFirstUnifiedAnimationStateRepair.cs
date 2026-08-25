using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Reparación idempotente de la arquitectura consolidada. A diferencia del
    /// materializador inicial, esta rutina también corrige capas que ya existen,
    /// por lo que una versión antigua con estados sin Motion no queda congelada.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstUnifiedAnimationStateRepair
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";
        private const string MaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";
        private const string ReloadStandingPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadStandingHip.fbx";
        private const string ReloadCrouchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadCrouchHip.fbx";
        private const string WeaponSwitchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleSwitch_UpperBody.fbx";

        static EditorFirstUnifiedAnimationStateRepair()
        {
            EditorApplication.delayCall += Repair;
        }

        [MenuItem("Rules Of Survival/Editor First/Repair Consolidated Upper Body Motions")]
        public static void Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (controller == null || mask == null)
                return;

            Motion rifleLocomotion = FindState(controller, "BT_RifleLocomotion")?.motion;
            Motion rifleCrouch = FindState(controller, "BT_RifleCrouch")?.motion;
            Motion aimLocomotion = FindState(controller, "BT_AimLocomotion")?.motion;
            Motion healing = FindLegacyState(controller, "Healing")?.motion;

            AnimationClip reloadStanding = LoadClip(ReloadStandingPath);
            AnimationClip reloadCrouch = LoadClip(ReloadCrouchPath);
            AnimationClip weaponSwitch = LoadClip(WeaponSwitchPath);

            bool changed = false;
            AnimatorControllerLayer[] layers = controller.layers;

            int combatIndex = FindLayer(layers, PlayerAnimationCoordinator.CombatLayerName);
            if (combatIndex >= 0)
            {
                AnimatorControllerLayer layer = layers[combatIndex];
                if (layer.avatarMask != mask)
                {
                    layer.avatarMask = mask;
                    layers[combatIndex] = layer;
                    changed = true;
                }

                changed |= AssignMotion(layer.stateMachine, "ArmedLocomotion", rifleLocomotion);
                changed |= AssignMotion(
                    layer.stateMachine,
                    "ArmedCrouch",
                    rifleCrouch != null ? rifleCrouch : rifleLocomotion
                );
                changed |= AssignMotion(layer.stateMachine, "AimLocomotion", aimLocomotion);
            }

            int actionsIndex = FindLayer(layers, PlayerAnimationCoordinator.ActionsLayerName);
            if (actionsIndex >= 0)
            {
                AnimatorControllerLayer layer = layers[actionsIndex];
                if (layer.avatarMask != mask)
                {
                    layer.avatarMask = mask;
                    layers[actionsIndex] = layer;
                    changed = true;
                }

                changed |= AssignMotion(layer.stateMachine, "Healing", healing);
                changed |= AssignMotion(layer.stateMachine, "ReloadStanding", reloadStanding);
                changed |= AssignMotion(
                    layer.stateMachine,
                    "ReloadCrouch",
                    reloadCrouch != null ? reloadCrouch : reloadStanding
                );
                changed |= AssignMotion(layer.stateMachine, "WeaponSwitch", weaponSwitch);

                AnimatorState standing = FindState(layer.stateMachine, "ReloadStanding");
                AnimatorState crouch = FindState(layer.stateMachine, "ReloadCrouch");
                changed |= ConfigureReloadSpeed(standing);
                changed |= ConfigureReloadSpeed(crouch);
            }

            controller.layers = layers;

            // Las capas legacy no deben volver a congelar el torso.
            changed |= SetLayerWeight(controller, "Actions", 0f);
            changed |= SetLayerWeight(controller, "WeaponUpperBody", 0f);
            changed |= SetLayerWeight(controller, "CrouchAimUpperBody", 0f);
            changed |= SetLayerWeight(controller, PlayerAnimationCoordinator.CombatLayerName, 1f);
            changed |= SetLayerWeight(controller, PlayerAnimationCoordinator.ActionsLayerName, 1f);

            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Editor First] Upper Body reparado: locomoción armada, Aim y Reload tienen Motion real y máscara de torso."
                );
            }

            // También garantiza Coordinator activo y PlayerAnimatorDriver legacy apagado.
            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }

        private static bool ConfigureReloadSpeed(AnimatorState state)
        {
            if (state == null)
                return false;

            bool changed = false;
            if (!state.speedParameterActive)
            {
                state.speedParameterActive = true;
                changed = true;
            }
            if (state.speedParameter != "ReloadSpeed")
            {
                state.speedParameter = "ReloadSpeed";
                changed = true;
            }
            if (changed)
                EditorUtility.SetDirty(state);
            return changed;
        }

        private static bool AssignMotion(
            AnimatorStateMachine machine,
            string stateName,
            Motion motion)
        {
            if (machine == null || motion == null)
                return false;

            AnimatorState state = FindState(machine, stateName);
            if (state == null || state.motion == motion)
                return false;

            state.motion = motion;
            EditorUtility.SetDirty(state);
            return true;
        }

        private static AnimatorState FindLegacyState(
            AnimatorController controller,
            string stateName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == PlayerAnimationCoordinator.ActionsLayerName)
                    continue;

                AnimatorState state = FindState(layers[i].stateMachine, stateName);
                if (state != null && state.motion != null)
                    return state;
            }
            return null;
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string stateName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorState state = FindState(layers[i].stateMachine, stateName);
                if (state != null && state.motion != null)
                    return state;
            }
            return null;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string stateName)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState found = FindState(children[i].stateMachine, stateName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static int FindLayer(AnimatorControllerLayer[] layers, string name)
        {
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == name)
                    return i;
            return -1;
        }

        private static bool SetLayerWeight(
            AnimatorController controller,
            string name,
            float weight)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            int index = FindLayer(layers, name);
            if (index < 0 || Mathf.Approximately(layers[index].defaultWeight, weight))
                return false;

            layers[index].defaultWeight = weight;
            controller.layers = layers;
            return true;
        }

        private static AnimationClip LoadClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;
                return clip;
            }
            return null;
        }
    }
}
