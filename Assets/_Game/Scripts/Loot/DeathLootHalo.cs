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

        private Transform _floatingModel;

        private Vector3 _floatingBasePosition;

        private Quaternion _diamondRotation =
            Quaternion.identity;

        public bool HasFloatingModel =>
            _floatingModel != null;

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
            if (model != null)
            {
                Renderer[] renderers =
                    model.GetComponentsInChildren<Renderer>(
                        true
                    );

                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;

                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    GameObject pivotObject =
                        new GameObject("Pivote_Caja_Flotante");

                    Transform pivot = pivotObject.transform;
                    pivot.SetParent(transform, false);
                    pivot.position = bounds.center;
                    model.SetParent(pivot, true);

                    _floatingModel = pivot;
                    _floatingBasePosition =
                        _floatingModel.localPosition;

                    _diamondRotation =
                        Quaternion.FromToRotation(
                            Vector3.one.normalized,
                            Vector3.up
                        );
                }
            }
        }
    }
}
