using UnityEngine;

namespace ROS.Game.Character
{
    /// <summary>
    /// Aplica visualmente el lean después de que Animator y Animation Rigging
    /// hayan evaluado la pose del personaje. Busca de forma explícita el
    /// Animator Humanoid para evitar tomar animators de armas u objetos hijos.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(PlayerLeanController))]
    public sealed class PlayerLeanRigApplier : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerLeanController leanController;
        [SerializeField] private Animator humanoidAnimator;

        [Header("Upper Body Distribution")]
        [Range(0f, 1f)]
        [SerializeField] private float hipsWeight = 0.10f;
        [Range(0f, 1f)]
        [SerializeField] private float spineWeight = 0.28f;
        [Range(0f, 1f)]
        [SerializeField] private float chestWeight = 0.32f;
        [Range(0f, 1f)]
        [SerializeField] private float upperChestWeight = 0.30f;

        [Header("Visual")]
        [Range(5f, 25f)]
        [SerializeField] private float maximumLeanDegrees = 15f;

        private Transform _hips;
        private Transform _spine;
        private Transform _chest;
        private Transform _upperChest;

        private void Awake()
        {
            if (leanController == null)
            {
                leanController = GetComponent<PlayerLeanController>();
            }

            FindHumanoidAnimator();
            CacheBones();
        }

        private void LateUpdate()
        {
            if (leanController == null)
            {
                return;
            }

            if (humanoidAnimator == null || !humanoidAnimator.isHuman)
            {
                FindHumanoidAnimator();
                CacheBones();
            }

            ApplyLean();
        }

        private void FindHumanoidAnimator()
        {
            if (humanoidAnimator != null && humanoidAnimator.isHuman)
            {
                return;
            }

            humanoidAnimator = null;

            Animator[] animators = GetComponentsInChildren<Animator>(true);

            foreach (Animator candidate in animators)
            {
                if (candidate != null && candidate.isHuman)
                {
                    humanoidAnimator = candidate;
                    break;
                }
            }
        }

        private void CacheBones()
        {
            _hips = null;
            _spine = null;
            _chest = null;
            _upperChest = null;

            if (humanoidAnimator == null || !humanoidAnimator.isHuman)
            {
                return;
            }

            _hips = humanoidAnimator.GetBoneTransform(HumanBodyBones.Hips);
            _spine = humanoidAnimator.GetBoneTransform(HumanBodyBones.Spine);
            _chest = humanoidAnimator.GetBoneTransform(HumanBodyBones.Chest);
            _upperChest = humanoidAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
        }

        private void ApplyLean()
        {
            float lean = leanController.CurrentLean;

            if (Mathf.Abs(lean) <= 0.0001f)
            {
                return;
            }

            float totalWeight = 0f;

            if (_hips != null)
                totalWeight += hipsWeight;
            if (_spine != null)
                totalWeight += spineWeight;
            if (_chest != null)
                totalWeight += chestWeight;
            if (_upperChest != null)
                totalWeight += upperChestWeight;

            if (totalWeight <= 0.0001f)
            {
                return;
            }

            // El signo visual del rig Humanoid es opuesto al valor lógico
            // usado por cámara/input: Left = -1 y Right = +1.
            float totalAngle = -maximumLeanDegrees * lean;
            Vector3 worldAxis = transform.forward;

            ApplyBoneRotation(
                _hips,
                worldAxis,
                totalAngle * hipsWeight / totalWeight
            );

            ApplyBoneRotation(
                _spine,
                worldAxis,
                totalAngle * spineWeight / totalWeight
            );

            ApplyBoneRotation(
                _chest,
                worldAxis,
                totalAngle * chestWeight / totalWeight
            );

            ApplyBoneRotation(
                _upperChest,
                worldAxis,
                totalAngle * upperChestWeight / totalWeight
            );
        }

        private static void ApplyBoneRotation(
            Transform bone,
            Vector3 worldAxis,
            float angle
        )
        {
            if (bone == null || Mathf.Abs(angle) <= 0.0001f)
            {
                return;
            }

            bone.rotation =
                Quaternion.AngleAxis(angle, worldAxis) *
                bone.rotation;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToExistingPlayers()
        {
            PlayerLeanController[] controllers =
                FindObjectsByType<PlayerLeanController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (PlayerLeanController controller in controllers)
            {
                if (controller == null)
                {
                    continue;
                }

                if (controller.GetComponent<PlayerLeanRigApplier>() == null)
                {
                    controller.gameObject.AddComponent<PlayerLeanRigApplier>();
                }
            }
        }
    }
}
