using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Full-screen Research Tree overlay: 4 branch tabs, a scrollable list of the branch's
    /// 15 stages, a KP readout, and a close button. Rows are instantiated once from a
    /// template on first open. Purchases go through ResearchManager.TryBuy.
    /// </summary>
    public sealed class ResearchPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Button activeTabButton;
        [SerializeField] private Button autoTabButton;
        [SerializeField] private Button economyTabButton;
        [SerializeField] private Button intuitionTabButton;
        [SerializeField] private Text kpText;
        [SerializeField] private Button closeButton;

        private static readonly Color ActiveTab = new Color(0.85f, 0.65f, 0.25f);
        private static readonly Color InactiveTab = new Color(0.30f, 0.27f, 0.23f);

        private readonly List<ResearchRowView> _rows = new List<ResearchRowView>();
        private ResearchBranch _branch = ResearchBranch.ActiveExcavation;
        private bool _built;

        private void OnEnable()
        {
            GameEvents.KnowledgePointsChanged += OnKnowledgeChanged;
            GameEvents.ResearchNodeChanged += OnResearchChanged;

            if (_built)
            {
                RefreshKp();
                RefreshVisible();
            }
        }

        private void OnDisable()
        {
            GameEvents.KnowledgePointsChanged -= OnKnowledgeChanged;
            GameEvents.ResearchNodeChanged -= OnResearchChanged;
        }

        private void Start()
        {
            activeTabButton.onClick.AddListener(() => SetBranch(ResearchBranch.ActiveExcavation));
            autoTabButton.onClick.AddListener(() => SetBranch(ResearchBranch.AutomationLogistics));
            economyTabButton.onClick.AddListener(() => SetBranch(ResearchBranch.CampEconomy));
            intuitionTabButton.onClick.AddListener(() => SetBranch(ResearchBranch.ArchaeologicalIntuition));
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            BuildRows();
            RefreshKp();
            SetBranch(ResearchBranch.ActiveExcavation);
        }

        private void BuildRows()
        {
            var mgr = ResearchManager.Instance;
            if (_built || mgr == null || rowTemplate == null)
            {
                return;
            }

            foreach (var node in mgr.Nodes)
            {
                var row = Instantiate(rowTemplate, content).GetComponent<ResearchRowView>();
                row.Init(node);
                row.gameObject.SetActive(false);
                _rows.Add(row);
            }

            _built = true;
        }

        private void SetBranch(ResearchBranch branch)
        {
            _branch = branch;
            activeTabButton.image.color = branch == ResearchBranch.ActiveExcavation ? ActiveTab : InactiveTab;
            autoTabButton.image.color = branch == ResearchBranch.AutomationLogistics ? ActiveTab : InactiveTab;
            economyTabButton.image.color = branch == ResearchBranch.CampEconomy ? ActiveTab : InactiveTab;
            intuitionTabButton.image.color = branch == ResearchBranch.ArchaeologicalIntuition ? ActiveTab : InactiveTab;

            foreach (var row in _rows)
            {
                row.gameObject.SetActive(row.Branch == branch);
            }

            RefreshVisible();
        }

        private void RefreshVisible()
        {
            foreach (var row in _rows)
            {
                if (row.gameObject.activeSelf)
                {
                    row.Refresh();
                }
            }
        }

        private void RefreshKp()
        {
            if (kpText != null && CurrencyManager.Instance != null)
            {
                kpText.text = "KP " + NumberFormatter.Format(CurrencyManager.Instance.KnowledgePoints);
            }
        }

        private void OnKnowledgeChanged(BigDouble total)
        {
            RefreshKp();
            if (_built)
            {
                RefreshVisible();
            }
        }

        private void OnResearchChanged(string id, int level)
        {
            if (_built)
            {
                RefreshVisible(); // unlocks can cascade down the branch
            }
        }
    }
}
