using System;
using UnityEngine;

namespace Crumble.Data
{
    /// <summary>The 4 branches of the Research Tree (GDD §7).</summary>
    public enum ResearchBranch
    {
        ActiveExcavation,
        AutomationLogistics,
        CampEconomy,
        ArchaeologicalIntuition,
    }

    /// <summary>
    /// What a node improves. Interpretation of EffectPerLevel depends on this
    /// (e.g. ClickDamageMultiplier 0.05 = +5% per level; OfflineCapHours 1 = +1h per level).
    /// </summary>
    public enum ResearchEffectType
    {
        // Branch 1: Active Excavation
        ClickDamageMultiplier,
        CritChance,
        CritMultiplier,
        FeverDuration,

        // Branch 2: Automation Logistics
        AssistantDpsMultiplier,
        AssistantSynergy,
        OfflineEfficiency,

        // Branch 3: Camp Economy
        CoinDropMultiplier,
        OfflineCapHours,
        UpgradeCostReduction,

        // Branch 4: Archaeological Intuition
        ArtifactDropRate,
        ExpeditionSpeed,
        MuseumBonus,
    }

    [Serializable]
    public struct ResearchPrerequisite
    {
        public ResearchNodeSO Node;
        [Min(1)] public int RequiredLevel;
    }

    /// <summary>
    /// One node of the prerequisite-based Research Tree (GDD §7). Visibility rule:
    /// unlocked = bright, locked-but-reachable = grayed-out readable, Stage 15 = "?"
    /// silhouette until Stage 14 is fully maxed. That logic reads Stage + Prerequisites.
    /// </summary>
    [CreateAssetMenu(fileName = "Research_", menuName = "Crumble/Research Node")]
    public sealed class ResearchNodeSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"research_sharper_brushes\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public ResearchBranch Branch;
        [Range(1, 15)] public int Stage = 1;

        [Header("Cost in KP (Cost = BaseKpCost × Growth^Level)")]
        public double BaseKpCost = 1;
        public double CostGrowthFactor = 2;
        [Min(1)] public int MaxLevel = 5;

        [Header("Effect")]
        public ResearchEffectType EffectType;
        [Tooltip("Effect magnitude gained per level; meaning depends on EffectType.")]
        public double EffectPerLevel = 0.05;

        [Header("Prerequisites (ALL must be met to unlock)")]
        public ResearchPrerequisite[] Prerequisites = Array.Empty<ResearchPrerequisite>();
    }
}
