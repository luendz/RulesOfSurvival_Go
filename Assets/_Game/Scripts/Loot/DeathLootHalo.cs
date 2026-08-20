using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ROS.Game.Loot
{
    public sealed class DeathLootHalo : MonoBehaviour
    {
        [Header("Color")]
        [SerializeField]
        private Color haloColor =
            new Color(0.1f, 0.62f, 1f, 1f);

        [Header("Luz")]
        [SerializeField]
        private float lightIntensity = 0.75f;

        [SerializeField]
        private float lightRange = 2f;

        [Header("Animación")]
        [SerializeField]
        private float pulseSpeed = 1.25f;

        [Header("Flotación del modelo")]
        [SerializeField]
        private float hoverHeight = 0.9f;

        [SerializeField]
        private float bobAmplitude = 0.035f;

        [SerializeField]
        private float bobSpeed = 1.6f;

        [Header("Rotación")]
        [SerializeField]
        private float spinSpeed = 35f;

        [SerializeField]
        private float rayOrbitSpeed = 50f;

        private readonly List<Transform> _auraLayers =
            new List<Transform>();

        private readonly List<Vector3> _baseScales =
            new List<Vector3>();

        private readonly List<Material> _runtimeMaterials =
            new List<Material>();

        private Light _haloLight;

        private Transform _effectRoot;

        private Transform _floatingModel;

        private Vector3 _floatingBasePosition;

        private Vector3 _effectBasePosition;

        private Quaternion _diamondRotation =
            Quaternion.identity;

        public bool HasFloatingModel =>
            _floatingModel != null;

        private void Awake()
        {
            BuildHalo();
        }

        private void Update()
        {
            float pulse =
                0.5f +
                Mathf.Sin(Time.time * pulseSpeed) * 0.5f;

            if (_haloLight != null)
            {
                _haloLight.intensity =
                    lightIntensity * Mathf.Lerp(0.8f, 1.15f, pulse);
            }

            for (int i = 0; i < _auraLayers.Count; i++)
            {
                if (_auraLayers[i] == null)
                {
                    continue;
                }

                _auraLayers[i].localScale =
                    _baseScales[i] *
                    Mathf.Lerp(0.99f, 1.02f, pulse);
            }

            if (_floatingModel != null)
            {
                float bobOffset =
                    hoverHeight +
                    Mathf.Sin(Time.time * bobSpeed) *
                    bobAmplitude;

                _floatingModel.localPosition =
                    _floatingBasePosition +
                    Vector3.up * bobOffset;

                _floatingModel.localRotation =
                    Quaternion.AngleAxis(
                        Time.time * spinSpeed,
                        Vector3.up
                    ) *
                    _diamondRotation;

                if (_effectRoot != null)
                {
                    _effectRoot.localPosition =
                        _effectBasePosition +
                        Vector3.up * bobOffset;

                    _effectRoot.localRotation =
                        Quaternion.AngleAxis(
                            Time.time * rayOrbitSpeed,
                            Vector3.up
                        );
                }
            }
        }

        public void ConfigureFloatingModel(
            Transform model
        )
        {
            if (model != null)
            {
                Renderer[] renderers =
                    model.GetComponentsInChildren<Renderer>(
                        true
                    );

                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;

                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    GameObject pivotObject =
                        new GameObject("Pivote_Caja_Flotante");

                    Transform pivot = pivotObject.transform;
                    pivot.SetParent(transform, false);
                    pivot.position = bounds.center;
                    model.SetParent(pivot, true);

                    _floatingModel = pivot;
                    _floatingBasePosition =
                        _floatingModel.localPosition;

                    _diamondRotation =
                        Quaternion.FromToRotation(
                            Vector3.one.normalized,
                            Vector3.up
                        );

                    _effectBasePosition =
                        transform.InverseTransformPoint(
                            bounds.center
                        );
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _runtimeMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        private void BuildHalo()
        {
            GameObject effectObject =
                new GameObject("Halo_Azul_Caja");

            effectObject.transform.SetParent(transform, false);
            _effectRoot = effectObject.transform;

            CreateLight();

            CreateAuraLayer(
                "Aura_Azul_Exterior",
                Vector3.zero,
                new Vector3(0.32f, 0.2f, 0.36f),
                0.018f
            );

            CreateRayRing();
        }

        private void CreateRayRing()
        {
            GameObject rayRootObject =
                new GameObject("Rayos_Azules");

            Transform rayRoot = rayRootObject.transform;
            rayRoot.SetParent(_effectRoot, false);

            const int rayCount = 8;
            const float radius = 0.3f;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = 360f * i / rayCount;
                float radians = angle * Mathf.Deg2Rad;
                float height = 0.18f + (i % 3) * 0.025f;

                GameObject ray =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cylinder
                    );

                ray.name = $"Rayo_Azul_{i + 1:00}";
                ray.transform.SetParent(rayRoot, false);
                ray.transform.localPosition = new Vector3(
                    Mathf.Cos(radians) * radius,
                    Mathf.Sin(radians * 2f) * 0.04f,
                    Mathf.Sin(radians) * radius
                );
                ray.transform.localRotation =
                    Quaternion.Euler(
                        Mathf.Sin(radians) * 18f,
                        -angle,
                        Mathf.Cos(radians) * 18f
                    );
                ray.transform.localScale =
                    new Vector3(0.012f, height, 0.012f);

                Collider collider = ray.GetComponent<Collider>();

                if (collider != null)
                {
                    collider.enabled = false;
                    Destroy(collider);
                }

                Renderer renderer = ray.GetComponent<Renderer>();

                if (renderer != null)
                {
                    Material material = CreateAuraMaterial(0.16f);
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode =
                        ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                _auraLayers.Add(ray.transform);
                _baseScales.Add(ray.transform.localScale);
            }
        }

        private void CreateLight()
        {
            GameObject lightObject =
                new GameObject("Luz_Azul_Loot");

            lightObject.transform.SetParent(_effectRoot, false);
            lightObject.transform.localPosition =
                Vector3.up * 0.12f;

            _haloLight = lightObject.AddComponent<Light>();
            _haloLight.type = LightType.Point;
            _haloLight.color = haloColor;
            _haloLight.range = lightRange;
            _haloLight.intensity = lightIntensity;
            _haloLight.shadows = LightShadows.None;
        }

        private void CreateAuraLayer(
            string layerName,
            Vector3 localPosition,
            Vector3 localScale,
            float alpha
        )
        {
            GameObject layer =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            layer.name = layerName;
            layer.transform.SetParent(_effectRoot, false);
            layer.transform.localPosition = localPosition;
            layer.transform.localScale = localScale;

            Collider collider = layer.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            Renderer renderer = layer.GetComponent<Renderer>();

            if (renderer != null)
            {
                Material material = CreateAuraMaterial(alpha);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            _auraLayers.Add(layer.transform);
            _baseScales.Add(localScale);
        }

        private Material CreateAuraMaterial(float alpha)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = "AuraAzul_Runtime",
                color = new Color(
                    haloColor.r,
                    haloColor.g,
                    haloColor.b,
                    alpha
                ),
                renderQueue = 3000,
                hideFlags = HideFlags.DontSave
            };

            _runtimeMaterials.Add(material);
            return material;
        }
    }
}
