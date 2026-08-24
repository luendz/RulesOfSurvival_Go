using UnityEngine;
using UnityEngine.Rendering;

namespace ROS.Game.BattleRoyale
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SafeZoneWallVisual : MonoBehaviour
    {
        private const string MaterialResourcePath = "EditorFirst/SafeZoneWall";
        private const string MeshResourcePath = "EditorFirst/SafeZoneWallMesh";

        [Header("References")]
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private Mesh wallMesh;
        [SerializeField] private Material wallMaterial;

        [Header("Wall")]
        [Range(32, 256)]
        [SerializeField] private int segments = 128;
        [Min(1f)]
        [SerializeField] private float wallHeight = 50f;
        [SerializeField] private float groundOffset;

        [Header("Visual")]
        [SerializeField] private Color wallColor =
            new Color(0.10f, 0.55f, 1f, 0.22f);

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private MaterialPropertyBlock _properties;
        private float _lastRadius = -1f;
        private int _lastSegments = -1;
        private float _lastWallHeight = -1f;

        private void Awake()
        {
            EnsureReferences();
            ConfigureEditableAssets();
        }

        private void OnEnable()
        {
            EnsureReferences();
            ConfigureEditableAssets();
        }

        private void LateUpdate()
        {
            EnsureReferences();

            if (safeZone == null || safeZone.Radius <= 0f || _mesh == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            transform.position = safeZone.Center + Vector3.up * groundOffset;

            if (!Mathf.Approximately(_lastRadius, safeZone.Radius) ||
                _lastSegments != segments ||
                !Mathf.Approximately(_lastWallHeight, wallHeight))
            {
                BuildWallMesh(safeZone.Radius);
                _lastRadius = safeZone.Radius;
                _lastSegments = segments;
                _lastWallHeight = wallHeight;
            }

            ApplyColor();
        }

        private void EnsureReferences()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (safeZone == null)
                safeZone = GetComponentInParent<SafeZoneController>();
            if (safeZone == null)
                safeZone = FindFirstObjectByType<SafeZoneController>();

            if (wallMesh == null)
                wallMesh = Resources.Load<Mesh>(MeshResourcePath);
            if (wallMaterial == null)
                wallMaterial = Resources.Load<Material>(MaterialResourcePath);
        }

        private void ConfigureEditableAssets()
        {
            if (_meshFilter == null || _meshRenderer == null)
                return;

            if (wallMesh != null && _meshFilter.sharedMesh != wallMesh)
                _meshFilter.sharedMesh = wallMesh;

            _mesh = _meshFilter.sharedMesh;
            if (_mesh != null)
                _mesh.MarkDynamic();

            if (wallMaterial != null)
                _meshRenderer.sharedMaterial = wallMaterial;

            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _properties ??= new MaterialPropertyBlock();
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (_meshRenderer == null)
                return;

            _properties ??= new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_properties);
            _properties.SetColor("_Color", wallColor);
            _meshRenderer.SetPropertyBlock(_properties);
        }

        private void BuildWallMesh(float radius)
        {
            if (_mesh == null)
                return;

            int count = Mathf.Max(32, segments);
            int ringCount = count + 1;
            Vector3[] vertices = new Vector3[ringCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i <= count; i++)
            {
                float normalized = (float)i / count;
                float angle = normalized * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                int bottomIndex = i * 2;
                int topIndex = bottomIndex + 1;

                vertices[bottomIndex] = new Vector3(x, 0f, z);
                vertices[topIndex] = new Vector3(x, wallHeight, z);
                uvs[bottomIndex] = new Vector2(normalized, 0f);
                uvs[topIndex] = new Vector2(normalized, 1f);
            }

            int[] triangles = new int[count * 12];
            int triangleIndex = 0;

            for (int i = 0; i < count; i++)
            {
                int bottomLeft = i * 2;
                int topLeft = bottomLeft + 1;
                int bottomRight = bottomLeft + 2;
                int topRight = bottomLeft + 3;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topRight;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }

        private void SetVisible(bool visible)
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = visible;
        }
    }
}
