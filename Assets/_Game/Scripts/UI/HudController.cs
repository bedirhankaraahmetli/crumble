using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Gameplay;
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

        [Header("Endgame")]
        [SerializeField] private Text tcText;

        // Measured DPS: all damage (taps + assistant ticks) accumulates into a rolling
        // window of fixed buckets — fast tapping spikes the readout, idle settles back
        // to the assistants' rate. Fixed arrays, zero allocations on the damage path.
        private const float BucketSeconds = 0.25f;
        private const int BucketCount = 8; // 2-second window

        private readonly BigDouble[] _damageBuckets = new BigDouble[BucketCount];
        private int _bucketIndex;
        private float _bucketTimer;

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.TabletChanged += OnTabletChanged;
            GameEvents.TabletHpChanged += OnHpChanged;
            GameEvents.TabletDamaged += OnTabletDamaged;
            GameEvents.KnowledgePointsChanged += OnKnowledgeChanged;
            GameEvents.TimeCrystalsChanged += OnTimeCrystalsChanged;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.TabletChanged -= OnTabletChanged;
            GameEvents.TabletHpChanged -= OnHpChanged;
            GameEvents.TabletDamaged -= OnTabletDamaged;
            GameEvents.KnowledgePointsChanged -= OnKnowledgeChanged;
            GameEvents.TimeCrystalsChanged -= OnTimeCrystalsChanged;
        }

        private void OnTabletDamaged(DamageInfo info)
        {
            _damageBuckets[_bucketIndex] += info.Amount;
        }

        private void Update()
        {
            _bucketTimer += Time.unscaledDeltaTime;
            while (_bucketTimer >= BucketSeconds)
            {
                _bucketTimer -= BucketSeconds;
                _bucketIndex = (_bucketIndex + 1) % BucketCount;
                _damageBuckets[_bucketIndex] = BigDouble.Zero;
                RefreshStatsLine();
            }
        }

        private void RefreshStatsLine()
        {
            if (statsText == null)
            {
                return;
            }

            // effective click damage from the source of truth — includes the fever multiplier
            var tap = TabletManager.Instance != null ? TabletManager.Instance.ClickDamage : BigDouble.One;

            var windowDamage = BigDouble.Zero;
            foreach (var bucket in _damageBuckets)
            {
                windowDamage += bucket;
            }

            var measuredDps = windowDamage / (BucketSeconds * BucketCount);
            statsText.text =
                $"Tap {NumberFormatter.Format(tap)}    DPS {NumberFormatter.Format(measuredDps)}";
        }

        private void OnKnowledgeChanged(BigDouble total)
        {
            if (kpText != null)
            {
                kpText.text = "KP " + NumberFormatter.Format(total);
            }
        }

        /// <summary>Hidden until the endgame currency exists — no HUD clutter early on.</summary>
        private void OnTimeCrystalsChanged(BigDouble total)
        {
            if (tcText != null)
            {
                tcText.text = "TC " + NumberFormatter.Format(total);
                tcText.gameObject.SetActive(total > 0);
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
