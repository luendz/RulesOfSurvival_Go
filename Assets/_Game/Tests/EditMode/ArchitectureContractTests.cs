using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class ArchitectureContractTests
    {
        [Test]
        public void RuntimeCode_UsesExplicitReferencesAndSingleAnimatorWriter()
        {
            string root = Path.Combine(Application.dataPath, "_Game", "Code");
            List<string> violations = new List<string>();

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/Editor/")) continue;

                string source = File.ReadAllText(path);
                foreach (string token in new[]
                {
                    "RuntimeInitializeOnLoadMethod",
                    "Resources.Load",
                    "FindFirstObjectByType",
                    "FindAnyObjectByType",
                    "GameObject.Find(",
                    "Camera.main"
                })
                    if (source.Contains(token)) violations.Add($"{token}: {normalized}");

                bool allowedFactory = normalized.EndsWith("/Loot/DeathLootContainer.cs") ||
                                      normalized.EndsWith("/Loot/LootPickup.cs") ||
                                      normalized.EndsWith("/World/EchoValleyMapAuthoring.cs");
                if (!allowedFactory && source.Contains("AddComponent<"))
                    violations.Add($"AddComponent: {normalized}");

                bool writesAnimator = source.Contains("animator.SetBool(") ||
                                      source.Contains("animator.SetFloat(") ||
                                      source.Contains("animator.SetInteger(") ||
                                      source.Contains("animator.SetTrigger(") ||
                                      source.Contains("animator.CrossFade(") ||
                                      source.Contains("animator.SetLayerWeight(");
                if (writesAnimator && !normalized.EndsWith("/Animation/PlayerAnimationCoordinator.cs"))
                    violations.Add($"Animator writer: {normalized}");
            }

            Assert.That(violations, Is.Empty, string.Join("\n", violations));
        }
    }
}
