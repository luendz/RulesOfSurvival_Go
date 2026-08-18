using UnityEngine;

namespace ROS.Game.Combat
{
    public sealed class PlayerAimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;

        [Header("Aim")]
        [SerializeField] private float maxAimDistance = 1000f;
        [SerializeField] private LayerMask aimMask = ~0;
        [SerializeField]
        private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Ignore;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay;

        public Vector3 AimWorldPoint { get; private set; }
        public Vector3 AimDirection { get; private set; }
        public bool HasHit { get; private set; }
        public RaycastHit LastHit { get; private set; }

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            UpdateAim();
        }

        private void UpdateAim()
        {
            if (aimCamera == null)
            {
                return;
            }

            Ray ray = new Ray(
                aimCamera.transform.position,
                aimCamera.transform.forward
            );

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maxAimDistance,
                    aimMask,
                    triggerInteraction))
            {
                HasHit = true;
                LastHit = hit;
                AimWorldPoint = hit.point;
            }
            else
            {
                HasHit = false;

                AimWorldPoint =
                    ray.origin +
                    ray.direction * maxAimDistance;
            }

            AimDirection =
                (AimWorldPoint - aimCamera.transform.position).normalized;

            if (drawDebugRay)
            {
                Debug.DrawLine(
                    aimCamera.transform.position,
                    AimWorldPoint,
                    HasHit ? Color.green : Color.red
                );
            }
        }

        public Vector3 GetDirectionFrom(Vector3 origin)
        {
            Vector3 direction =
                AimWorldPoint - origin;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return aimCamera != null
                    ? aimCamera.transform.forward
                    : transform.forward;
            }

            return direction.normalized;
        }

        public void SetCamera(Camera newCamera)
        {
            aimCamera = newCamera;
        }
    }
}