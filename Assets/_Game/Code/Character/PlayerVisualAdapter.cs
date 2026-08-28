using UnityEngine;

namespace ROS.Game.Character
{
    public sealed class PlayerVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform head;
        [SerializeField] private Transform spine;

        public Animator Animator => animator;
        public Transform RightHand => rightHand;
        public Transform LeftHand => leftHand;
        public Transform Head => head;
        public Transform Spine => spine;

        private void Awake() => AutoBindHumanoid();
        private void OnValidate() => AutoBindHumanoid();

        public void AutoBindHumanoid()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            spine = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        }
    }
}
