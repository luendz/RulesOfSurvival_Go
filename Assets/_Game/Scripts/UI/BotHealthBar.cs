using ROS.Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class BotHealthBar : MonoBehaviour
    {
        private const float VerticalOffset = 2.5f;
        private const float BarWidth       = 1.1f;
        private const float BarHeight      = 0.12f;

        private Health    _health;
        private Transform _root;
        private Transform _fillTransform;
        private RawImage  _fillImage;
        private Camera    _cam;

        public static void Attach(GameObject botRoot)
        {
            botRoot.AddComponent<BotHealthBar>();
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _cam    = Camera.main;
            BuildCanvas();
            Refresh();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.HealthChanged += OnHealthChanged;
                _health.Died          += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died          -= OnDied;
            }
        }

        private void LateUpdate()
        {
            if (_root == null) return;

            bool alive = _health != null && _health.IsAlive;
            if (!alive)
            {
                Destroy(_root.gameObject);
                _root = null;
                return;
            }

            if (_cam == null) _cam = Camera.main;
            if (_cam != null) _root.rotation = _cam.transform.rotation;
        }

        private void OnHealthChanged(float current, float max) => Refresh();

        public void ForceDestroy()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
            }
            enabled = false;
        }

        private void OnDied(ROS.Game.Combat.DamageInfo _)
        {
            ForceDestroy();
        }

        private void Refresh()
        {
            if (_fillTransform == null || _health == null) return;

            float t = _health.MaxHealth > 0f
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
                : 0f;

            // Escalar desde el borde izquierdo (pivot = left-center)
            Vector3 s = _fillTransform.localScale;
            s.x = Mathf.Max(0f, t);
            _fillTransform.localScale = s;

            if (_fillImage != null)
            {
                _fillImage.color = t > 0.5f
                    ? Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f)
                    : Color.Lerp(Color.red, Color.yellow, t * 2f);
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObj = new GameObject("BotHealthBarCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, VerticalOffset, 0f);
            _root = canvasObj.transform;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            RectTransform cr = canvasObj.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(BarWidth, BarHeight);

            // Fondo oscuro
            GameObject bgObj  = new GameObject("BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RawImage bg       = bgObj.AddComponent<RawImage>();
            bg.texture        = Texture2D.whiteTexture;
            bg.color          = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform bgr = bgObj.GetComponent<RectTransform>();
            bgr.anchorMin     = Vector2.zero;
            bgr.anchorMax     = Vector2.one;
            bgr.offsetMin     = Vector2.zero;
            bgr.offsetMax     = Vector2.zero;

            // Relleno: pivot en el borde izquierdo, sin offsets
            // localScale.x controla qué fracción se muestra
            GameObject fillObj  = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            _fillImage            = fillObj.AddComponent<RawImage>();
            _fillImage.texture    = Texture2D.whiteTexture;
            _fillImage.color      = Color.green;
            RectTransform fr      = fillObj.GetComponent<RectTransform>();
            fr.pivot              = new Vector2(0f, 0.5f); // pivot izquierdo
            fr.anchorMin          = Vector2.zero;
            fr.anchorMax          = Vector2.one;
            fr.offsetMin          = Vector2.zero;
            fr.offsetMax          = Vector2.zero;
            _fillTransform        = fillObj.transform;
        }
    }
}
