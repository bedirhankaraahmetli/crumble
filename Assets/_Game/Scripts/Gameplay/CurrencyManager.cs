using BreakInfinity;
using Crumble.Core;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Owns all currency state (a live view over SaveData.Currencies). All gains and
    /// spends go through here so events fire and lifetime totals stay correct.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class CurrencyManager : Singleton<CurrencyManager>
    {
        private CurrencyState _state;

        public BigDouble AntiqueCoins => _state != null ? _state.AntiqueCoins : BigDouble.Zero;
        public BigDouble KnowledgePoints => _state != null ? _state.KnowledgePoints : BigDouble.Zero;
        public BigDouble TimeCrystals => _state != null ? _state.TimeCrystals : BigDouble.Zero;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            _state = data.Currencies;
            GameEvents.RaiseCoinsChanged(_state.AntiqueCoins);
            GameEvents.RaiseKnowledgePointsChanged(_state.KnowledgePoints);
            GameEvents.RaiseTimeCrystalsChanged(_state.TimeCrystals);
        }

        public void AddCoins(BigDouble amount)
        {
            if (_state == null || amount <= 0)
            {
                return;
            }

            _state.AntiqueCoins += amount;
            _state.LifetimeCoinsThisRun += amount;
            GameEvents.RaiseCoinsChanged(_state.AntiqueCoins);
        }

        public bool TrySpendCoins(BigDouble cost)
        {
            if (_state == null || cost < 0 || _state.AntiqueCoins < cost)
            {
                return false;
            }

            _state.AntiqueCoins -= cost;
            GameEvents.RaiseCoinsChanged(_state.AntiqueCoins);
            return true;
        }

        public void AddKnowledge(BigDouble amount)
        {
            if (_state == null || amount <= 0)
            {
                return;
            }

            _state.KnowledgePoints += amount;
            GameEvents.RaiseKnowledgePointsChanged(_state.KnowledgePoints);
        }

        public bool TrySpendKnowledge(BigDouble cost)
        {
            if (_state == null || cost < 0 || _state.KnowledgePoints < cost)
            {
                return false;
            }

            _state.KnowledgePoints -= cost;
            GameEvents.RaiseKnowledgePointsChanged(_state.KnowledgePoints);
            return true;
        }

        public void AddTimeCrystals(BigDouble amount)
        {
            if (_state == null || amount <= 0)
            {
                return;
            }

            _state.TimeCrystals += amount;
            GameEvents.RaiseTimeCrystalsChanged(_state.TimeCrystals);
        }

        public bool TrySpendTimeCrystals(BigDouble cost)
        {
            if (_state == null || cost < 0 || _state.TimeCrystals < cost)
            {
                return false;
            }

            _state.TimeCrystals -= cost;
            GameEvents.RaiseTimeCrystalsChanged(_state.TimeCrystals);
            return true;
        }
    }
}
