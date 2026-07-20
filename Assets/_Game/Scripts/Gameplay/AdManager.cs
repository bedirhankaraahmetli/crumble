using System;
using System.Collections;
using Crumble.Core;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Rewarded-ad service. Currently a simulator: pretends to show an ad and grants the
    /// reward after a short delay, so every ad-gated flow (offline x2, future boosts) works
    /// end-to-end today. Step 10 swaps the simulated path for a real ad SDK behind this
    /// same API — callers never change. GDD monetization rule: rewarded ads only, always
    /// optional, never forced.
    /// </summary>
    public sealed class AdManager : Singleton<AdManager>
    {
        [Header("Development")]
        [Tooltip("Simulate rewarded ads (always succeed after a short delay). Step 10 replaces this with a real SDK.")]
        [SerializeField] private bool simulateAds = true;
        [SerializeField] private float simulatedAdSeconds = 0.5f;

        private bool _showing;

        /// <summary>UI should hide/disable ad buttons when this is false.</summary>
        public bool IsRewardedAdAvailable => simulateAds && !_showing;

        public void ShowRewardedAd(Action onRewarded, Action onFailed = null)
        {
            if (!IsRewardedAdAvailable)
            {
                onFailed?.Invoke();
                return;
            }

            _showing = true;
            StartCoroutine(SimulateAd(onRewarded));
        }

        private IEnumerator SimulateAd(Action onRewarded)
        {
            yield return new WaitForSecondsRealtime(simulatedAdSeconds);
            _showing = false;
            onRewarded?.Invoke();
        }
    }
}
