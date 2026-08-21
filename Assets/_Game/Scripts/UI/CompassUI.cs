using ROS.Game.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Tira horizontal de brújula en el borde superior de pantalla.
    /// Muestra N / NE / E / SE / S / SO / O / NO y se desplaza con
    /// el yaw del jugador. Una línea amarilla marca el rumbo actual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CompassUI : MonoBehaviour
    {
        private const float StripWidth  = 400f;
        private const float StripHeight = 28f;
        private const float FovDegrees  = 120f;
        private const float PixPerDeg   = StripWidth / FovDegrees;

        private static readonly (string label, float angle)[] Cardinals =
        {
            ("N",   0f), ("NE",  45f), ("E",   90f), ("SE", 135f),
            ("S", 180f), ("SO", 225f), ("O",  270f), ("NO", 315f),
        };

        private Transform          _playerTransform;
        private ThirdPersonCamera  _camera;
        private GameObject         _root;
        private readonly Text[]    _labels = new Text[Cardinals.Length];

        // ---------------------------------------------------------------

        public void Bind(Transform playerTransform, ThirdPersonCamera camera = null)
        {
            _playerTransform = playerTransform;
            _camera          = camera;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root);
        }

        private void LateUpdate()
        {
            if (_playerTransform == null || _root == null) return;

            // En free-look la cámara se mueve independiente del cuerpo; usar su yaw
            float yaw = _camera != null
                ? _camera.CameraYaw
                : _playerTransform.eulerAngles.y;

            for (int i = 0; i < Cardinals.Length; i++)
            {
                // delta positivo = cardinal está a la derecha del forward del jugador
                float delta = Mathf.DeltaAngle(yaw, Cardinals[i].angle);
                float x     = delta * PixPerDeg;

                bool visible = Mathf.Abs(x) < StripWidth * 0.5f + 30f;
                _labels[i].gameObject.SetActive(visible);
                if (visible)
                    _labels[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
            }
        }

        // ---------------------------------------------------------------

        private void BuildUI()
        {
            _root = new GameObject("CompassCanvas");

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 18;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Tira: top-center
            GameObject container = new GameObject("CompassStrip");
            container.transform.SetParent(_root.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(0.5f, 1f);
            cr.anchorMax        = new Vector2(0.5f, 1f);
            cr.pivot            = new Vector2(0.5f, 1f);
            cr.anchoredPosition = new Vector2(0f, -10f);
            cr.sizeDelta        = new Vector2(StripWidth, StripHeight);

            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.48f);

            // Mask: los labels no salen fuera de la tira
            container.AddComponent<RectMask2D>();

            // Raíz de labels, anclada al centro de la tira
            GameObject labelsRoot = new GameObject("Labels");
            labelsRoot.transform.SetParent(container.transform, false);
            RectTransform lr = labelsRoot.AddComponent<RectTransform>();
            lr.anchorMin        = new Vector2(0.5f, 0.5f);
            lr.anchorMax        = new Vector2(0.5f, 0.5f);
            lr.sizeDelta        = Vector2.zero;
            lr.anchoredPosition = Vector2.zero;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < Cardinals.Length; i++)
            {
                bool isMain = i % 2 == 0; // N E S O son principales

                GameObject go = new GameObject(Cardinals[i].label);
                go.transform.SetParent(labelsRoot.transform, false);

                Text txt = go.AddComponent<Text>();
                txt.font      = font;
                txt.fontSize  = isMain ? 15 : 12;
                txt.fontStyle = isMain ? FontStyle.Bold : FontStyle.Normal;
                txt.color     = isMain
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(0.72f, 0.72f, 0.72f, 0.82f);
                txt.alignment = TextAnchor.MiddleCenter;
                txt.text      = Cardinals[i].label;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(36f, StripHeight);
                rt.anchoredPosition = Vector2.zero;

                _labels[i] = txt;
            }

            // Línea central amarilla (rumbo actual)
            GameObject lineGo = new GameObject("CenterLine");
            lineGo.transform.SetParent(container.transform, false);
            Image line = lineGo.AddComponent<Image>();
            line.color = new Color(1f, 0.85f, 0.15f, 0.92f);
            RectTransform lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin        = new Vector2(0.5f, 0f);
            lineRt.anchorMax        = new Vector2(0.5f, 1f);
            lineRt.sizeDelta        = new Vector2(2f, 0f);
            lineRt.anchoredPosition = Vector2.zero;
        }
    }
}
