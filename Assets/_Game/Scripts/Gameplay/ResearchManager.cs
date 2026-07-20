using System;
using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// The Research Tree (GDD §7): node levels live in SaveData.ResearchTree (id → level),
    /// unlocking follows each node's prerequisite list, purchases spend KP, and all node
    /// effects aggregate into a handful of global modifiers read by the other managers.
    /// Research survives Standard Prestige; only Hard Prestige (Step 9) wipes it.
    /// </summary>
    [DefaultExecutionOrder(-57)]
    public sealed class ResearchManager : Singleton<ResearchManager>
    {
        [Header("Content (all nodes, any order — UI groups by branch)")]
        [SerializeField] private ResearchNodeSO[] nodes;

        private System.Collections.Generic.Dictionary<string, int> _levels;

        public System.Collections.Generic.IReadOnlyList<ResearchNodeSO> Nodes => nodes;

        // ---- aggregated modifiers (recomputed on load and purchase) ----
        /// <summary>Multiplies total click damage (1 = no bonus).</summary>
        public double ClickDamageMultiplier { get; private set; } = 1;

        /// <summary>Multiplies total assistant DPS.</summary>
        public double DpsMultiplier { get; private set; } = 1;

        /// <summary>Multiplies all coin gains (trickle and shatter rewards).</summary>
        public double CoinMultiplier { get; private set; } = 1;

        /// <summary>Upgrade cost discount, already clamped to GameMath.MaxUpgradeCostReduction.</summary>
        public double UpgradeCostReduction { get; private set; }

        /// <summary>Added to the base offline efficiency fraction (0.10 = +10%).</summary>
        public double OfflineEfficiencyBonus { get; private set; }

        /// <summary>Extra hours added to the offline accumulation cap.</summary>
        public double OfflineCapBonusHours { get; private set; }

        /// <summary>Extra seconds added to Fever Mode's duration.</summary>
        public double FeverDurationBonusSeconds { get; private set; }

        /// <summary>Added to the base tap crit chance (0.05 = +5%).</summary>
        public double CritChanceBonus { get; private set; }

        /// <summary>Added to the base crit damage multiplier (0.5 = +0.5×).</summary>
        public double CritMultiplierBonus { get; private set; }

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            _levels = data.ResearchTree;
            Recompute();
        }

        public int GetLevel(ResearchNodeSO node)
        {
            return _levels != null && _levels.TryGetValue(node.Id, out var level) ? level : 0;
        }

        public bool IsMaxed(ResearchNodeSO node) => GetLevel(node) >= node.MaxLevel;

        /// <summary>All prerequisites met? (Stage-15 nodes require the stage-14 node maxed.)</summary>
        public bool IsUnlocked(ResearchNodeSO node)
        {
            if (node.Prerequisites == null)
            {
                return true;
            }

            foreach (var prerequisite in node.Prerequisites)
            {
                if (prerequisite.Node == null)
                {
                    continue;
                }

                if (GetLevel(prerequisite.Node) < prerequisite.RequiredLevel)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>KP price of the node's next level (same growth formula as upgrades).</summary>
        public BigDouble NodeCost(ResearchNodeSO node)
        {
            return GameMath.UpgradeCost(node.BaseKpCost, node.CostGrowthFactor, GetLevel(node));
        }

        public bool TryBuy(ResearchNodeSO node)
        {
            if (_levels == null || node == null || !IsUnlocked(node) || IsMaxed(node))
            {
                return false;
            }

            if (!CurrencyManager.Instance.TrySpendKnowledge(NodeCost(node)))
            {
                return false;
            }

            var level = GetLevel(node) + 1;
            _levels[node.Id] = level;

            Recompute();
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.RefreshStats(); // damage/DPS totals absorb the new multipliers
            }

            GameEvents.RaiseResearchNodeChanged(node.Id, level);
            return true;
        }

        private void Recompute()
        {
            double click = 1, dps = 1, coin = 1, reduction = 0, offlineEfficiency = 0, offlineCapHours = 0,
                feverSeconds = 0, critChance = 0, critMultiplier = 0;

            if (nodes != null && _levels != null)
            {
                foreach (var node in nodes)
                {
                    var level = GetLevel(node);
                    if (level <= 0)
                    {
                        continue;
                    }

                    var total = node.EffectPerLevel * level;
                    switch (node.EffectType)
                    {
                        case ResearchEffectType.ClickDamageMultiplier:
                            click += total;
                            break;
                        case ResearchEffectType.AssistantDpsMultiplier:
                            dps += total;
                            break;
                        case ResearchEffectType.CoinDropMultiplier:
                            coin += total;
                            break;
                        case ResearchEffectType.UpgradeCostReduction:
                            reduction += total;
                            break;
                        case ResearchEffectType.OfflineEfficiency:
                            offlineEfficiency += total;
                            break;
                        case ResearchEffectType.OfflineCapHours:
                            offlineCapHours += total;
                            break;
                        case ResearchEffectType.FeverDuration:
                            feverSeconds += total;
                            break;
                        case ResearchEffectType.CritChance:
                            critChance += total;
                            break;
                        case ResearchEffectType.CritMultiplier:
                            critMultiplier += total;
                            break;
                        // Remaining effect types belong to systems that arrive in later
                        // steps (crit, Fever, artifacts, expeditions, museum); their nodes
                        // can be bought now and take hold when those land.
                    }
                }
            }

            ClickDamageMultiplier = click;
            DpsMultiplier = dps;
            CoinMultiplier = coin;
            UpgradeCostReduction = Math.Min(reduction, GameMath.MaxUpgradeCostReduction);
            OfflineEfficiencyBonus = offlineEfficiency;
            OfflineCapBonusHours = offlineCapHours;
            FeverDurationBonusSeconds = feverSeconds;
            CritChanceBonus = critChance;
            CritMultiplierBonus = critMultiplier;
        }
    }
}
