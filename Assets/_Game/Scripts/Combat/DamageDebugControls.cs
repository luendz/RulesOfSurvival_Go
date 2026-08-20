using ROS.Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageDebugControls : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private bool showHelp = true;

        private ProtectiveEquipment _protection;
        private FallDamageReceiver _fallDamage;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            _protection = GetComponent<ProtectiveEquipment>();
            _fallDamage = GetComponent<FallDamageReceiver>();
        }

        private void Update()
        {
            if (health == null ||
                !health.IsAlive ||
                Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                EnsureProtection();
                _protection.EquipHelmet(ProtectionLevel.Level2);
                _protection.EquipVest(ProtectionLevel.Level2);
            }

            if (Keyboard.current.f6Key.wasPressedThisFrame)
            {
                ApplyDebugDamage(
                    health.MaxHealth * 0.2f,
                    DamageType.Firearm,
                    HitZone.Torso,
                    transform.forward
                );
            }

            if (Keyboard.current.f7Key.wasPressedThisFrame)
            {
                ApplyDebugDamage(
                    health.MaxHealth * 0.2f,
                    DamageType.Firearm,
                    HitZone.Head,
                    transform.forward
                );
            }

            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                ApplyDebugDamage(
                    health.MaxHealth * 0.35f,
                    DamageType.Explosion,
                    HitZone.Torso,
                    transform.right
                );
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (_fallDamage == null)
                {
                    _fallDamage = GetComponent<FallDamageReceiver>();
                }

                _fallDamage?.ApplyLandingDamage(14f);
            }
        }

        private void ApplyDebugDamage(
            float amount,
            DamageType damageType,
            HitZone hitZone,
            Vector3 direction)
        {
            health.ApplyDamage(
                new DamageInfo(
                    amount,
                    transform.position,
                    direction,
                    null,
                    damageType,
                    hitZone
                )
            );
        }

        private void EnsureProtection()
        {
            if (_protection == null)
            {
                _protection = GetComponent<ProtectiveEquipment>();
            }

            if (_protection == null)
            {
                _protection = gameObject
                    .AddComponent<ProtectiveEquipment>();
            }
        }

        private void OnGUI()
        {
            if (!showHelp)
            {
                return;
            }

            GUI.Box(
                new Rect(Screen.width - 315f, 16f, 299f, 132f),
                "PRUEBA RAPIDA DE DAÑO\n" +
                "F5: equipar casco/chaleco N2\n" +
                "F6: disparo al torso | F7: headshot\n" +
                "F8: explosión | F9: caída"
            );
        }
    }
}
