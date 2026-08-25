using ROS.Game.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDebugHealthHotkeys : MonoBehaviour
    {
        [Header("Debug Health")]
        [SerializeField, Min(0.1f)] private float f5Damage = 5f;

        private Health _health;
        private PlayerInputReader _input;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (_health == null || !_health.IsAlive)
                return;

            // Nunca reaccionar al teclado en bots/control externo.
            if (_input != null && _input.UsesExternalControl)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f5Key.wasPressedThisFrame)
                return;

            _health.ApplyDirectHealthDamage(f5Damage, gameObject);
        }
    }
}
