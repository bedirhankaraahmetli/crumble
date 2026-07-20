using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Minimal portrait HUD: coin counter, material/stage label, HP bar.
    /// Subscribe-only: reads GameEvents, never calls managers.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Text coinText;
        [SerializeField] private Text stageText;

        [Header("HP bar")]
        [SerializeField] private RectTransform hpFill;
        [SerializeField] private Text hpText;

        [Header("Stats")]
        [SerializeField] private Text statsText;

        [Header("Prestige")]
        [SerializeField] private Text kpText;

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.TabletChanged += OnTabletChanged;
            GameEvents.TabletHpChanged += OnHpChanged;
            GameEvents.StatsChanged += OnStatsChanged;
            GameEvents.KnowledgePointsChanged += OnKnowledgeChanged;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.TabletChanged -= OnTabletChanged;
            GameEvents.TabletHpChanged -= OnHpChanged;
            GameEvents.StatsChanged -= OnStatsChanged;
            GameEvents.KnowledgePointsChanged -= OnKnowledgeChanged;
        }

        private void OnKnowledgeChanged(BigDouble total)
        {
            if (kpText != null)
            {
                kpText.text = "KP " + NumberFormatter.Format(total);
            }
        }

        private void OnStatsChanged(BigDouble clickDamage, BigDouble dps)
        {
            if (statsText != null)
            {
                statsText.text =
                    $"Tap {NumberFormatter.Format(clickDamage)}    DPS {NumberFormatter.Format(dps)}";
            }
        }

        private void OnCoinsChanged(BigDouble total)
        {
            if (coinText != null)
            {
                coinText.text = NumberFormatter.Format(total);
            }
        }

        private void OnTabletChanged(TabletMaterialSO material, int stage, bool isMilestone)
        {
            if (stageText != null)
            {
                stageText.text = isMilestone
                    ? $"{material.DisplayName} — Stage {stage + 1} (Hard)"
                    : $"{material.DisplayName} — Stage {stage + 1}";
            }
        }

        private void OnHpChanged(BigDouble current, BigDouble max)
        {
            var pct = max > 0 ? Mathf.Clamp01((float)(current / max).ToDouble()) : 0f;
            if (hpFill != null)
            {
                hpFill.anchorMax = new Vector2(pct, 1f);
                hpFill.offsetMin = Vector2.zero;
                hpFill.offsetMax = Vector2.zero;
            }

            if (hpText != null)
            {
                hpText.text = current <= 0
                    ? "0"
                    : $"{NumberFormatter.Format(current)} / {NumberFormatter.Format(max)}";
            }
        }
    }
}
