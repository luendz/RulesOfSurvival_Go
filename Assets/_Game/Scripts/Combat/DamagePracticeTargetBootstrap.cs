using System.Collections;
using ROS.Game.Core;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Combat
{
    public static class DamagePracticeTargetBootstrap
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const string TargetName = "Objetivo_Dano_Demo";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // Desactivado: objetivo de práctica eliminado de la escena
            return;

#pragma warning disable CS0162
            if (SceneManager.GetActiveScene().name != SceneName ||
                GameObject.Find(TargetName) != null)
            {
                return;
            }
#pragma warning restore CS0162

            GameObject runner = new GameObject(
                "DamagePracticeTargetBootstrap"
            );

            runner.hideFlags = HideFlags.DontSave;
            runner.AddComponent<DelayedTargetCreator>();
        }

        private sealed class DelayedTargetCreator : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null;
                CreateTarget();
                Destroy(gameObject);
            }
        }

        private static void CreateTarget()
        {
            if (GameObject.Find(TargetName) != null)
            {
                return;
            }

            PlayerInputReader player =
                Object.FindFirstObjectByType<PlayerInputReader>();

            if (player == null)
            {
                return;
            }

            Vector3 position =
                player.transform.position +
                player.transform.forward * 9f +
                player.transform.right * 2.5f;

            if (Physics.Raycast(
                    position + Vector3.up * 5f,
                    Vector3.down,
                    out RaycastHit groundHit,
                    20f,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                position.y = groundHit.point.y;
            }

            GameObject root = new GameObject(TargetName);
            root.transform.position = position;

            Health health = root.AddComponent<Health>();
            ProtectiveEquipment protection =
                root.AddComponent<ProtectiveEquipment>();

            protection.EquipHelmet(ProtectionLevel.Level2);
            protection.EquipVest(ProtectionLevel.Level2);

            CreatePart(
                root.transform,
                PrimitiveType.Sphere,
                "Cabeza_x2",
                HitZone.Head,
                health,
                new Vector3(0f, 1.68f, 0f),
                new Vector3(0.42f, 0.42f, 0.42f),
                Quaternion.identity,
                new Color(0.95f, 0.75f, 0.15f)
            );

            CreatePart(
                root.transform,
                PrimitiveType.Capsule,
                "Torso_x1",
                HitZone.Torso,
                health,
                new Vector3(0f, 1.08f, 0f),
                new Vector3(0.62f, 0.7f, 0.42f),
                Quaternion.identity,
                new Color(0.16f, 0.38f, 0.72f)
            );

            CreatePart(
                root.transform,
                PrimitiveType.Capsule,
                "Brazo_L_x075",
                HitZone.Arm,
                health,
                new Vector3(-0.5f, 1.17f, 0f),
                new Vector3(0.22f, 0.5f, 0.22f),
                Quaternion.Euler(0f, 0f, 65f),
                new Color(0.58f, 0.32f, 0.2f)
            );

            CreatePart(
                root.transform,
                PrimitiveType.Capsule,
                "Brazo_R_x075",
                HitZone.Arm,
                health,
                new Vector3(0.5f, 1.17f, 0f),
                new Vector3(0.22f, 0.5f, 0.22f),
                Quaternion.Euler(0f, 0f, -65f),
                new Color(0.58f, 0.32f, 0.2f)
            );

            CreatePart(
                root.transform,
                PrimitiveType.Capsule,
                "Pierna_L_x065",
                HitZone.Leg,
                health,
                new Vector3(-0.18f, 0.43f, 0f),
                new Vector3(0.26f, 0.55f, 0.26f),
                Quaternion.identity,
                new Color(0.23f, 0.24f, 0.28f)
            );

            CreatePart(
                root.transform,
                PrimitiveType.Capsule,
                "Pierna_R_x065",
                HitZone.Leg,
                health,
                new Vector3(0.18f, 0.43f, 0f),
                new Vector3(0.26f, 0.55f, 0.26f),
                Quaternion.identity,
                new Color(0.23f, 0.24f, 0.28f)
            );

            CreateLabel(root.transform);
        }

        private static void CreatePart(
            Transform parent,
            PrimitiveType primitive,
            string objectName,
            HitZone hitZone,
            Health health,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

            part.AddComponent<DamageHitbox>()
                .Configure(health, hitZone);

            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private static void CreateLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("Etiqueta");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition =
                new Vector3(0f, 2.25f, 0f);

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "OBJETIVO DE DAÑO\nAMARILLO: CASCO N2 | AZUL: CHALECO N2";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.055f;
            label.fontSize = 42;
            label.color = Color.white;

            if (Camera.main != null)
            {
                labelObject.transform.rotation = Quaternion.LookRotation(
                    labelObject.transform.position -
                    Camera.main.transform.position
                );
            }
        }
    }
}
