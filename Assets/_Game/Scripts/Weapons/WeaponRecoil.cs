using UnityEngine;

namespace ROS.Game.Weapons
{
    public sealed class WeaponRecoil : MonoBehaviour
    {
        [Header("Camera Recoil")]
        [SerializeField] private float verticalRecoil = 1.2f;
        [SerializeField] private float horizontalRecoil = 0.35f;

        [Header("Recovery")]
        [SerializeField] private float returnSpeed = 8f;
        [SerializeField] private float snappiness = 14f;

        private Vector2 _targetRecoil;
        private Vector2 _currentRecoil;

        public Vector2 CurrentRecoil => _currentRecoil;

        public void ConfigureDefinition(
            WeaponDefinition definition
        )
        {
            if (definition == null)
            {
                return;
            }

            verticalRecoil = definition.verticalRecoil;
            horizontalRecoil = definition.horizontalRecoil;
            returnSpeed = definition.recoilReturnSpeed;
            snappiness = definition.recoilSnappiness;
        }

        public void AddRecoil()
        {
            float horizontal =
                Random.Range(
                    -horizontalRecoil,
                    horizontalRecoil
                );

            _targetRecoil += new Vector2(
                horizontal,
                verticalRecoil
            );
        }

        private void Update()
        {
            _targetRecoil =
                Vector2.Lerp(
                    _targetRecoil,
                    Vector2.zero,
                    returnSpeed * Time.deltaTime
                );

            _currentRecoil =
                Vector2.Lerp(
                    _currentRecoil,
                    _targetRecoil,
                    snappiness * Time.deltaTime
                );
        }
    }
}
