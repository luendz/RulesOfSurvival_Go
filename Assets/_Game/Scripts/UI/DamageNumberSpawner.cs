using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class DamageNumberSpawner : MonoBehaviour
    {
        private const float FloatHeight = 1.3f;
        private const float Duration    = 1.1f;

        private WeaponController[] _weapons;

        private void Start()
        {
            _weapons = GetComponentsInChildren<WeaponController>(true);
            foreach (WeaponController w in _weapons)
                if (w != null) w.HitConfirmed += OnHit;
        }

        private void OnDestroy()
        {
            if (_weapons == null) return;
            foreach (WeaponController w in _weapons)
                if (w != null) w.HitConfirmed -= OnHit;
        }

        private void OnHit(DamageResult result)
        {
            if (result.HealthDamage <= 0f) return;

            int shown = Mathf.Max(1, Mathf.RoundToInt(result.HealthDamage));

            Vector3 offset = new Vector3(
                Random.Range(-0.45f, 0.45f),
                Random.Range(0.20f, 0.55f),
                Random.Range(-0.45f, 0.45f)
            );

            Color color;
            if (result.IsHeadshot)
                color = new Color(1.00f, 0.18f, 0.08f); // rojo – headshot
            else if (result.HealthDamage >= 30f)
                color = new Color(1.00f, 0.55f, 0.00f); // naranja – daño alto
            else
                color = new Color(1.00f, 0.92f, 0.10f); // amarillo – normal

            StartCoroutine(
                Popup(result.Damage.Point + offset, shown, color, result.WasFatal)
            );
        }

        private static IEnumerator Popup(
            Vector3 origin,
            int amount,
            Color color,
            bool fatal)
        {
            GameObject go = new GameObject("DmgNum");
            go.transform.position = origin;

            TextMesh tm       = go.AddComponent<TextMesh>();
            tm.text           = amount.ToString();
            tm.fontSize       = fatal ? 46 : 36;
            tm.characterSize  = fatal ? 0.10f : 0.08f;
            tm.color          = color;
            tm.anchor         = TextAnchor.MiddleCenter;
            tm.alignment      = TextAlignment.Center;
            tm.fontStyle      = fatal ? FontStyle.Bold : FontStyle.Normal;

            // Dirección de flotación: principalmente arriba + deriva aleatoria
            Vector3 drift = new Vector3(
                Random.Range(-0.30f, 0.30f),
                FloatHeight,
                Random.Range(-0.20f, 0.20f)
            );

            Camera cam    = Camera.main;
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;

                go.transform.position = origin + drift * t;

                if (cam != null)
                    go.transform.rotation = cam.transform.rotation;

                // Opaco los primeros 35 %, luego desvanece
                float alpha = t < 0.35f
                    ? 1f
                    : 1f - (t - 0.35f) / 0.65f;

                Color c = color;
                c.a      = alpha;
                tm.color = c;

                yield return null;
            }

            Destroy(go);
        }
    }
}
