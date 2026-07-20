using System;
using BreakInfinity;
using Crumble.Core;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Welcome-back flow: lives on the always-active HUD so it can hear the boot-time
    /// OfflineEarningsReady event, then shows the popup. COLLECT banks the pending coins;
    /// COLLECT x2 first runs a rewarded ad through AdManager and doubles on success
    /// (GDD monetization: optional rewarded ads only).
    /// </summary>
    public sealed class WelcomeBackPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button doubleButton;

        private void Awake()
        {
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(() => Collect(doubled: false));
            }

            if (doubleButton != null)
            {
                doubleButton.onClick.AddListener(OnDoubleClicked);
            }
        }

        private void OnEnable() => GameEvents.OfflineEarningsReady += OnEarningsReady;
        private void OnDisable() => GameEvents.OfflineEarningsReady -= OnEarningsReady;

        private void OnEarningsReady(BigDouble coins, double seconds)
        {
            if (root == null)
            {
                return;
            }

            if (bodyText != null)
            {
                bodyText.text =
                    "You were away for " + FormatDuration(seconds) + ".\n"
                    + "Your assistants kept digging and unearthed\n\n"
                    + NumberFormatter.Format(coins) + " coins";
            }

            SetButtonsInteractable(true);
            if (doubleButton != null)
            {
                doubleButton.gameObject.SetActive(
                    AdManager.Instance != null && AdManager.Instance.IsRewardedAdAvailable);
            }

            root.SetActive(true);
        }

        private void OnDoubleClicked()
        {
            SetButtonsInteractable(false); // no double-collecting while the ad runs
            AdManager.Instance.ShowRewardedAd(
                onRewarded: () => Collect(doubled: true),
                onFailed: () => SetButtonsInteractable(true));
        }

        private void Collect(bool doubled)
        {
            if (OfflineProgressManager.Instance != null)
            {
                OfflineProgressManager.Instance.Collect(doubled);
            }

            root.SetActive(false);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (collectButton != null)
            {
                collectButton.interactable = value;
            }

            if (doubleButton != null)
            {
                doubleButton.interactable = value;
            }
        }

        private static string FormatDuration(double seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1)
            {
                return (int)span.TotalHours + "h " + span.Minutes + "m";
            }

            return span.TotalMinutes >= 1 ? span.Minutes + "m " + span.Seconds + "s" : span.Seconds + "s";
        }
    }
}
