using ROS.Game.Combat;
using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterDeathDissolver : MonoBehaviour
    {
        private const float DissolveDuration = 2.5f;
        private const float SinkDistance     = 0.7f;

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
            if (bar != null) bar.ForceDestroy();

            Collider[] cols = GetComponentsInChildren<Collider>(false);
            foreach (Collider col in cols)
                if (col != null) col.enabled = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            foreach (Renderer r in renderers)
                if (r != null) r.enabled = false;
        }

        private void SpawnDustParticles()
        {
            GameObject psObj = new GameObject("DeathDust");
            psObj.transform.position =
                transform.position + Vector3.up * 0.9f;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

            // Material neutro: evita que el shader por defecto tinte los colores
            ParticleSystemRenderer psr =
                psObj.GetComponent<ParticleSystemRenderer>();
            Shader particleShader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (particleShader != null)
            {
                Material pm = new Material(particleShader);
                pm.color      = Color.white;
                psr.material  = pm;
                Destroy(pm, 3.5f);
            }

            ParticleSystem.MainModule main = ps.main;
            main.loop            = false;
            main.playOnAwake     = true;
            main.duration        = 0.5f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 2.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.20f);
            // Azul puro: sin componente rojo para evitar lila
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.00f, 0.45f, 1.00f, 1.00f),
                new Color(0.00f, 0.20f, 0.85f, 0.70f)
            );
            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 80;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 50, 70)
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.45f;

            ParticleSystem.ColorOverLifetimeModule col =
                ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.00f, 0.55f, 1.00f), 0f),
                    new GradientColorKey(new Color(0.00f, 0.15f, 0.75f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            Destroy(psObj, 3.5f);
        }
    }
}
