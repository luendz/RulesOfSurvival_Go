using ROS.Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class BotHealthBar : MonoBehaviour
    {
        private const string ResourcePath = "EditorFirst/BotHealthBar";

        [Header("Editable View")]
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Graphic fillGraphic;
        [SerializeField] private float verticalOffset = 2.5f;

        private Health _health;
        private Transform _target;
        private Camera _cam;
        private bool _subscribed;

        public static BotHealthBar Attach(GameObject botRoot)
        {
            if (botRoot == null)
                return null;

            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    "No existe el prefab editable EditorFirst/BotHealthBar. " +
                    "Abre el proyecto en Unity para materializar los assets editor-first."
                );
                return null;
            }

            GameObject instance = Instantiate(prefab);
            BotHealthBar bar = instance.GetComponent<BotHealthBar>();
            if (bar == null)
            {
                Debug.LogError("El prefab BotHealthBar no contiene el componente BotHealthBar.");
                Destroy(instance);
                return null;
            }

            bar.Bind(botRoot);
            return bar;
        }

        private void Awake()
        {
            ResolveViewReferences();
            _cam = Camera.main;
        }

        public void Bind(GameObject botRoot)
        {
            Unsubscribe();

            _target = botRoot != null ? botRoot.transform : null;
            _health = botRoot != null ? botRoot.GetComponent<Health>() : null;
            gameObject.name = botRoot != null
                ? botRoot.name + "_HealthBar"
                : "BotHealthBar";

            ResolveViewReferences();
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (_target == null || _health == null || !_health.IsAlive)
            {
                ForceDestroy();
                return;
            }

            transform.position = _target.position + Vector3.up * verticalOffset;

            if (_cam == null)
                _cam = Camera.main;
            if (_cam != null)
                transform.rotation = _cam.transform.rotation;
        }

        private void ResolveViewReferences()
        {
            if (fillRect == null)
            {
                Transform fill = FindChild("Fill");
                if (fill != null)
                    fillRect = fill as RectTransform;
            }

            if (fillGraphic == null && fillRect != null)
                fillGraphic = fillRect.GetComponent<Graphic>();
        }

        private Transform FindChild(string childName)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName)
                    return all[i];
            }
            return null;
        }

        private void Subscribe()
        {
            if (_subscribed || _health == null)
                return;

            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _health == null)
                return;

            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
            _subscribed = false;
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh();
        }

        private void OnDied(DamageInfo _)
        {
            ForceDestroy();
        }

        public void ForceDestroy()
        {
            if (this == null || gameObject == null)
                return;

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void Refresh()
        {
            if (fillRect == null || _health == null)
                return;

            float t = _health.MaxHealth > 0f
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
                : 0f;

            Vector3 scale = fillRect.localScale;
            scale.x = Mathf.Max(0f, t);
            fillRect.localScale = scale;

            if (fillGraphic != null)
            {
                fillGraphic.color = t > 0.5f
                    ? Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f)
                    : Color.Lerp(Color.red, Color.yellow, t * 2f);
            }
        }
    }
}
