using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 12 del Animator ROS Classic.
    ///
    /// Separa las acciones temporales del torso de la pose permanente del arma:
    /// UpperBody_Weapon conserva Idle/Aim/Fire y UpperBody_Actions toma Reload,
    /// Switch, consumibles e interacciones. De esta forma la locomocion de piernas
    /// continua reproduciendose mientras una accion de torso esta activa.
    ///
    /// Solo se conectan automaticamente estados con soporte runtime y motion real.
    /// Los huecos de Pistol/Shotgun/Sniper/Throwable/Revive quedan visibles como
    /// Motion=None y sin transiciones inventadas.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicUpperBodyActions
    {
        private const string PrototypeControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string ReloadStandingPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadStandingHip.fbx";

        private const string ReloadCrouchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadCrouchHip.fbx";

        private const string WeaponSwitchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleSwitch_UpperBody.fbx";

        private const string TakingItemPath =
            "Assets/_Game/Animations/Character Animator/04. Interaction/Ch28_nonPBR@Taking Item.fbx";

        private const string GenericHealingPath =
            "Assets/_Game/Animations/Character Animator/05. Consumables - Healing/Ch28_nonPBR@Arm Stretching.fbx";

        private const string DrinkingPath =
            "Assets/_Game/Animations/Character Animator/05. Consumables - Healing/Ch28_nonPBR@Drinking.fbx";

        private const int WeaponRifle = 1;
        private const int StanceStanding = 0;
        private const int StanceCrouch = 1;

        static EditorFirstRosClassicUpperBodyActions()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/12 - Construir UpperBody Actions")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstRosClassicUpperBodyWeapon.Materialize();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            AnimatorControllerLayer actionsLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyActionsLayer
            );

            AnimatorControllerLayer weaponLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyWeaponLayer
            );

            if (actionsLayer == null || actionsLayer.stateMachine == null)
                return;

            EnsureParameters(controller);

            bool migratedWeaponLayer = RemoveReloadStatesFromWeaponLayer(weaponLayer);
            AnimatorStateMachine root = actionsLayer.stateMachine;

            bool alreadyBuilt =
                FindChildStateMachine(root, "Reload") != null &&
                FindChildStateMachine(root, "Weapon") != null &&
                FindChildStateMachine(root, "Consumables") != null &&
                FindChildStateMachine(root, "Throwable") != null &&
                FindChildStateMachine(root, "Interaction") != null;

            if (alreadyBuilt)
            {
                if (migratedWeaponLayer)
                {
                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                }
                return;
            }

            AnimatorController prototype =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(PrototypeControllerPath);

            Motion reloadStanding = FindMotionRecursive(
                prototype,
                "Firearm_ReloadStanding",
                "ReloadStanding",
                "RifleReload Standing Hip",
                "RifleReloadStandingHip"
            ) ?? LoadFirstAnimationClip(ReloadStandingPath);

            Motion reloadCrouch = FindMotionRecursive(
                prototype,
                "Firearm_ReloadCrouch",
                "ReloadCrouch",
                "RifleReload Crouch Hip",
                "RifleReloadCrouchHip"
            ) ?? LoadFirstAnimationClip(ReloadCrouchPath);

            Motion weaponSwitch = FindMotionRecursive(
                prototype,
                "WeaponSwitch",
                "RifleSwitch_UpperBody"
            ) ?? LoadFirstAnimationClip(WeaponSwitchPath);

            Motion genericHealing = FindMotionRecursive(
                prototype,
                "Healing"
            ) ?? LoadFirstAnimationClip(GenericHealingPath);

            Motion takingItem = FindMotionRecursive(
                prototype,
                "TakingItem"
            ) ?? LoadFirstAnimationClip(TakingItemPath);

            Motion drinking = LoadFirstAnimationClip(DrinkingPath);

            ClearRootStatesAndChildMachines(root);

            AnimatorState empty = AddState(root, "Empty", null, 180f, 40f);
            root.defaultState = empty;

            AnimatorStateMachine reload = BuildReloadMachine(
                root,
                reloadStanding,
                reloadCrouch,
                new Vector3(480f, -280f, 0f)
            );

            AnimatorStateMachine weapon = BuildWeaponMachine(
                root,
                weaponSwitch,
                new Vector3(720f, -280f, 0f)
            );

            AnimatorStateMachine consumables = BuildConsumablesMachine(
                root,
                genericHealing,
                drinking,
                new Vector3(960f, -280f, 0f)
            );

            AnimatorStateMachine throwable = BuildThrowableMachine(
                root,
                new Vector3(1200f, -280f, 0f)
            );

            AnimatorStateMachine interaction = BuildInteractionMachine(
                root,
                takingItem,
                new Vector3(1440f, -280f, 0f)
            );

            if (reloadStanding != null || reloadCrouch != null)
            {
                AnimatorStateTransition toReload = empty.AddTransition(reload);
                ConfigureTransition(toReload, 0.04f);
                toReload.AddCondition(AnimatorConditionMode.If, 0f, "IsReloading");
                toReload.AddCondition(AnimatorConditionMode.Equals, WeaponRifle, "WeaponType");

                AnimatorTransition fromReload =
                    root.AddStateMachineTransition(reload, empty);
                fromReload.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsReloading");
            }

            if (weaponSwitch != null)
            {
                AnimatorStateTransition toWeapon = empty.AddTransition(weapon);
                ConfigureTransition(toWeapon, 0.04f);
                toWeapon.AddCondition(AnimatorConditionMode.If, 0f, "IsSwitchingWeapon");

                AnimatorTransition fromWeapon =
                    root.AddStateMachineTransition(weapon, empty);
                fromWeapon.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSwitchingWeapon");
            }

            if (genericHealing != null)
            {
                AnimatorStateTransition toConsumables = empty.AddTransition(consumables);
                ConfigureTransition(toConsumables, 0.05f);
                toConsumables.AddCondition(AnimatorConditionMode.If, 0f, "IsUsingConsumable");

                AnimatorTransition fromConsumables =
                    root.AddStateMachineTransition(consumables, empty);
                fromConsumables.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsUsingConsumable");
            }

            if (takingItem != null)
            {
                AnimatorStateTransition toInteraction = empty.AddTransition(interaction);
                ConfigureTransition(toInteraction, 0.04f);
                toInteraction.AddCondition(AnimatorConditionMode.If, 0f, "IsPickingUp");

                AnimatorTransition fromInteraction =
                    root.AddStateMachineTransition(interaction, empty);
                fromInteraction.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsPickingUp");
            }

            // Throwable queda preparado pero sin transicion: todavia no existe un
            // controlador runtime de lanzables que permita conectar Equip/Prepare/
            // Aim/Throw/Cancel sin inventar estados de gameplay.
            _ = throwable;

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] UpperBody_Actions creado: Reload, Weapon, " +
                "Consumables, Throwable e Interaction. Reload fue retirado de " +
                "UpperBody_Weapon para mantener la composicion por capas."
            );

            if (reloadStanding == null || reloadCrouch == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Falta al menos una variante Rifle Reload " +
                    "Standing/Crouch. La variante ausente queda Motion=None."
                );
            }

            Debug.LogWarning(
                "[ROS Classic Animator] Pistol/Shotgun/Sniper Reload, Draw/Holster, " +
                "Throwables y Revive permanecen Motion=None y sin rutas inventadas. " +
                "Drink dispone del clip real Drinking, pero queda sin seleccion " +
                "runtime hasta que ConsumableDefinition exponga un tipo de animacion."
            );
        }

        private static AnimatorStateMachine BuildReloadMachine(
            AnimatorStateMachine root,
            Motion rifleStanding,
            Motion rifleCrouch,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Reload", position);

            AnimatorState standing = AddState(
                machine,
                "Rifle Reload Standing",
                rifleStanding,
                240f,
                -80f
            );
            AnimatorState crouch = AddState(
                machine,
                "Rifle Reload Crouch",
                rifleCrouch,
                240f,
                40f
            );

            AddState(machine, "SMG Reload", null, 520f, -140f);
            AddState(machine, "Shotgun Reload", null, 520f, -50f);
            AddState(machine, "Sniper Reload", null, 520f, 40f);
            AddState(machine, "Pistol Reload", null, 520f, 130f);

            machine.defaultState = standing;

            if (rifleStanding != null)
            {
                standing.speedParameterActive = true;
                standing.speedParameter = "ReloadSpeed";

                AnimatorTransition entryStanding = machine.AddEntryTransition(standing);
                entryStanding.AddCondition(AnimatorConditionMode.Equals, WeaponRifle, "WeaponType");
                entryStanding.AddCondition(AnimatorConditionMode.Equals, StanceStanding, "Stance");
            }

            if (rifleCrouch != null)
            {
                crouch.speedParameterActive = true;
                crouch.speedParameter = "ReloadSpeed";

                AnimatorTransition entryCrouch = machine.AddEntryTransition(crouch);
                entryCrouch.AddCondition(AnimatorConditionMode.Equals, WeaponRifle, "WeaponType");
                entryCrouch.AddCondition(AnimatorConditionMode.Equals, StanceCrouch, "Stance");
            }

            return machine;
        }

        private static AnimatorStateMachine BuildWeaponMachine(
            AnimatorStateMachine root,
            Motion switchMotion,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Weapon", position);

            AddState(machine, "Draw Weapon", null, 240f, -80f);
            AddState(machine, "Holster Weapon", null, 240f, 40f);
            AnimatorState weaponSwitch = AddState(
                machine,
                "Switch Weapon",
                switchMotion,
                520f,
                -20f
            );

            machine.defaultState = weaponSwitch;
            return machine;
        }

        private static AnimatorStateMachine BuildConsumablesMachine(
            AnimatorStateMachine root,
            Motion genericHealing,
            Motion drinking,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Consumables", position);

            AnimatorState generic = AddState(
                machine,
                "Consumable Use",
                genericHealing,
                240f,
                -120f
            );

            AddState(machine, "Bandage", null, 520f, -140f);
            AddState(machine, "MedKit", null, 520f, -50f);
            AddState(machine, "Drink", drinking, 520f, 40f);
            AddState(machine, "Booster", null, 520f, 130f);

            machine.defaultState = generic;
            return machine;
        }

        private static AnimatorStateMachine BuildThrowableMachine(
            AnimatorStateMachine root,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Throwable", position);

            AnimatorState equip = AddState(machine, "Equip", null, 240f, -160f);
            AddState(machine, "Prepare", null, 500f, -80f);
            AddState(machine, "Aim", null, 500f, 0f);
            AddState(machine, "Throw", null, 500f, 80f);
            AddState(machine, "Cancel", null, 500f, 160f);
            machine.defaultState = equip;

            return machine;
        }

        private static AnimatorStateMachine BuildInteractionMachine(
            AnimatorStateMachine root,
            Motion pickupMotion,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Interaction", position);

            AnimatorState pickup = AddState(
                machine,
                "Pickup",
                pickupMotion,
                240f,
                -50f
            );
            AddState(machine, "Revive", null, 500f, 50f);
            machine.defaultState = pickup;

            return machine;
        }

        private static bool RemoveReloadStatesFromWeaponLayer(
            AnimatorControllerLayer weaponLayer)
        {
            if (weaponLayer == null || weaponLayer.stateMachine == null)
                return false;

            bool changed = false;
            string[] firearmFamilies = { "Rifle", "Pistol", "Shotgun", "Sniper" };

            for (int i = 0; i < firearmFamilies.Length; i++)
            {
                AnimatorStateMachine family = FindChildStateMachine(
                    weaponLayer.stateMachine,
                    firearmFamilies[i]
                );

                if (family == null)
                    continue;

                AnimatorState reload = FindState(
                    family,
                    firearmFamilies[i] + " Reload"
                );

                if (reload == null)
                    continue;

                family.RemoveState(reload);
                EditorUtility.SetDirty(family);
                changed = true;
            }

            return changed;
        }

        private static void EnsureParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "WeaponType", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "Stance", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "IsReloading", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "ReloadSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsSwitchingWeapon", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsUsingConsumable", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsPickingUp", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "WeaponSwitch", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "PickupItem", AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            Motion motion,
            float x,
            float y)
        {
            AnimatorState state = machine.AddState(name, new Vector3(x, y, 0f));
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            transition.exitTime = 0f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
        }

        private static void ClearRootStatesAndChildMachines(AnimatorStateMachine root)
        {
            ChildAnimatorState[] states = root.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                if (states[i].state != null)
                    root.RemoveState(states[i].state);
            }

            ChildAnimatorStateMachine[] machines = root.stateMachines;
            for (int i = machines.Length - 1; i >= 0; i--)
            {
                if (machines[i].stateMachine != null)
                    root.RemoveStateMachine(machines[i].stateMachine);
            }
        }

        private static Motion FindMotionRecursive(
            AnimatorController controller,
            params string[] stateNames)
        {
            if (controller == null || stateNames == null || stateNames.Length == 0)
                return null;

            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                Motion found = FindMotionRecursive(layers[i].stateMachine, stateNames);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Motion FindMotionRecursive(
            AnimatorStateMachine machine,
            string[] stateNames)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state.motion == null)
                    continue;

                for (int nameIndex = 0; nameIndex < stateNames.Length; nameIndex++)
                {
                    if (string.Equals(
                        state.name,
                        stateNames[nameIndex],
                        StringComparison.Ordinal
                    ))
                    {
                        return state.motion;
                    }
                }
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                Motion found = FindMotionRecursive(children[i].stateMachine, stateNames);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string layerName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                    return layers[i];
            }

            return null;
        }

        private static AnimatorStateMachine FindChildStateMachine(
            AnimatorStateMachine parent,
            string name)
        {
            if (parent == null)
                return null;

            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == name)
                    return state;
            }

            return null;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }
    }
}
