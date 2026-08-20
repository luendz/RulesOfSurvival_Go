using System.Collections;
using System.Collections.Generic;
using ROS.Game.Combat;
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
            StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveRoutine()
        {
            // Desactivar colisionadores para que el cuerpo no bloquee
            Collider[] cols = GetComponentsInChildren<Collider>(false);
            foreach (Collider col in cols)
            {
                if (col != null) col.enabled = false;
            }

            // Recolectar renderers y preparar materiales para fade
            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            List<Material> fadeMaterials = new List<Material>();

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material mat in r.materials)
                {
                    if (mat == null) continue;
                    EnableTransparency(mat);
                    fadeMaterials.Add(mat);
                }
            }

            SpawnDustParticles();

            Vector3 startPos   = transform.position;
            Vector3 startScale = transform.localScale;
            float elapsed      = 0f;

            while (elapsed < DissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DissolveDuration);

                // Hundir ligeramente
                Vector3 pos = transform.position;
                pos.y = startPos.y - SinkDistance * t;
                transform.position = pos;

                // Encoger suavemente al final
                float scaleFactor = Mathf.Lerp(1f, 0.05f, Mathf.Pow(t, 2f));
                transform.localScale = startScale * scaleFactor;

                // Desvanecer y añadir brillo azul al desintegrarse
                float alpha = 1f - t;
                foreach (Material mat in fadeMaterials)
                {
                    if (mat == null) continue;
                    ApplyAlpha(mat, alpha);
                    ApplyBlueEmission(mat, t);
                }

                yield return null;
            }

            // Ocultar renderers; mantener el GameObject activo para loot
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = false;
            }

            transform.localScale = startScale;
        }

        private static void EnableTransparency(Material mat)
        {
            // URP / Lit shader
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
                return;
            }

            // Built-in Standard shader
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2f);
                mat.SetInt("_SrcBlend",
                    (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }

        private static void ApplyBlueEmission(Material mat, float t)
        {
            // Pico de brillo azul a mitad de la desintegración
            float intensity = Mathf.Sin(t * Mathf.PI) * 2.2f;
            Color glow      = new Color(0.2f, 0.5f, 1.0f) * intensity;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow);
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow);
            }
        }

        private static void ApplyAlpha(Material mat, float alpha)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
                return;
            }

            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }

        private void SpawnDustParticles()
        {
            GameObject psObj = new GameObject("DeathDust");
            psObj.transform.position =
                transform.position + Vector3.up * 0.9f;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop            = false;
            main.playOnAwake     = true;
            main.duration        = 0.5f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 2.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.30f, 0.60f, 1.00f, 0.95f),
                new Color(0.10f, 0.30f, 0.80f, 0.60f)
            );
            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(-0.1f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 80;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 50, 70)
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled    = true;
            shape.shapeType  = ParticleSystemShapeType.Sphere;
            shape.radius     = 0.45f;

            ParticleSystem.ColorOverLifetimeModule col =
                ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(
                        new Color(0.4f, 0.7f, 1.0f), 0f),
                    new GradientColorKey(
                        new Color(0.1f, 0.2f, 0.7f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            Destroy(psObj, 3f);
        }
    }
}
