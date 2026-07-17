using UnityEngine;

namespace Crumble.Data
{
    public enum TabletTier
    {
        Surface = 1,
        Bedrock = 2,
        Gemstones = 3,
        Mythological = 4,
        Cosmic = 5,
    }

    /// <summary>
    /// Content definition for one of the 20 tablet materials (GDD §4). Balance is authored
    /// here in the Inspector; runtime state (remaining HP, stage) lives in SaveData.
    /// Base values are doubles — runtime math converts to BigDouble.
    /// </summary>
    [CreateAssetMenu(fileName = "Tablet_", menuName = "Crumble/Tablet Material")]
    public sealed class TabletMaterialSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"tablet_dried_mud\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        public TabletTier Tier = TabletTier.Surface;
        [Tooltip("Global progression order across all 20 materials (0 = Dried Mud).")]
        public int OrderIndex;

        [Header("Balance")]
        public double BaseHp = 10;
        [Tooltip("Coins awarded on shatter, before multipliers.")]
        public double BreakReward = 5;

        [Header("Visuals")]
        [Tooltip("5 crack states: Full, Little Cracked, Cracked, Heavily Cracked, Shattered.")]
        public Sprite[] CrackStates = new Sprite[5];
    }
}
