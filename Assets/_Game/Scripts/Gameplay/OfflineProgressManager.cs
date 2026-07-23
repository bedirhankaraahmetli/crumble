using System;
using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Computes what the assistants earned while the app was closed (GDD §6): DPS × capped
    /// away-time, paid as trickle + amortized shatter rewards at the player's current
    /// tablet, scaled by offline efficiency. Research extends both the cap (OfflineCapHours)
    /// and the efficiency (OfflineEfficiency). Earnings are held as Pending until the
    /// welcome-back popup collects them (x1, or x2 via rewarded ad).
    /// Runs once per app session — prestige/reset rebinds must not re-trigger it.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public sealed class OfflineProgressManager : Singleton<OfflineProgressManager>
    {
        [Header("Balance")]
        [Tooltip("Offline hours credited before research extends the cap.")]
        [SerializeField] private double baseOfflineCapHours = 2;
        [Tooltip("Fraction of active income earned while away (research adds to this).")]
        [SerializeField] private double baseOfflineEfficiency = 0.5;
        [Tooltip("Absences shorter than this are ignored (quick restarts).")]
        [SerializeField] private double minimumSeconds = 60;

        public BigDouble PendingCoins { get; private set; } = BigDouble.Zero;
        public double PendingSeconds { get; private set; }

        private bool _checkedThisSession;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            if (_checkedThisSession)
            {
                return; // GameLoaded also fires on prestige/dev-reset rebinds
            }

            _checkedThisSession = true;
            if (data.LastLoginUnixUtc <= 0)
            {
                return; // fresh install — nothing was missed
            }

            var elapsed = (double)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.LastLoginUnixUtc);
            if (elapsed < minimumSeconds)
            {
                return;
            }

            var research = ResearchManager.Instance;
            var capHours = baseOfflineCapHours + (research != null ? research.OfflineCapBonusHours : 0);
            var seconds = GameMath.CappedOfflineSeconds(elapsed, capHours);

            var dps = UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalDps : BigDouble.Zero;
            var tablet = TabletManager.Instance;
            var efficiency = baseOfflineEfficiency + (research != null ? research.OfflineEfficiencyBonus : 0);

            var coins = GameMath.OfflineCoins(
                dps, seconds, tablet != null ? tablet.CoinPerDamageRatio : 0,
                tablet != null ? tablet.MaxHp : BigDouble.Zero,
                tablet != null ? tablet.CurrentShatterReward : BigDouble.Zero,
                efficiency);
            coins *= research != null ? research.CoinMultiplier : 1;
            coins *= MuseumManager.Instance != null ? MuseumManager.Instance.CoinMultiplier : 1;
            coins *= CosmicAltarManager.Instance != null
                ? CosmicAltarManager.Instance.CoinMultiplier
                : BigDouble.One;

            if (coins <= 0)
            {
                return;
            }

            PendingCoins = coins;
            PendingSeconds = seconds;
            GameEvents.RaiseOfflineEarningsReady(coins, seconds);
        }

        /// <summary>Called by the welcome-back popup. doubled = the rewarded ad was watched.</summary>
        public void Collect(bool doubled)
        {
            if (PendingCoins <= 0)
            {
                return;
            }

            CurrencyManager.Instance.AddCoins(doubled ? PendingCoins * 2 : PendingCoins);
            PendingCoins = BigDouble.Zero;
            PendingSeconds = 0;
            SaveManager.Instance.Save();
        }
    }
}
