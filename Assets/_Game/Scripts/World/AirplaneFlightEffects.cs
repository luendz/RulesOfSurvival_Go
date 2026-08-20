using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ROS.Game.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AirplaneController))]
    public sealed class AirplaneFlightEffects : MonoBehaviour
    {
        private static readonly Vector3[] JetPositions =
        {
            new Vector3(-4.2f, -0.35f, -5.3f),
            new Vector3(4.2f, -0.35f, -5.3f)
        };

        private static readonly Vector3[] WindLinePositions =
        {
            new Vector3(-8.2f, 1.4f, 0.8f),
            new Vector3(8.2f, 1.4f, 0.8f),
            new Vector3(-7.1f, -1.2f, 0.1f),
            new Vector3(7.1f, -1.2f, 0.1f),
            new Vector3(-3.7f, 2.3f, 1.8f),
            new Vector3(3.7f, 2.3f, 1.8f),
            new Vector3(-1.7f, -2f, 1.1f),
            new Vector3(1.7f, -2f, 1.1f)
        };

        [SerializeField] private Color jetColor =
            new Color(0.12f, 0.72f, 1f, 1f);
        [SerializeField] private Color jetCoreColor =
            new Color(0.72f, 0.35f, 1f, 1f);
        [SerializeField] private Color windColor =
            new Color(0.65f, 0.9f, 1f, 0.55f);
        [SerializeField] private float jetLightIntensity = 5.5f;
        [SerializeField] private float navigationLightIntensity = 3.5f;

        private readonly List<ParticleSystem> _jetParticles =
            new List<ParticleSystem>();
        private readonly List<Light> _jetLights = new List<Light>();
        private readonly List<TrailRenderer> _windLines =
            new List<TrailRenderer>();
        private readonly List<Material> _runtimeMaterials =
            new List<Material>();

        private AirplaneController _airplane;
        private Light _leftNavigationLight;
        private Light _rightNavigationLight;
        private bool _wasFlying;

        private void Awake()
        {
            _airplane = GetComponent<AirplaneController>();
            BuildEffects();
            SetFlightEffectsActive(false);
        }

        private void Update()
        {
            bool isFlying = _airplane != null && _airplane.IsFlying;

            if (isFlying != _wasFlying)
            {
                SetFlightEffectsActive(isFlying);
                _wasFlying = isFlying;
            }

            if (!isFlying)
            {
                return;
            }

            float pulse = 0.82f +
                          Mathf.Sin(Time.time * 18f) * 0.18f;

            for (int i = 0; i < _jetLights.Count; i++)
            {
                _jetLights[i].intensity = jetLightIntensity * pulse;
            }

            float navigationPulse = 0.72f +
                                    Mathf.Sin(Time.time * 7f) * 0.28f;
            if (_leftNavigationLight != null)
            {
                _leftNavigationLight.intensity =
                    navigationLightIntensity * navigationPulse;
            }

            if (_rightNavigationLight != null)
            {
                _rightNavigationLight.intensity =
                    navigationLightIntensity * navigationPulse;
            }
        }

        private void BuildEffects()
        {
            Transform existing = transform.Find("FlightEffects");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject root = new GameObject("FlightEffects");
            root.transform.SetParent(transform, false);

            Material jetMaterial = CreateAdditiveMaterial(
                "JetTrailMaterial",
                jetColor
            );
            Material windMaterial = CreateAdditiveMaterial(
                "WindLineMaterial",
                windColor
            );

            for (int i = 0; i < JetPositions.Length; i++)
            {
                CreateJet(root.transform, JetPositions[i], jetMaterial, i);
            }

            CreateNavigationLights(root.transform);

            for (int i = 0; i < WindLinePositions.Length; i++)
            {
                CreateWindLine(
                    root.transform,
                    WindLinePositions[i],
                    windMaterial,
                    i
                );
            }
        }

        private void CreateJet(
            Transform parent,
            Vector3 localPosition,
            Material material,
            int index
        )
        {
            GameObject jet = new GameObject($"JetEngine_{index + 1}");
            jet.transform.SetParent(parent, false);
            jet.transform.localPosition = localPosition;
            jet.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            ParticleSystem particles = jet.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.82f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                jetColor,
                jetCoreColor
            );
            main.maxParticles = 220;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 85f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.32f;
            shape.length = 0.35f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(jetColor, 0.25f),
                    new GradientColorKey(jetCoreColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.72f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer =
                particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.12f;
            renderer.material = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Light light = jet.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = jetColor;
            light.range = 13f;
            light.intensity = jetLightIntensity;
            light.shadows = LightShadows.None;

            _jetParticles.Add(particles);
            _jetLights.Add(light);
        }

        private void CreateNavigationLights(Transform parent)
        {
            _leftNavigationLight = CreateNavigationLight(
                parent,
                "NavigationLight_Left",
                new Vector3(-8.5f, 0.25f, 0.1f),
                new Color(1f, 0.08f, 0.05f)
            );
            _rightNavigationLight = CreateNavigationLight(
                parent,
                "NavigationLight_Right",
                new Vector3(8.5f, 0.25f, 0.1f),
                new Color(0.1f, 1f, 0.35f)
            );
        }

        private Light CreateNavigationLight(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Color color
        )
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 8f;
            light.intensity = navigationLightIntensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private void CreateWindLine(
            Transform parent,
            Vector3 localPosition,
            Material material,
            int index
        )
        {
            GameObject line = new GameObject($"WindLine_{index + 1}");
            line.transform.SetParent(parent, false);
            line.transform.localPosition = localPosition;

            TrailRenderer trail = line.AddComponent<TrailRenderer>();
            trail.time = 0.95f + (index % 3) * 0.12f;
            trail.minVertexDistance = 0.18f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.025f),
                new Keyframe(0.16f, 0.13f),
                new Keyframe(1f, 0f)
            );
            trail.colorGradient = CreateWindGradient(index);
            trail.material = material;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            _windLines.Add(trail);
        }

        private Gradient CreateWindGradient(int index)
        {
            float alpha = 0.42f + (index % 2) * 0.13f;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(windColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            return gradient;
        }

        private Material CreateAdditiveMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = materialName,
                color = color,
                hideFlags = HideFlags.DontSave
            };
            _runtimeMaterials.Add(material);
            return material;
        }

        private void SetFlightEffectsActive(bool active)
        {
            for (int i = 0; i < _jetParticles.Count; i++)
            {
                if (active)
                {
                    _jetParticles[i].Play(true);
                }
                else
                {
                    _jetParticles[i].Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear
                    );
                }
            }

            for (int i = 0; i < _jetLights.Count; i++)
            {
                _jetLights[i].enabled = active;
            }

            if (_leftNavigationLight != null)
            {
                _leftNavigationLight.enabled = active;
            }

            if (_rightNavigationLight != null)
            {
                _rightNavigationLight.enabled = active;
            }

            for (int i = 0; i < _windLines.Count; i++)
            {
                _windLines[i].Clear();
                _windLines[i].emitting = active;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _runtimeMaterials.Count; i++)
            {
                if (_runtimeMaterials[i] != null)
                {
                    Destroy(_runtimeMaterials[i]);
                }
            }
        }
    }
}
