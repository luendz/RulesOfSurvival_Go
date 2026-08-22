using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyBackgroundController : MonoBehaviour
    {
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private float planeDistance = 90f;

        private Canvas _backgroundCanvas;

        private void Start()
        {
            BuildBackground();
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
            image.color = Color.white;

            AspectRatioFitter fitter = backgroundObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
        }
    }
}
