using System;
using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// The Expedition Tent (GDD §6): one real-time mission at a time, persisted by end
    /// timestamp so it keeps ticking while the app is closed. Rewards a window of full-DPS
    /// income (floored at several tablet rewards) and often an artifact. ExpeditionSpeed
    /// research shortens waits; ArtifactDropRate research raises the haul chance.
    /// </summary>
    [DefaultExecutionOrder(-47)]
    public sealed class ExpeditionManager : Singleton<ExpeditionManager>
    {
        [Header("Content")]
        [SerializeField] private ExpeditionSO[] expeditions;

        private ExpeditionSaveState _state;

        public IReadOnlyList<ExpeditionSO> Expeditions => expeditions;

        public ExpeditionSO ActiveExpedition
        {
            get
            {
                if (_state == null || string.IsNullOrEmpty(_state.ExpeditionId) || expeditions == null)
                {
                    return null;
                }

                foreach (var expedition in expeditions)
                {
                    if (expedition.Id == _state.ExpeditionId)
                    {
                        return expedition;
                    }
                }

                return null;
            }
        }

        public bool IsActive => ActiveExpedition != null;

        public bool IsReady => IsActive && RemainingSeconds <= 0;

        public double RemainingSeconds => _state == null
            ? 0
            : Math.Max(0, _state.EndUnixUtc - DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data) => _state = data.Expedition;

        public double EffectiveDurationHours(ExpeditionSO expedition)
        {
            var research = ResearchManager.Instance;
            return GameMath.ExpeditionDurationHours(
                expedition.BaseDurationHours, research != null ? research.ExpeditionSpeedBonus : 0);
        }

        /// <summary>Coin payout if collected now (income multipliers included).</summary>
        public BigDouble RewardPreview(ExpeditionSO expedition)
        {
            var dps = UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalDps : BigDouble.Zero;
            var tablet = TabletManager.Instance;
            var reward = GameMath.EventReward(
                dps, expedition.RewardDpsSeconds,
                tablet != null ? tablet.CurrentShatterReward : BigDouble.One,
                expedition.MinRewardTabletMultiple);
            return reward * IncomeMultiplier;
        }

        public bool TryStart(ExpeditionSO expedition)
        {
            if (_state == null || expedition == null || IsActive)
            {
                return false;
            }

            _state.ExpeditionId = expedition.Id;
            _state.EndUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                + (long)(EffectiveDurationHours(expedition) * 3600.0);
            GameEvents.RaiseExpeditionStarted(expedition, _state.EndUnixUtc);
            SaveManager.Instance.Save(); // the timer must survive an immediate quit
            return true;
        }

        public bool TryCollect()
        {
            var expedition = ActiveExpedition;
            if (expedition == null || !IsReady)
            {
                return false;
            }

            var coins = RewardPreview(expedition);
            CurrencyManager.Instance.AddCoins(coins);

            ArtifactSO artifact = null;
            var research = ResearchManager.Instance;
            var chance = expedition.ArtifactChance * (1 + (research != null ? research.ArtifactDropRateBonus : 0));
            if (MuseumManager.Instance != null && UnityEngine.Random.value < chance)
            {
                artifact = MuseumManager.Instance.PickRandomArtifact();
            }

            _state.ExpeditionId = "";
            _state.EndUnixUtc = 0;
            GameEvents.RaiseExpeditionCollected(coins, artifact);
            if (artifact != null)
            {
                MuseumManager.Instance.GrantArtifact(artifact); // after the collect event so the artifact toast wins
            }

            SaveManager.Instance.Save();
            return true;
        }

        /// <summary>Dev helper: finish the active expedition immediately.</summary>
        public void DebugCompleteActive()
        {
            if (_state != null && IsActive)
            {
                _state.EndUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        private static double IncomeMultiplier
        {
            get
            {
                var research = ResearchManager.Instance != null ? ResearchManager.Instance.CoinMultiplier : 1;
                var museum = MuseumManager.Instance != null ? MuseumManager.Instance.CoinMultiplier : 1;
                return research * museum;
            }
        }
    }
}
