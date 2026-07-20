using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Owns the run economy: tool/assistant levels (a live view over SaveData.Upgrades)
    /// and the aggregated damage stats derived from them. All purchases go through here;
    /// costs come exclusively from GameMath. UI calls TryBuy* and reads the getters.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public sealed class UpgradeManager : Singleton<UpgradeManager>
    {
        [Header("Content (ordered by progression)")]
        [SerializeField] private ToolSO[] tools;
        [SerializeField] private AssistantSO[] assistants;

        [Header("Balance")]
        [Tooltip("Click damage before any tools (research multipliers arrive in Step 5).")]
        [SerializeField] private double baseClickDamage = 1;
        [Tooltip("Chance for a tap to crit before research bonuses (0.05 = 5%).")]
        [SerializeField] private double baseCritChance = 0.05;
        [Tooltip("Crit damage multiplier before research bonuses (starts at x2).")]
        [SerializeField] private double baseCritMultiplier = 2.0;

        private UpgradesState _state;

        public IReadOnlyList<ToolSO> Tools => tools;
        public IReadOnlyList<AssistantSO> Assistants => assistants;

        /// <summary>Base click damage + every tool's contribution.</summary>
        public BigDouble TotalClickDamage { get; private set; } = BigDouble.One;

        /// <summary>Sum of every assistant's passive damage per second.</summary>
        public BigDouble TotalDps { get; private set; } = BigDouble.Zero;

        /// <summary>Tap crit chance 0..1 (base + CritChance research).</summary>
        public double CritChance { get; private set; }

        /// <summary>Crit damage multiplier (base ×2 + CritMultiplier research).</summary>
        public double CritMultiplier { get; private set; } = 2.0;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            _state = data.Upgrades;
            RecomputeStats();
        }

        /// <summary>
        /// Inspector tuning support: stats are cached, so a live edit of baseClickDamage
        /// (or the content arrays) must recompute immediately, not on the next purchase.
        /// Note Unity still reverts Inspector edits made during Play mode when you stop —
        /// set persistent values in Edit mode.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying && _state != null)
            {
                RecomputeStats();
            }
        }

        /// <summary>External modifiers changed (research purchase) — re-aggregate stats.</summary>
        public void RefreshStats()
        {
            if (_state != null)
            {
                RecomputeStats();
            }
        }

        public int GetToolLevel(ToolSO tool)
        {
            return _state != null && _state.Tools.TryGetValue(tool.Id, out var level) ? level : 0;
        }

        public int GetAssistantLevel(AssistantSO assistant)
        {
            return _state != null && _state.Assistants.TryGetValue(assistant.Id, out var level) ? level : 0;
        }

        /// <summary>Research discount applied to every tool/assistant price (already capped).</summary>
        private static double CostReduction =>
            ResearchManager.Instance != null ? ResearchManager.Instance.UpgradeCostReduction : 0;

        public BigDouble ToolCost(ToolSO tool, int count)
        {
            var baseCost = GameMath.ApplyCostReduction(tool.BaseCost, CostReduction);
            return GameMath.BulkUpgradeCost(baseCost, tool.GrowthFactor, GetToolLevel(tool), count);
        }

        public BigDouble AssistantCost(AssistantSO assistant, int count)
        {
            var baseCost = GameMath.ApplyCostReduction(assistant.BaseCost, CostReduction);
            return GameMath.BulkUpgradeCost(baseCost, assistant.GrowthFactor, GetAssistantLevel(assistant), count);
        }

        public int MaxAffordableTool(ToolSO tool)
        {
            var baseCost = GameMath.ApplyCostReduction(tool.BaseCost, CostReduction);
            return GameMath.MaxAffordable(
                baseCost, tool.GrowthFactor, GetToolLevel(tool), CurrencyManager.Instance.AntiqueCoins);
        }

        public int MaxAffordableAssistant(AssistantSO assistant)
        {
            var baseCost = GameMath.ApplyCostReduction(assistant.BaseCost, CostReduction);
            return GameMath.MaxAffordable(
                baseCost, assistant.GrowthFactor, GetAssistantLevel(assistant),
                CurrencyManager.Instance.AntiqueCoins);
        }

        public bool TryBuyTool(ToolSO tool, int count)
        {
            if (_state == null || tool == null || count <= 0)
            {
                return false;
            }

            if (!CurrencyManager.Instance.TrySpendCoins(ToolCost(tool, count)))
            {
                return false;
            }

            var level = GetToolLevel(tool) + count;
            _state.Tools[tool.Id] = level;
            RecomputeStats();
            GameEvents.RaiseToolLevelChanged(tool.Id, level);
            return true;
        }

        public bool TryBuyAssistant(AssistantSO assistant, int count)
        {
            if (_state == null || assistant == null || count <= 0)
            {
                return false;
            }

            if (!CurrencyManager.Instance.TrySpendCoins(AssistantCost(assistant, count)))
            {
                return false;
            }

            var level = GetAssistantLevel(assistant) + count;
            _state.Assistants[assistant.Id] = level;
            RecomputeStats();
            GameEvents.RaiseAssistantLevelChanged(assistant.Id, level);
            return true;
        }

        private void RecomputeStats()
        {
            BigDouble click = baseClickDamage;
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    var level = GetToolLevel(tool);
                    if (level > 0)
                    {
                        click += (BigDouble)tool.BaseDamagePerLevel * level;
                    }
                }
            }

            var dps = BigDouble.Zero;
            if (assistants != null)
            {
                foreach (var assistant in assistants)
                {
                    var level = GetAssistantLevel(assistant);
                    if (level > 0)
                    {
                        dps += (BigDouble)assistant.BaseDpsPerLevel * level;
                    }
                }
            }

            var research = ResearchManager.Instance;
            if (research != null)
            {
                click *= research.ClickDamageMultiplier;
                dps *= research.DpsMultiplier;
            }

            TotalClickDamage = click;
            TotalDps = dps;
            CritChance = System.Math.Min(1.0,
                baseCritChance + (research != null ? research.CritChanceBonus : 0));
            CritMultiplier = baseCritMultiplier + (research != null ? research.CritMultiplierBonus : 0);
            GameEvents.RaiseStatsChanged(click, dps);
        }
    }
}
