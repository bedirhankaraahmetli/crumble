using System.Globalization;
using Crumble.Data;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One research node row. Three visibility states (GDD §7):
    /// unlocked = bright + buyable; locked-but-reachable = grayed yet fully readable so
    /// players can strategize; Stage-15 ultimates = "???" silhouette until the stage-14
    /// node is maxed (that IS their unlock condition, so silhouette == locked ultimate).
    /// </summary>
    public sealed class ResearchRowView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text stageText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text effectText;
        [SerializeField] private Text levelText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text costText;

        private ResearchNodeSO _node;

        public ResearchBranch Branch => _node != null ? _node.Branch : ResearchBranch.ActiveExcavation;

        public void Init(ResearchNodeSO node)
        {
            _node = node;
            stageText.text = "S" + node.Stage;
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Refresh()
        {
            var mgr = ResearchManager.Instance;
            if (mgr == null || _node == null || CurrencyManager.Instance == null)
            {
                return;
            }

            var unlocked = mgr.IsUnlocked(_node);
            var silhouette = _node.Stage == 15 && !unlocked;

            if (silhouette)
            {
                nameText.text = "???";
                effectText.text = "Max out the previous research to reveal this secret.";
                levelText.text = "";
                costText.text = "?";
                buyButton.interactable = false;
                canvasGroup.alpha = 0.35f;
                return;
            }

            var level = mgr.GetLevel(_node);
            nameText.text = _node.DisplayName;
            effectText.text = EffectLine(_node);
            levelText.text = level + "/" + _node.MaxLevel;

            if (!unlocked)
            {
                costText.text = "LOCKED";
                buyButton.interactable = false;
                canvasGroup.alpha = 0.55f;
                return;
            }

            canvasGroup.alpha = 1f;
            if (mgr.IsMaxed(_node))
            {
                costText.text = "MAXED";
                buyButton.interactable = false;
                return;
            }

            var cost = mgr.NodeCost(_node);
            costText.text = NumberFormatter.Format(cost) + " KP";
            buyButton.interactable = CurrencyManager.Instance.KnowledgePoints >= cost;
        }

        private static string EffectLine(ResearchNodeSO node)
        {
            var p = node.EffectPerLevel.ToString("0.#%", CultureInfo.InvariantCulture);
            switch (node.EffectType)
            {
                case ResearchEffectType.ClickDamageMultiplier: return "+" + p + " click damage / lv";
                case ResearchEffectType.AssistantDpsMultiplier: return "+" + p + " assistant DPS / lv";
                case ResearchEffectType.CoinDropMultiplier: return "+" + p + " coins / lv";
                case ResearchEffectType.UpgradeCostReduction: return "-" + p + " upgrade costs / lv";
                case ResearchEffectType.CritChance: return "+" + p + " crit chance / lv (soon)";
                case ResearchEffectType.CritMultiplier: return "+" + p + " crit damage / lv (soon)";
                case ResearchEffectType.FeverDuration: return "+" + p + " fever duration / lv (soon)";
                case ResearchEffectType.AssistantSynergy: return "+" + p + " synergy / lv (soon)";
                case ResearchEffectType.OfflineEfficiency: return "+" + p + " offline gains / lv (soon)";
                case ResearchEffectType.OfflineCapHours: return "+" + p + " offline cap / lv (soon)";
                case ResearchEffectType.ArtifactDropRate: return "+" + p + " artifact drops / lv (soon)";
                case ResearchEffectType.ExpeditionSpeed: return "+" + p + " expedition speed / lv (soon)";
                case ResearchEffectType.MuseumBonus: return "+" + p + " museum bonus / lv (soon)";
                default: return "+" + p + " / lv";
            }
        }

        private void OnBuyClicked()
        {
            if (ResearchManager.Instance != null && _node != null)
            {
                ResearchManager.Instance.TryBuy(_node);
            }
        }
    }
}
