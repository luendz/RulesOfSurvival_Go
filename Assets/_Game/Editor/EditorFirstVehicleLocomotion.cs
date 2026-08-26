using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa un bloque Vehicle dentro de Locomotion sin crear layers por
    /// tipo de vehiculo. Todo queda visible y editable desde el Animator.
    ///
    /// Parametros:
    /// VehicleState: 0=None, 1=Seated, 2=Entering(reservado), 3=Exiting(reservado)
    /// VehicleRole : 0=None, 1=Driver, 2=Passenger
    /// VehicleStyle: 0=Generic, 1=Car, 2=Motorcycle, 3=ATV, 4=Boat, 5=Truck
    /// VehicleSpeed: 0..1
    /// VehicleSteer: -1..1
    ///
    /// La implementacion actual de VehicleSeat entra directamente a Seated.
    /// VehicleEnter/VehicleExit quedan preparados para cuando se asignen clips
    /// y se quiera activar una transicion de entrada/salida temporizada.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstVehicleLocomotion
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string DriverIdleClipPath =
            "Assets/_Game/Animations/Character Animator/11. Vehicle/Driver/Ch28_nonPBR@Driver Idle.fbx";

        private const string StateMachineName = "Vehicle";
        private const string VehicleStateParameter = "VehicleState";
        private const string VehicleRoleParameter = "VehicleRole";
        private const string VehicleStyleParameter = "VehicleStyle";
        private const string VehicleSpeedParameter = "VehicleSpeed";
        private const string VehicleSteerParameter = "VehicleSteer";

        static EditorFirstVehicleLocomotion()
        {
            EditorApplication.delayCall += ScheduleAfterAnimatorRefactors;
        }

        private static void ScheduleAfterAnimatorRefactors()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Vehicle Locomotion")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return;

            bool changed = false;
            changed |= EnsureParameter(
                controller,
                VehicleStateParameter,
                AnimatorControllerParameterType.Int
            );
            changed |= EnsureParameter(
                controller,
                VehicleRoleParameter,
                AnimatorControllerParameterType.Int
            );
            changed |= EnsureParameter(
                controller,
                VehicleStyleParameter,
                AnimatorControllerParameterType.Int
            );
            changed |= EnsureParameter(
                controller,
                VehicleSpeedParameter,
                AnimatorControllerParameterType.Float
            );
            changed |= EnsureParameter(
                controller,
                VehicleSteerParameter,
                AnimatorControllerParameterType.Float
            );

            AnimatorControllerLayer locomotionLayer = FindLayer(
                controller,
                PlayerAnimationCoordinator.LocomotionLayerName
            );
            if (locomotionLayer == null || locomotionLayer.stateMachine == null)
                return;

            AnimatorStateMachine root = locomotionLayer.stateMachine;
            AnimatorState locomotion = FindStateRecursive(root, "BT_Locomotion");
            if (locomotion == null)
                return;

            AnimatorStateMachine vehicle = FindChildStateMachine(root, StateMachineName);
            if (vehicle == null)
            {
                vehicle = root.AddStateMachine(
                    StateMachineName,
                    new Vector3(930f, 650f, 0f)
                );
                changed = true;
            }

            AnimatorState vehicleEnter = FindState(vehicle, "VehicleEnter");
            AnimatorState driver = FindState(vehicle, "Driver");
            AnimatorState passenger = FindState(vehicle, "Passenger");
            AnimatorState vehicleExit = FindState(vehicle, "VehicleExit");

            bool alreadyMaterialized =
                vehicleEnter != null &&
                driver != null &&
                passenger != null &&
                vehicleExit != null;

            if (!alreadyMaterialized)
            {
                if (vehicleEnter == null)
                {
                    vehicleEnter = vehicle.AddState(
                        "VehicleEnter",
                        new Vector3(240f, 40f, 0f)
                    );
                    vehicleEnter.writeDefaultValues = false;
                    changed = true;
                }

                if (driver == null)
                {
                    driver = vehicle.AddState(
                        "Driver",
                        new Vector3(520f, -30f, 0f)
                    );
                    driver.motion = LoadFirstAnimationClip(DriverIdleClipPath);
                    driver.writeDefaultValues = false;
                    changed = true;
                }

                if (passenger == null)
                {
                    passenger = vehicle.AddState(
                        "Passenger",
                        new Vector3(520f, 120f, 0f)
                    );
                    passenger.writeDefaultValues = false;
                    changed = true;
                }

                if (vehicleExit == null)
                {
                    vehicleExit = vehicle.AddState(
                        "VehicleExit",
                        new Vector3(800f, 40f, 0f)
                    );
                    vehicleExit.writeDefaultValues = false;
                    changed = true;
                }

                vehicle.defaultState = vehicleEnter;

                // Cualquier estado de locomocion puede entrar a Vehicle cuando
                // el asiento marca que el jugador ya pertenece a un vehiculo.
                AnimatorStateTransition enterVehicle =
                    root.AddAnyStateTransition(vehicle);
                ConfigureStateTransition(enterVehicle, 0.05f);
                enterVehicle.AddCondition(
                    AnimatorConditionMode.Greater,
                    0f,
                    VehicleStateParameter
                );

                AnimatorStateTransition enterToDriver = vehicleEnter.AddTransition(driver);
                ConfigureStateTransition(enterToDriver, 0.04f);
                enterToDriver.AddCondition(
                    AnimatorConditionMode.Equals,
                    1f,
                    VehicleStateParameter
                );
                enterToDriver.AddCondition(
                    AnimatorConditionMode.Equals,
                    1f,
                    VehicleRoleParameter
                );

                AnimatorStateTransition enterToPassenger =
                    vehicleEnter.AddTransition(passenger);
                ConfigureStateTransition(enterToPassenger, 0.04f);
                enterToPassenger.AddCondition(
                    AnimatorConditionMode.Equals,
                    1f,
                    VehicleStateParameter
                );
                enterToPassenger.AddCondition(
                    AnimatorConditionMode.Equals,
                    2f,
                    VehicleRoleParameter
                );

                // Reserva para una futura animacion de salida.
                AddIntTransition(driver, vehicleExit, VehicleStateParameter, 3, 0.04f);
                AddIntTransition(passenger, vehicleExit, VehicleStateParameter, 3, 0.04f);

                // El sistema actual sale de forma inmediata: VehicleState pasa
                // directamente a 0 y el Animator vuelve a locomocion.
                AddExitTransition(driver, VehicleStateParameter, 0, false);
                AddExitTransition(passenger, VehicleStateParameter, 0, false);

                AnimatorStateTransition animatedExit = vehicleExit.AddExitTransition();
                ConfigureStateTransition(animatedExit, 0.08f);
                animatedExit.hasExitTime = true;
                animatedExit.exitTime = 0.90f;
                animatedExit.AddCondition(
                    AnimatorConditionMode.Equals,
                    0f,
                    VehicleStateParameter
                );

                root.AddStateMachineTransition(vehicle, locomotion);
                changed = true;
            }
            else if (driver.motion == null)
            {
                AnimationClip driverIdle = LoadFirstAnimationClip(DriverIdleClipPath);
                if (driverIdle != null)
                {
                    driver.motion = driverIdle;
                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(vehicle);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Vehicle preparado dentro de Locomotion: " +
                "VehicleEnter / Driver / Passenger / VehicleExit."
            );
        }

        private static void AddIntTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
            int value,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureStateTransition(transition, duration);
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                value,
                parameter
            );
        }

        private static void AddExitTransition(
            AnimatorState source,
            string parameter,
            int value,
            bool useExitTime)
        {
            AnimatorStateTransition transition = source.AddExitTransition();
            ConfigureStateTransition(transition, 0.05f);
            transition.hasExitTime = useExitTime;
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                value,
                parameter
            );
        }

        private static bool EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != name)
                    continue;

                if (parameters[i].type == type)
                    return false;

                controller.RemoveParameter(i);
                controller.AddParameter(name, type);
                return true;
            }

            controller.AddParameter(name, type);
            return true;
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
            string stateName)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == stateName)
                    return state;
            }

            return null;
        }

        private static AnimatorState FindStateRecursive(
            AnimatorStateMachine machine,
            string stateName)
        {
            AnimatorState state = FindState(machine, stateName);
            if (state != null)
                return state;

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                state = FindStateRecursive(child, stateName);
                if (state != null)
                    return state;
            }

            return null;
        }

        private static void ConfigureStateTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
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
