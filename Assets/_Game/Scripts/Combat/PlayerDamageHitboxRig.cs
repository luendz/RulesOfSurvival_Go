using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDamageHitboxRig : MonoBehaviour
    {
        private const string RigName = "RuntimeDamageHitboxes";

        [SerializeField] private bool generateAtRuntime = true;

        public bool HasGeneratedHitboxes =>
            transform.Find(RigName) != null;

        private void Awake()
        {
            if (Application.isPlaying && generateAtRuntime)
            {
                EnsureHitboxes();
            }
        }

        public void EnsureHitboxes()
        {
            if (HasGeneratedHitboxes)
            {
                return;
            }

            Health health = GetComponent<Health>();

            GameObject rigObject = new GameObject(RigName);
            rigObject.layer = gameObject.layer;
            rigObject.transform.SetParent(transform, false);

            CreateSphere(
                rigObject.transform,
                "Hitbox_Head",
                HitZone.Head,
                health,
                new Vector3(0f, 1.62f, 0f),
                0.19f
            );

            CreateCapsule(
                rigObject.transform,
                "Hitbox_Torso",
                HitZone.Torso,
                health,
                new Vector3(0f, 1.05f, 0f),
                Quaternion.identity,
                0.27f,
                0.82f
            );

            CreateCapsule(
                rigObject.transform,
                "Hitbox_Arm_L",
                HitZone.Arm,
                health,
                new Vector3(-0.38f, 1.14f, 0f),
                Quaternion.Euler(0f, 0f, 90f),
                0.12f,
                0.58f
            );

            CreateCapsule(
                rigObject.transform,
                "Hitbox_Arm_R",
                HitZone.Arm,
                health,
                new Vector3(0.38f, 1.14f, 0f),
                Quaternion.Euler(0f, 0f, 90f),
                0.12f,
                0.58f
            );

            CreateCapsule(
                rigObject.transform,
                "Hitbox_Leg_L",
                HitZone.Leg,
                health,
                new Vector3(-0.14f, 0.43f, 0f),
                Quaternion.identity,
                0.14f,
                0.78f
            );

            CreateCapsule(
                rigObject.transform,
                "Hitbox_Leg_R",
                HitZone.Leg,
                health,
                new Vector3(0.14f, 0.43f, 0f),
                Quaternion.identity,
                0.14f,
                0.78f
            );
        }

        private static void CreateSphere(
            Transform parent,
            string objectName,
            HitZone hitZone,
            Health health,
            Vector3 localPosition,
            float radius)
        {
            GameObject target = CreateTarget(
                parent,
                objectName,
                hitZone,
                health,
                localPosition,
                Quaternion.identity
            );

            SphereCollider collider =
                target.AddComponent<SphereCollider>();

            collider.radius = radius;
            collider.isTrigger = true;
        }

        private static void CreateCapsule(
            Transform parent,
            string objectName,
            HitZone hitZone,
            Health health,
            Vector3 localPosition,
            Quaternion localRotation,
            float radius,
            float height)
        {
            GameObject target = CreateTarget(
                parent,
                objectName,
                hitZone,
                health,
                localPosition,
                localRotation
            );

            CapsuleCollider collider =
                target.AddComponent<CapsuleCollider>();

            collider.radius = radius;
            collider.height = Mathf.Max(height, radius * 2f);
            collider.direction = 1;
            collider.isTrigger = true;
        }

        private static GameObject CreateTarget(
            Transform parent,
            string objectName,
            HitZone hitZone,
            Health health,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject target = new GameObject(objectName);
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;
            target.transform.localRotation = localRotation;

            target
                .AddComponent<DamageHitbox>()
                .Configure(health, hitZone);

            return target;
        }
    }
}
