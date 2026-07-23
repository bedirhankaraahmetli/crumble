using UnityEngine;

namespace Crumble.Data
{
    public enum MuseumBonusType
    {
        CoinMultiplier,
        ClickDamageMultiplier,
        DpsMultiplier,
    }

    /// <summary>
    /// A museum collection (GDD §6): owning every member artifact completes the set and
    /// grants a permanent passive bonus (amplified by MuseumBonus research).
    /// </summary>
    [CreateAssetMenu(fileName = "MuseumSet_", menuName = "Crumble/Museum Set")]
    public sealed class MuseumSetSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"set_fossil_record\".")]
        public string Id;
        public string DisplayName;

        [Header("Members")]
        public ArtifactSO[] Artifacts;

        [Header("Completion bonus")]
        public MuseumBonusType BonusType;
        [Tooltip("Additive bonus when complete (0.25 = +25%).")]
        public double BonusAmount = 0.25;
    }
}
