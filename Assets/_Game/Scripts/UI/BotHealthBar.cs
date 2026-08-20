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
        private const float BarHeight      = 0.13f;

        private Health    _health;
        private Transform _root;
        private RawImage  _fill;
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
                _health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.HealthChanged -= OnHealthChanged;
        }

        private void LateUpdate()
        {
            if (_root == null) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null) _root.rotation = _cam.transform.rotation;

            bool alive = _health != null && _health.IsAlive;
            if (_root.gameObject.activeSelf != alive)
                _root.gameObject.SetActive(alive);
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_fill == null || _health == null) return;

            float t = _health.MaxHealth > 0f
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
                : 0f;

            _fill.rectTransform.anchorMax = new Vector2(
                t,
                _fill.rectTransform.anchorMax.y
            );

            Color color;
            if (t > 0.5f)
                color = Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f);
            else
                color = Color.Lerp(Color.red, Color.yellow, t * 2f);

            _fill.color = color;
        }

        private void BuildCanvas()
        {
            GameObject canvasObj = new GameObject("BotHealthBarCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, VerticalOffset, 0f);
            _root = canvasObj.transform;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            RectTransform cr = canvasObj.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(BarWidth, BarHeight);

            // fondo oscuro
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RawImage bg = bgObj.AddComponent<RawImage>();
            bg.texture = Texture2D.whiteTexture;
            bg.color   = new Color(0.08f, 0.08f, 0.08f, 0.88f);
            RectTransform bgr = bgObj.GetComponent<RectTransform>();
            bgr.anchorMin = Vector2.zero;
            bgr.anchorMax = Vector2.one;
            bgr.offsetMin = Vector2.zero;
            bgr.offsetMax = Vector2.zero;

            // relleno de vida
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            _fill         = fillObj.AddComponent<RawImage>();
            _fill.texture = Texture2D.whiteTexture;
            _fill.color   = Color.green;
            RectTransform fr = fillObj.GetComponent<RectTransform>();
            fr.anchorMin = new Vector2(0f, 0.12f);
            fr.anchorMax = new Vector2(1f, 0.88f);
            fr.offsetMin = new Vector2(2f, 0f);
            fr.offsetMax = new Vector2(-2f, 0f);
        }
    }
}
