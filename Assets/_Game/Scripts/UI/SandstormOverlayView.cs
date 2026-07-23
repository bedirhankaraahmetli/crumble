using BreakInfinity;
using Crumble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Sandstorm presentation: lives on the always-active HUD so it can hear the start
    /// event, then shows the dust overlay. Swiping thins the dust; clearing or timing out
    /// hides it.
    /// </summary>
    public sealed class SandstormOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image dustImage;
        [SerializeField] private RectTransform progressFill;

        private const float MaxDustAlpha = 0.86f;

        private void OnEnable()
        {
            GameEvents.SandstormStarted += OnStarted;
            GameEvents.SandstormProgress += OnProgress;
            GameEvents.SandstormEnded += OnEnded;
        }

        private void OnDisable()
        {
            GameEvents.SandstormStarted -= OnStarted;
            GameEvents.SandstormProgress -= OnProgress;
            GameEvents.SandstormEnded -= OnEnded;
        }

        private void OnStarted()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }
        }

        private void OnProgress(float progress)
        {
            if (dustImage != null)
            {
                var color = dustImage.color;
                color.a = MaxDustAlpha * (1f - 0.75f * progress); // dust thins as you swipe
                dustImage.color = color;
            }

            if (progressFill != null)
            {
                progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                progressFill.offsetMin = Vector2.zero;
                progressFill.offsetMax = Vector2.zero;
            }
        }

        private void OnEnded()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }
    }
}
