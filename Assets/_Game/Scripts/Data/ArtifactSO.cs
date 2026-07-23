using UnityEngine;

namespace Crumble.Data
{
    /// <summary>
    /// A museum artifact (GDD §6): dropped rarely by shattered tablets or brought home by
    /// expeditions. Belongs to a set (MuseumSetSO); completing sets grants passive bonuses.
    /// </summary>
    [CreateAssetMenu(fileName = "Artifact_", menuName = "Crumble/Artifact")]
    public sealed class ArtifactSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, e.g. \"artifact_ancient_fern\". NEVER rename after shipping.")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Drops")]
        [Tooltip("Relative weight among artifacts when a drop occurs.")]
        public double DropWeight = 1;

        [Header("Visuals")]
        public Sprite Icon;
    }
}
