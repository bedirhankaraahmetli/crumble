using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// The Cosmic Archive — Hard Prestige (GDD §8). Unlocks once every research branch's
    /// Stage-15 ultimate is owned ("Solve the Universal Secret"). Solving it erases the
    /// entire epoch — coins, tools, assistants, KP, the whole Research Tree, the Museum,
    /// any running expedition — and pays Time Crystals based on all KP earned this epoch
    /// (balance + KP sunk into the tree). Only Time Crystals and the Cosmic Altar survive.
    /// The wipe reuses the GameLoaded rebind path, same as Standard Prestige.
    /// </summary>
    [DefaultExecutionOrder(-53)]
    public sealed class CosmicArchiveManager : Singleton<CosmicArchiveManager>
    {
        [Header("Balance (GDD §8: TC = floor(√(total KP this epoch / divisor)))")]
        [Tooltip("Total epoch KP needed for the first Time Crystal.")]
        [SerializeField] private double timeCrystalDivisor = 1500;

        private SaveData _data;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data) => _data = data;

        /// <summary>How many Stage-15 research ultimates the player owns.</summary>
        public int UltimatesOwned
        {
            get
            {
                var research = ResearchManager.Instance;
                if (research == null)
                {
                    return 0;
                }

                var owned = 0;
                foreach (var node in research.Nodes)
                {
                    if (node != null && node.Stage == 15 && research.GetLevel(node) >= node.MaxLevel)
                    {
                        owned++;
                    }
                }

                return owned;
            }
        }

        /// <summary>Total Stage-15 ultimates in the tree (one per branch — 4).</summary>
        public int UltimatesTotal
        {
            get
            {
                var research = ResearchManager.Instance;
                if (research == null)
                {
                    return 0;
                }

                var total = 0;
                foreach (var node in research.Nodes)
                {
                    if (node != null && node.Stage == 15)
                    {
                        total++;
                    }
                }

                return total;
            }
        }

        /// <summary>The Universal Secret can be solved: every branch's ultimate is owned.</summary>
        public bool IsUnlocked => UltimatesTotal > 0 && UltimatesOwned >= UltimatesTotal;

        public int ArchiveCount => _data != null ? _data.Cosmic.ArchiveCount : 0;

        /// <summary>
        /// Should the player see the Cosmic button at all? The Archive stays a mystery
        /// until the first ultimate is researched (or the player already has crystals).
        /// </summary>
        public bool IsRevealed =>
            UltimatesOwned > 0
            || ArchiveCount > 0
            || (CurrencyManager.Instance != null && CurrencyManager.Instance.TimeCrystals > 0);

        /// <summary>KP balance + KP ever spent in the tree — everything this epoch earned.</summary>
        public BigDouble TotalKnowledgeThisEpoch
        {
            get
            {
                var kp = CurrencyManager.Instance != null
                    ? CurrencyManager.Instance.KnowledgePoints
                    : BigDouble.Zero;
                return kp + KnowledgeSpentOnResearch();
            }
        }

        /// <summary>TC a Hard Prestige would award right now.</summary>
        public BigDouble PendingTimeCrystals =>
            GameMath.TimeCrystalsForArchive(TotalKnowledgeThisEpoch, timeCrystalDivisor);

        /// <summary>
        /// Exact KP paid for the current tree: each node's levels are a geometric series,
        /// so the total is BulkUpgradeCost from level 0 — no purchase log needed.
        /// </summary>
        private static BigDouble KnowledgeSpentOnResearch()
        {
            var research = ResearchManager.Instance;
            if (research == null)
            {
                return BigDouble.Zero;
            }

            var spent = BigDouble.Zero;
            foreach (var node in research.Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var level = research.GetLevel(node);
                if (level > 0)
                {
                    spent += GameMath.BulkUpgradeCost(node.BaseKpCost, node.CostGrowthFactor, 0, level);
                }
            }

            return spent;
        }

        public bool HardPrestige()
        {
            if (_data == null || !IsUnlocked)
            {
                return false;
            }

            var crystals = PendingTimeCrystals;
            if (crystals < 1)
            {
                return false;
            }

            CurrencyManager.Instance.AddTimeCrystals(crystals);
            _data.Cosmic.ArchiveCount++;

            // Erase the epoch. Only Time Crystals + altar levels (SaveData.Cosmic) survive.
            _data.Currencies.AntiqueCoins = BigDouble.Zero;
            _data.Currencies.LifetimeCoinsThisRun = BigDouble.Zero;
            _data.Currencies.KnowledgePoints = BigDouble.Zero;
            _data.Upgrades.Tools.Clear();
            _data.Upgrades.Assistants.Clear();
            _data.ResearchTree.Clear();
            _data.Museum.Clear();
            _data.Expedition.ExpeditionId = "";
            _data.Expedition.EndUnixUtc = 0;
            _data.CurrentExcavation.TabletId = "";
            _data.CurrentExcavation.Stage = 0;
            _data.CurrentExcavation.RemainingHp = BigDouble.Zero;

            GameEvents.RaiseGameLoaded(_data); // full rebind: every manager and panel resets
            GameEvents.RaiseHardPrestige(crystals);
            SaveManager.Instance.Save();       // an Archive is THE checkpoint — persist now
            return true;
        }
    }
}
