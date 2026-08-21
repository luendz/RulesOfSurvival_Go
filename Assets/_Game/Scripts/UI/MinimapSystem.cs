using System.Collections.Generic;
using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Minimapa ortográfico en la esquina superior izquierda.
    /// Muestra al jugador (punto blanco), los bots vivos (rojos) y la Safe Zone (círculo azul).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapSystem : MonoBehaviour
    {
        // Parámetros de la cámara ortográfica
        private const float CamHeight   = 200f;   // altura sobre el suelo
        private const float CamOrthoSize = 110f;  // radio visible en unidades del mundo
        private const int   TexSize     = 256;

        // Parámetros de la UI
        private const float MapSize     = 180f;   // píxeles en pantalla

        private Transform                        _playerTransform;
        private SafeZoneController               _safeZone;
        private BattleRoyaleBotDirector          _botDirector;

        private Camera          _minimapCam;
        private RenderTexture   _rt;
        private GameObject      _camObj;
        private GameObject      _uiRoot;
        private RawImage        _mapImage;
        private RectTransform   _mapRect;

        // Dots overlay
        private GameObject      _overlayRoot;
        private Image           _playerDot;
        private readonly List<Image> _botDots = new List<Image>();
        private Image           _zoneDotPlayer; // punto de referencia de zona en UI

        // Circle de zona (dibujado con textura dinámica en pantalla)
        private Texture2D       _zoneCircleTex;

        // ---------------------------------------------------------------

        public void Bind(
            Transform playerTransform,
            SafeZoneController safeZone,
            BattleRoyaleBotDirector botDirector)
        {
            _playerTransform = playerTransform;
            _safeZone        = safeZone;
            _botDirector     = botDirector;

            BuildCamera();
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_camObj   != null) Destroy(_camObj);
            if (_uiRoot   != null) Destroy(_uiRoot);
            if (_rt       != null) _rt.Release();
            if (_zoneCircleTex != null) Destroy(_zoneCircleTex);
        }

        private void LateUpdate()
        {
            if (_playerTransform == null || _minimapCam == null) return;

            // Mover cámara sobre el jugador
            Vector3 camPos = _playerTransform.position;
            camPos.y = CamHeight;
            _minimapCam.transform.position = camPos;

            // Rotar cámara con el jugador (norte siempre hacia arriba de la pantalla)
            _minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            UpdateBotDots();
            UpdateZoneCircle();
        }

        // ---------------------------------------------------------------

        private void UpdateBotDots()
        {
            if (_botDirector == null) return;

            var bots = _botDirector.Bots;

            // Ajustar cantidad de dots
            while (_botDots.Count < bots.Count)
                _botDots.Add(CreateDot(_overlayRoot.transform, new Color(1f, 0.2f, 0.2f), 5f));

            for (int i = 0; i < _botDots.Count; i++)
            {
                if (i >= bots.Count || bots[i] == null)
                {
                    _botDots[i].gameObject.SetActive(false);
                    continue;
                }

                var bot = bots[i];
                bool alive = bot.GetComponent<ROS.Game.Combat.Health>() is { } h && h.IsAlive;
                _botDots[i].gameObject.SetActive(alive);
                if (!alive) continue;

                Vector2 uv = WorldToMapUV(bot.transform.position);
                _botDots[i].rectTransform.anchoredPosition = UVToMapPos(uv);
            }

            // Jugador: siempre en el centro
            if (_playerDot != null)
                _playerDot.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void UpdateZoneCircle()
        {
            // La zona se dibuja como un overlay de píxeles en _zoneCircleTex
            // Para mantenerlo simple, se redibuja cada N frames
            // (frame count throttle para rendimiento)
            if (Time.frameCount % 8 != 0) return;
            if (_safeZone == null || _zoneCircleTex == null) return;

            int size = _zoneCircleTex.width;
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float worldRadius = _safeZone.Radius;
            Vector3 zoneCenter = _safeZone.Center;

            // Radio en UV normalizado (0-1 dentro de CamOrthoSize)
            float uvRadius = worldRadius / CamOrthoSize;

            // Centro de la zona en UV respecto al jugador
            Vector2 uvCenter = WorldToMapUV(zoneCenter);

            DrawCircleOnTexture(pixels, size, uvCenter, uvRadius,
                new Color(0.2f, 0.6f, 1f, 0.9f), thickness: 2);

            _zoneCircleTex.SetPixels(pixels);
            _zoneCircleTex.Apply();
        }

        /// <summary>Dibuja el perímetro de un círculo en el array de píxeles.</summary>
        private static void DrawCircleOnTexture(
            Color[] pixels, int size,
            Vector2 uvCenter, float uvRadius,
            Color color, int thickness)
        {
            int cx = Mathf.RoundToInt(uvCenter.x * size);
            int cy = Mathf.RoundToInt(uvCenter.y * size);
            int r  = Mathf.RoundToInt(uvRadius   * size);

            for (int t = 0; t < thickness; t++)
            {
                int ri = r + t - thickness / 2;
                if (ri <= 0) continue;

                for (int angle = 0; angle < 360; angle++)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    int px = cx + Mathf.RoundToInt(Mathf.Cos(rad) * ri);
                    int py = cy + Mathf.RoundToInt(Mathf.Sin(rad) * ri);
                    if (px >= 0 && px < size && py >= 0 && py < size)
                        pixels[py * size + px] = color;
                }
            }
        }

        /// <summary>Convierte una posición del mundo a UV (0-1) relativo al jugador.</summary>
        private Vector2 WorldToMapUV(Vector3 worldPos)
        {
            if (_playerTransform == null) return new Vector2(0.5f, 0.5f);

            Vector3 delta = worldPos - _playerTransform.position;
            // La cámara mira hacia abajo; X = right, Z = forward
            float uvX = 0.5f + delta.x / (CamOrthoSize * 2f);
            float uvY = 0.5f + delta.z / (CamOrthoSize * 2f);
            return new Vector2(uvX, uvY);
        }

        /// <summary>Convierte UV (0-1) a posición anclada dentro del mapa UI.</summary>
        private Vector2 UVToMapPos(Vector2 uv)
        {
            return new Vector2(
                (uv.x - 0.5f) * MapSize,
                (uv.y - 0.5f) * MapSize
            );
        }

        // ---------------------------------------------------------------

        private void BuildCamera()
        {
            _camObj = new GameObject("MinimapCamera");
            _minimapCam = _camObj.AddComponent<Camera>();
            _minimapCam.orthographic     = true;
            _minimapCam.orthographicSize = CamOrthoSize;
            _minimapCam.clearFlags       = CameraClearFlags.SolidColor;
            _minimapCam.backgroundColor  = new Color(0.12f, 0.14f, 0.12f, 1f);
            _minimapCam.nearClipPlane    = 0.1f;
            _minimapCam.farClipPlane     = 300f;
            _minimapCam.depth            = -2;

            _rt = new RenderTexture(TexSize, TexSize, 16)
            {
                name        = "MinimapRT",
                filterMode  = FilterMode.Bilinear,
                antiAliasing = 1
            };
            _rt.Create();
            _minimapCam.targetTexture = _rt;
        }

        private void BuildUI()
        {
            _uiRoot = new GameObject("MinimapCanvas");

            Canvas canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;

            CanvasScaler scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Contenedor: esquina superior izquierda
            GameObject container = new GameObject("MinimapContainer");
            container.transform.SetParent(_uiRoot.transform, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(0f, 1f);
            cr.anchorMax        = new Vector2(0f, 1f);
            cr.pivot            = new Vector2(0f, 1f);
            cr.anchoredPosition = new Vector2(16f, -16f);
            cr.sizeDelta        = new Vector2(MapSize + 4f, MapSize + 4f);

            // Borde oscuro
            Image border = container.AddComponent<Image>();
            border.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

            // Mapa (RenderTexture)
            GameObject mapObj = new GameObject("Map");
            mapObj.transform.SetParent(container.transform, false);
            _mapImage = mapObj.AddComponent<RawImage>();
            _mapImage.texture = _rt;
            RectTransform mr = mapObj.GetComponent<RectTransform>();
            mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one;
            mr.offsetMin = new Vector2(2f, 2f);
            mr.offsetMax = new Vector2(-2f, -2f);
            _mapRect = mr;

            // Overlay de círculo de zona (Texture2D dinámica sobre el mapa)
            _zoneCircleTex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            GameObject zoneOverlay = new GameObject("ZoneOverlay");
            zoneOverlay.transform.SetParent(mapObj.transform, false);
            RawImage zoneImg = zoneOverlay.AddComponent<RawImage>();
            zoneImg.texture = _zoneCircleTex;
            RectTransform zr = zoneOverlay.GetComponent<RectTransform>();
            zr.anchorMin = Vector2.zero; zr.anchorMax = Vector2.one;
            zr.offsetMin = zr.offsetMax = Vector2.zero;

            // Overlay de dots
            _overlayRoot = new GameObject("DotsOverlay");
            _overlayRoot.transform.SetParent(mapObj.transform, false);
            RectTransform or = _overlayRoot.AddComponent<RectTransform>();
            or.anchorMin = new Vector2(0.5f, 0.5f);
            or.anchorMax = new Vector2(0.5f, 0.5f);
            or.sizeDelta = Vector2.zero;
            or.anchoredPosition = Vector2.zero;

            // Punto del jugador (verde brillante, siempre en el centro)
            _playerDot = CreateDot(_overlayRoot.transform, new Color(0.1f, 1f, 0.3f), 7f);
        }

        private static Image CreateDot(Transform parent, Color color, float size)
        {
            GameObject go = new GameObject("Dot");
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = color;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;

            return img;
        }
    }
}
