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
        /// <summary>(damage dealt, was it a click). Drives floating damage numbers.</summary>
        public static event Action<BigDouble, bool> TabletDamaged;

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
        public static void RaiseTabletDamaged(BigDouble damage, bool fromClick) => TabletDamaged?.Invoke(damage, fromClick);
        public static void RaiseTabletHpChanged(BigDouble current, BigDouble max) => TabletHpChanged?.Invoke(current, max);
        public static void RaiseTabletShattered(string materialId, int stage) => TabletShattered?.Invoke(materialId, stage);
        public static void RaiseTabletChanged(TabletMaterialSO material, int stage, bool isMilestone) => TabletChanged?.Invoke(material, stage, isMilestone);
        public static void RaiseToolLevelChanged(string id, int level) => ToolLevelChanged?.Invoke(id, level);
        public static void RaiseAssistantLevelChanged(string id, int level) => AssistantLevelChanged?.Invoke(id, level);
        public static void RaiseStatsChanged(BigDouble clickDamage, BigDouble dps) => StatsChanged?.Invoke(clickDamage, dps);
        public static void RaiseResearchNodeChanged(string id, int level) => ResearchNodeChanged?.Invoke(id, level);
        public static void RaiseOfflineEarningsReady(BigDouble coins, double seconds) => OfflineEarningsReady?.Invoke(coins, seconds);
        public static void RaisePrestige(BigDouble kpGained) => Prestige?.Invoke(kpGained);
        public static void RaiseGameLoaded(SaveData data) => GameLoaded?.Invoke(data);
        public static void RaiseGameSaved() => GameSaved?.Invoke();
    }
}
