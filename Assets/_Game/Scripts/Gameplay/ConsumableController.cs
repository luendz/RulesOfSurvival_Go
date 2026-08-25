using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Core;
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
        private Health _health;
        private InventoryComponent _inventory;

        public bool IsUsing => _isUsing;
        private bool _isUsing;
        private bool _damagedDuringUse;
        private Coroutine _useRoutine;
        private ConsumableDefinition _activeDef;
        private InventoryItemDefinition _activeItem;
        private ConsumableDefinition _defaultHealDef;

        [Header("Physical HUD References")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private GameObject barRoot;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Text label;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _inventory = GetComponent<InventoryComponent>();
            ResolvePhysicalHud();
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

        private void OnDestroy()
        {
            if (_defaultHealDef != null) Destroy(_defaultHealDef);
        }

        private void Update()
        {
            if (_health == null || !_health.IsAlive) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (_isUsing)
                    CancelUse();
                else
                    TryUseFirstHealing();
            }
        }

        public void SetHudEnabled(bool enabled)
        {
            showHud = enabled;
            if (!showHud)
            {
                barRoot = null;
                fill = null;
                label = null;
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

                ConsumableDefinition def = stack.item.consumableDefinition
                    ?? GetDefaultHealDef();

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

        private ConsumableDefinition GetDefaultHealDef()
        {
            if (_defaultHealDef != null) return _defaultHealDef;

            _defaultHealDef = ScriptableObject.CreateInstance<ConsumableDefinition>();
            _defaultHealDef.healAmount = 75f;
            _defaultHealDef.useDuration = 3f;
            _defaultHealDef.cancelOnDamage = true;
            _defaultHealDef.cancelOnMove = false;
            _defaultHealDef.hideFlags = HideFlags.DontSave;
            return _defaultHealDef;
        }

        private void BeginUse(InventoryItemDefinition item, ConsumableDefinition def)
        {
            _activeDef = def;
            _activeItem = item;
            _isUsing = true;
            _damagedDuringUse = false;

            ResolvePhysicalHud();
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

        private void ResolvePhysicalHud()
        {
            if (!showHud)
                return;

            if (barRoot != null && fill != null && label != null)
                return;

            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                RulesOfSurvivalHUD rosHud =
                    FindFirstObjectByType<RulesOfSurvivalHUD>();
                hud = rosHud != null ? rosHud.gameObject : null;
            }

            if (hud == null)
                return;

            Transform[] all = hud.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform current = all[i];
                if (barRoot == null && current.name == "ConsumableProgressBar")
                    barRoot = current.gameObject;
                else if (fill == null && current.name == "ConsumableProgressFill")
                    fill = current as RectTransform;
                else if (label == null && current.name == "ConsumableProgressLabel")
                    label = current.GetComponent<Text>();
            }
        }

        private void SetBarVisible(bool visible)
        {
            if (!showHud)
                return;

            ResolvePhysicalHud();
            if (barRoot != null && barRoot.activeSelf != visible)
                barRoot.SetActive(visible);
        }

        private void SetFill(float t)
        {
            if (!showHud)
                return;

            if (fill == null)
            {
                ResolvePhysicalHud();
                if (fill == null) return;
            }

            Vector3 scale = fill.localScale;
            scale.x = Mathf.Clamp01(t);
            fill.localScale = scale;
        }
    }
}
