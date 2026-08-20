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
        private float lightIntensity = 2.4f;

        [SerializeField]
        private float lightRange = 4.5f;

        [Header("Animación")]
        [SerializeField]
        private float pulseSpeed = 1.8f;

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
                    Mathf.Lerp(0.97f, 1.06f, pulse);
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
                "Aura_Azul_Interior",
                new Vector3(0f, 0.65f, 0f),
                new Vector3(1.15f, 0.75f, 1.15f),
                0.12f
            );

            CreateAuraLayer(
                "Aura_Azul_Exterior",
                new Vector3(0f, 0.95f, 0f),
                new Vector3(1.7f, 1.25f, 1.7f),
                0.045f
            );

            CreateAuraLayer(
                "Halo_Azul_Suelo",
                new Vector3(0f, -0.27f, 0f),
                new Vector3(1.55f, 0.025f, 1.55f),
                0.16f
            );
        }

        private void CreateLight()
        {
            GameObject lightObject =
                new GameObject("Luz_Azul_Loot");

            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition =
                Vector3.up * 0.55f;

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
