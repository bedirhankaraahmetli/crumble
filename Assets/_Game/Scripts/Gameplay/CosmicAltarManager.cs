using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// The Cosmic Altar (GDD §8): uncapped upgrades bought with Time Crystals whose
    /// effects compound multiplicatively — the endgame's "infinite multipliers" layer.
    /// Levels live in SaveData.Cosmic.AltarUpgrades and survive everything, including
    /// Hard Prestige. All aggregated multipliers are BigDouble because compounding
    /// growth is unbounded and will eventually leave double range.
    /// </summary>
    [DefaultExecutionOrder(-56)]
    public sealed class CosmicAltarManager : Singleton<CosmicAltarManager>
    {
        [Header("Content")]
        [SerializeField] private AltarUpgradeSO[] upgrades;

        private System.Collections.Generic.Dictionary<string, int> _levels;

        public System.Collections.Generic.IReadOnlyList<AltarUpgradeSO> Upgrades => upgrades;

        // ---- aggregated multipliers (recomputed on load and purchase) ----
        /// <summary>Multiplies total click damage (×1 = no altar levels).</summary>
        public BigDouble ClickMultiplier { get; private set; } = BigDouble.One;

        /// <summary>Multiplies total assistant DPS.</summary>
        public BigDouble DpsMultiplier { get; private set; } = BigDouble.One;

        /// <summary>Multiplies all coin gains (trickle, shatter rewards, offline).</summary>
        public BigDouble CoinMultiplier { get; private set; } = BigDouble.One;

        /// <summary>Multiplies the KP paid out by Standard Prestige.</summary>
        public BigDouble KnowledgeMultiplier { get; private set; } = BigDouble.One;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            _levels = data.Cosmic.AltarUpgrades;
            Recompute();
        }

        public int GetLevel(AltarUpgradeSO upgrade)
        {
            return _levels != null && _levels.TryGetValue(upgrade.Id, out var level) ? level : 0;
        }

        /// <summary>TC price of the upgrade's next level (the altar has no level cap).</summary>
        public BigDouble NextCost(AltarUpgradeSO upgrade)
        {
            return GameMath.UpgradeCost(upgrade.BaseTimeCrystalCost, upgrade.CostGrowthFactor, GetLevel(upgrade));
        }

        /// <summary>This upgrade's current compounded multiplier (×1 at level 0).</summary>
        public BigDouble CurrentMultiplier(AltarUpgradeSO upgrade)
        {
            return GameMath.AltarMultiplier(upgrade.MultiplierPerLevel, GetLevel(upgrade));
        }

        public bool TryBuy(AltarUpgradeSO upgrade)
        {
            if (_levels == null || upgrade == null)
            {
                return false;
            }

            if (!CurrencyManager.Instance.TrySpendTimeCrystals(NextCost(upgrade)))
            {
                return false;
            }

            var level = GetLevel(upgrade) + 1;
            _levels[upgrade.Id] = level;

            Recompute();
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.RefreshStats(); // damage/DPS totals absorb the new multipliers
            }

            GameEvents.RaiseAltarUpgradeChanged(upgrade.Id, level);
            SaveManager.Instance.Save(); // Time Crystals are too precious to lose to a crash
            return true;
        }

        private void Recompute()
        {
            BigDouble click = BigDouble.One, dps = BigDouble.One,
                coin = BigDouble.One, knowledge = BigDouble.One;

            if (upgrades != null && _levels != null)
            {
                foreach (var upgrade in upgrades)
                {
                    var level = GetLevel(upgrade);
                    if (level <= 0)
                    {
                        continue;
                    }

                    var multiplier = GameMath.AltarMultiplier(upgrade.MultiplierPerLevel, level);
                    switch (upgrade.EffectType)
                    {
                        case AltarEffectType.ClickDamage:
                            click *= multiplier;
                            break;
                        case AltarEffectType.AssistantDps:
                            dps *= multiplier;
                            break;
                        case AltarEffectType.CoinGain:
                            coin *= multiplier;
                            break;
                        case AltarEffectType.KnowledgeGain:
                            knowledge *= multiplier;
                            break;
                    }
                }
            }

            ClickMultiplier = click;
            DpsMultiplier = dps;
            CoinMultiplier = coin;
            KnowledgeMultiplier = knowledge;
        }
    }
}
