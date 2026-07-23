using System.Collections.Generic;
using BreakInfinity;
using Newtonsoft.Json;

namespace Crumble.Core
{
    /// <summary>
    /// Root save model (GDD §10). Stores runtime STATE only, keyed by stable string ids —
    /// never content definitions (those live in ScriptableObjects), which is what lets
    /// saves survive balance changes and game updates.
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        [JsonProperty("version")]
        public int Version = CurrentVersion;

        /// <summary>Unix seconds (UTC) of the last save; drives offline progress on next launch.</summary>
        [JsonProperty("last_login_timestamp")]
        public long LastLoginUnixUtc;

        [JsonProperty("currencies")]
        public CurrencyState Currencies = new CurrencyState();

        [JsonProperty("current_excavation")]
        public ExcavationState CurrentExcavation = new ExcavationState();

        [JsonProperty("upgrades_state")]
        public UpgradesState Upgrades = new UpgradesState();

        /// <summary>Research node id → current level.</summary>
        [JsonProperty("research_tree_state")]
        public Dictionary<string, int> ResearchTree = new Dictionary<string, int>();

        /// <summary>Artifact id → owned count.</summary>
        [JsonProperty("museum_state")]
        public Dictionary<string, int> Museum = new Dictionary<string, int>();

        [JsonProperty("expedition_state")]
        public ExpeditionSaveState Expedition = new ExpeditionSaveState();
    }

    public sealed class ExpeditionSaveState
    {
        /// <summary>Empty when no expedition is running.</summary>
        [JsonProperty("expedition_id")]
        public string ExpeditionId = "";

        /// <summary>Unix seconds (UTC) when the active expedition finishes.</summary>
        [JsonProperty("end_timestamp")]
        public long EndUnixUtc;
    }

    public sealed class CurrencyState
    {
        [JsonProperty("antique_coins")]
        public BigDouble AntiqueCoins = BigDouble.Zero;

        [JsonProperty("knowledge_points")]
        public BigDouble KnowledgePoints = BigDouble.Zero;

        [JsonProperty("time_crystals")]
        public BigDouble TimeCrystals = BigDouble.Zero;

        /// <summary>Coins earned since the last Prestige; feeds the cube-root KP formula.</summary>
        [JsonProperty("lifetime_coins_this_run")]
        public BigDouble LifetimeCoinsThisRun = BigDouble.Zero;
    }

    public sealed class ExcavationState
    {
        [JsonProperty("tablet_id")]
        public string TabletId = "";

        [JsonProperty("stage")]
        public int Stage;

        [JsonProperty("remaining_hp")]
        public BigDouble RemainingHp = BigDouble.Zero;
    }

    public sealed class UpgradesState
    {
        /// <summary>Tool id → level.</summary>
        [JsonProperty("tools")]
        public Dictionary<string, int> Tools = new Dictionary<string, int>();

        /// <summary>Assistant id → level.</summary>
        [JsonProperty("assistants")]
        public Dictionary<string, int> Assistants = new Dictionary<string, int>();
    }
}
