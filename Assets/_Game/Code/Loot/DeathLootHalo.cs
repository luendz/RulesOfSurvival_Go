using UnityEngine;

namespace ROS.Game.Loot
{
    public sealed class DeathLootHalo : MonoBehaviour
    {
        [Header("Flotación del modelo")]
        [SerializeField]
        private float hoverHeight = 0.9f;

        [SerializeField]
        private float bobAmplitude = 0.035f;

        [SerializeField]
        private float bobSpeed = 1.6f;

        [Header("Rotación")]
        [SerializeField]
        private float spinSpeed = 35f;

        [SerializeField]
        private Transform _floatingModel;

        private Vector3 _floatingBasePosition;

        private Quaternion _diamondRotation =
            Quaternion.identity;

        public bool HasFloatingModel =>
            _floatingModel != null;

        private void Awake()
        {
            CacheFloatingPose();
        }

        private void Update()
        {
            if (_floatingModel != null)
            {
                float bobOffset =
                    hoverHeight +
                    Mathf.Sin(Time.time * bobSpeed) *
                    bobAmplitude;

                _floatingModel.localPosition =
                    _floatingBasePosition +
                    Vector3.up * bobOffset;

                _floatingModel.localRotation =
                    Quaternion.AngleAxis(
                        Time.time * spinSpeed,
                        Vector3.up
                    ) *
                    _diamondRotation;
            }
        }

        public void ConfigureFloatingModel(
            Transform model
        )
        {
            if (model == null)
            {
                return;
            }

            _floatingModel = model;
            CacheFloatingPose();
        }

        private void CacheFloatingPose()
        {
            if (_floatingModel == null)
                return;

            _floatingBasePosition = _floatingModel.localPosition;
            _diamondRotation = _floatingModel.localRotation;
        }
    }
}
