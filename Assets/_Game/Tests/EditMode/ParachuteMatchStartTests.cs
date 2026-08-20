using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.Core;
using ROS.Game.Parachute;
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
    }
}
