using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Owns the excavation loop: current tablet material + stage + HP (a live view over
    /// SaveData.CurrentExcavation). Applies damage, pays out coins, advances stages.
    /// Pure logic — visuals live in TabletView, which subscribes to GameEvents.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TabletManager : Singleton<TabletManager>
    {
        [Header("Content (ordered by progression tier)")]
        [SerializeField] private TabletMaterialSO[] materials;

        [Header("Balance")]
        [Tooltip("Global stages spent on each material before advancing to the next.")]
        [SerializeField] private int stagesPerMaterial = 10;
        [Tooltip("Coins granted per point of damage dealt (GDD §2: earn per tap/tick).")]
        [SerializeField] private double coinPerDamageRatio = 0.02;

        private const float DpsTickInterval = 0.25f;

        private ExcavationState _state;
        private float _dpsTimer;

        public TabletMaterialSO CurrentMaterial { get; private set; }
        public BigDouble MaxHp { get; private set; }
        public BigDouble CurrentHp => _state != null ? _state.RemainingHp : BigDouble.Zero;
        public int Stage => _state != null ? _state.Stage : 0;

        /// <summary>True while the current tablet is a material's final-stage "boss".</summary>
        public bool IsMilestone { get; private set; }

        /// <summary>Aggregated by UpgradeManager (base + tools); research multiplies in Step 5.</summary>
        public BigDouble ClickDamage =>
            UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalClickDamage : BigDouble.One;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            _state = data.CurrentExcavation;
            if (materials == null || materials.Length == 0)
            {
                Debug.LogError("[TabletManager] No tablet materials assigned.");
                return;
            }

            CurrentMaterial = MaterialForStage(_state.Stage);
            IsMilestone = GameMath.IsMilestoneStage(_state.Stage, stagesPerMaterial, materials.Length);
            MaxHp = MaxHpForStage(_state.Stage);

            var savedHpInvalid = _state.RemainingHp <= 0 || _state.RemainingHp > MaxHp;
            if (_state.TabletId != CurrentMaterial.Id || savedHpInvalid)
            {
                SpawnTablet(_state.Stage); // fresh save, or content changed since last save
            }
            else
            {
                GameEvents.RaiseTabletChanged(CurrentMaterial, _state.Stage, IsMilestone);
                GameEvents.RaiseTabletHpChanged(_state.RemainingHp, MaxHp);
            }
        }

        /// <summary>Entry point for tap input. UI/input may call this; logic stays here.</summary>
        public void Tap()
        {
            ApplyDamage(ClickDamage, fromClick: true);
        }

        /// <summary>Assistant DPS lands in fixed ticks so HP/coin events stay bounded.</summary>
        private void Update()
        {
            if (_state == null)
            {
                return;
            }

            var dps = UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalDps : BigDouble.Zero;
            if (dps <= 0)
            {
                _dpsTimer = 0f;
                return;
            }

            _dpsTimer += Time.deltaTime;
            while (_dpsTimer >= DpsTickInterval)
            {
                _dpsTimer -= DpsTickInterval;
                ApplyDamage(dps * DpsTickInterval, fromClick: false);
            }
        }

        public void ApplyDamage(BigDouble damage, bool fromClick)
        {
            if (_state == null || CurrentMaterial == null || damage <= 0)
            {
                return;
            }

            _state.RemainingHp -= damage;
            CurrencyManager.Instance.AddCoins(GameMath.CoinsForDamage(damage, coinPerDamageRatio));
            GameEvents.RaiseTabletDamaged(damage, fromClick);

            if (_state.RemainingHp <= 0)
            {
                Shatter();
            }
            else
            {
                GameEvents.RaiseTabletHpChanged(_state.RemainingHp, MaxHp);
            }
        }

        private void Shatter()
        {
            var stageInMaterial = GameMath.StageWithinMaterial(_state.Stage, stagesPerMaterial, materials.Length);
            var reward = GameMath.TabletReward(
                CurrentMaterial.BreakReward, CurrentMaterial.RewardGrowthFactor, stageInMaterial,
                IsMilestone, CurrentMaterial.MilestoneRewardMultiplier);
            CurrencyManager.Instance.AddCoins(reward);

            GameEvents.RaiseTabletHpChanged(BigDouble.Zero, MaxHp);
            GameEvents.RaiseTabletShattered(CurrentMaterial.Id, _state.Stage);

            SpawnTablet(_state.Stage + 1);
        }

        private void SpawnTablet(int stage)
        {
            _state.Stage = stage;
            CurrentMaterial = MaterialForStage(stage);
            IsMilestone = GameMath.IsMilestoneStage(stage, stagesPerMaterial, materials.Length);
            MaxHp = MaxHpForStage(stage);
            _state.TabletId = CurrentMaterial.Id;
            _state.RemainingHp = MaxHp;

            GameEvents.RaiseTabletChanged(CurrentMaterial, stage, IsMilestone);
            GameEvents.RaiseTabletHpChanged(_state.RemainingHp, MaxHp);
        }

        private TabletMaterialSO MaterialForStage(int stage)
        {
            return materials[GameMath.MaterialIndexForStage(stage, stagesPerMaterial, materials.Length)];
        }

        private BigDouble MaxHpForStage(int stage)
        {
            var material = MaterialForStage(stage);
            var stageInMaterial = GameMath.StageWithinMaterial(stage, stagesPerMaterial, materials.Length);
            var isMilestone = GameMath.IsMilestoneStage(stage, stagesPerMaterial, materials.Length);
            return GameMath.TabletHp(
                material.BaseHp, material.DifficultyFactor, stageInMaterial,
                isMilestone, material.MilestoneHpMultiplier);
        }
    }
}
