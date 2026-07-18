using BreakInfinity;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One row of the upgrade list (a tool or an assistant). Reads levels/costs from
    /// UpgradeManager and forwards buy clicks to it; refreshed by UpgradePanelController.
    /// </summary>
    public sealed class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text effectText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;

        private ToolSO _tool;
        private AssistantSO _assistant;
        private BuyMode _mode = BuyMode.X1;
        private BigDouble _displayedCost;
        private int _displayedCount;

        public bool IsTool => _tool != null;

        public string Id => _tool != null ? _tool.Id : _assistant != null ? _assistant.Id : null;

        public void InitTool(ToolSO tool)
        {
            _tool = tool;
            icon.sprite = tool.Icon;
            nameText.text = tool.DisplayName;
            effectText.text = "+" + NumberFormatter.Format(tool.BaseDamagePerLevel) + " dmg / lv";
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void InitAssistant(AssistantSO assistant)
        {
            _assistant = assistant;
            icon.sprite = assistant.Icon;
            nameText.text = assistant.DisplayName;
            effectText.text = "+" + NumberFormatter.Format(assistant.BaseDpsPerLevel) + " DPS / lv";
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Refresh(BuyMode mode)
        {
            _mode = mode;
            var mgr = UpgradeManager.Instance;
            if (mgr == null || CurrencyManager.Instance == null)
            {
                return;
            }

            var level = IsTool ? mgr.GetToolLevel(_tool) : mgr.GetAssistantLevel(_assistant);
            levelText.text = "Lv " + level;

            var count = ResolveCount(mgr);
            var affordableCount = count > 0;
            if (!affordableCount)
            {
                count = 1; // MAX with empty pockets: show the single-level price, disabled
            }

            _displayedCount = count;
            _displayedCost = IsTool ? mgr.ToolCost(_tool, count) : mgr.AssistantCost(_assistant, count);

            costText.text = "x" + count + "\n" + NumberFormatter.Format(_displayedCost);
            buyButton.interactable = affordableCount
                                     && CurrencyManager.Instance.AntiqueCoins >= _displayedCost;
        }

        /// <summary>Cheap per-coin-change path: no strings, only the button state.</summary>
        public void RefreshAffordability(BigDouble coins)
        {
            buyButton.interactable = _displayedCount > 0 && coins >= _displayedCost;
        }

        private int ResolveCount(UpgradeManager mgr)
        {
            switch (_mode)
            {
                case BuyMode.X10:
                    return 10;
                case BuyMode.Max:
                    return IsTool ? mgr.MaxAffordableTool(_tool) : mgr.MaxAffordableAssistant(_assistant);
                default:
                    return 1;
            }
        }

        private void OnBuyClicked()
        {
            var mgr = UpgradeManager.Instance;
            if (mgr == null)
            {
                return;
            }

            var count = ResolveCount(mgr);
            if (count <= 0)
            {
                return;
            }

            if (IsTool)
            {
                mgr.TryBuyTool(_tool, count);
            }
            else
            {
                mgr.TryBuyAssistant(_assistant, count);
            }
        }
    }
}
