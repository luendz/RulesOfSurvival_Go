using System.Collections;
using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class DamageNumberSpawner : MonoBehaviour
    {
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

        private readonly HashSet<WeaponController> _subscribedWeapons =
            new HashSet<WeaponController>();

        [Header("References")]
        [SerializeField] private WeaponEquipmentController _equipment;
        [SerializeField] private GameObject _damageNumberPrefab;
        [SerializeField] private Camera _worldCamera;

        private void Awake()
        {
            if (_equipment == null || _damageNumberPrefab == null || _worldCamera == null)
            {
                Debug.LogError($"[{nameof(DamageNumberSpawner)}] Referencias incompletas en '{name}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            BindEquipment();
            RefreshWeaponSubscriptions();
        }

        private void BindEquipment()
        {
            if (_equipment == null)
                _equipment = GetComponent<WeaponEquipmentController>();

            if (_equipment == null)
                return;

            // Evita suscripciones duplicadas si OnEnable/Start se ejecutan en la
            // misma carga o el componente se vuelve a habilitar.
            _equipment.SlotChanged -= HandleSlotChanged;
            _equipment.SlotChanged += HandleSlotChanged;
        }

        private void HandleSlotChanged(int slot, WeaponController weapon)
        {
            // El evento se dispara justo cuando un arma recogida entra en uno
            // de los slots. La registramos inmediatamente para HitConfirmed.
            if (weapon != null)
                SubscribeWeapon(weapon);

            // También limpia suscripciones de armas que hayan sido sustituidas
            // o retiradas del equipamiento.
            RefreshWeaponSubscriptions();
        }

        private void RefreshWeaponSubscriptions()
        {
            HashSet<WeaponController> current = new HashSet<WeaponController>();

            if (_equipment != null)
            {
                for (int slot = 1; slot <= 3; slot++)
                {
                    WeaponController weapon = _equipment.GetWeaponForSlot(slot);
                    if (weapon == null)
                        continue;

                    current.Add(weapon);
                    SubscribeWeapon(weapon);
                }
            }
            else
            {
                // Compatibilidad con escenas antiguas sin WeaponEquipmentController.
                WeaponController[] weapons =
                    GetComponentsInChildren<WeaponController>(true);

                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponController weapon = weapons[i];
                    if (weapon == null)
                        continue;

                    current.Add(weapon);
                    SubscribeWeapon(weapon);
                }
            }

            if (_subscribedWeapons.Count == 0)
                return;

            List<WeaponController> stale = new List<WeaponController>();
            foreach (WeaponController weapon in _subscribedWeapons)
            {
                if (weapon == null || !current.Contains(weapon))
                    stale.Add(weapon);
            }

            for (int i = 0; i < stale.Count; i++)
                UnsubscribeWeapon(stale[i]);
        }

        private void SubscribeWeapon(WeaponController weapon)
        {
            if (weapon == null || _subscribedWeapons.Contains(weapon))
                return;

            weapon.HitConfirmed += OnHit;
            _subscribedWeapons.Add(weapon);
        }

        private void UnsubscribeWeapon(WeaponController weapon)
        {
            if (weapon != null)
                weapon.HitConfirmed -= OnHit;

            _subscribedWeapons.Remove(weapon);
        }

        private void UnbindAll()
        {
            if (_equipment != null)
                _equipment.SlotChanged -= HandleSlotChanged;

            if (_subscribedWeapons.Count == 0)
                return;

            List<WeaponController> snapshot =
                new List<WeaponController>(_subscribedWeapons);

            for (int i = 0; i < snapshot.Count; i++)
                UnsubscribeWeapon(snapshot[i]);

            _subscribedWeapons.Clear();
        }

        private void OnDisable()
        {
            UnbindAll();
        }

        private void OnDestroy()
        {
            UnbindAll();
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
                    outlineColor,
                    _worldCamera
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
            Color outlineColor,
            Camera worldCamera
        )
        {
            Vector3 drift = new Vector3(
                Random.Range(-0.22f, 0.22f),
                FloatHeight,
                Random.Range(-0.14f, 0.14f)
            );

            float elapsed = 0f;

            while (elapsed < Duration && instance != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;
                instance.transform.position = origin + drift * t;

                instance.transform.rotation = worldCamera.transform.rotation;

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
