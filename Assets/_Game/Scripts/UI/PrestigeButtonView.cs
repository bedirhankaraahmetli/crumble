using BreakInfinity;
using Crumble.Core;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The HUD prestige button with a live "+X KP" preview. Disabled until a prestige
    /// would award at least 1 KP; opens the confirmation dialog when pressed.
    /// Text only re-renders when the floored KP value actually changes — coin ticks
    /// on the tap path stay allocation-free.
    /// </summary>
    public sealed class PrestigeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private GameObject dialog;

        private BigDouble _lastShownKp = -1;

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.Prestige += OnPrestige;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.Prestige -= OnPrestige;
        }

        private void Start()
        {
            button.onClick.AddListener(OpenDialog);
            RefreshPreview();
        }

        private void OnCoinsChanged(BigDouble total) => RefreshPreview();

        private void OnPrestige(BigDouble kpGained) => RefreshPreview();

        private void RefreshPreview()
        {
            var mgr = PrestigeManager.Instance;
            if (mgr == null || label == null)
            {
                return;
            }

            var kp = mgr.PendingKnowledge;
            if (kp == _lastShownKp)
            {
                return;
            }

            _lastShownKp = kp;
            label.text = "PRESTIGE\n+" + NumberFormatter.Format(kp) + " KP";
            button.interactable = kp >= 1;
        }

        private void OpenDialog()
        {
            if (dialog != null && PrestigeManager.Instance != null && PrestigeManager.Instance.CanPrestige)
            {
                dialog.SetActive(true);
            }
        }
    }
}
