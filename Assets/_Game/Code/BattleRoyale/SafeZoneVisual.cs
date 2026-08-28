using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SafeZoneVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private Material lineMaterial;

        [Header("Circle")]
        [Range(32, 256)]
        [SerializeField] private int segments = 128;
        [SerializeField] private float yOffset = 0.15f;
        [SerializeField] private float lineWidth = 0.35f;

        [Header("Color")]
        [SerializeField] private Color zoneColor =
            new Color(0.15f, 0.65f, 1f, 0.95f);

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (safeZone == null || lineMaterial == null || _lineRenderer == null)
            {
                Debug.LogError($"[{nameof(SafeZoneVisual)}] Referencias incompletas en '{name}'.", this);
                enabled = false;
                return;
            }
            ConfigureLineRenderer();
        }

        private void OnEnable()
        {
            ConfigureLineRenderer();
        }

        private void LateUpdate()
        {
            if (safeZone == null || safeZone.Radius <= 0f)
            {
                if (_lineRenderer != null)
                    _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            UpdateCircle();
        }

        private void ConfigureLineRenderer()
        {
            if (_lineRenderer == null)
                return;

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = Mathf.Max(32, segments);
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = zoneColor;
            _lineRenderer.endColor = zoneColor;
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.numCapVertices = 2;

            if (lineMaterial != null)
                _lineRenderer.sharedMaterial = lineMaterial;
        }

        private void UpdateCircle()
        {
            int count = Mathf.Max(32, segments);
            if (_lineRenderer.positionCount != count)
                _lineRenderer.positionCount = count;

            Vector3 center = safeZone.Center;
            float radius = safeZone.Radius;

            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                _lineRenderer.SetPosition(
                    i,
                    new Vector3(center.x + x, center.y + yOffset, center.z + z)
                );
            }
        }
    }
}
