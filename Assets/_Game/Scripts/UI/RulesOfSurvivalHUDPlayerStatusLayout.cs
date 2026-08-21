using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Ajuste visual final del HUD reconstruido tomando como referencia
    /// la interfaz original de Rules of Survival. No modifica lógica jugable.
    /// </summary>
    [DefaultExecutionOrder(1700)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDPlayerStatusLayout : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float SourceHealthWidth = 276f;
        private const float TargetHealthWidth = 390f;

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
            RemoveCompassDarkBar(hud.transform);
            ApplyMinimap(hud.transform);

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
            stats.anchoredPosition = new Vector2(-24f, -10f);
            stats.sizeDelta = new Vector2(214f, 44f);

            Image statsBackground = stats.GetComponent<Image>();
            if (statsBackground != null)
            {
                statsBackground.color =
                    new Color(0.025f, 0.035f, 0.045f, 0.93f);
            }

            SetTopRightChild(
                hud.Find("Canvas/TopRightStats/KillText") as RectTransform,
                new Vector2(74f, 44f),
                new Vector2(-177f, -22f)
            );

            SetTopRightChild(
                hud.Find("Canvas/TopRightStats/LeftText") as RectTransform,
                new Vector2(74f, 44f),
                new Vector2(-103f, -22f)
            );

            RectTransform distancePanel =
                hud.Find("Canvas/TopRightStats/DistancePanel") as RectTransform;

            SetTopRightChild(
                distancePanel,
                new Vector2(66f, 44f),
                new Vector2(-33f, -22f)
            );

            if (distancePanel != null)
            {
                Image distanceBackground = distancePanel.GetComponent<Image>();
                if (distanceBackground != null)
                {
                    distanceBackground.color =
                        new Color(0.95f, 0.95f, 0.95f, 0.98f);
                }
            }

            Text kill = hud.Find("Canvas/TopRightStats/KillText")
                ?.GetComponent<Text>();
            Text left = hud.Find("Canvas/TopRightStats/LeftText")
                ?.GetComponent<Text>();
            Text distance = hud.Find(
                "Canvas/TopRightStats/DistancePanel/DistanceText"
            )?.GetComponent<Text>();

            ConfigureTopStat(kill, 18, Color.white);
            ConfigureTopStat(left, 18, Color.white);

            if (distance != null)
            {
                distance.fontSize = 14;
                distance.fontStyle = FontStyle.Bold;
                distance.alignment = TextAnchor.MiddleCenter;
                distance.color = Color.black;
                distance.lineSpacing = 0.78f;
            }
        }

        private static void ConfigureTopStat(
            Text text,
            int fontSize,
            Color color
        )
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
        }

        private static void RemoveCompassDarkBar(Transform hud)
        {
            RectTransform strip =
                hud.Find("Canvas/CompassStrip") as RectTransform;

            if (strip != null)
            {
                strip.sizeDelta = new Vector2(650f, 52f);

                Image stripImage = strip.GetComponent<Image>();
                if (stripImage != null)
                {
                    stripImage.color = Color.clear;
                }
            }

            Transform fidelity =
                hud.Find("Canvas/CompassStrip/CompassFidelity");

            if (fidelity != null)
            {
                RectTransform fidelityRect = fidelity as RectTransform;
                if (fidelityRect != null)
                {
                    fidelityRect.sizeDelta = new Vector2(650f, 52f);
                }

                // La referencia no tiene una banda oscura continua detrás de
                // grados/cardinales; solamente LEFT REAR y RIGHT REAR usan caja.
                Transform track = fidelity.Find("CompassTrackBackground");
                if (track != null && track.gameObject.activeSelf)
                {
                    track.gameObject.SetActive(false);
                }
            }
        }

        private static void ApplyMinimap(Transform hud)
        {
            RectTransform frame =
                hud.Find("Canvas/MinimapFrame") as RectTransform;

            if (frame == null)
            {
                return;
            }

            frame.sizeDelta = new Vector2(205f, 205f);
            frame.anchoredPosition = new Vector2(110.5f, 112.5f);

            // Quitar el cuadrado negro exterior. En ROS domina el mapa circular.
            Image frameBackground = frame.GetComponent<Image>();
            if (frameBackground != null)
            {
                frameBackground.color = Color.clear;
            }

            RectTransform circleMask =
                frame.Find("CircleMask") as RectTransform;
            if (circleMask != null)
            {
                circleMask.sizeDelta = new Vector2(196f, 196f);
                circleMask.anchoredPosition = Vector2.zero;
            }

            RectTransform arrow =
                frame.Find("PlayerArrow") as RectTransform;
            if (arrow != null)
            {
                arrow.sizeDelta = new Vector2(23f, 23f);
            }

            RectTransform badge =
                frame.Find("MapBadge") as RectTransform;
            if (badge != null)
            {
                badge.sizeDelta = new Vector2(26f, 26f);
                badge.anchoredPosition = new Vector2(12f, -58f);
            }

            RectTransform latency =
                hud.Find("Canvas/Latency") as RectTransform;
            if (latency != null)
            {
                latency.anchorMin = Vector2.zero;
                latency.anchorMax = Vector2.zero;
                latency.pivot = new Vector2(0f, 0f);
                latency.sizeDelta = new Vector2(70f, 22f);
                latency.anchoredPosition = new Vector2(153f, 16f);
            }
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
                vitals.anchoredPosition = new Vector2(0f, 18f);
                vitals.sizeDelta = new Vector2(456f, 48f);

                Image background = vitals.GetComponent<Image>();
                if (background != null)
                {
                    background.color =
                        new Color(0f, 0f, 0f, 0.08f);
                }
            }

            SetCentered(
                status.Find("PlayerVitals/PlayerName") as RectTransform,
                new Vector2(190f, 18f),
                new Vector2(-73f, 14f)
            );

            RectTransform healthBack =
                status.Find("PlayerVitals/HealthBack") as RectTransform;
            SetCentered(
                healthBack,
                new Vector2(TargetHealthWidth, 12f),
                new Vector2(0f, -4f)
            );

            RectTransform armorBack =
                status.Find("PlayerVitals/ArmorBack") as RectTransform;
            SetCentered(
                armorBack,
                new Vector2(TargetHealthWidth, 4f),
                new Vector2(0f, -13f)
            );

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
                new Vector2(44f, 18f),
                new Vector2(215f, -4f)
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
                weapons.anchoredPosition = new Vector2(-18f, 16f);
                weapons.sizeDelta = new Vector2(330f, 122f);
            }

            ApplyPrimarySlot(status, 1, new Vector2(0f, 61f));
            ApplySmallSlot(status, 4, new Vector2(248f, 61f));
            ApplyPrimarySlot(status, 2, new Vector2(0f, 0f));
            ApplySmallSlot(status, 3, new Vector2(248f, 0f));
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
            root.sizeDelta = new Vector2(244f, 58f);

            RectTransform selection = root.Find("Selection") as RectTransform;
            if (selection != null)
            {
                selection.sizeDelta = new Vector2(4f, 0f);
            }

            SetCentered(
                root.Find("Number") as RectTransform,
                new Vector2(18f, 18f),
                new Vector2(-112f, 19f)
            );

            SetCentered(
                root.Find("Icon") as RectTransform,
                new Vector2(150f, 44f),
                new Vector2(-22f, 1f)
            );

            SetCentered(
                root.Find("Name") as RectTransform,
                new Vector2(88f, 18f),
                new Vector2(75f, 16f)
            );

            SetCentered(
                root.Find("Ammo") as RectTransform,
                new Vector2(88f, 20f),
                new Vector2(78f, -18f)
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
            root.sizeDelta = new Vector2(80f, 58f);

            RectTransform selection = root.Find("Selection") as RectTransform;
            if (selection != null)
            {
                selection.sizeDelta = new Vector2(4f, 0f);
            }

            SetCentered(
                root.Find("Number") as RectTransform,
                new Vector2(16f, 16f),
                new Vector2(-30f, 19f)
            );

            SetCentered(
                root.Find("Icon") as RectTransform,
                new Vector2(54f, 38f),
                new Vector2(0f, 2f)
            );

            SetCentered(
                root.Find("Name") as RectTransform,
                new Vector2(70f, 16f),
                new Vector2(0f, 16f)
            );

            SetCentered(
                root.Find("Ammo") as RectTransform,
                new Vector2(58f, 18f),
                new Vector2(8f, -18f)
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
                new Vector2(-275f, 47f),
                "C",
                "CONSUM."
            );

            EnsureCirclePlaceholder(
                status,
                "GrenadePlaceholder",
                new Vector2(275f, 47f),
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
                    new Color(0.025f, 0.03f, 0.035f, 0.82f);
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
                symbolText.fontSize = 23;
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
            root.sizeDelta = new Vector2(70f, 70f);
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
