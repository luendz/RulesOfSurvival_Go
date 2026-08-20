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

        private readonly List<Transform> _auraLayers =
            new List<Transform>();

        private readonly List<Vector3> _baseScales =
            new List<Vector3>();

        private readonly List<Material> _runtimeMaterials =
            new List<Material>();

        private Light _haloLight;

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
            CreateLight();

            CreateAuraLayer(
                "Aura_Azul_Caja",
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.72f, 0.3f, 0.72f),
                0.035f
            );

            CreateAuraLayer(
                "Halo_Azul_Suelo",
                new Vector3(0f, -0.285f, 0f),
                new Vector3(0.95f, 0.015f, 0.95f),
                0.08f
            );
        }

        private void CreateLight()
        {
            GameObject lightObject =
                new GameObject("Luz_Azul_Loot");

            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition =
                Vector3.up * 0.25f;

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
            layer.transform.SetParent(transform, false);
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
