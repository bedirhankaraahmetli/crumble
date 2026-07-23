using BreakInfinity;
using Crumble.Core;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The Cosmic Archive panel: Time Crystal balance, the four Cosmic Altar upgrades,
    /// and the "SOLVE THE UNIVERSAL SECRET" Hard Prestige button (locked until every
    /// research branch's Stage-15 ultimate is owned). Refreshes on open and on the
    /// currency/altar/research events that can change what it shows.
    /// </summary>
    public sealed class CosmicPanelController : MonoBehaviour
    {
        [SerializeField] private Text tcBalanceText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button solveButton;
        [SerializeField] private Text solveLabel;
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Button closeButton;
        [SerializeField] private CosmicAltarRowView[] rows;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (solveButton != null)
            {
                solveButton.onClick.AddListener(OpenConfirm);
            }
        }

        private void OnEnable()
        {
            GameEvents.TimeCrystalsChanged += OnCrystalsChanged;
            GameEvents.AltarUpgradeChanged += OnAltarChanged;
            GameEvents.ResearchNodeChanged += OnResearchChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            GameEvents.TimeCrystalsChanged -= OnCrystalsChanged;
            GameEvents.AltarUpgradeChanged -= OnAltarChanged;
            GameEvents.ResearchNodeChanged -= OnResearchChanged;
        }

        private void OnCrystalsChanged(BigDouble total) => RefreshAll();
        private void OnAltarChanged(string id, int level) => RefreshAll();
        private void OnResearchChanged(string id, int level) => RefreshAll();

        private void RefreshAll()
        {
            var archive = CosmicArchiveManager.Instance;
            var currency = CurrencyManager.Instance;
            if (archive == null || currency == null)
            {
                return;
            }

            if (tcBalanceText != null)
            {
                tcBalanceText.text = "Time Crystals: " + NumberFormatter.Format(currency.TimeCrystals);
            }

            var unlocked = archive.IsUnlocked;
            var pending = archive.PendingTimeCrystals;

            if (statusText != null)
            {
                statusText.text = unlocked
                    ? "The four ultimates resonate. The Universal Secret can be solved."
                    : "Master all four research ultimates to open the Archive."
                      + $"  ({archive.UltimatesOwned}/{archive.UltimatesTotal})";
            }

            if (solveLabel != null)
            {
                solveLabel.text = "SOLVE THE UNIVERSAL SECRET\n+" + NumberFormatter.Format(pending) + " TC";
            }

            if (solveButton != null)
            {
                solveButton.interactable = unlocked && pending >= 1;
            }

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row != null)
                    {
                        row.Refresh();
                    }
                }
            }
        }

        private void OpenConfirm()
        {
            var archive = CosmicArchiveManager.Instance;
            if (confirmDialog != null && archive != null && archive.IsUnlocked)
            {
                confirmDialog.SetActive(true);
            }
        }
    }
}
