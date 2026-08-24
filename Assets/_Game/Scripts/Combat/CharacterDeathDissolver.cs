using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterDeathDissolver : MonoBehaviour
    {
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void OnDied(DamageInfo _)
        {
            HideBodyInstant();
            SpawnDustParticles();
        }

        private void HideBodyInstant()
        {
            BotHealthBar bar = GetComponent<BotHealthBar>();
            if (bar != null)
                bar.ForceDestroy();

            Collider[] cols = GetComponentsInChildren<Collider>(false);
            foreach (Collider col in cols)
            {
                if (col != null)
                    col.enabled = false;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private void SpawnDustParticles()
        {
            GameObject psObj = new GameObject("DeathDust");
            psObj.transform.position =
                transform.position + Vector3.up * 0.9f;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

            // Un ParticleSystem agregado por codigo puede empezar a reproducirse
            // inmediatamente porque playOnAwake viene activo por defecto. Unity
            // no permite modificar MainModule.duration mientras esta jugando.
            // Lo detenemos y limpiamos antes de tocar cualquiera de sus modulos.
            ps.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(0.3f, 2.2f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(0.05f, 0.20f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.00f, 0.45f, 1.00f, 1.00f),
                new Color(0.00f, 0.20f, 0.85f, 0.70f)
            );
            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 50, 70)
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.00f, 0.55f, 1.00f),
                        0f
                    ),
                    new GradientColorKey(
                        new Color(0.00f, 0.15f, 0.75f),
                        1f
                    )
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color =
                new ParticleSystem.MinMaxGradient(gradient);

            // Material neutro: evita que el shader por defecto tinte los colores.
            ParticleSystemRenderer particleRenderer =
                psObj.GetComponent<ParticleSystemRenderer>();
            Shader particleShader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (particleShader != null)
            {
                Material particleMaterial = new Material(particleShader)
                {
                    color = Color.white
                };

                particleRenderer.material = particleMaterial;
                Destroy(particleMaterial, 3.5f);
            }

            // Reproducir solo cuando toda la configuracion ya esta terminada.
            ps.Play(true);
            Destroy(psObj, 3.5f);
        }
    }
}
