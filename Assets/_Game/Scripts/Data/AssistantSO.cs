using UnityEngine;

namespace Crumble.Data
{
    /// <summary>
    /// Idle Assistant definition (GDD §5): passive DPS. 12 assistants from Water Dripper
    /// to Cosmic Watcher. Cost curve per GDD §9 with ~1.15 growth.
    /// </summary>
    [CreateAssetMenu(fileName = "Assistant_", menuName = "Crumble/Idle Assistant")]
    public sealed class AssistantSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"assistant_water_dripper\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        [Tooltip("Progression order (0 = Water Dripper).")]
        public int OrderIndex;

        [Header("Balance (Cost = BaseCost × Growth^Level)")]
        public double BaseCost = 25;
        public double GrowthFactor = 1.15;
        [Tooltip("Passive damage per second contributed per level, before multipliers.")]
        public double BaseDpsPerLevel = 1;

        [Header("Visuals")]
        public Sprite Icon;
    }
}
