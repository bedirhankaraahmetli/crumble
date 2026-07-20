using Crumble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The Fever combo bar: fills as taps chain, drains while idle, flips color and shows
    /// "FEVER!" while the multiplier is live (bar then shows remaining time).
    /// Subscribe-only; the per-frame progress updates touch only an anchor — no allocs.
    /// </summary>
    public sealed class FeverBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;

        private static readonly Color ChargeColor = new Color(1f, 0.62f, 0.15f);
        private static readonly Color ActiveColor = new Color(1f, 0.27f, 0.1f);

        private void OnEnable()
        {
            GameEvents.FeverProgressChanged += OnProgress;
            GameEvents.FeverStarted += OnStarted;
            GameEvents.FeverEnded += OnEnded;
        }

        private void OnDisable()
        {
            GameEvents.FeverProgressChanged -= OnProgress;
            GameEvents.FeverStarted -= OnStarted;
            GameEvents.FeverEnded -= OnEnded;
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
    }
}
