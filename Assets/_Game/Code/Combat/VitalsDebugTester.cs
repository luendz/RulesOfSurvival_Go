using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Combat
{
    public sealed class VitalsDebugTester : MonoBehaviour
    {
        [SerializeField] private Health health;

        private void Awake()
        {
            if (health == null)
                health = GetComponent<Health>();
        }

        private void Update()
        {
            if (health == null || Keyboard.current == null)
                return;

            // H = recibir 10 de daño
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                health.ApplyDamage(
                    new DamageInfo(
                        10f,
                        transform.position,
                        Vector3.zero,
                        null
                    )
                );
            }

            // J = curar 10
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                health.Heal(10f);
            }

            // K = agregar 25 de armadura
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                health.AddArmor(25f);
            }
        }
    }
}