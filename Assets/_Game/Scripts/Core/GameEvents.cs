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
        public static void RaiseGameLoaded(SaveData data) => GameLoaded?.Invoke(data);
        public static void RaiseGameSaved() => GameSaved?.Invoke();
    }
}
