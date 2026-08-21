using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Ajuste final de anclajes y proporciones del HUD de referencia.
    /// Mantiene la funcionalidad existente y corrige únicamente presentación.
    /// </summary>
    [DefaultExecutionOrder(1700)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDPlayerStatusLayout : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float SourceHealthWidth = 276f;
        private const float TargetHealthWidth = 360f;

        private static Sprite _circleSprite;
        private float _nextApplyTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDPlayerStatusLayout>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_PlayerStatus_Layout")
                .AddComponent<RulesOfSurvivalHUDPlayerStatusLayout>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextApplyTime)
            {
                return;
            }

            _nextApplyTime = Time.unscaledTime + 0.20f;
            ApplyLayout();
        }

        private static void ApplyLayout()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            ApplyTopRight(hud.transform);
            ApplyCompassTrack(hud.transform);

            Transform status = hud.transform.Find("Canvas/PlayerStatusFidelity");
            if (status == null)
            {
                return;
            }

            ApplyVitals(status);
            ApplyWeaponSlots(status);
            EnsureSidePlaceholders(status);
        }

        private static void ApplyTopRight(Transform hud)
        {
            RectTransform stats =
                hud.Find("Canvas/TopRightStats") as RectTransform;

            if (stats == null)
            {
                return;
            }

            stats.anchorMin = Vector2.one;
            stats.anchorMax = Vector2.one;
            stats.pivot = Vector2.one;
            stats.anchoredPosition = new Vector2(-18f, -8f);
            stats.sizeDelta = new Vector2(220f, 44f);

            SetTopRightChild(
                hud.Find("Canvas/TopRightStats/KillText") as RectTransform,
                new Vector2(78f, 44f),
                new Vector2(-181f, -22f)
            );

            SetTopRightChild(
                hud.Find("Canvas/TopRightStats/LeftText") as RectTransform,
                new Vector2(76f, 44f),
                new Vector2(-104f, -22f)
            );

            RectTransform distancePanel =
                hud.Find("Canvas/TopRightStats/DistancePanel") as RectTransform;

            SetTopRightChild(
                distancePanel,
                new Vector2(66f, 44f),
                new Vector2(-33f, -22f)
            );

            Text kill = hud.Find("Canvas/TopRightStats/KillText")
                ?.GetComponent<Text>();
            Text left = hud.Find("Canvas/TopRightStats/LeftText")
                ?.GetComponent<Text>();
            Text distance = hud.Find(
                "Canvas/TopRightStats/DistancePanel/DistanceText"
            )?.GetComponent<Text>();

            if (kill != null)
            {
                kill.fontSize = 18;
                kill.fontStyle = FontStyle.Bold;
                kill.alignment = TextAnchor.MiddleCenter;
            }

            if (left != null)
            {
                left.fontSize = 18;
                left.fontStyle = FontStyle.Bold;
                left.alignment = TextAnchor.MiddleCenter;
            }

            if (distance != null)
            {
                distance.fontSize = 13;
                distance.fontStyle = FontStyle.Bold;
                distance.alignment = TextAnchor.MiddleCenter;
                distance.lineSpacing = 0.85f;
            }
        }

        private static void ApplyCompassTrack(Transform hud)
        {
            Transform fidelity =
                hud.Find("Canvas/CompassStrip/CompassFidelity");

            if (fidelity == null)
            {
                return;
            }

            Transform existing = fidelity.Find("CompassTrackBackground");
            RectTransform track;
            Image image;

            if (existing == null)
            {
                GameObject trackObject =
                    new GameObject("CompassTrackBackground");
                trackObject.transform.SetParent(fidelity, false);

                track = trackObject.AddComponent<RectTransform>();
                image = trackObject.AddComponent<Image>();
                image.raycastTarget = false;
                trackObject.transform.SetAsFirstSibling();
            }
            else
            {
                track = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            if (track == null || image == null)
            {
                return;
            }

            // Los tags laterales tienen su borde interior aproximadamente en
            // +/-217 px. La banda queda a pocos píxeles de ellos como en ROS.
            track.anchorMin = new Vector2(0.5f, 0.5f);
            track.anchorMax = new Vector2(0.5f, 0.5f);
            track.pivot = new Vector2(0.5f, 0.5f);
            track.anchoredPosition = new Vector2(0f, -5f);
            track.sizeDelta = new Vector2(428f, 24f);
            image.color = new Color(0.11f, 0.13f, 0.15f, 0.38f);
        }

        private static void ApplyVitals(Transform status)
        {
            RectTransform vitals =
                status.Find("PlayerVitals") as RectTransform;

            if (vitals != null)
            {
                vitals.anchorMin = new Vector2(0.5f, 0f);
                vitals.anchorMax = new Vector2(0.5f, 0f);
                vitals.pivot = new Vector2(0.5f, 0f);
                vitals.anchoredPosition = new Vector2(0f, 20f);
                vitals.sizeDelta = new Vector2(430f, 52f);

                Image background = vitals.GetComponent<Image>();
                if (background != null)
                {
                    background.color =
                        new Color(0f, 0f, 0f, 0.14f);
                }
            }

            SetCentered(
                status.Find("PlayerVitals/PlayerName") as RectTransform,
                new Vector2(175f, 18f),
                new Vector2(-68f, 15f)
            );

            RectTransform healthBack =
                status.Find("PlayerVitals/HealthBack") as RectTransform;
            SetCentered(
                healthBack,
                new Vector2(TargetHealthWidth, 13f),
                new Vector2(0f, -3f)
            );

            RectTransform armorBack =
                status.Find("PlayerVitals/ArmorBack") as RectTransform;
            SetCentered(
                armorBack,
                new Vector2(TargetHealthWidth, 4f),
                new Vector2(0f, -13f)
            );

            // El presenter funcional sigue calculando sobre 276 px. Mantener una
            // escala X fija convierte ese valor a los 360 px visuales deseados.
            RectTransform healthFill =
                status.Find("PlayerVitals/HealthBack/HealthFill")
                    as RectTransform;
            if (healthFill != null)
            {
                healthFill.localScale = new Vector3(
                    TargetHealthWidth / SourceHealthWidth,
                    1f,
                    1f
                );
            }

            RectTransform armorFill =
                status.Find("PlayerVitals/ArmorBack/ArmorFill")
                    as RectTransform;
            if (armorFill != null)
            {
                armorFill.localScale = new Vector3(
                    TargetHealthWidth / SourceHealthWidth,
                    1f,
                    1f
                );
            }

            SetCentered(
                status.Find("PlayerVitals/HealthValue") as RectTransform,
                new Vector2(45f, 18f),
                new Vector2(199f, -3f)
            );

            Transform oldHealthIcon =
                status.Find("PlayerVitals/HealthIcon");
            if (oldHealthIcon != null)
            {
                oldHealthIcon.gameObject.SetActive(false);
            }
        }

        private static void ApplyWeaponSlots(Transform status)
        {
            RectTransform weapons =
                status.Find("WeaponSlots") as RectTransform;

            if (weapons != null)
            {
                weapons.anchorMin = new Vector2(1f, 0f);
                weapons.anchorMax = new Vector2(1f, 0f);
                weapons.pivot = new Vector2(1f, 0f);
                weapons.anchoredPosition = new Vector2(-18f, 18f);
                weapons.sizeDelta = new Vector2(302f, 118f);
            }

            ApplyPrimarySlot(status, 1, new Vector2(0f, 59f));
            ApplySmallSlot(status, 4, new Vector2(224f, 59f));
            ApplyPrimarySlot(status, 2, new Vector2(0f, 0f));
            ApplySmallSlot(status, 3, new Vector2(224f, 0f));
        }

        private static void ApplyPrimarySlot(
            Transform status,
            int slot,
            Vector2 position
        )
        {
            RectTransform root = status.Find($"WeaponSlots/Slot_{slot}")
                as RectTransform;

            if (root == null)
            {
                return;
            }

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(220f, 56f);

            SetCentered(
                root.Find("Number") as RectTransform,
                new Vector2(18f, 18f),
                new Vector2(-100f, 18f)
            );

            SetCentered(
                root.Find("Icon") as RectTransform,
                new Vector2(132f, 40f),
                new Vector2(-16f, 2f)
            );

            SetCentered(
                root.Find("Name") as RectTransform,
                new Vector2(82f, 18f),
                new Vector2(65f, 15f)
            );

            SetCentered(
                root.Find("Ammo") as RectTransform,
                new Vector2(82f, 20f),
                new Vector2(68f, -17f)
            );

            Text name = root.Find("Name")?.GetComponent<Text>();
            Text ammo = root.Find("Ammo")?.GetComponent<Text>();
            Image icon = root.Find("Icon")?.GetComponent<Image>();

            if (name != null)
            {
                name.fontSize = 9;
                name.alignment = TextAnchor.MiddleRight;
            }

            if (ammo != null)
            {
                ammo.fontSize = 14;
                ammo.alignment = TextAnchor.LowerRight;
            }

            if (icon != null)
            {
                icon.preserveAspect = true;
            }
        }

        private static void ApplySmallSlot(
            Transform status,
            int slot,
            Vector2 position
        )
        {
            RectTransform root = status.Find($"WeaponSlots/Slot_{slot}")
                as RectTransform;

            if (root == null)
            {
                return;
            }

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(78f, 56f);

            SetCentered(
                root.Find("Number") as RectTransform,
                new Vector2(16f, 16f),
                new Vector2(-29f, 18f)
            );

            SetCentered(
                root.Find("Icon") as RectTransform,
                new Vector2(52f, 36f),
                new Vector2(0f, 3f)
            );

            SetCentered(
                root.Find("Name") as RectTransform,
                new Vector2(68f, 16f),
                new Vector2(0f, 15f)
            );

            SetCentered(
                root.Find("Ammo") as RectTransform,
                new Vector2(55f, 18f),
                new Vector2(8f, -17f)
            );

            Text name = root.Find("Name")?.GetComponent<Text>();
            Text ammo = root.Find("Ammo")?.GetComponent<Text>();

            if (name != null)
            {
                name.fontSize = 8;
            }

            if (ammo != null)
            {
                ammo.fontSize = 11;
            }
        }

        private static void EnsureSidePlaceholders(Transform status)
        {
            EnsureCirclePlaceholder(
                status,
                "ConsumablePlaceholder",
                new Vector2(-255f, 48f),
                "C",
                "CONSUM."
            );

            EnsureCirclePlaceholder(
                status,
                "GrenadePlaceholder",
                new Vector2(255f, 48f),
                "G",
                "GRAN."
            );
        }

        private static void EnsureCirclePlaceholder(
            Transform status,
            string name,
            Vector2 position,
            string symbol,
            string caption
        )
        {
            Transform existing = status.Find(name);
            RectTransform root;

            if (existing == null)
            {
                GameObject rootObject = new GameObject(name);
                rootObject.transform.SetParent(status, false);
                root = rootObject.AddComponent<RectTransform>();

                Image background = rootObject.AddComponent<Image>();
                background.sprite = GetCircleSprite();
                background.color =
                    new Color(0.025f, 0.03f, 0.035f, 0.78f);
                background.raycastTarget = false;

                Font font =
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                GameObject symbolObject = new GameObject("Symbol");
                symbolObject.transform.SetParent(root, false);
                RectTransform symbolRect =
                    symbolObject.AddComponent<RectTransform>();
                symbolRect.anchorMin = Vector2.zero;
                symbolRect.anchorMax = Vector2.one;
                symbolRect.offsetMin = new Vector2(4f, 4f);
                symbolRect.offsetMax = new Vector2(-4f, -11f);

                Text symbolText = symbolObject.AddComponent<Text>();
                symbolText.font = font;
                symbolText.text = symbol;
                symbolText.fontSize = 22;
                symbolText.fontStyle = FontStyle.Bold;
                symbolText.alignment = TextAnchor.MiddleCenter;
                symbolText.color = Color.white;
                symbolText.raycastTarget = false;

                GameObject captionObject = new GameObject("Caption");
                captionObject.transform.SetParent(root, false);
                RectTransform captionRect =
                    captionObject.AddComponent<RectTransform>();
                captionRect.anchorMin = new Vector2(0f, 0f);
                captionRect.anchorMax = new Vector2(1f, 0f);
                captionRect.pivot = new Vector2(0.5f, 0f);
                captionRect.anchoredPosition = new Vector2(0f, 5f);
                captionRect.sizeDelta = new Vector2(0f, 13f);

                Text captionText = captionObject.AddComponent<Text>();
                captionText.font = font;
                captionText.text = caption;
                captionText.fontSize = 8;
                captionText.fontStyle = FontStyle.Bold;
                captionText.alignment = TextAnchor.MiddleCenter;
                captionText.color =
                    new Color(1f, 1f, 1f, 0.72f);
                captionText.raycastTarget = false;
            }
            else
            {
                root = existing as RectTransform;
            }

            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(64f, 64f);
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );
            texture.name = "ROS_HUD_Circle_Runtime";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(
                (size - 1) * 0.5f,
                (size - 1) * 0.5f
            );
            float radius = size * 0.49f;
            float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        center
                    );
                    float alpha = Mathf.Clamp01(
                        (radius - distance) / feather
                    );
                    pixels[y * size + x] =
                        new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            _circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            _circleSprite.name = "ROS_HUD_Circle_Runtime";
            return _circleSprite;
        }

        private static void SetTopRightChild(
            RectTransform rect,
            Vector2 size,
            Vector2 position
        )
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 size,
            Vector2 position
        )
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
