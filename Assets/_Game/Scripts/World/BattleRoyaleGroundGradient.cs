using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    public sealed class BattleRoyaleGroundGradient : MonoBehaviour
    {
        public const string ShaderName =
            "ROS/Environment/GroundGradient";

        public static readonly Color DarkGreen =
            new Color(0.025f, 0.19f, 0.055f, 1f);
        public static readonly Color LightGreen =
            new Color(0.24f, 0.64f, 0.14f, 1f);

        private const string BattleRoyaleScene = "07_BattleRoyaleTest";

        private Material _runtimeMaterial;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != BattleRoyaleScene)
            {
                return;
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogWarning(
                    "No se encontró Ground para aplicar el degradado verde."
                );
                return;
            }

            BattleRoyaleGroundGradient gradient =
                ground.GetComponent<BattleRoyaleGroundGradient>();
            if (gradient == null)
            {
                gradient = ground.AddComponent<
                    BattleRoyaleGroundGradient
                >();
            }

            gradient.Apply(ground.GetComponent<Renderer>());
        }

        public void Apply(Renderer groundRenderer)
        {
            if (groundRenderer == null)
            {
                Debug.LogWarning(
                    "Ground no tiene un Renderer para aplicar el degradado."
                );
                return;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError(
                    $"No se encontró el shader {ShaderName}."
                );
                return;
            }

            ReleaseMaterial();
            _runtimeMaterial = new Material(shader)
            {
                name = "GroundGradient_Runtime"
            };
            _runtimeMaterial.SetColor("_DarkColor", DarkGreen);
            _runtimeMaterial.SetColor("_LightColor", LightGreen);
            _runtimeMaterial.SetFloat("_Smoothness", 0.08f);
            Bounds bounds = groundRenderer.bounds;
            float gradientMinimum = bounds.min.x + bounds.min.z;
            float gradientMaximum = bounds.max.x + bounds.max.z;
            _runtimeMaterial.SetFloat(
                "_GradientMinimum",
                gradientMinimum
            );
            _runtimeMaterial.SetFloat(
                "_GradientRange",
                Mathf.Max(0.001f, gradientMaximum - gradientMinimum)
            );
            groundRenderer.sharedMaterial = _runtimeMaterial;
        }

        private void OnDestroy()
        {
            ReleaseMaterial();
        }

        private void ReleaseMaterial()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }
}
