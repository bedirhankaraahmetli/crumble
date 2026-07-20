using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    public enum BuyMode
    {
        X1,
        X10,
        Max,
    }

    /// <summary>
    /// The bottom upgrade panel: Tools/Assistants tabs, x1/x10/MAX buy-mode cycle, and a
    /// scrollable list of UpgradeRowView rows (instantiated once at boot from a template —
    /// never per interaction). Reads UpgradeManager; buys go through TryBuy*.
    /// </summary>
    public sealed class UpgradePanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Button toolsTabButton;
        [SerializeField] private Button assistantsTabButton;
        [SerializeField] private Button buyModeButton;
        [SerializeField] private Text buyModeLabel;

        private static readonly Color ActiveTab = new Color(0.85f, 0.65f, 0.25f);
        private static readonly Color InactiveTab = new Color(0.30f, 0.27f, 0.23f);

        private readonly List<UpgradeRowView> _rows = new List<UpgradeRowView>();
        private BuyMode _mode = BuyMode.X1;
        private bool _built;

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.ToolLevelChanged += OnLevelChanged;
            GameEvents.AssistantLevelChanged += OnLevelChanged;
            GameEvents.Prestige += OnPrestige;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.ToolLevelChanged -= OnLevelChanged;
            GameEvents.AssistantLevelChanged -= OnLevelChanged;
            GameEvents.Prestige -= OnPrestige;
        }

        private void OnPrestige(BigDouble kpGained)
        {
            if (_built)
            {
                RefreshVisible(); // every level went back to 0 — full row refresh
            }
        }

        private void Start()
        {
            toolsTabButton.onClick.AddListener(() => SetTab(true));
            assistantsTabButton.onClick.AddListener(() => SetTab(false));
            buyModeButton.onClick.AddListener(CycleBuyMode);
            buyModeLabel.text = "x1";

            BuildRows();
            SetTab(showTools: true);
        }

        private void BuildRows()
        {
            var mgr = UpgradeManager.Instance;
            if (_built || mgr == null || rowTemplate == null)
            {
                return;
            }

            foreach (var tool in mgr.Tools)
            {
                var row = Instantiate(rowTemplate, content).GetComponent<UpgradeRowView>();
                row.InitTool(tool);
                _rows.Add(row);
            }

            foreach (var assistant in mgr.Assistants)
            {
                var row = Instantiate(rowTemplate, content).GetComponent<UpgradeRowView>();
                row.InitAssistant(assistant);
                _rows.Add(row);
            }

            _built = true;
        }

        private void SetTab(bool showTools)
        {
            toolsTabButton.image.color = showTools ? ActiveTab : InactiveTab;
            assistantsTabButton.image.color = showTools ? InactiveTab : ActiveTab;

            foreach (var row in _rows)
            {
                row.gameObject.SetActive(row.IsTool == showTools);
            }

            RefreshVisible();
        }

        private void CycleBuyMode()
        {
            _mode = _mode == BuyMode.X1 ? BuyMode.X10 : _mode == BuyMode.X10 ? BuyMode.Max : BuyMode.X1;
            buyModeLabel.text = _mode == BuyMode.X1 ? "x1" : _mode == BuyMode.X10 ? "x10" : "MAX";
            RefreshVisible();
        }

        private void RefreshVisible()
        {
            foreach (var row in _rows)
            {
                if (row.gameObject.activeSelf)
                {
                    row.Refresh(_mode);
                }
            }
        }

        private void OnCoinsChanged(BigDouble total)
        {
            if (!_built)
            {
                return;
            }

            if (_mode == BuyMode.Max)
            {
                RefreshVisible(); // MAX counts shift with the balance — full refresh
                return;
            }

            foreach (var row in _rows)
            {
                if (row.gameObject.activeSelf)
                {
                    row.RefreshAffordability(total); // no allocations on the tap path
                }
            }
        }

        private void OnLevelChanged(string id, int level)
        {
            if (_built)
            {
                RefreshVisible();
            }
        }
    }
}
