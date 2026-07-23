using System;
using Crumble.Core;
using UnityEngine;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Day/night cycle (GDD §6): night follows the player's real clock and boosts idle
    /// (assistant) gains; views darken the scene. debugForceNight lets you preview night
    /// in the editor.
    /// </summary>
    [DefaultExecutionOrder(-46)]
    public sealed class EnvironmentManager : Singleton<EnvironmentManager>
    {
        [Header("Balance")]
        [Tooltip("Local hour when night begins (24h).")]
        [SerializeField] private int nightStartHour = 20;
        [Tooltip("Local hour when night ends (24h).")]
        [SerializeField] private int nightEndHour = 7;
        [Tooltip("Assistant DPS multiplier while it's night.")]
        [SerializeField] private double nightIdleMultiplier = 1.5;

        [Header("Development")]
        [SerializeField] private bool debugForceNight;

        private const float CheckIntervalSeconds = 2f;
        private float _timer;
        private bool _initialized;

        public bool IsNight { get; private set; }

        public double NightIdleMultiplier => nightIdleMultiplier;

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < CheckIntervalSeconds && _initialized)
            {
                return;
            }

            _timer = 0f;
            var hour = DateTime.Now.Hour;
            var night = debugForceNight
                        || (nightStartHour > nightEndHour
                            ? hour >= nightStartHour || hour < nightEndHour
                            : hour >= nightStartHour && hour < nightEndHour);

            if (!_initialized || night != IsNight)
            {
                _initialized = true;
                IsNight = night;
                GameEvents.RaiseNightChanged(night);
            }
        }
    }
}
