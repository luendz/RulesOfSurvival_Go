using System.Collections;
using System.Collections.Generic;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Muestra las últimas eliminaciones en pantalla (esquina superior derecha).
    /// Las entradas se desvanecen tras KillDisplayTime segundos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KillFeedPresenter : MonoBehaviour
    {
        private const int   MaxEntries      = 5;
        private const float KillDisplayTime = 5f;
        private const float FadeTime        = 0.8f;

        private BattleRoyaleManager _manager;
        private Health              _localHealth;

        private readonly List<KillEntry> _entries = new List<KillEntry>();

        private GameObject _root;

        // ---------------------------------------------------------------

        public void Bind(BattleRoyaleManager manager, Health localHealth)
        {
            _manager     = manager;
            _localHealth = localHealth;

            if (_manager != null)
                _manager.PlayerEliminated += OnElimination;

            BuildUI();
        }

        private void OnDestroy()
        {
            if (_manager != null)
                _manager.PlayerEliminated -= OnElimination;

            if (_root != null) Destroy(_root);
        }

        // ---------------------------------------------------------------

        private void OnElimination(EliminationInfo info)
        {
            string victimName  = info.Victim  != null ? info.Victim.gameObject.name  : "?";
            string killerName  = info.Killer  != null ? info.Killer.gameObject.name  : "Zona";
            bool   isLocalKill = info.Killer  == _localHealth;
            bool   localDied   = info.Victim  == _localHealth;

            // Simplificar nombres de bots
            victimName = SimplifyName(victimName);
            killerName = SimplifyName(killerName);

            string line = $"{killerName}  ▶  {victimName}";

            // Si hay demasiadas, quitar la más antigua
            if (_entries.Count >= MaxEntries && _entries.Count > 0)
            {
                KillEntry oldest = _entries[0];
                _entries.RemoveAt(0);
                if (oldest.Label != null) Destroy(oldest.Label.gameObject);
                RebuildLayout();
            }

            // Crear nueva entrada
            GameObject go = new GameObject("KillEntry");
            go.transform.SetParent(_root.transform, false);

            Text txt = go.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 15;
            txt.color     = isLocalKill
                ? new Color(1f, 0.85f, 0.1f)   // amarillo: el jugador hizo el kill
                : localDied
                    ? new Color(1f, 0.3f, 0.2f) // rojo: el jugador murió
                    : Color.white;
            txt.alignment = TextAnchor.MiddleRight;
            txt.text      = line;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 22f);

            KillEntry entry = new KillEntry { Label = txt };
            _entries.Add(entry);
            RebuildLayout();

            StartCoroutine(FadeAndRemove(entry));
        }

        private IEnumerator FadeAndRemove(KillEntry entry)
        {
            yield return new WaitForSeconds(KillDisplayTime - FadeTime);

            float elapsed = 0f;
            while (elapsed < FadeTime)
            {
                elapsed += Time.deltaTime;
                if (entry.Label != null)
                {
                    Color c = entry.Label.color;
                    c.a = 1f - elapsed / FadeTime;
                    entry.Label.color = c;
                }
                yield return null;
            }

            if (entry.Label != null)
                Destroy(entry.Label.gameObject);

            _entries.Remove(entry);
        }

        private void RebuildLayout()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                RectTransform rt = _entries[i].Label?.GetComponent<RectTransform>();
                if (rt == null) continue;
                rt.anchoredPosition = new Vector2(0f, -i * 24f);
            }
        }

        private static string SimplifyName(string raw)
        {
            // "Bot_BattleRoyale_03" → "Bot_03"
            if (raw.StartsWith("Bot_BattleRoyale_"))
                return "Bot_" + raw.Substring("Bot_BattleRoyale_".Length);
            return raw;
        }

        private void BuildUI()
        {
            _root = new GameObject("KillFeedCanvas");

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Ancla: esquina superior derecha
            RectTransform rt = _root.GetComponent<RectTransform>();
            if (rt == null) rt = _root.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
        }

        // ---------------------------------------------------------------

        private sealed class KillEntry
        {
            public Text Label;
        }
    }
}
