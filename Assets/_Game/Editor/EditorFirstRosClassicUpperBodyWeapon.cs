using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 08 del Animator ROS Classic.
    ///
    /// Materializa el layer UpperBody_Weapon separado de Base_Locomotion.
    /// La locomocion de piernas sigue reproduciendose en el layer 0 mientras
    /// este layer controla torso/brazos/manos mediante AM_ROS_UpperBody.
    ///
    /// WeaponType es un selector interno del nuevo Animator:
    /// 0 Unarmed, 1 Rifle, 2 Pistol, 3 Shotgun, 4 Sniper, 5 Melee, 6 Throwable.
    /// Estos numeros son una convencion del proyecto, no valores documentados
    /// del Rules of Survival original.
    ///
    /// Los motions de Rifle se recuperan del AC_Player_Prototype cuando existen
    /// (Firearm_Hip, Firearm_Aim y Firearm_ReloadStanding/Crouch). No se inventan
    /// clips para Pistol/Shotgun/Sniper/Throwable ni para ataques que no existan.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicUpperBodyWeapon
    {
        private const string PrototypeControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const int WeaponUnarmed = 0;
        private const int WeaponRifle = 1;
        private const int WeaponPistol = 2;
        private const int WeaponShotgun = 3;
        private const int WeaponSniper = 4;
        private const int WeaponMelee = 5;
        private const int WeaponThrowable = 6;

        static EditorFirstRosClassicUpperBodyWeapon()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/08 - Construir UpperBody Weapon")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            // Garantiza 01 -> 07. Todos los pasos anteriores son idempotentes y
            // respetan cualquier ajuste manual ya materializado.
            EditorFirstRosClassicAirborne.Materialize();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            AnimatorControllerLayer weaponLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyWeaponLayer
            );

            if (weaponLayer == null || weaponLayer.stateMachine == null)
                return;

            AnimatorStateMachine root = weaponLayer.stateMachine;

            // Ya materializado: no reconstruir para no pisar motions/transiciones
            // que el usuario haya ajustado manualmente desde Unity.
            if (FindChildStateMachine(root, "Rifle") != null &&
                FindChildStateMachine(root, "Melee") != null &&
                FindChildStateMachine(root, "Throwable") != null)
            {
                return;
            }

            EnsureParameter(controller, "WeaponType", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "IsAiming", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsFiring", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsReloading", AnimatorControllerParameterType.Bool);

            AnimatorController prototype =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(PrototypeControllerPath);

            Motion rifleIdleMotion = FindMotionRecursive(
                prototype,
                "Firearm_Hip",
                "ArmedLocomotion",
                "ArmedCrouch"
            );
            Motion rifleAimMotion = FindMotionRecursive(
                prototype,
                "Firearm_Aim",
                "AimLocomotion"
            );
            Motion rifleReloadMotion = FindMotionRecursive(
                prototype,
                "Firearm_ReloadStanding",
                "ReloadStanding",
                "RifleReload Standing Hip",
                "RifleReloadStandingHip"
            );
            Motion rifleReloadCrouchMotion = FindMotionRecursive(
                prototype,
                "Firearm_ReloadCrouch",
                "ReloadCrouch",
                "RifleReload Crouch Hip",
                "RifleReloadCrouchHip"
            );
            Motion meleeIdleMotion = FindMotionRecursive(
                prototype,
                "Melee_Hold"
            );

            ClearRootStatesAndChildMachines(root);

            AnimatorState selector = root.AddState(
                "Weapon Selector",
                new Vector3(180f, 60f, 0f)
            );
            selector.motion = null;
            selector.writeDefaultValues = false;
            root.defaultState = selector;

            AnimatorStateMachine unarmed = CreateUnarmed(root);
            AnimatorStateMachine rifle = CreateFirearmFamily(
                root,
                "Rifle",
                rifleIdleMotion,
                rifleAimMotion,
                rifleReloadMotion,
                new Vector3(460f, -260f, 0f)
            );
            AnimatorStateMachine pistol = CreateFirearmFamily(
                root,
                "Pistol",
                null,
                null,
                null,
                new Vector3(700f, -260f, 0f)
            );
            AnimatorStateMachine shotgun = CreateFirearmFamily(
                root,
                "Shotgun",
                null,
                null,
                null,
                new Vector3(940f, -260f, 0f)
            );
            AnimatorStateMachine sniper = CreateFirearmFamily(
                root,
                "Sniper",
                null,
                null,
                null,
                new Vector3(1180f, -260f, 0f)
            );
            AnimatorStateMachine melee = CreateMelee(
                root,
                meleeIdleMotion,
                new Vector3(1420f, -260f, 0f)
            );
            AnimatorStateMachine throwable = CreateThrowable(
                root,
                new Vector3(1660f, -260f, 0f)
            );

            AddSelectorTransition(selector, unarmed, WeaponUnarmed);
            AddSelectorTransition(selector, rifle, WeaponRifle);
            AddSelectorTransition(selector, pistol, WeaponPistol);
            AddSelectorTransition(selector, shotgun, WeaponShotgun);
            AddSelectorTransition(selector, sniper, WeaponSniper);
            AddSelectorTransition(selector, melee, WeaponMelee);
            AddSelectorTransition(selector, throwable, WeaponThrowable);

            AnimatorStateMachine[] families =
            {
                unarmed,
                rifle,
                pistol,
                shotgun,
                sniper,
                melee,
                throwable
            };

            int[] familyValues =
            {
                WeaponUnarmed,
                WeaponRifle,
                WeaponPistol,
                WeaponShotgun,
                WeaponSniper,
                WeaponMelee,
                WeaponThrowable
            };

            // Cambio directo entre familias. WeaponType solo decide la familia;
            // Aim/Fire/Reload siguen siendo estados internos del torso.
            for (int from = 0; from < families.Length; from++)
            {
                for (int to = 0; to < families.Length; to++)
                {
                    if (from == to)
                        continue;

                    AnimatorTransition transition =
                        root.AddStateMachineTransition(families[from], families[to]);
                    transition.AddCondition(
                        AnimatorConditionMode.Equals,
                        familyValues[to],
                        "WeaponType"
                    );
                }
            }

            EditorUtility.SetDirty(selector);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] UpperBody_Weapon creado: " +
                "Unarmed/Rifle/Pistol/Shotgun/Sniper/Melee/Throwable. " +
                "Base_Locomotion permanece independiente."
            );

            if (rifleIdleMotion == null || rifleAimMotion == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No se pudieron recuperar todas las " +
                    "poses Rifle Hip/Aim del AC_Player_Prototype. Los estados " +
                    "faltantes quedan Motion=None para asignarlos manualmente."
                );
            }

            if (rifleReloadMotion == null && rifleReloadCrouchMotion == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Rifle Reload queda Motion=None porque " +
                    "no se encontro una recarga reutilizable en el Animator anterior."
                );
            }

            Debug.LogWarning(
                "[ROS Classic Animator] Pistol, Shotgun, Sniper y Throwable no " +
                "reciben clips inventados. Sus estados quedan preparados como " +
                "Motion=None hasta disponer de animaciones reales."
            );
        }

        private static AnimatorStateMachine CreateUnarmed(AnimatorStateMachine root)
        {
            AnimatorStateMachine machine = root.AddStateMachine(
                "Unarmed",
                new Vector3(220f, -260f, 0f)
            );

            AnimatorState idle = AddState(machine, "Upper Idle", null, 240f, 20f);
            AnimatorState aim = AddState(machine, "Aim", null, 500f, -60f);
            AnimatorState punch = AddState(machine, "Fire / Punch", null, 500f, 90f);
            machine.defaultState = idle;

            AddBoolTransition(idle, aim, "IsAiming", true, 0.06f);
            AddBoolTransition(aim, idle, "IsAiming", false, 0.06f);
            AddBoolTransition(idle, punch, "IsFiring", true, 0.03f);
            AddBoolTransition(aim, punch, "IsFiring", true, 0.03f);

            AnimatorStateTransition punchToAim = punch.AddTransition(aim);
            ConfigureTransition(punchToAim, 0.04f);
            punchToAim.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            punchToAim.AddCondition(AnimatorConditionMode.If, 0f, "IsAiming");

            AnimatorStateTransition punchToIdle = punch.AddTransition(idle);
            ConfigureTransition(punchToIdle, 0.04f);
            punchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            punchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAiming");

            return machine;
        }

        private static AnimatorStateMachine CreateFirearmFamily(
            AnimatorStateMachine root,
            string familyName,
            Motion idleMotion,
            Motion aimMotion,
            Motion reloadMotion,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine(familyName, position);

            AnimatorState idle = AddState(
                machine,
                familyName + " Idle",
                idleMotion,
                240f,
                20f
            );
            AnimatorState aim = AddState(
                machine,
                familyName + " Aim",
                aimMotion,
                500f,
                -70f
            );
            AnimatorState fire = AddState(
                machine,
                familyName + " Fire",
                null,
                760f,
                -70f
            );
            AnimatorState reload = AddState(
                machine,
                familyName + " Reload",
                reloadMotion,
                500f,
                100f
            );

            machine.defaultState = idle;

            AddBoolTransition(idle, reload, "IsReloading", true, 0.04f);
            AddBoolTransition(aim, reload, "IsReloading", true, 0.04f);
            AddBoolTransition(fire, reload, "IsReloading", true, 0.03f);

            AddBoolTransition(idle, aim, "IsAiming", true, 0.06f);
            AddBoolTransition(aim, idle, "IsAiming", false, 0.06f);

            AddBoolTransition(idle, fire, "IsFiring", true, 0.02f);
            AddBoolTransition(aim, fire, "IsFiring", true, 0.02f);

            AnimatorStateTransition fireToAim = fire.AddTransition(aim);
            ConfigureTransition(fireToAim, 0.03f);
            fireToAim.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            fireToAim.AddCondition(AnimatorConditionMode.If, 0f, "IsAiming");
            fireToAim.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsReloading");

            AnimatorStateTransition fireToIdle = fire.AddTransition(idle);
            ConfigureTransition(fireToIdle, 0.03f);
            fireToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            fireToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAiming");
            fireToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsReloading");

            AnimatorStateTransition reloadToAim = reload.AddTransition(aim);
            ConfigureTransition(reloadToAim, 0.05f);
            reloadToAim.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsReloading");
            reloadToAim.AddCondition(AnimatorConditionMode.If, 0f, "IsAiming");

            AnimatorStateTransition reloadToIdle = reload.AddTransition(idle);
            ConfigureTransition(reloadToIdle, 0.05f);
            reloadToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsReloading");
            reloadToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAiming");

            return machine;
        }

        private static AnimatorStateMachine CreateMelee(
            AnimatorStateMachine root,
            Motion meleeIdleMotion,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Melee", position);

            AnimatorState idle = AddState(
                machine,
                "Melee Idle",
                meleeIdleMotion,
                240f,
                20f
            );
            AnimatorState light = AddState(
                machine,
                "Light Attack",
                null,
                520f,
                -50f
            );
            AddState(machine, "Heavy Attack", null, 520f, 100f);
            machine.defaultState = idle;

            // IsFiring se usa como ataque primario movil. Heavy Attack queda sin
            // condicion hasta definir una entrada propia; no inventamos un trigger.
            AddBoolTransition(idle, light, "IsFiring", true, 0.03f);
            AddBoolTransition(light, idle, "IsFiring", false, 0.04f);

            return machine;
        }

        private static AnimatorStateMachine CreateThrowable(
            AnimatorStateMachine root,
            Vector3 position)
        {
            AnimatorStateMachine machine = root.AddStateMachine("Throwable", position);

            AnimatorState idle = AddState(machine, "Throw Idle", null, 240f, 20f);
            AddState(machine, "Throw Ready", null, 500f, -110f);
            AddState(machine, "Throw Aim", null, 500f, -20f);
            AddState(machine, "Throw Release", null, 500f, 70f);
            AddState(machine, "Throw Cancel", null, 500f, 160f);
            machine.defaultState = idle;

            // No se conectan Ready/Cancel con parametros inventados. El bloque
            // queda visible y listo para la fase especifica de Throwables.
            return machine;
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

        private static void AddSelectorTransition(
            AnimatorState selector,
            AnimatorStateMachine destination,
            int weaponType)
        {
            AnimatorStateTransition transition = selector.AddTransition(destination);
            ConfigureTransition(transition, 0.03f);
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                weaponType,
                "WeaponType"
            );
        }

        private static void AddBoolTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            bool value,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, duration);
            transition.AddCondition(
                value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                parameter
            );
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
