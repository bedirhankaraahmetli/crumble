using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Standard Prestige (GDD §2/§9): trade the current run for Knowledge Points.
    /// KP = floor(∛(lifetime coins this run / divisor)) — computed on demand from
    /// GameMath, never cached. Prestige wipes coins, tools, assistants and tablet
    /// progress; KP (and later research + Time Crystals) persist. The wipe reuses the
    /// GameLoaded rebind path so every manager and UI panel resets through one flow.
    /// </summary>
    [DefaultExecutionOrder(-58)]
    public sealed class PrestigeManager : Singleton<PrestigeManager>
    {
        [Header("Balance (GDD §9: KP = floor(∛(lifetimeCoins / divisor)))")]
        [Tooltip("Lifetime coins this run needed for the first Knowledge Point.")]
        [SerializeField] private double kpDivisor = 1e9;

        private SaveData _data;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data) => _data = data;

        /// <summary>KP a prestige would award right now.</summary>
        public BigDouble PendingKnowledge => _data == null
            ? BigDouble.Zero
            : GameMath.PrestigeKnowledge(_data.Currencies.LifetimeCoinsThisRun, kpDivisor);

        public bool CanPrestige => PendingKnowledge >= 1;

        public bool Prestige()
        {
            if (_data == null || !CanPrestige)
            {
                return false;
            }

            var kp = PendingKnowledge;
            CurrencyManager.Instance.AddKnowledge(kp);

            // Wipe the run. KP stays; research_tree_state and Time Crystals (future
            // systems) are deliberately untouched — only Hard Prestige clears those.
            _data.Currencies.AntiqueCoins = BigDouble.Zero;
            _data.Currencies.LifetimeCoinsThisRun = BigDouble.Zero;
            _data.Upgrades.Tools.Clear();
            _data.Upgrades.Assistants.Clear();
            _data.CurrentExcavation.TabletId = "";
            _data.CurrentExcavation.Stage = 0;
            _data.CurrentExcavation.RemainingHp = BigDouble.Zero;

            GameEvents.RaiseGameLoaded(_data); // full rebind: fresh tablet, zeroed stats, UI refresh
            GameEvents.RaisePrestige(kp);
            SaveManager.Instance.Save();       // a prestige is a checkpoint — persist immediately
            return true;
        }
    }
}
