using System.Collections;
using System.Collections.Generic;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class KillFeedPresenter : MonoBehaviour
    {
        private const int MaxEntries = 5;
        private const float KillDisplayTime = 5f;
        private const float FadeTime = 0.8f;

        [SerializeField] private BattleRoyaleManager manager;
        [SerializeField] private Health localHealth;
        [SerializeField] private Text[] rows = new Text[MaxEntries];

        private readonly List<Entry> _entries = new List<Entry>();

        private void Awake()
        {
            ResolvePhysicalRows();
            ResolveGameplayReferences();
        }

        private void OnEnable()
        {
            ResolveGameplayReferences();
            Subscribe();
        }

        public void Bind(BattleRoyaleManager matchManager, Health health)
        {
            Unsubscribe();
            manager = matchManager;
            localHealth = health;
            ResolvePhysicalRows();
            Subscribe();
        }

        private void ResolveGameplayReferences()
        {
            if (manager == null)
                manager = FindFirstObjectByType<BattleRoyaleManager>();

            if (localHealth == null)
            {
                PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
                if (input != null)
                    localHealth = input.GetComponent<Health>();
            }
        }

        private void ResolvePhysicalRows()
        {
            if (rows == null || rows.Length != MaxEntries)
                rows = new Text[MaxEntries];

            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < MaxEntries; i++)
            {
                string wanted = "KillFeedRow_" + i;
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j].name != wanted) continue;
                    rows[i] = all[j].GetComponent<Text>();
                    break;
                }

                if (rows[i] != null)
                {
                    rows[i].text = string.Empty;
                    rows[i].gameObject.SetActive(false);
                }
            }
        }

        private void Subscribe()
        {
            if (manager == null) return;
            manager.PlayerEliminated -= OnElimination;
            manager.PlayerEliminated += OnElimination;
        }

        private void Unsubscribe()
        {
            if (manager != null)
                manager.PlayerEliminated -= OnElimination;
        }

        private void OnElimination(EliminationInfo info)
        {
            if (rows == null || rows.Length == 0)
                return;

            string victim = info.Victim != null
                ? SimplifyName(info.Victim.gameObject.name)
                : "?";
            string killer = info.Killer != null
                ? SimplifyName(info.Killer.gameObject.name)
                : "Zona";

            bool isLocalKill = info.Killer == localHealth;
            bool localDied = info.Victim == localHealth;

            Entry entry = new Entry
            {
                Text = $"{killer}  ▶  {victim}",
                Color = isLocalKill
                    ? new Color(1f, 0.85f, 0.1f)
                    : localDied
                        ? new Color(1f, 0.3f, 0.2f)
                        : Color.white
            };

            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            RefreshRows();
            StartCoroutine(RemoveLater(entry));
        }

        private IEnumerator RemoveLater(Entry entry)
        {
            yield return new WaitForSeconds(KillDisplayTime - FadeTime);

            float elapsed = 0f;
            while (elapsed < FadeTime)
            {
                elapsed += Time.deltaTime;
                entry.Alpha = 1f - Mathf.Clamp01(elapsed / FadeTime);
                RefreshRows();
                yield return null;
            }

            _entries.Remove(entry);
            RefreshRows();
        }

        private void RefreshRows()
        {
            for (int i = 0; i < MaxEntries; i++)
            {
                Text row = rows != null && i < rows.Length ? rows[i] : null;
                if (row == null) continue;

                if (i >= _entries.Count)
                {
                    row.text = string.Empty;
                    row.gameObject.SetActive(false);
                    continue;
                }

                Entry entry = _entries[i];
                Color color = entry.Color;
                color.a *= entry.Alpha;
                row.color = color;
                row.text = entry.Text;
                row.gameObject.SetActive(true);
            }
        }

        private static string SimplifyName(string raw)
        {
            const string prefix = "Bot_BattleRoyale_";
            return raw.StartsWith(prefix)
                ? "Bot_" + raw.Substring(prefix.Length)
                : raw;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private sealed class Entry
        {
            public string Text;
            public Color Color;
            public float Alpha = 1f;
        }
    }
}
