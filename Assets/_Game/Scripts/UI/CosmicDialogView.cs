using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The Hard Prestige confirmation. Spells out exactly how much is about to be erased
    /// — this wipe is the deepest in the game, so the warning is loud. CONFIRM solves
    /// the Universal Secret and closes both the dialog and the Archive panel beneath it.
    /// </summary>
    public sealed class CosmicDialogView : MonoBehaviour
    {
        [SerializeField] private Text bodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject panelToClose;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            var archive = CosmicArchiveManager.Instance;
            if (archive == null || bodyText == null)
            {
                return;
            }

            bodyText.text =
                "The Archive consumes your entire dig:\ncoins, tools, assistants, Knowledge,"
                + " the whole Research Tree — even the Museum.\n\n"
                + "In return, the cosmos remembers:\n+"
                + NumberFormatter.Format(archive.PendingTimeCrystals)
                + " Time Crystals, forever.\n\nThere is no undo.";
        }

        private void OnConfirm()
        {
            if (CosmicArchiveManager.Instance != null)
            {
                CosmicArchiveManager.Instance.HardPrestige();
            }

            Close();
            if (panelToClose != null)
            {
                panelToClose.SetActive(false);
            }
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
