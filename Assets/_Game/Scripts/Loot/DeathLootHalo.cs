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

                if (_effectRoot != null)
                {
                    _effectRoot.localPosition =
                        _effectBasePosition +
                        Vector3.up * bobOffset;
                }
            }
        }

        public void ConfigureFloatingModel(
            Transform model
        )
        {
            _floatingModel = model;

            if (_floatingModel != null)
            {
                _floatingBasePosition =
                    _floatingModel.localPosition;

                Renderer[] renderers =
                    _floatingModel.GetComponentsInChildren<Renderer>(
                        true
                    );

                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;

                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

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
                new Vector3(0.36f, 0.18f, 0.42f),
                0.03f
            );

            CreateAuraLayer(
                "Halo_Azul_Base_Caja",
                new Vector3(0f, -0.16f, 0f),
                new Vector3(0.48f, 0.015f, 0.54f),
                0.065f
            );
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
