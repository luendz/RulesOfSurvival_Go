using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Core;
using ROS.Game.Parachute;
using ROS.Game.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class ParachuteMatchStartTests
    {
        private const string ClassicAnimatorPath =
            "Assets/_Game/Animations/AC_Player_ROS_Classic.controller";

        [TestCase(32f, 32f, true)]
        [TestCase(12f, 32f, true)]
        [TestCase(33f, 32f, false)]
        [TestCase(float.PositiveInfinity, 32f, false)]
        public void AutoDeploy_UsesGroundClearance(
            float clearance,
            float threshold,
            bool expected
        )
        {
            Assert.That(
                ParachuteFlightMath.ShouldAutoDeploy(clearance, threshold),
                Is.EqualTo(expected)
            );
        }

        [TestCase(0.04f, 0.05f, 0.88f, false)]
        [TestCase(0.05f, 0.05f, 0.88f, true)]
        [TestCase(0.5f, 0.05f, 0.88f, true)]
        [TestCase(0.89f, 0.05f, 0.88f, false)]
        public void JumpWindow_UsesConfiguredFlightProgress(
            float progress,
            float minimum,
            float maximum,
            bool expected
        )
        {
            Assert.That(
                ParachuteFlightMath.CanJumpFromPlane(
                    progress,
                    minimum,
                    maximum
                ),
                Is.EqualTo(expected)
            );
        }

        [Test]
        public void MatchManager_TransitionsFromWarmupToGameplay()
        {
            GameObject gameObject = new GameObject("MatchManager_Test");

            try
            {
                BattleRoyaleManager manager =
                    gameObject.AddComponent<BattleRoyaleManager>();

                manager.BeginWarmup();
                Assert.That(manager.State, Is.EqualTo(MatchState.Warmup));

                manager.BeginPlanePhase();
                Assert.That(manager.State, Is.EqualTo(MatchState.Plane));

                manager.BeginGameplay();
                Assert.That(manager.State, Is.EqualTo(MatchState.Playing));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AirplaneModel_UsesRequestedLocalRotation()
        {
            Assert.That(
                AirplaneController.ModelEulerAngles,
                Is.EqualTo(new Vector3(-90f, -90f, 0f))
            );
        }

        [Test]
        public void ParachuteModel_UsesRequestedLocalRotation()
        {
            Assert.That(
                ParachuteController.ModelEulerAngles,
                Is.EqualTo(new Vector3(-120f, 0f, 0f))
            );
        }

        [Test]
        public void ParachuteCamera_UsesCloserWideView()
        {
            Assert.That(
                ThirdPersonCamera.DefaultAirDropDistanceMultiplier,
                Is.EqualTo(5.5f)
            );
        }

        [Test]
        public void ParachuteCamera_DistanceCanBeConfiguredInInspector()
        {
            var field = typeof(ThirdPersonCamera).GetField(
                "airDropDistanceMultiplier",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic
            );

            Assert.That(field, Is.Not.Null);
            Assert.That(
                field.IsDefined(typeof(SerializeField), false),
                Is.True
            );
        }

        [Test]
        public void ClassicAnimator_ContainsRuntimeAirDropAndGestureStates()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ClassicAnimatorPath
                );

            Assert.That(controller, Is.Not.Null);
            AssertParameter(
                controller,
                "IsFreeFalling",
                AnimatorControllerParameterType.Bool
            );
            AssertParameter(
                controller,
                "IsParachuting",
                AnimatorControllerParameterType.Bool
            );
            AssertParameter(
                controller,
                "FullBodyAction",
                AnimatorControllerParameterType.Int
            );
            AssertParameter(
                controller,
                "ParachuteState",
                AnimatorControllerParameterType.Int
            );
            AssertParameter(
                controller,
                "AirDropAnimationComplete",
                AnimatorControllerParameterType.Bool
            );

            string[] gestureStates =
            {
                "Gesture_Dancing",
                "Gesture_Fishing_Cast",
                "Gesture_Hip_Hop_Dancing",
                "Gesture_Joyful_Jump",
                "Gesture_Opening",
                "Gesture_Rumba_Dancing",
                "Gesture_Salute",
                "Gesture_Talking_On_Phone",
                "Gesture_Waving_Gesture"
            };

            for (int i = 0; i < gestureStates.Length; i++)
            {
                AnimatorState state = FindState(controller, gestureStates[i]);
                Assert.That(state, Is.Not.Null, gestureStates[i]);
                Assert.That(state.motion, Is.Not.Null, gestureStates[i]);
            }

            AnimatorControllerLayer fullBody = FindLayer(
                controller,
                "FullBody_Actions"
            );
            Assert.That(fullBody, Is.Not.Null);

            AnimatorStateMachine airDrop = FindStateMachine(
                fullBody.stateMachine,
                "AirDrop"
            );
            Assert.That(airDrop, Is.Not.Null);
            Assert.That(airDrop.defaultState, Is.Not.Null);
            Assert.That(airDrop.defaultState.name, Is.EqualTo("FreeFall Enter"));
            Assert.That(airDrop.defaultState.motion, Is.Not.Null);

            string[] runtimeAirDropStates =
            {
                "FreeFall Enter",
                "FreeFall",
                "Parachute Deploy",
                "Parachute Glide",
                "Parachute Land"
            };

            for (int i = 0; i < runtimeAirDropStates.Length; i++)
            {
                AnimatorState state = FindState(
                    airDrop,
                    runtimeAirDropStates[i]
                );
                Assert.That(state, Is.Not.Null, runtimeAirDropStates[i]);
                Assert.That(state.motion, Is.Not.Null, runtimeAirDropStates[i]);
            }

            Assert.That(
                FindState(airDrop, "FreeFall Enter").transitions.Length,
                Is.EqualTo(3)
            );
            Assert.That(
                FindState(airDrop, "FreeFall").transitions.Length,
                Is.EqualTo(3)
            );
            Assert.That(
                FindState(airDrop, "Parachute Deploy").transitions.Length,
                Is.EqualTo(2)
            );
            Assert.That(
                FindState(airDrop, "Parachute Glide").transitions.Length,
                Is.EqualTo(1)
            );

            AnimatorStateMachine vehicle = FindStateMachine(
                fullBody.stateMachine,
                "Vehicle"
            );
            Assert.That(vehicle, Is.Not.Null);
            Assert.That(vehicle.defaultState, Is.Not.Null);
            Assert.That(vehicle.defaultState.name, Is.EqualTo("Driver"));
            Assert.That(vehicle.defaultState.motion, Is.Not.Null);
        }

        [Test]
        public void ClassicAnimator_ContainsVisibleFastWeaponSwitchFlow()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ClassicAnimatorPath
                );

            Assert.That(controller, Is.Not.Null);
            AssertParameter(
                controller,
                "IsSwitchingWeapon",
                AnimatorControllerParameterType.Bool
            );
            AssertParameter(
                controller,
                "WeaponSwitch",
                AnimatorControllerParameterType.Trigger
            );

            AnimatorControllerLayer actions = FindLayer(
                controller,
                "UpperBody_Actions"
            );
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions.defaultWeight, Is.EqualTo(1f));

            AnimatorStateMachine weapon = FindStateMachine(
                actions.stateMachine,
                "Weapon"
            );
            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon.defaultState, Is.Not.Null);
            Assert.That(
                weapon.defaultState.name,
                Is.EqualTo("RifleSwitch_UpperBody")
            );
            Assert.That(weapon.defaultState.motion, Is.Not.Null);
            Assert.That(weapon.defaultState.speed, Is.EqualTo(3f));

            AnimatorState empty = actions.stateMachine.defaultState;
            Assert.That(empty, Is.Not.Null);

            AnimatorStateTransition switchTransition = null;
            AnimatorStateTransition[] transitions = empty.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i].destinationStateMachine == weapon)
                {
                    switchTransition = transitions[i];
                    break;
                }
            }

            Assert.That(switchTransition, Is.Not.Null);
            Assert.That(switchTransition.duration, Is.EqualTo(0.03f));

            bool hasSwitchingCondition = false;
            AnimatorCondition[] conditions = switchTransition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                hasSwitchingCondition |=
                    conditions[i].parameter == "IsSwitchingWeapon";
            }

            Assert.That(hasSwitchingCondition, Is.True);
            Assert.That(conditions.Length, Is.EqualTo(1));
        }

        [Test]
        public void ParachuteController_DoesNotWriteAnimatorParametersDirectly()
        {
            string source = System.IO.File.ReadAllText(
                "Assets/_Game/Code/Parachute/ParachuteController.cs"
            );

            Assert.That(source, Does.Not.Contain("SetBool("));
            Assert.That(source, Does.Not.Contain("SetInteger("));
            Assert.That(source, Does.Not.Contain("SetTrigger("));
        }

        [Test]
        public void StaticSedan_HasConfiguredBattleRoyalePlacement()
        {
            Assert.That(
                BattleRoyaleSceneLayout.SedanPosition.y,
                Is.GreaterThanOrEqualTo(0f)
            );
            Assert.That(
                BattleRoyaleSceneLayout.SedanPosition,
                Is.Not.EqualTo(Vector3.zero)
            );
        }

        private static void AssertParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name && parameters[i].type == type)
                    return;
            }

            Assert.Fail($"Missing Animator parameter: {name} ({type}).");
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string name)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == name)
                    return layers[i];
            }

            return null;
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string name)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorState state = FindState(layers[i].stateMachine, name);
                if (state != null)
                    return state;
            }

            return null;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == name)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                AnimatorState state = FindState(child, name);
                if (state != null)
                    return state;
            }

            return null;
        }

        private static AnimatorStateMachine FindStateMachine(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                if (child.name == name)
                    return child;

                AnimatorStateMachine nested = FindStateMachine(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
