using Crumble.Core;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Fever Mode (GDD §6): rapid consecutive taps fill the combo bar; when full, click
    /// damage is massively multiplied for a duration (base + FeverDuration research).
    /// While charging, the bar decays after a short idle grace. During fever the bar
    /// shows remaining time and taps don't recharge it.
    /// </summary>
    [DefaultExecutionOrder(-52)]
    public sealed class FeverManager : Singleton<FeverManager>
    {
        [Header("Balance (GDD §6)")]
        [Tooltip("Consecutive taps needed to fill the combo bar.")]
        [SerializeField] private int tapsToFill = 25;
        [Tooltip("Click damage multiplier while fever is active.")]
        [SerializeField] private double feverMultiplier = 5;
        [Tooltip("Fever duration in seconds before FeverDuration research bonuses.")]
        [SerializeField] private double baseFeverSeconds = 10;
        [Tooltip("Seconds without a tap before the charge starts decaying.")]
        [SerializeField] private float comboIdleGrace = 0.6f;
        [Tooltip("Charge fraction lost per second once decaying.")]
        [SerializeField] private float comboDecayPerSecond = 0.6f;

        public bool IsFeverActive { get; private set; }
        public double FeverMultiplier => feverMultiplier;

        private float _progress;
        private float _sinceLastTap;
        private double _feverRemaining;
        private double _feverDuration;

        private void OnEnable() => GameEvents.GameLoaded += OnGameLoaded;
        private void OnDisable() => GameEvents.GameLoaded -= OnGameLoaded;

        private void OnGameLoaded(SaveData data)
        {
            // fever never survives a load/prestige/reset
            var wasActive = IsFeverActive;
            IsFeverActive = false;
            _progress = 0f;
            _feverRemaining = 0;
            GameEvents.RaiseFeverProgressChanged(0f);
            if (wasActive)
            {
                GameEvents.RaiseFeverEnded();
            }
        }

        /// <summary>Called by TabletManager for every click tap.</summary>
        public void RegisterTap()
        {
            if (IsFeverActive)
            {
                return;
            }

            _sinceLastTap = 0f;
            _progress += 1f / Mathf.Max(1, tapsToFill);
            if (_progress >= 1f)
            {
                StartFever();
            }
            else
            {
                GameEvents.RaiseFeverProgressChanged(_progress);
            }
        }

        private void StartFever()
        {
            IsFeverActive = true;
            _progress = 0f;
            var research = ResearchManager.Instance;
            _feverDuration = baseFeverSeconds + (research != null ? research.FeverDurationBonusSeconds : 0);
            _feverRemaining = _feverDuration;
            GameEvents.RaiseFeverStarted(_feverDuration);
            GameEvents.RaiseFeverProgressChanged(1f);
            Haptics.Impact();
        }

        private void Update()
        {
            if (IsFeverActive)
            {
                _feverRemaining -= Time.deltaTime;
                if (_feverRemaining <= 0)
                {
                    IsFeverActive = false;
                    GameEvents.RaiseFeverProgressChanged(0f);
                    GameEvents.RaiseFeverEnded();
                }
                else
                {
                    GameEvents.RaiseFeverProgressChanged((float)(_feverRemaining / _feverDuration));
                }

                return;
            }

            if (_progress <= 0f)
            {
                return;
            }

            _sinceLastTap += Time.deltaTime;
            if (_sinceLastTap >= comboIdleGrace)
            {
                _progress = Mathf.Max(0f, _progress - comboDecayPerSecond * Time.deltaTime);
                GameEvents.RaiseFeverProgressChanged(_progress);
            }
        }
    }
}
