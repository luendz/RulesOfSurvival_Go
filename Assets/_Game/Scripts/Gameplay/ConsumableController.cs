using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.Gameplay
{
    /// <summary>
    /// Permite al jugador usar consumibles del inventario pulsando F (vendaje/botiquín).
    /// Muestra una barra de progreso en pantalla durante el uso.
    /// Se cancela si recibe daño (configurable en ConsumableDefinition).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConsumableController : MonoBehaviour
    {
        private Health             _health;
        private InventoryComponent _inventory;

        public  bool                  IsUsing => _isUsing;
        private bool                  _isUsing;
        private bool                  _damagedDuringUse;
        private Coroutine             _useRoutine;
        private ConsumableDefinition  _activeDef;
        private InventoryItemDefinition _activeItem;
        private ConsumableDefinition  _defaultHealDef;

        // UI
        private GameObject _barRoot;
        private Transform  _fill;
        private Text       _label;

        // ---------------------------------------------------------------

        private void Awake()
        {
            _health    = GetComponent<Health>();
            _inventory = GetComponent<InventoryComponent>();
            BuildUI();
        }

        private void OnEnable()
        {
            if (_health != null) _health.Damaged += OnDamaged;
            if (_health != null) _health.Died    += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
            if (_health != null) _health.Died    -= OnDied;
        }

        private void OnDestroy()
        {
            if (_barRoot != null) Destroy(_barRoot);
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

        // ---------------------------------------------------------------

        private void TryUseFirstHealing()
        {
            if (_inventory == null) return;
            if (_health.CurrentHealth >= _health.MaxHealth) return;

            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.item == null || stack.amount <= 0) continue;
                if (stack.item.itemType != ItemType.Healing) continue;

                // Si el ítem no tiene ConsumableDefinition asignado, usar valores por defecto
                ConsumableDefinition def = stack.item.consumableDefinition
                    ?? GetDefaultHealDef();

                if (def.healAmount <= 0f) continue;

                if (def.respectMaxFraction && _health.MaxHealth > 0f)
                {
                    if (_health.CurrentHealth / _health.MaxHealth >= def.maxHealthFraction)
                        continue;
                }

                BeginUse(stack.item, def);
                return;
            }
        }

        private ConsumableDefinition GetDefaultHealDef()
        {
            if (_defaultHealDef != null) return _defaultHealDef;
            _defaultHealDef = ScriptableObject.CreateInstance<ConsumableDefinition>();
            _defaultHealDef.healAmount   = 75f;
            _defaultHealDef.useDuration  = 3f;
            _defaultHealDef.cancelOnDamage = true;
            _defaultHealDef.cancelOnMove = false;
            return _defaultHealDef;
        }

        private void BeginUse(InventoryItemDefinition item, ConsumableDefinition def)
        {
            _activeDef        = def;
            _activeItem       = item;
            _isUsing          = true;
            _damagedDuringUse = false;

            if (_label != null)
                _label.text = $"Usando {item.displayName}…";

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
            _isUsing    = false;
            _activeDef  = null;
            _activeItem = null;
            SetBarVisible(false);
        }

        private IEnumerator UseRoutine()
        {
            float elapsed  = 0f;
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

                if (_activeDef.cancelOnMove)
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

            _isUsing    = false;
            _activeDef  = null;
            _activeItem = null;
            SetBarVisible(false);
        }

        private void ApplyEffect()
        {
            if (_health == null || _activeDef == null) return;

            if (_activeDef.healAmount > 0f)
            {
                float max   = _health.MaxHealth;
                float limit = _activeDef.respectMaxFraction
                    ? max * _activeDef.maxHealthFraction
                    : max;
                float toHeal = Mathf.Min(_activeDef.healAmount, limit - _health.CurrentHealth);
                if (toHeal > 0f) _health.Heal(toHeal);
            }
        }

        private void OnDamaged(DamageResult _) => _damagedDuringUse = true;
        private void OnDied(DamageInfo _) { if (_isUsing) CancelUse(); }

        // ---------------------------------------------------------------  UI

        private void BuildUI()
        {
            _barRoot = new GameObject("ConsumableProgressBar");

            Canvas canvas = _barRoot.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;

            CanvasScaler scaler = _barRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Contenedor: centrado horizontalmente, 18 % desde abajo
            GameObject container = new GameObject("Container");
            container.transform.SetParent(_barRoot.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin       = new Vector2(0.5f, 0.18f);
            cr.anchorMax       = new Vector2(0.5f, 0.18f);
            cr.sizeDelta       = new Vector2(320f, 22f);
            cr.anchoredPosition = Vector2.zero;

            // Fondo
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(container.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            RectTransform bgr = bg.GetComponent<RectTransform>();
            bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one;
            bgr.offsetMin = bgr.offsetMax = Vector2.zero;

            // Relleno
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(container.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.92f, 0.35f, 1f);
            RectTransform fr = fillObj.GetComponent<RectTransform>();
            fr.pivot    = new Vector2(0f, 0.5f);
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            _fill = fillObj.transform;

            // Etiqueta
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            _label = labelObj.AddComponent<Text>();
            _label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize  = 13;
            _label.color     = Color.white;
            _label.alignment = TextAnchor.MiddleCenter;
            RectTransform lr = labelObj.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;

            SetBarVisible(false);
        }

        private void SetBarVisible(bool visible)
        {
            if (_barRoot != null) _barRoot.SetActive(visible);
        }

        private void SetFill(float t)
        {
            if (_fill == null) return;
            Vector3 s = _fill.localScale;
            s.x = Mathf.Clamp01(t);
            _fill.localScale = s;
        }
    }
}
