using UnityEngine;
using UnityEngine.Rendering;

namespace ROS.Game.BattleRoyale
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SafeZoneWallVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SafeZoneController safeZone;

        [Header("Wall")]
        [Range(32, 256)]
        [SerializeField] private int segments = 128;

        [Min(1f)]
        [SerializeField] private float wallHeight = 50f;

        [SerializeField] private float groundOffset = 0f;

        [Header("Visual")]
        [SerializeField]
        private Color wallColor =
            new Color(
                0.10f,
                0.55f,
                1f,
                0.22f
            );

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Mesh _mesh;
        private Material _runtimeMaterial;

        private float _lastRadius = -1f;
        private int _lastSegments = -1;
        private float _lastWallHeight = -1f;

        private void Awake()
        {
            EnsureReferences();
            CreateRuntimeObjects();
        }

        private void OnEnable()
        {
            EnsureReferences();
            CreateRuntimeObjects();
        }

        private void LateUpdate()
        {
            EnsureReferences();

            if (safeZone == null)
            {
                SetVisible(false);
                return;
            }

            if (safeZone.Radius <= 0f)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            transform.position =
                safeZone.Center +
                Vector3.up * groundOffset;

            if (
                !Mathf.Approximately(
                    _lastRadius,
                    safeZone.Radius
                ) ||
                _lastSegments != segments ||
                !Mathf.Approximately(
                    _lastWallHeight,
                    wallHeight
                )
            )
            {
                BuildWallMesh(
                    safeZone.Radius
                );

                _lastRadius =
                    safeZone.Radius;

                _lastSegments =
                    segments;

                _lastWallHeight =
                    wallHeight;
            }

            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.color =
                    wallColor;
            }
        }

        private void EnsureReferences()
        {
            if (_meshFilter == null)
            {
                _meshFilter =
                    GetComponent<MeshFilter>();
            }

            if (_meshRenderer == null)
            {
                _meshRenderer =
                    GetComponent<MeshRenderer>();
            }

            if (safeZone == null)
            {
                safeZone =
                    GetComponentInParent<
                        SafeZoneController
                    >();
            }

            if (safeZone == null)
            {
                safeZone =
                    FindFirstObjectByType<
                        SafeZoneController
                    >();
            }
        }

        private void CreateRuntimeObjects()
        {
            if (_mesh == null)
            {
                _mesh =
                    new Mesh
                    {
                        name =
                            "SafeZoneWall_Runtime"
                    };

                _mesh.MarkDynamic();

                _meshFilter.sharedMesh =
                    _mesh;
            }

            if (_runtimeMaterial == null)
            {
                CreateTransparentMaterial();
            }
        }

        private void CreateTransparentMaterial()
        {
            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning(
                    "SafeZoneWallVisual: no se encontró el shader Sprites/Default."
                );

                return;
            }

            _runtimeMaterial =
                new Material(shader)
                {
                    name =
                        "SafeZoneWall_RuntimeMaterial"
                };

            _runtimeMaterial.color =
                wallColor;

            _runtimeMaterial.renderQueue =
                (int)RenderQueue.Transparent;

            _meshRenderer.sharedMaterial =
                _runtimeMaterial;

            _meshRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            _meshRenderer.receiveShadows =
                false;
        }

        private void BuildWallMesh(
            float radius
        )
        {
            if (_mesh == null)
                return;

            int count =
                Mathf.Max(
                    32,
                    segments
                );

            int ringCount =
                count + 1;

            Vector3[] vertices =
                new Vector3[
                    ringCount * 2
                ];

            Vector2[] uvs =
                new Vector2[
                    vertices.Length
                ];

            for (
                int i = 0;
                i <= count;
                i++
            )
            {
                float normalized =
                    (float)i / count;

                float angle =
                    normalized *
                    Mathf.PI *
                    2f;

                float x =
                    Mathf.Cos(angle) *
                    radius;

                float z =
                    Mathf.Sin(angle) *
                    radius;

                int bottomIndex =
                    i * 2;

                int topIndex =
                    bottomIndex + 1;

                vertices[bottomIndex] =
                    new Vector3(
                        x,
                        0f,
                        z
                    );

                vertices[topIndex] =
                    new Vector3(
                        x,
                        wallHeight,
                        z
                    );

                uvs[bottomIndex] =
                    new Vector2(
                        normalized,
                        0f
                    );

                uvs[topIndex] =
                    new Vector2(
                        normalized,
                        1f
                    );
            }

            int[] triangles =
                new int[
                    count * 12
                ];

            int triangleIndex = 0;

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                int bottomLeft =
                    i * 2;

                int topLeft =
                    bottomLeft + 1;

                int bottomRight =
                    bottomLeft + 2;

                int topRight =
                    bottomLeft + 3;

                // Exterior.
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    topRight;

                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topRight;

                triangles[triangleIndex++] =
                    bottomRight;

                // Interior.
                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topRight;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    bottomRight;

                triangles[triangleIndex++] =
                    topRight;
            }

            _mesh.Clear();

            _mesh.vertices =
                vertices;

            _mesh.uv =
                uvs;

            _mesh.triangles =
                triangles;

            _mesh.RecalculateBounds();
        }

        private void SetVisible(
            bool visible
        )
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled =
                    visible;
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            if (_runtimeMaterial != null)
            {
                Destroy(
                    _runtimeMaterial
                );
            }
        }
    }
}