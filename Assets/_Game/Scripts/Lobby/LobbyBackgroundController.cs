using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyBackgroundController : MonoBehaviour
    {
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private float planeDistance = 90f;

        [Header("Presentation")]
        [SerializeField, Range(0.5f, 1f)] private float backgroundBrightness = 0.90f;
        [SerializeField, Range(0f, 0.35f)] private float vignetteStrength = 0.16f;

        private Canvas _backgroundCanvas;
        private Texture2D _vignetteTexture;
        private Sprite _vignetteSprite;

        private void Start()
        {
            BuildBackground();
        }

        private void OnDestroy()
        {
            if (_vignetteSprite != null)
            {
                Destroy(_vignetteSprite);
            }

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
            }
        }

        private void BuildBackground()
        {
            if (backgroundSprite == null)
            {
                Debug.LogWarning("LobbyBackgroundController no tiene un Sprite de fondo asignado.");
                return;
            }

            Camera lobbyCamera = Camera.main;
            if (lobbyCamera == null)
            {
                Debug.LogWarning("No se encontró la cámara principal del lobby para mostrar el fondo.");
                return;
            }

            GameObject canvasObject = new GameObject("Lobby Background Canvas");
            canvasObject.transform.SetParent(transform, false);

            _backgroundCanvas = canvasObject.AddComponent<Canvas>();
            _backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _backgroundCanvas.worldCamera = lobbyCamera;
            _backgroundCanvas.planeDistance = Mathf.Clamp(
                planeDistance,
                lobbyCamera.nearClipPlane + 0.1f,
                lobbyCamera.farClipPlane - 0.5f
            );
            _backgroundCanvas.sortingOrder = -100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = new GameObject(
                "Lobby Background",
                typeof(RectTransform),
                typeof(Image),
                typeof(AspectRatioFitter)
            );
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1920f, 1080f);

            Image image = backgroundObject.GetComponent<Image>();
            image.sprite = backgroundSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            float brightness = Mathf.Clamp01(backgroundBrightness);
            image.color = new Color(brightness, brightness, brightness, 1f);

            AspectRatioFitter fitter = backgroundObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;

            CreateFocusVignette(canvasObject.transform);
        }

        private void CreateFocusVignette(Transform parent)
        {
            if (vignetteStrength <= 0.001f)
            {
                return;
            }

            _vignetteTexture = BuildVignetteTexture(256);
            _vignetteSprite = Sprite.Create(
                _vignetteTexture,
                new Rect(0f, 0f, _vignetteTexture.width, _vignetteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            _vignetteSprite.name = "Lobby Background Focus Vignette";

            GameObject vignetteObject = new GameObject(
                "Lobby Background Vignette",
                typeof(RectTransform),
                typeof(Image)
            );
            vignetteObject.transform.SetParent(parent, false);

            RectTransform rect = vignetteObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = vignetteObject.GetComponent<Image>();
            image.sprite = _vignetteSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private Texture2D BuildVignetteTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Lobby Background Vignette Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.InverseLerp(0.48f, 1.22f, distance);
                    edge = Mathf.SmoothStep(0f, 1f, edge);
                    float alpha = edge * vignetteStrength;
                    pixels[y * size + x] = new Color(0.015f, 0.025f, 0.05f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
