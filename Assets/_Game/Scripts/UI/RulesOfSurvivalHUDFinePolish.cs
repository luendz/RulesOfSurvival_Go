using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Microajustes visuales sobre el HUD funcional ya construido.
    /// No altera datos jugables: solo jerarquía visual, espaciado, bordes,
    /// tipografía y proporciones para acercarlo a la referencia de ROS.
    /// </summary>
    [DefaultExecutionOrder(1900)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDFinePolish : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";

        private static Sprite _circleSprite;
        private static Sprite _ringSprite;
        private float _nextApplyTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDFinePolish>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_FinePolish")
                .AddComponent<RulesOfSurvivalHUDFinePolish>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextApplyTime)
            {
                return;
            }

            _nextApplyTime = Time.unscaledTime + 0.20f;

            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform canvas = hud.transform.Find("Canvas");
            if (canvas == null)
            {
                return;
            }

            PolishCompass(canvas);
            PolishTopRight(canvas);
            PolishMinimap(canvas);
            PolishLootPanel(canvas);

            Transform status = canvas.Find("PlayerStatusFidelity");
            if (status != null)
            {
                PolishVitals(status);
                PolishWeaponSlots(status);
                PolishSidePlaceholders(status);
            }
        }

        private static void PolishCompass(Transform canvas)
        {
            RectTransform strip = canvas.Find("CompassStrip") as RectTransform;
            if (strip == null)
            {
                return;
            }

            strip.sizeDelta = new Vector2(660f, 50f);

            Image stripImage = strip.GetComponent<Image>();
            if (stripImage != null)
            {
                stripImage.color = Color.clear;
                stripImage.raycastTarget = false;
            }

            Transform fidelity = strip.Find("CompassFidelity");
            if (fidelity == null)
            {
                return;
            }

            RectTransform fidelityRect = fidelity as RectTransform;
            if (fidelityRect != null)
            {
                fidelityRect.sizeDelta = new Vector2(660f, 50f);
            }

            Transform track = fidelity.Find("CompassTrackBackground");
            if (track != null)
            {
                track.gameObject.SetActive(false);
            }

            PolishRearTag(fidelity.Find("LeftRearTag") as RectTransform, -286f);
            PolishRearTag(fidelity.Find("RightRearTag") as RectTransform, 286f);

            for (int i = 0; i < 15; i++)
            {
                Transform tick = fidelity.Find($"Tick_{i:00}");
                if (tick == null)
                {
                    continue;
                }

                Text label = tick.Find("Label")?.GetComponent<Text>();
                Image line = tick.Find("TickLine")?.GetComponent<Image>();
                if (label == null)
                {
                    continue;
                }

                bool cardinal = IsCardinal(label.text);
                bool diagonal = IsDiagonal(label.text);

                label.fontSize = cardinal ? 16 : diagonal ? 14 : 11;
                label.fontStyle = FontStyle.Bold;
                label.color = cardinal || diagonal
                    ? Color.white
                    : new Color(0.92f, 0.94f, 0.96f, 0.88f);

                EnsureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.86f));

                if (line != null)
                {
                    RectTransform lineRect = line.rectTransform;
                    lineRect.sizeDelta = new Vector2(
                        cardinal ? 2.5f : diagonal ? 2f : 1.5f,
                        cardinal ? 18f : diagonal ? 14f : 8f
                    );
                    line.color = cardinal || diagonal
                        ? new Color(1f, 1f, 1f, 0.96f)
                        : new Color(1f, 1f, 1f, 0.78f);
                }
            }

            Text centerMarker = fidelity.Find("CenterMarker")?.GetComponent<Text>();
            if (centerMarker != null)
            {
                centerMarker.fontSize = 16;
                centerMarker.fontStyle = FontStyle.Bold;
                centerMarker.color = new Color(1f, 1f, 1f, 0.98f);
                centerMarker.rectTransform.anchoredPosition = new Vector2(0f, 19f);
                EnsureOutline(centerMarker.gameObject, new Color(0f, 0f, 0f, 0.92f));
            }
        }

        private static void PolishRearTag(RectTransform tag, float x)
        {
            if (tag == null)
            {
                return;
            }

            tag.anchoredPosition = new Vector2(x, -5f);
            tag.sizeDelta = new Vector2(88f, 24f);

            Image background = tag.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.025f, 0.035f, 0.045f, 0.90f);
            }

            Text text = tag.Find("Text")?.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = 11;
                text.fontStyle = FontStyle.BoldAndItalic;
                text.color = Color.white;
                EnsureOutline(text.gameObject, new Color(0f, 0f, 0f, 0.85f));
            }
        }

        private static bool IsCardinal(string value)
        {
            return value == "N" || value == "E" || value == "S" || value == "W";
        }

        private static bool IsDiagonal(string value)
        {
            return value == "NE" || value == "SE" || value == "SW" || value == "NW";
        }

        private static void PolishTopRight(Transform canvas)
        {
            RectTransform stats = canvas.Find("TopRightStats") as RectTransform;
            if (stats == null)
            {
                return;
            }

            stats.anchoredPosition = new Vector2(-24f, -10f);
            stats.sizeDelta = new Vector2(214f, 42f);

            Image background = stats.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.025f, 0.035f, 0.045f, 0.95f);
            }

            Text kill = stats.Find("KillText")?.GetComponent<Text>();
            Text left = stats.Find("LeftText")?.GetComponent<Text>();
            Text distance = stats.Find("DistancePanel/DistanceText")?.GetComponent<Text>();

            PolishTopStat(kill, 18);
            PolishTopStat(left, 18);

            if (distance != null)
            {
                distance.fontSize = 14;
                distance.fontStyle = FontStyle.BoldAndItalic;
                distance.lineSpacing = 0.76f;
                distance.color = Color.black;
            }

            EnsureVerticalDivider(stats, "KillLeftDivider", -140f);
        }

        private static void PolishTopStat(Text text, int size)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = size;
            text.fontStyle = FontStyle.BoldAndItalic;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            EnsureOutline(text.gameObject, new Color(0f, 0f, 0f, 0.70f));
        }

        private static void EnsureVerticalDivider(RectTransform parent, string name, float x)
        {
            Transform existing = parent.Find(name);
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                rect = go.AddComponent<RectTransform>();
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            if (rect == null || image == null)
            {
                return;
            }

            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -5f);
            rect.sizeDelta = new Vector2(1f, 32f);
            image.color = new Color(1f, 1f, 1f, 0.16f);
        }

        private static void PolishMinimap(Transform canvas)
        {
            RectTransform frame = canvas.Find("MinimapFrame") as RectTransform;
            if (frame == null)
            {
                return;
            }

            Image frameBackground = frame.GetComponent<Image>();
            if (frameBackground != null)
            {
                frameBackground.color = Color.clear;
            }

            RectTransform mask = frame.Find("CircleMask") as RectTransform;
            if (mask != null)
            {
                mask.sizeDelta = new Vector2(196f, 196f);
            }

            EnsureMinimapRing(frame);

            Image arrow = frame.Find("PlayerArrow")?.GetComponent<Image>();
            if (arrow != null)
            {
                arrow.rectTransform.sizeDelta = new Vector2(22f, 22f);
                arrow.color = new Color(1f, 0.88f, 0.05f, 1f);
                arrow.transform.SetAsLastSibling();
            }

            Text badge = frame.Find("MapBadge")?.GetComponent<Text>();
            if (badge != null)
            {
                badge.fontSize = 14;
                badge.fontStyle = FontStyle.Bold;
                badge.alignment = TextAnchor.MiddleCenter;
                badge.rectTransform.sizeDelta = new Vector2(29f, 29f);

                Image badgeBackground = badge.GetComponent<Image>();
                if (badgeBackground != null)
                {
                    badgeBackground.sprite = GetCircleSprite();
                    badgeBackground.color = new Color(1f, 0.88f, 0.05f, 1f);
                }
            }

            Text latency = canvas.Find("Latency")?.GetComponent<Text>();
            if (latency != null)
            {
                latency.fontSize = 13;
                latency.fontStyle = FontStyle.Bold;
                latency.alignment = TextAnchor.MiddleRight;
                latency.color = new Color(1f, 1f, 1f, 0.92f);
            }
        }

        private static void EnsureMinimapRing(RectTransform frame)
        {
            Transform existing = frame.Find("FineBorder");
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                GameObject go = new GameObject("FineBorder");
                go.transform.SetParent(frame, false);
                rect = go.AddComponent<RectTransform>();
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            if (rect == null || image == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(201f, 201f);
            image.sprite = GetRingSprite();
            image.color = new Color(0.02f, 0.03f, 0.035f, 0.94f);
            image.transform.SetAsLastSibling();
        }

        private static void PolishVitals(Transform status)
        {
            RectTransform vitals = status.Find("PlayerVitals") as RectTransform;
            if (vitals == null)
            {
                return;
            }

            Image background = vitals.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0f, 0f, 0f, 0.055f);
            }

            RectTransform healthBack = vitals.Find("HealthBack") as RectTransform;
            RectTransform armorBack = vitals.Find("ArmorBack") as RectTransform;

            if (healthBack != null)
            {
                Image image = healthBack.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.035f, 0.04f, 0.045f, 0.92f);
                }
            }

            if (armorBack != null)
            {
                Image image = armorBack.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.025f, 0.035f, 0.045f, 0.88f);
                }
            }

            Text name = vitals.Find("PlayerName")?.GetComponent<Text>();
            if (name != null)
            {
                name.fontSize = 12;
                name.fontStyle = FontStyle.Bold;
                name.color = new Color(1f, 1f, 1f, 0.96f);
                EnsureOutline(name.gameObject, new Color(0f, 0f, 0f, 0.72f));
            }

            Text healthValue = vitals.Find("HealthValue")?.GetComponent<Text>();
            if (healthValue != null)
            {
                healthValue.fontSize = 11;
                healthValue.fontStyle = FontStyle.Bold;
                healthValue.color = new Color(1f, 1f, 1f, 0.88f);
            }

            EnsureSquadBadge(vitals);
        }

        private static void EnsureSquadBadge(RectTransform vitals)
        {
            Transform existing = vitals.Find("SquadBadge");
            RectTransform rect;
            Image image;
            Text label;

            if (existing == null)
            {
                GameObject go = new GameObject("SquadBadge");
                go.transform.SetParent(vitals, false);
                rect = go.AddComponent<RectTransform>();
                image = go.AddComponent<Image>();
                image.raycastTarget = false;

                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(rect, false);
                RectTransform textRect = textObject.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                label = textObject.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
                label = existing.Find("Text")?.GetComponent<Text>();
            }

            if (rect == null || image == null || label == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-180f, 14f);
            rect.sizeDelta = new Vector2(22f, 22f);

            image.sprite = GetCircleSprite();
            image.color = new Color(0.05f, 0.74f, 0.90f, 0.98f);

            label.text = "1";
            label.fontSize = 12;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static void PolishWeaponSlots(Transform status)
        {
            for (int slot = 1; slot <= 4; slot++)
            {
                RectTransform root = status.Find($"WeaponSlots/Slot_{slot}") as RectTransform;
                if (root == null)
                {
                    continue;
                }

                bool primary = slot == 1 || slot == 2;
                Image background = root.GetComponent<Image>();
                Image selection = root.Find("Selection")?.GetComponent<Image>();
                bool selected = selection != null && selection.color.a > 0.20f;

                if (background != null)
                {
                    background.color = selected
                        ? new Color(0.16f, 0.17f, 0.17f, 0.94f)
                        : new Color(0.08f, 0.095f, 0.105f, 0.88f);
                }

                if (selection != null)
                {
                    selection.rectTransform.sizeDelta = new Vector2(3f, 0f);
                    if (selected)
                    {
                        selection.color = new Color(1f, 0.86f, 0.06f, 1f);
                    }
                }

                Text number = root.Find("Number")?.GetComponent<Text>();
                if (number != null)
                {
                    number.fontSize = primary ? 10 : 9;
                    number.fontStyle = FontStyle.Bold;
                    number.color = new Color(1f, 1f, 1f, 0.82f);
                }

                Image icon = root.Find("Icon")?.GetComponent<Image>();
                Text name = root.Find("Name")?.GetComponent<Text>();
                Text ammo = root.Find("Ammo")?.GetComponent<Text>();

                if (icon != null)
                {
                    icon.preserveAspect = true;
                    icon.color = Color.white;
                }

                if (name != null)
                {
                    bool hasIcon = icon != null && icon.enabled && icon.sprite != null;
                    name.enabled = !hasIcon;
                    name.fontStyle = FontStyle.Bold;
                    name.color = new Color(1f, 1f, 1f, 0.72f);
                }

                if (ammo != null)
                {
                    ammo.fontSize = primary ? 14 : 11;
                    ammo.fontStyle = FontStyle.Bold;
                    ammo.color = Color.white;
                    EnsureOutline(ammo.gameObject, new Color(0f, 0f, 0f, 0.78f));
                }

                EnsureSlotDivider(root);
            }
        }

        private static void EnsureSlotDivider(RectTransform root)
        {
            Transform existing = root.Find("FineDivider");
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                GameObject go = new GameObject("FineDivider");
                go.transform.SetParent(root, false);
                rect = go.AddComponent<RectTransform>();
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            if (rect == null || image == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 1f);
            image.color = new Color(0.70f, 0.75f, 0.78f, 0.20f);
        }

        private static void PolishSidePlaceholders(Transform status)
        {
            PolishPlaceholder(status.Find("ConsumablePlaceholder") as RectTransform);
            PolishPlaceholder(status.Find("GrenadePlaceholder") as RectTransform);
        }

        private static void PolishPlaceholder(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            root.sizeDelta = new Vector2(68f, 68f);

            Image background = root.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.035f, 0.045f, 0.05f, 0.78f);
            }

            Outline outline = root.GetComponent<Outline>();
            if (outline == null)
            {
                outline = root.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            outline.effectDistance = new Vector2(1f, -1f);

            Text symbol = root.Find("Symbol")?.GetComponent<Text>();
            Text caption = root.Find("Caption")?.GetComponent<Text>();

            if (symbol != null)
            {
                symbol.fontSize = 21;
                symbol.fontStyle = FontStyle.Bold;
            }

            if (caption != null)
            {
                caption.fontSize = 7;
                caption.color = new Color(1f, 1f, 1f, 0.68f);
            }
        }

        private static void PolishLootPanel(Transform canvas)
        {
            RectTransform panel = canvas.Find("NearbyLoot") as RectTransform;
            if (panel == null)
            {
                return;
            }

            Text title = panel.Find("Title/TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.fontSize = 16;
                title.fontStyle = FontStyle.BoldAndItalic;
                title.color = Color.white;
                EnsureOutline(title.gameObject, new Color(0f, 0f, 0f, 0.70f));
            }

            for (int i = 0; i < 7; i++)
            {
                RectTransform row = panel.Find($"LootRow_{i}") as RectTransform;
                if (row == null)
                {
                    continue;
                }

                Text rowText = row.GetComponent<Text>();
                if (rowText != null)
                {
                    rowText.fontSize = 13;
                    rowText.fontStyle = FontStyle.Bold;
                    rowText.lineSpacing = 0.90f;
                }

                EnsureLootRowDivider(row);
            }

            RectTransform toggle = panel.Find("ToggleBg") as RectTransform;
            if (toggle != null)
            {
                toggle.sizeDelta = new Vector2(150f, 22f);
                toggle.anchoredPosition = new Vector2(-108f, 132f);
                toggle.localEulerAngles = new Vector3(0f, 0f, 90f);

                Image toggleBackground = toggle.GetComponent<Image>();
                if (toggleBackground != null)
                {
                    toggleBackground.color = new Color(0.025f, 0.035f, 0.045f, 0.92f);
                }

                Text toggleText = toggle.Find("ToggleHint")?.GetComponent<Text>();
                if (toggleText != null)
                {
                    toggleText.fontSize = 9;
                    toggleText.fontStyle = FontStyle.Bold;
                    toggleText.color = new Color(1f, 0.88f, 0.05f, 1f);
                }
            }
        }

        private static void EnsureLootRowDivider(RectTransform row)
        {
            Transform existing = row.Find("RowDivider");
            RectTransform rect;
            Image image;

            if (existing == null)
            {
                GameObject go = new GameObject("RowDivider");
                go.transform.SetParent(row, false);
                rect = go.AddComponent<RectTransform>();
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }

            if (rect == null || image == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 1f);
            image.color = new Color(0f, 0f, 0f, 0.18f);
        }

        private static void EnsureOutline(GameObject target, Color color)
        {
            if (target == null)
            {
                return;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ROS_HUD_FineCircle";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.49f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
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
            _circleSprite.name = "ROS_HUD_FineCircle";
            return _circleSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (_ringSprite != null)
            {
                return _ringSprite;
            }

            const int size = 256;
            const float thickness = 5f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ROS_HUD_MinimapRing";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.49f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float ringDistance = Mathf.Abs(distance - (radius - thickness * 0.5f));
                    float alpha = Mathf.Clamp01((thickness * 0.5f + 1.2f - ringDistance) / 1.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            _ringSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            _ringSprite.name = "ROS_HUD_MinimapRing";
            return _ringSprite;
        }
    }
}
