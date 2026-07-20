using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The prestige confirmation dialog. Refreshes its body text each time it opens;
    /// CONFIRM calls PrestigeManager.Prestige() and closes, CANCEL just closes.
    /// </summary>
    public sealed class PrestigeDialogView : MonoBehaviour
    {
        [SerializeField] private Text bodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            var mgr = PrestigeManager.Instance;
            if (mgr == null || bodyText == null)
            {
                return;
            }

            bodyText.text =
                "Shattering your dig site distills its story into pure knowledge.\n\n"
                + "You will gain +" + NumberFormatter.Format(mgr.PendingKnowledge) + " KP.\n\n"
                + "Coins, tools, assistants and tablet progress will be lost.\n"
                + "Knowledge Points are permanent.";
        }

        private void OnConfirm()
        {
            if (PrestigeManager.Instance != null)
            {
                PrestigeManager.Instance.Prestige();
            }

            Close();
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
