using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ConsumableController : MonoBehaviour
    {
        [Header("Gameplay References")]
        [SerializeField] private Health _health;
        [SerializeField] private InventoryComponent _inventory;
        [SerializeField] private PlayerInputReader _input;

        public bool IsUsing => _isUsing;
        private bool _isUsing;
        private bool _damagedDuringUse;
        private Coroutine _useRoutine;
        private ConsumableDefinition _activeDef;
        private InventoryItemDefinition _activeItem;

        [Header("Physical HUD References")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private GameObject barRoot;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Text label;

        private void Awake()
        {
            if (_health == null) _health = GetComponent<Health>();
            if (_inventory == null) _inventory = GetComponent<InventoryComponent>();
            if (_input == null) _input = GetComponent<PlayerInputReader>();

            if (_health == null || _inventory == null || _input == null ||
                (showHud && (barRoot == null || fill == null || label == null)))
            {
                Debug.LogError(
                    "ConsumableController tiene referencias sin asignar. " +
                    "Completa el prefab antes de ejecutar.",
                    this
                );
                enabled = false;
                return;
            }

            SetBarVisible(false);
        }

        private void OnEnable()
        {
            if (_health != null) _health.Damaged += OnDamaged;
            if (_health != null) _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
            if (_health != null) _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_health == null || !_health.IsAlive) return;

            // Los bots utilizan TryUseFirstHealing desde su IA. Nunca deben
            // reaccionar a la tecla H global del jugador local.
            if (_input != null && _input.UsesExternalControl)
                return;

            if (Keyboard.current == null) return;

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (_isUsing)
                    CancelUse();
                else
                    TryUseFirstHealing();
            }
        }

        /// <summary>
        /// Intenta usar la primera cura valida del inventario. Es la misma ruta
        /// jugable usada por H y por la IA: respeta tiempo de uso, limites del
        /// consumible, cancelacion por daño y consume una unidad al completar.
        /// </summary>
        public bool TryUseFirstHealing()
        {
            if (_isUsing || _inventory == null || _health == null)
                return false;
            if (!_health.IsAlive || _health.CurrentHealth >= _health.MaxHealth)
                return false;

            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.item == null || stack.amount <= 0) continue;
                if (stack.item.itemType != ItemType.Healing) continue;

                ConsumableDefinition def = stack.item.consumableDefinition;

                if (def == null)
                    continue;

                if (def.healAmount <= 0f) continue;

                if (def.respectMaxFraction && _health.MaxHealth > 0f)
                {
                    if (_health.CurrentHealth / _health.MaxHealth >= def.maxHealthFraction)
                        continue;
                }

                BeginUse(stack.item, def);
                return true;
            }

            return false;
        }

        private void BeginUse(InventoryItemDefinition item, ConsumableDefinition def)
        {
            _activeDef = def;
            _activeItem = item;
            _isUsing = true;
            _damagedDuringUse = false;

            if (showHud && label != null)
                label.text = $"Usando {item.displayName}…";

            SetFill(0f);
            SetBarVisible(true);
            _useRoutine = StartCoroutine(UseRoutine());
        }

        private void CancelUse()
        {
            if (_useRoutine != null)
            {
                StopCoroutine(_useRoutine);
                _useRoutine = null;
            }

            _isUsing = false;
            _activeDef = null;
            _activeItem = null;
            SetBarVisible(false);
        }

        private IEnumerator UseRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, _activeDef.useDuration);
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetFill(elapsed / duration);

                if (_activeDef.cancelOnDamage && _damagedDuringUse)
                {
                    CancelUse();
                    yield break;
                }

                // Las curas ROS se pueden utilizar mientras el jugador se mueve.
                // Se conserva cancelOnMove para consumibles no curativos futuros,
                // pero nunca cancela una accion cuyo efecto restaura vida.
                if (_activeDef.cancelOnMove && _activeDef.healAmount <= 0f)
                {
                    Vector3 delta = transform.position - startPos;
                    delta.y = 0f;
                    if (delta.sqrMagnitude > 0.25f)
                    {
                        CancelUse();
                        yield break;
                    }
                }

                yield return null;
            }

            ApplyEffect();

            if (_inventory != null && _activeItem != null)
                _inventory.Remove(_activeItem, 1);

            _isUsing = false;
            _activeDef = null;
            _activeItem = null;
            _useRoutine = null;
            SetBarVisible(false);
        }

        private void ApplyEffect()
        {
            if (_health == null || _activeDef == null) return;

            if (_activeDef.healAmount > 0f)
            {
                float max = _health.MaxHealth;
                float limit = _activeDef.respectMaxFraction
                    ? max * _activeDef.maxHealthFraction
                    : max;
                float toHeal = Mathf.Min(
                    _activeDef.healAmount,
                    limit - _health.CurrentHealth
                );
                if (toHeal > 0f) _health.Heal(toHeal);
            }
        }

        private void OnDamaged(DamageResult _) => _damagedDuringUse = true;

        private void OnDied(DamageInfo _)
        {
            if (_isUsing) CancelUse();
        }

        private void SetBarVisible(bool visible)
        {
            if (!showHud)
                return;

            if (barRoot != null && barRoot.activeSelf != visible)
                barRoot.SetActive(visible);
        }

        private void SetFill(float t)
        {
            if (!showHud)
                return;

            if (fill == null) return;

            Vector3 scale = fill.localScale;
            scale.x = Mathf.Clamp01(t);
            fill.localScale = scale;
        }
    }
}
