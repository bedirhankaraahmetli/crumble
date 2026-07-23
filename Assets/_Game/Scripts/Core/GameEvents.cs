using System;
using BreakInfinity;
using Crumble.Data;

namespace Crumble.Core
{
    /// <summary>
    /// Static event bus decoupling game logic from UI. Managers raise; UI subscribes.
    /// UI must unsubscribe in OnDisable/OnDestroy. Managers never reference UI types.
    /// </summary>
    public static class GameEvents
    {
        // ---- Currencies ----
        /// <summary>New Antique Coins total.</summary>
        public static event Action<BigDouble> CoinsChanged;

        /// <summary>New Knowledge Points total.</summary>
        public static event Action<BigDouble> KnowledgePointsChanged;

        /// <summary>New Time Crystals total.</summary>
        public static event Action<BigDouble> TimeCrystalsChanged;

        // ---- Excavation ----
        /// <summary>One damage application (amount, click/crit flags, press position).</summary>
        public static event Action<DamageInfo> TabletDamaged;

        /// <summary>(current HP, max HP). Drives the HP bar and crack-state visuals.</summary>
        public static event Action<BigDouble, BigDouble> TabletHpChanged;

        /// <summary>(material id, stage). Fired when a tablet reaches 0 HP.</summary>
        public static event Action<string, int> TabletShattered;

        /// <summary>
        /// (material, stage, isMilestone). Fired when a new tablet spawns (boot, or after
        /// a shatter). Milestone = the material's final-stage "boss" tablet.
        /// </summary>
        public static event Action<TabletMaterialSO, int, bool> TabletChanged;

        // ---- Upgrades ----
        /// <summary>(tool id, new level). Fired after a successful tool purchase.</summary>
        public static event Action<string, int> ToolLevelChanged;

        /// <summary>(assistant id, new level). Fired after a successful assistant purchase.</summary>
        public static event Action<string, int> AssistantLevelChanged;

        /// <summary>(total click damage, total passive DPS). Fired after load and any purchase.</summary>
        public static event Action<BigDouble, BigDouble> StatsChanged;

        // ---- Research ----
        /// <summary>(node id, new level). Fired after a successful research purchase.</summary>
        public static event Action<string, int> ResearchNodeChanged;

        // ---- Museum & artifacts ----
        /// <summary>An artifact entered the museum (tablet drop or expedition haul).</summary>
        public static event Action<ArtifactSO> ArtifactDropped;

        /// <summary>Museum contents / set bonuses changed.</summary>
        public static event Action MuseumChanged;

        // ---- Expeditions ----
        /// <summary>(mission, end unix seconds). A team departed.</summary>
        public static event Action<ExpeditionSO, long> ExpeditionStarted;

        /// <summary>(coins, artifact or null). The haul was collected.</summary>
        public static event Action<BigDouble, ArtifactSO> ExpeditionCollected;

        // ---- Environment ----
        /// <summary>True when night falls (idle gains boosted), false at dawn.</summary>
        public static event Action<bool> NightChanged;

        public static event Action SandstormStarted;

        /// <summary>Swipe-clearing progress 0..1.</summary>
        public static event Action<float> SandstormProgress;

        /// <summary>(coin reward). The storm was swiped clean in time.</summary>
        public static event Action<BigDouble> SandstormCleared;

        public static event Action SandstormEnded;

        // ---- Fever Mode ----
        /// <summary>Bar fill 0..1 (charge while tapping; remaining-time fraction during fever).</summary>
        public static event Action<float> FeverProgressChanged;

        /// <summary>(duration seconds). The combo bar filled — massive click multiplier begins.</summary>
        public static event Action<double> FeverStarted;

        public static event Action FeverEnded;

        // ---- Offline progress ----
        /// <summary>(pending coins, credited seconds). Fired once at boot when the player returns.</summary>
        public static event Action<BigDouble, double> OfflineEarningsReady;

        // ---- Prestige ----
        /// <summary>(KP gained). Fired after a Standard Prestige completes (state already reset).</summary>
        public static event Action<BigDouble> Prestige;

        // ---- Lifecycle ----
        /// <summary>Fired once the save file has been loaded (or a fresh one created).</summary>
        public static event Action<SaveData> GameLoaded;

        /// <summary>Fired after every successful save to disk.</summary>
        public static event Action GameSaved;

        public static void RaiseCoinsChanged(BigDouble total) => CoinsChanged?.Invoke(total);
        public static void RaiseKnowledgePointsChanged(BigDouble total) => KnowledgePointsChanged?.Invoke(total);
        public static void RaiseTimeCrystalsChanged(BigDouble total) => TimeCrystalsChanged?.Invoke(total);
        public static void RaiseTabletDamaged(in DamageInfo info) => TabletDamaged?.Invoke(info);
        public static void RaiseTabletHpChanged(BigDouble current, BigDouble max) => TabletHpChanged?.Invoke(current, max);
        public static void RaiseTabletShattered(string materialId, int stage) => TabletShattered?.Invoke(materialId, stage);
        public static void RaiseTabletChanged(TabletMaterialSO material, int stage, bool isMilestone) => TabletChanged?.Invoke(material, stage, isMilestone);
        public static void RaiseToolLevelChanged(string id, int level) => ToolLevelChanged?.Invoke(id, level);
        public static void RaiseAssistantLevelChanged(string id, int level) => AssistantLevelChanged?.Invoke(id, level);
        public static void RaiseStatsChanged(BigDouble clickDamage, BigDouble dps) => StatsChanged?.Invoke(clickDamage, dps);
        public static void RaiseResearchNodeChanged(string id, int level) => ResearchNodeChanged?.Invoke(id, level);
        public static void RaiseArtifactDropped(ArtifactSO artifact) => ArtifactDropped?.Invoke(artifact);
        public static void RaiseMuseumChanged() => MuseumChanged?.Invoke();
        public static void RaiseExpeditionStarted(ExpeditionSO expedition, long endUnixUtc) => ExpeditionStarted?.Invoke(expedition, endUnixUtc);
        public static void RaiseExpeditionCollected(BigDouble coins, ArtifactSO artifact) => ExpeditionCollected?.Invoke(coins, artifact);
        public static void RaiseNightChanged(bool isNight) => NightChanged?.Invoke(isNight);
        public static void RaiseSandstormStarted() => SandstormStarted?.Invoke();
        public static void RaiseSandstormProgress(float progress) => SandstormProgress?.Invoke(progress);
        public static void RaiseSandstormCleared(BigDouble reward) => SandstormCleared?.Invoke(reward);
        public static void RaiseSandstormEnded() => SandstormEnded?.Invoke();
        public static void RaiseFeverProgressChanged(float progress) => FeverProgressChanged?.Invoke(progress);
        public static void RaiseFeverStarted(double durationSeconds) => FeverStarted?.Invoke(durationSeconds);
        public static void RaiseFeverEnded() => FeverEnded?.Invoke();
        public static void RaiseOfflineEarningsReady(BigDouble coins, double seconds) => OfflineEarningsReady?.Invoke(coins, seconds);
        public static void RaisePrestige(BigDouble kpGained) => Prestige?.Invoke(kpGained);
        public static void RaiseGameLoaded(SaveData data) => GameLoaded?.Invoke(data);
        public static void RaiseGameSaved() => GameSaved?.Invoke();
    }
}
