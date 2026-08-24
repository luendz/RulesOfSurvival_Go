using System.Collections;
using ROS.Game.Combat;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class DamageNumberSpawner : MonoBehaviour
    {
        private const string ResourcePath = "EditorFirst/DamageNumber";
        private const float FloatHeight = 1.3f;
        private const float Duration = 1.1f;

        private WeaponController[] _weapons;
        private GameObject _damageNumberPrefab;

        private void Start()
        {
            _damageNumberPrefab = Resources.Load<GameObject>(ResourcePath);
            if (_damageNumberPrefab == null)
            {
                Debug.LogError(
                    "No existe el prefab editable EditorFirst/DamageNumber. " +
                    "Abre el proyecto en Unity para materializar los assets editor-first.",
                    this
                );
            }

            _weapons = GetComponentsInChildren<WeaponController>(true);
            foreach (WeaponController weapon in _weapons)
            {
                if (weapon != null)
                    weapon.HitConfirmed += OnHit;
            }
        }

        private void OnDestroy()
        {
            if (_weapons == null)
                return;

            foreach (WeaponController weapon in _weapons)
            {
                if (weapon != null)
                    weapon.HitConfirmed -= OnHit;
            }
        }

        private void OnHit(DamageResult result)
        {
            if (result.HealthDamage <= 0f || _damageNumberPrefab == null)
                return;

            int shown = Mathf.Max(1, Mathf.RoundToInt(result.HealthDamage));
            Vector3 offset = new Vector3(
                Random.Range(-0.45f, 0.45f),
                Random.Range(0.20f, 0.55f),
                Random.Range(-0.45f, 0.45f)
            );

            Color color;
            if (result.IsHeadshot)
                color = new Color(1.00f, 0.18f, 0.08f);
            else if (result.HealthDamage >= 30f)
                color = new Color(1.00f, 0.55f, 0.00f);
            else
                color = new Color(1.00f, 0.92f, 0.10f);

            GameObject instance = Instantiate(
                _damageNumberPrefab,
                result.Damage.Point + offset,
                Quaternion.identity
            );

            TextMesh text = instance.GetComponentInChildren<TextMesh>(true);
            if (text == null)
            {
                Debug.LogError("El prefab DamageNumber no contiene TextMesh.", instance);
                Destroy(instance);
                return;
            }

            text.text = shown.ToString();
            text.fontSize = result.WasFatal ? 46 : 36;
            text.characterSize = result.WasFatal ? 0.10f : 0.08f;
            text.color = color;
            text.fontStyle = result.WasFatal ? FontStyle.Bold : FontStyle.Normal;

            StartCoroutine(Popup(instance, text, instance.transform.position, color));
        }

        private static IEnumerator Popup(
            GameObject instance,
            TextMesh text,
            Vector3 origin,
            Color color
        )
        {
            Vector3 drift = new Vector3(
                Random.Range(-0.30f, 0.30f),
                FloatHeight,
                Random.Range(-0.20f, 0.20f)
            );

            Camera cam = Camera.main;
            float elapsed = 0f;

            while (elapsed < Duration && instance != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;
                instance.transform.position = origin + drift * t;

                if (cam != null)
                    instance.transform.rotation = cam.transform.rotation;

                float alpha = t < 0.35f
                    ? 1f
                    : 1f - (t - 0.35f) / 0.65f;

                Color current = color;
                current.a = alpha;
                if (text != null)
                    text.color = current;

                yield return null;
            }

            if (instance != null)
                Destroy(instance);
        }
    }
}
