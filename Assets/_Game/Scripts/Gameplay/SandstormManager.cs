using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Random sandstorms (GDD §6): the screen fills with dust and the player must swipe it
    /// clean within the time limit for a large coin reward. Swipe distance is fed in by the
    /// overlay UI; missing the window just ends the storm.
    /// </summary>
    [DefaultExecutionOrder(-44)]
    public sealed class SandstormManager : Singleton<SandstormManager>
    {
        [Header("Scheduling")]
        [SerializeField] private float minIntervalMinutes = 4f;
        [SerializeField] private float maxIntervalMinutes = 9f;

        [Header("Balance")]
        [Tooltip("Total swipe distance (screen px) needed to clear the storm.")]
        [SerializeField] private float requiredSwipePixels = 6000f;
        [SerializeField] private float timeoutSeconds = 20f;
        [Tooltip("Reward equals this many seconds of full DPS income (with a floor).")]
        [SerializeField] private double rewardDpsSeconds = 90;
        [SerializeField] private double minRewardTabletMultiple = 15;

        public bool IsActive { get; private set; }

        private double _nextInSeconds;
        private float _timeLeft;
        private float _swipedPixels;

        private void Start()
        {
            ScheduleNext();
        }

        private void Update()
        {
            if (!IsActive)
            {
                if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                {
                    return;
                }

                _nextInSeconds -= Time.deltaTime;
                if (_nextInSeconds <= 0)
                {
                    StartStorm();
                }

                return;
            }

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0)
            {
                EndStorm(); // too slow — the dust settles on its own, no reward
            }
        }

        /// <summary>Called by the overlay UI with each drag's distance in screen pixels.</summary>
        public void RegisterSwipe(float pixels)
        {
            if (!IsActive || pixels <= 0)
            {
                return;
            }

            _swipedPixels += pixels;
            GameEvents.RaiseSandstormProgress(Mathf.Clamp01(_swipedPixels / requiredSwipePixels));

            if (_swipedPixels >= requiredSwipePixels)
            {
                var dps = UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalDps : BigDouble.Zero;
                var tablet = TabletManager.Instance;
                var reward = GameMath.EventReward(
                    dps, rewardDpsSeconds,
                    tablet != null ? tablet.CurrentShatterReward : BigDouble.One,
                    minRewardTabletMultiple);

                var research = ResearchManager.Instance != null ? ResearchManager.Instance.CoinMultiplier : 1;
                var museum = MuseumManager.Instance != null ? MuseumManager.Instance.CoinMultiplier : 1;
                reward *= research * museum;

                CurrencyManager.Instance.AddCoins(reward);
                Haptics.Impact();
                GameEvents.RaiseSandstormCleared(reward);
                EndStorm();
            }
        }

        /// <summary>Dev helper: summon a storm right now.</summary>
        public void DebugTriggerSandstorm()
        {
            if (!IsActive)
            {
                StartStorm();
            }
        }

        private void StartStorm()
        {
            IsActive = true;
            _swipedPixels = 0f;
            _timeLeft = timeoutSeconds;
            GameEvents.RaiseSandstormStarted();
            GameEvents.RaiseSandstormProgress(0f);
        }

        private void EndStorm()
        {
            IsActive = false;
            GameEvents.RaiseSandstormEnded();
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            _nextInSeconds = Random.Range(minIntervalMinutes, maxIntervalMinutes) * 60f;
        }
    }
}
