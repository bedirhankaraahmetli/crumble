using System.Globalization;
using Crumble.Data;
using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>A museum set's header: name, completion progress, and the set bonus.</summary>
    public sealed class MuseumSetHeaderView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text nameText;
        [SerializeField] private Text bonusText;
        [SerializeField] private Text progressText;

        private static readonly Color Incomplete = new Color(0.2f, 0.17f, 0.13f, 0.95f);
        private static readonly Color Complete = new Color(0.45f, 0.35f, 0.12f, 0.95f);

        private MuseumSetSO _set;

        public void Init(MuseumSetSO set)
        {
            _set = set;
            nameText.text = set.DisplayName;
        }

        public void Refresh()
        {
            var mgr = MuseumManager.Instance;
            if (mgr == null || _set == null)
            {
                return;
            }

            var owned = mgr.OwnedCountInSet(_set);
            var complete = mgr.IsSetComplete(_set);
            progressText.text = owned + "/" + _set.Artifacts.Length;
            background.color = complete ? Complete : Incomplete;
            bonusText.text = BonusLine(_set) + (complete ? "  — ACTIVE" : " when complete");
        }

        private static string BonusLine(MuseumSetSO set)
        {
            var p = set.BonusAmount.ToString("0.#%", CultureInfo.InvariantCulture);
            switch (set.BonusType)
            {
                case MuseumBonusType.CoinMultiplier: return "+" + p + " coins";
                case MuseumBonusType.ClickDamageMultiplier: return "+" + p + " click damage";
                case MuseumBonusType.DpsMultiplier: return "+" + p + " assistant DPS";
                default: return "+" + p;
            }
        }
    }
}
