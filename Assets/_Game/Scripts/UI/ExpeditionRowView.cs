using System.Globalization;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One expedition mission row: duration (research-adjusted), reward preview, artifact
    /// odds, and a START / countdown / COLLECT button depending on state.
    /// </summary>
    public sealed class ExpeditionRowView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionLabel;

        private ExpeditionSO _expedition;

        public void Init(ExpeditionSO expedition)
        {
            _expedition = expedition;
            nameText.text = expedition.DisplayName;
            actionButton.onClick.AddListener(OnActionClicked);
        }

        public void Refresh()
        {
            var mgr = ExpeditionManager.Instance;
            if (mgr == null || _expedition == null)
            {
                return;
            }

            var research = ResearchManager.Instance;
            var artifactChance = Mathf.Clamp01(
                _expedition.ArtifactChance * (1f + (float)(research != null ? research.ArtifactDropRateBonus : 0)));
            infoText.text = TimeText.Format(mgr.EffectiveDurationHours(_expedition) * 3600.0)
                            + "   |   " + artifactChance.ToString("0%", CultureInfo.InvariantCulture) + " artifact";
            rewardText.text = "~" + NumberFormatter.Format(mgr.RewardPreview(_expedition)) + " coins";

            var isThis = mgr.ActiveExpedition == _expedition;
            if (isThis)
            {
                if (mgr.IsReady)
                {
                    actionLabel.text = "COLLECT";
                    actionButton.interactable = true;
                }
                else
                {
                    actionLabel.text = TimeText.Format(mgr.RemainingSeconds);
                    actionButton.interactable = false;
                }
            }
            else
            {
                actionLabel.text = "START";
                actionButton.interactable = !mgr.IsActive;
            }
        }

        private void OnActionClicked()
        {
            var mgr = ExpeditionManager.Instance;
            if (mgr == null)
            {
                return;
            }

            if (mgr.ActiveExpedition == _expedition && mgr.IsReady)
            {
                mgr.TryCollect();
            }
            else
            {
                mgr.TryStart(_expedition);
            }
        }
    }
}
