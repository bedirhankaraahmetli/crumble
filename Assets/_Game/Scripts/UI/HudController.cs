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

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.TabletChanged += OnTabletChanged;
            GameEvents.TabletHpChanged += OnHpChanged;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.TabletChanged -= OnTabletChanged;
            GameEvents.TabletHpChanged -= OnHpChanged;
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
