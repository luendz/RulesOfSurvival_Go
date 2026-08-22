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

        public float Exposure => exposure;
        public float Contrast => contrast;
        public float Saturation => saturation;
        public float Vignette => vignette;
        public float VignetteSoftness => vignetteSoftness;

        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        public void SetExposure(float value)
        {
            exposure = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetContrast(float value)
        {
            contrast = Mathf.Clamp(value, 0.5f, 1.5f);
        }

        public void SetSaturation(float value)
        {
            saturation = Mathf.Clamp(value, 0f, 2f);
        }

        public void SetVignette(float value)
        {
            vignette = Mathf.Clamp(value, 0f, 0.5f);
        }

        public void SetVignetteSoftness(float value)
        {
            vignetteSoftness = Mathf.Clamp(value, 0.2f, 1f);
        }

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
