using System.Collections.Generic;
using Crumble.Core;
using Crumble.Data;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Museum & Artifacts (GDD §6): shattered tablets have a small chance (× ArtifactDropRate
    /// research) to drop an artifact; expeditions bring them home too. Owned artifacts live
    /// in SaveData.Museum (id → count); completed sets grant passive multipliers, amplified
    /// by MuseumBonus research. Museum contents survive Standard Prestige.
    /// </summary>
    [DefaultExecutionOrder(-48)]
    public sealed class MuseumManager : Singleton<MuseumManager>
    {
        [Header("Content")]
        [SerializeField] private ArtifactSO[] artifacts;
        [SerializeField] private MuseumSetSO[] sets;

        [Header("Balance")]
        [Tooltip("Chance per shattered tablet to drop an artifact, before research.")]
        [SerializeField] private double baseArtifactDropChance = 0.015;

        private Dictionary<string, int> _state;

        public IReadOnlyList<ArtifactSO> Artifacts => artifacts;
        public IReadOnlyList<MuseumSetSO> Sets => sets;

        // set-bonus aggregates (1 = no bonus)
        public double CoinMultiplier { get; private set; } = 1;
        public double ClickMultiplier { get; private set; } = 1;
        public double DpsMultiplier { get; private set; } = 1;

        private void OnEnable()
        {
            GameEvents.GameLoaded += OnGameLoaded;
            GameEvents.TabletShattered += OnTabletShattered;
        }

        private void OnDisable()
        {
            GameEvents.GameLoaded -= OnGameLoaded;
            GameEvents.TabletShattered -= OnTabletShattered;
        }

        private void OnGameLoaded(SaveData data)
        {
            _state = data.Museum;
            RecomputeBonuses();
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.RefreshStats(); // museum binds after UpgradeManager's own load pass
            }
        }

        public int GetCount(ArtifactSO artifact)
        {
            return _state != null && _state.TryGetValue(artifact.Id, out var count) ? count : 0;
        }

        public bool IsOwned(ArtifactSO artifact) => GetCount(artifact) > 0;

        public bool IsSetComplete(MuseumSetSO set)
        {
            if (set.Artifacts == null || set.Artifacts.Length == 0)
            {
                return false;
            }

            foreach (var artifact in set.Artifacts)
            {
                if (artifact == null || !IsOwned(artifact))
                {
                    return false;
                }
            }

            return true;
        }

        public int OwnedCountInSet(MuseumSetSO set)
        {
            var owned = 0;
            foreach (var artifact in set.Artifacts)
            {
                if (artifact != null && IsOwned(artifact))
                {
                    owned++;
                }
            }

            return owned;
        }

        private void OnTabletShattered(string materialId, int stage)
        {
            if (_state == null || artifacts == null || artifacts.Length == 0)
            {
                return;
            }

            var research = ResearchManager.Instance;
            var chance = baseArtifactDropChance * (1 + (research != null ? research.ArtifactDropRateBonus : 0));
            if (Random.value < chance)
            {
                var pick = PickRandomArtifact();
                if (pick != null)
                {
                    GrantArtifact(pick);
                }
            }
        }

        /// <summary>Weighted pick, preferring artifacts the player doesn't own yet.</summary>
        public ArtifactSO PickRandomArtifact()
        {
            if (artifacts == null || artifacts.Length == 0)
            {
                return null;
            }

            var pool = artifacts;
            var unownedWeight = 0.0;
            foreach (var artifact in artifacts)
            {
                if (!IsOwned(artifact))
                {
                    unownedWeight += artifact.DropWeight;
                }
            }

            var preferUnowned = unownedWeight > 0;
            var totalWeight = 0.0;
            foreach (var artifact in pool)
            {
                if (!preferUnowned || !IsOwned(artifact))
                {
                    totalWeight += artifact.DropWeight;
                }
            }

            var roll = Random.value * totalWeight;
            foreach (var artifact in pool)
            {
                if (preferUnowned && IsOwned(artifact))
                {
                    continue;
                }

                roll -= artifact.DropWeight;
                if (roll <= 0)
                {
                    return artifact;
                }
            }

            return pool[pool.Length - 1];
        }

        public void GrantArtifact(ArtifactSO artifact)
        {
            if (_state == null || artifact == null)
            {
                return;
            }

            _state[artifact.Id] = GetCount(artifact) + 1;
            RecomputeBonuses();
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.RefreshStats();
            }

            GameEvents.RaiseArtifactDropped(artifact);
            GameEvents.RaiseMuseumChanged();
        }

        private void RecomputeBonuses()
        {
            double coin = 0, click = 0, dps = 0;
            var research = ResearchManager.Instance;
            var amplifier = 1 + (research != null ? research.MuseumBonusMultiplier : 0);

            if (sets != null && _state != null)
            {
                foreach (var set in sets)
                {
                    if (set == null || !IsSetComplete(set))
                    {
                        continue;
                    }

                    var bonus = set.BonusAmount * amplifier;
                    switch (set.BonusType)
                    {
                        case MuseumBonusType.CoinMultiplier:
                            coin += bonus;
                            break;
                        case MuseumBonusType.ClickDamageMultiplier:
                            click += bonus;
                            break;
                        case MuseumBonusType.DpsMultiplier:
                            dps += bonus;
                            break;
                    }
                }
            }

            CoinMultiplier = 1 + coin;
            ClickMultiplier = 1 + click;
            DpsMultiplier = 1 + dps;
        }
    }
}
