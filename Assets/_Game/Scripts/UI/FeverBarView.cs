using Crumble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The Fever combo bar: fills as taps chain, drains while idle, flips color and shows
    /// "FEVER!" while the multiplier is live (bar then shows remaining time). After a
    /// fever, the bar turns cold and counts the cooldown down before it can charge again.
    /// Subscribe-only; the per-frame progress updates touch only an anchor — no allocs
    /// (the cooldown label re-renders once per second, not per frame).
    /// </summary>
    public sealed class FeverBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;

        private static readonly Color ChargeColor = new Color(1f, 0.62f, 0.15f);
        private static readonly Color ActiveColor = new Color(1f, 0.27f, 0.1f);
        private static readonly Color CooldownColor = new Color(0.35f, 0.45f, 0.6f);

        private float _cooldownTotal;
        private int _lastShownSecond = -1;

        private void OnEnable()
        {
            GameEvents.FeverProgressChanged += OnProgress;
            GameEvents.FeverStarted += OnStarted;
            GameEvents.FeverEnded += OnEnded;
            GameEvents.FeverCooldownStarted += OnCooldownStarted;
            GameEvents.FeverCooldownChanged += OnCooldownChanged;
            GameEvents.FeverCooldownEnded += OnCooldownEnded;
        }

        private void OnDisable()
        {
            GameEvents.FeverProgressChanged -= OnProgress;
            GameEvents.FeverStarted -= OnStarted;
            GameEvents.FeverEnded -= OnEnded;
            GameEvents.FeverCooldownStarted -= OnCooldownStarted;
            GameEvents.FeverCooldownChanged -= OnCooldownChanged;
            GameEvents.FeverCooldownEnded -= OnCooldownEnded;
        }

        private void OnProgress(float progress)
        {
            if (fill == null)
            {
                return;
            }

            fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        private void OnStarted(double duration)
        {
            if (fillImage != null)
            {
                fillImage.color = ActiveColor;
            }

            if (label != null)
            {
                label.text = "FEVER!";
            }
        }

        private void OnEnded()
        {
            if (fillImage != null)
            {
                fillImage.color = ChargeColor;
            }

            if (label != null)
            {
                label.text = "";
            }
        }

        private void OnCooldownStarted(double totalSeconds)
        {
            _cooldownTotal = (float)totalSeconds;
            _lastShownSecond = -1;
            if (fillImage != null)
            {
                fillImage.color = CooldownColor;
            }

            OnProgress(1f);
            OnCooldownChanged(_cooldownTotal);
        }

        private void OnCooldownChanged(float remainingSeconds)
        {
            if (_cooldownTotal > 0f)
            {
                OnProgress(remainingSeconds / _cooldownTotal); // drains as the bar warms back up
            }

            var second = Mathf.CeilToInt(remainingSeconds);
            if (second != _lastShownSecond && label != null)
            {
                _lastShownSecond = second;
                label.text = "COOLDOWN " + second + "s";
            }
        }

        private void OnCooldownEnded()
        {
            if (fillImage != null)
            {
                fillImage.color = ChargeColor;
            }

            if (label != null)
            {
                label.text = "";
            }

            OnProgress(0f);
        }
    }
}
