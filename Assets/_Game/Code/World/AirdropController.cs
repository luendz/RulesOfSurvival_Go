using UnityEngine;

namespace ROS.Game.World
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AirdropController : MonoBehaviour
    {
        [SerializeField] private float terminalSpeed = 7f;
        [SerializeField] private GameObject parachuteVisual;
        [SerializeField] private GameObject landingSmoke;
        private Rigidbody _rb;
        private bool _landed;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            if (_landed) return;
            Vector3 v = _rb.linearVelocity;
            if (v.y < -terminalSpeed) { v.y = -terminalSpeed; _rb.linearVelocity = v; }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_landed) return;
            _landed = true;
            if (parachuteVisual != null) parachuteVisual.SetActive(false);
            if (landingSmoke != null) landingSmoke.SetActive(true);
        }
    }
}
