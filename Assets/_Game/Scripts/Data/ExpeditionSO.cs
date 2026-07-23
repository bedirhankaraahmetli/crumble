using UnityEngine;

namespace Crumble.Data
{
    /// <summary>
    /// An Expedition Tent mission (GDD §6): a real-time timer that pays a large coin sum
    /// (scaled to the player's DPS economy) and often an artifact. ExpeditionSpeed research
    /// shortens the wait.
    /// </summary>
    [CreateAssetMenu(fileName = "Expedition_", menuName = "Crumble/Expedition")]
    public sealed class ExpeditionSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"expedition_short_scout\".")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Balance")]
        [Tooltip("Real-time duration before ExpeditionSpeed research.")]
        public double BaseDurationHours = 4;
        [Tooltip("Coin reward equals this many seconds of full DPS income (with a floor).")]
        public double RewardDpsSeconds = 3600;
        [Tooltip("Reward floor: this many times the current tablet's shatter reward.")]
        public double MinRewardTabletMultiple = 25;
        [Tooltip("Chance to bring home an artifact (before ArtifactDropRate research).")]
        [Range(0f, 1f)] public float ArtifactChance = 0.5f;
    }
}
