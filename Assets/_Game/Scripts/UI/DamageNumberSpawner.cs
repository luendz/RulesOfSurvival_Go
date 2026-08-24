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
        private const int NormalFontSize = 36;
        private const int CriticalFontSize = NormalFontSize + 4;
        private const float CharacterSize = 0.08f;

        private static readonly Color NormalTextColor = Color.white;
        private static readonly Color NormalOutlineColor =
            new Color(0.08f, 0.48f, 1f, 1f);
        private static readonly Color CriticalTextColor =
            new Color(1f, 0.06f, 0.04f, 1f);
        private static readonly Color CriticalOutlineColor = Color.black;

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
            bool critical = result.IsHeadshot;

            // Variación alrededor del punto REAL del impacto. Se mantiene
            // pequeña para que el número siga claramente asociado al personaje.
            Vector3 offset = new Vector3(
                Random.Range(-0.32f, 0.32f),
                Random.Range(0.12f, 0.42f),
                Random.Range(-0.24f, 0.24f)
            );

            GameObject instance = Instantiate(
                _damageNumberPrefab,
                result.Damage.Point + offset,
                Quaternion.identity
            );

            TextMesh[] texts = instance.GetComponentsInChildren<TextMesh>(true);
            if (texts == null || texts.Length == 0)
            {
                Debug.LogError("El prefab DamageNumber no contiene TextMesh.", instance);
                Destroy(instance);
                return;
            }

            TextMesh main = ResolveMainText(instance, texts);
            if (main == null)
            {
                Debug.LogError("El prefab DamageNumber no contiene texto principal.", instance);
                Destroy(instance);
                return;
            }

            int fontSize = critical ? CriticalFontSize : NormalFontSize;
            Color mainColor = critical ? CriticalTextColor : NormalTextColor;
            Color outlineColor = critical
                ? CriticalOutlineColor
                : NormalOutlineColor;

            for (int i = 0; i < texts.Length; i++)
            {
                TextMesh text = texts[i];
                if (text == null)
                    continue;

                bool isMain = text == main;
                text.text = shown.ToString();
                text.fontSize = fontSize;
                text.characterSize = CharacterSize;
                text.fontStyle = FontStyle.Bold;
                text.color = isMain ? mainColor : outlineColor;
            }

            StartCoroutine(
                Popup(
                    instance,
                    texts,
                    main,
                    instance.transform.position,
                    mainColor,
                    outlineColor
                )
            );
        }

        private static TextMesh ResolveMainText(
            GameObject instance,
            TextMesh[] texts
        )
        {
            TextMesh rootText = instance.GetComponent<TextMesh>();
            if (rootText != null)
                return rootText;

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "MainText")
                    return texts[i];
            }

            return texts.Length > 0 ? texts[0] : null;
        }

        private static IEnumerator Popup(
            GameObject instance,
            TextMesh[] texts,
            TextMesh main,
            Vector3 origin,
            Color mainColor,
            Color outlineColor
        )
        {
            Vector3 drift = new Vector3(
                Random.Range(-0.22f, 0.22f),
                FloatHeight,
                Random.Range(-0.14f, 0.14f)
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

                for (int i = 0; i < texts.Length; i++)
                {
                    TextMesh text = texts[i];
                    if (text == null)
                        continue;

                    Color current = text == main
                        ? mainColor
                        : outlineColor;
                    current.a = alpha;
                    text.color = current;
                }

                yield return null;
            }

            if (instance != null)
                Destroy(instance);
        }
    }
}
