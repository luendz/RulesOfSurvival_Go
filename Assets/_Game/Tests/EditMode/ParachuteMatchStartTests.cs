using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Core;
using ROS.Game.Parachute;
using ROS.Game.World;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class ParachuteMatchStartTests
    {
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
        public void StaticSedan_HasConfiguredBattleRoyalePlacement()
        {
            Assert.That(
                BattleRoyaleSetDressingBootstrap.SedanPosition.y,
                Is.GreaterThanOrEqualTo(0f)
            );
            Assert.That(
                BattleRoyaleSetDressingBootstrap.SedanPosition,
                Is.Not.EqualTo(Vector3.zero)
            );
        }
    }
}
