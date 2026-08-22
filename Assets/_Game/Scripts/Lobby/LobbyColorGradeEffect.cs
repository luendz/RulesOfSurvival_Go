using UnityEngine;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class LobbyColorGradeEffect : MonoBehaviour
    {
        [Header("Color grading")]
        [SerializeField, Range(-1f, 1f)] private float exposure = 0.04f;
        [SerializeField, Range(0.5f, 1.5f)] private float contrast = 1.04f;
        [SerializeField, Range(0f, 2f)] private float saturation = 1.04f;
        [SerializeField, Range(0f, 0.5f)] private float vignette = 0.10f;
        [SerializeField, Range(0.2f, 1f)] private float vignetteSoftness = 0.62f;

        private Material _material;

        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private void OnDisable()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!EnsureMaterial())
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material.SetFloat(ExposureId, exposure);
            _material.SetFloat(ContrastId, contrast);
            _material.SetFloat(SaturationId, saturation);
            _material.SetFloat(VignetteId, vignette);
            _material.SetFloat(VignetteSoftnessId, vignetteSoftness);
            _material.SetFloat(
                AspectId,
                source.height > 0 ? (float)source.width / source.height : 1.7777778f
            );

            Graphics.Blit(source, destination, _material);
        }

        private bool EnsureMaterial()
        {
            if (_material != null)
            {
                return true;
            }

            Shader shader = Shader.Find("Hidden/ROS/LobbyColorGrade");
            if (shader == null || !shader.isSupported)
            {
                return false;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            return true;
        }
    }
}
