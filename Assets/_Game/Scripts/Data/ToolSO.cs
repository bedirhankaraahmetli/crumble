using UnityEngine;

namespace Crumble.Data
{
    /// <summary>
    /// Active Tool definition (GDD §5): boosts click damage. 12 tools from Dusting Brush
    /// to Time Accelerator. Cost curve per GDD §9 with ~1.07 growth.
    /// </summary>
    [CreateAssetMenu(fileName = "Tool_", menuName = "Crumble/Active Tool")]
    public sealed class ToolSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"tool_dusting_brush\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        [Tooltip("Progression order (0 = Dusting Brush).")]
        public int OrderIndex;

        [Header("Balance (Cost = BaseCost × Growth^Level)")]
        public double BaseCost = 10;
        public double GrowthFactor = 1.07;
        [Tooltip("Click damage contributed per level, before multipliers.")]
        public double BaseDamagePerLevel = 1;

        [Header("Visuals")]
        public Sprite Icon;
    }
}
