using Crumble.Data;
using Crumble.Gameplay;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One Cosmic Altar upgrade row: current level + compounded multiplier, what the
    /// next level makes it, and a BUY button priced in Time Crystals. The panel calls
    /// Refresh(); the row only forwards the buy tap to the manager.
    /// </summary>
    public sealed class CosmicAltarRowView : MonoBehaviour
    {
        [SerializeField] private AltarUpgradeSO upgrade;
        [SerializeField] private Text nameText;
        [SerializeField] private Text effectText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buyLabel;

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(() =>
                {
                    if (CosmicAltarManager.Instance != null)
                    {
                        CosmicAltarManager.Instance.TryBuy(upgrade);
                    }
                });
            }
        }

        public void Refresh()
        {
            var altar = CosmicAltarManager.Instance;
            var currency = CurrencyManager.Instance;
            if (altar == null || currency == null || upgrade == null)
            {
                return;
            }

            var level = altar.GetLevel(upgrade);
            var current = altar.CurrentMultiplier(upgrade);
            var next = GameMath.AltarMultiplier(upgrade.MultiplierPerLevel, level + 1);
            var cost = altar.NextCost(upgrade);

            if (nameText != null)
            {
                nameText.text = $"{upgrade.DisplayName} — Lv {level}";
            }

            if (effectText != null)
            {
                effectText.text =
                    $"{EffectNoun(upgrade.EffectType)} ×{NumberFormatter.Format(current)}"
                    + $"  →  ×{NumberFormatter.Format(next)}";
            }

            if (buyLabel != null)
            {
                buyLabel.text = "BUY\n" + NumberFormatter.Format(cost) + " TC";
            }

            if (buyButton != null)
            {
                buyButton.interactable = currency.TimeCrystals >= cost;
            }
        }

        private static string EffectNoun(AltarEffectType type)
        {
            switch (type)
            {
                case AltarEffectType.ClickDamage: return "Click damage";
                case AltarEffectType.AssistantDps: return "Assistant DPS";
                case AltarEffectType.CoinGain: return "Coin gains";
                case AltarEffectType.KnowledgeGain: return "Prestige KP";
                default: return "Effect";
            }
        }
    }
}
