using ROS.Game.Combat;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Muestra flechas en pantalla indicando la dirección de la que viene el daño.
    /// Se anclará al jugador local via Bind().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageDirectionIndicator : MonoBehaviour
    {
        private const float ArrowSize    = 60f;
        private const float ArrowRadius  = 120f;  // píxeles desde el centro
        private const float FlashTime   = 0.9f;

        private Health    _health;
        private Transform _playerTransform;

        private GameObject _root;
        private Image[]    _arrows; // 0=front, 1=right, 2=back, 3=left

        // ---------------------------------------------------------------

        public void Bind(Health health, Transform playerTransform)
        {
            _health          = health;
            _playerTransform = playerTransform;

            if (_health != null)
                _health.Damaged += OnDamaged;

            BuildUI();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;

            if (_root != null) Destroy(_root);
        }

        // ---------------------------------------------------------------

        private void OnDamaged(DamageResult result)
        {
            if (_playerTransform == null) return;

            Vector3 hitPoint = result.Damage.Point;
            if (hitPoint == Vector3.zero) return; // daño sin posición (zona)

            Vector3 dir   = hitPoint - _playerTransform.position;
            dir.y         = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Vector3 forward = _playerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

            float angle = Vector3.SignedAngle(forward, dir.normalized, Vector3.up);
            // angle: 0=frente, 90=derecha, ±180=atrás, -90=izquierda

            // Elegir flecha más cercana (4 direcciones)
            int arrowIdx;
            if      (angle > -45f && angle <=  45f) arrowIdx = 0; // frente
            else if (angle >  45f && angle <= 135f) arrowIdx = 1; // derecha
            else if (angle < -45f && angle >= -135f) arrowIdx = 3; // izquierda
            else                                     arrowIdx = 2; // atrás

            StartCoroutine(FlashArrow(arrowIdx));
        }

        private IEnumerator FlashArrow(int idx)
        {
            if (_arrows == null || idx < 0 || idx >= _arrows.Length) yield break;

            Image arrow = _arrows[idx];
            if (arrow == null) yield break;

            float elapsed = 0f;
            while (elapsed < FlashTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / FlashTime;
                // Pulso: opaco → transparente
                Color c = arrow.color;
                c.a       = Mathf.SmoothStep(1f, 0f, t);
                arrow.color = c;
                yield return null;
            }

            Color final = arrow.color;
            final.a = 0f;
            arrow.color = final;
        }

        // ---------------------------------------------------------------

        private void BuildUI()
        {
            _root = new GameObject("DamageDirectionCanvas");

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;

            _root.AddComponent<CanvasScaler>().referenceResolution =
                new Vector2(1920, 1080);

            // Contenedor centrado
            GameObject center = new GameObject("Center");
            center.transform.SetParent(_root.transform, false);
            RectTransform cr = center.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(0.5f, 0.5f);
            cr.anchorMax        = new Vector2(0.5f, 0.5f);
            cr.sizeDelta        = Vector2.zero;
            cr.anchoredPosition = Vector2.zero;

            // 4 flechas: 0=arriba(frente), 1=derecha, 2=abajo(atrás), 3=izquierda
            Vector2[] offsets = {
                new Vector2(0f,  ArrowRadius),    // frente
                new Vector2( ArrowRadius, 0f),    // derecha
                new Vector2(0f, -ArrowRadius),    // atrás
                new Vector2(-ArrowRadius, 0f),    // izquierda
            };
            float[] rotations = { 0f, -90f, 180f, 90f };

            _arrows = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject($"Arrow_{i}");
                go.transform.SetParent(center.transform, false);

                Image img = go.AddComponent<Image>();
                img.color = new Color(1f, 0.15f, 0.1f, 0f); // rojo, invisible por defecto

                // Usar un cuadrado rotado como flecha provisional
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta       = new Vector2(ArrowSize * 0.4f, ArrowSize);
                rt.anchoredPosition = offsets[i];
                rt.localRotation    = Quaternion.Euler(0f, 0f, rotations[i]);

                _arrows[i] = img;
            }
        }
    }
}
