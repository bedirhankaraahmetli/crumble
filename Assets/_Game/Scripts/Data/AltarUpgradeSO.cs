using UnityEngine;

namespace Crumble.Data
{
    /// <summary>What a Cosmic Altar upgrade multiplies. All of these compound per level.</summary>
    public enum AltarEffectType
    {
        ClickDamage,
        AssistantDps,
        CoinGain,
        KnowledgeGain,
    }

    /// <summary>
    /// One Cosmic Altar upgrade (GDD §8): bought with Time Crystals, no level cap, and
    /// the effect compounds (MultiplierPerLevel ^ level) — the endgame's "infinite
    /// multipliers". Definitions live here; levels live in SaveData.Cosmic.AltarUpgrades.
    /// </summary>
    [CreateAssetMenu(fileName = "Altar_", menuName = "Crumble/Altar Upgrade")]
    public sealed class AltarUpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"altar_chrono_hammer\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Cost in Time Crystals (Cost = Base × Growth^Level, no level cap)")]
        public double BaseTimeCrystalCost = 5;
        public double CostGrowthFactor = 1.8;

        [Header("Effect (total = MultiplierPerLevel ^ level — compounds forever)")]
        public AltarEffectType EffectType;
        [Min(1f)] public double MultiplierPerLevel = 1.5;
    }
}
