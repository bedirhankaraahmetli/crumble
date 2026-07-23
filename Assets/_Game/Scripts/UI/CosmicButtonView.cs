using BreakInfinity;
using Crumble.Core;
using Crumble.Gameplay;
using UnityEngine;

namespace Crumble.UI
{
    /// <summary>
    /// Reveals the HUD's COSMIC button only once the endgame stops being a secret:
    /// after the first Stage-15 research ultimate (or if the player already holds
    /// Time Crystals). Lives on the always-active HUD so it can hear events while
    /// the button itself is hidden.
    /// </summary>
    public sealed class CosmicButtonView : MonoBehaviour
    {
        [SerializeField] private GameObject buttonRoot;

        private void OnEnable()
        {
            GameEvents.GameLoaded += OnGameLoaded;
            GameEvents.ResearchNodeChanged += OnResearchChanged;
            GameEvents.TimeCrystalsChanged += OnCrystalsChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameLoaded -= OnGameLoaded;
            GameEvents.ResearchNodeChanged -= OnResearchChanged;
            GameEvents.TimeCrystalsChanged -= OnCrystalsChanged;
        }

        private void OnGameLoaded(SaveData data) => Refresh();
        private void OnResearchChanged(string id, int level) => Refresh();
        private void OnCrystalsChanged(BigDouble total) => Refresh();

        private void Refresh()
        {
            var archive = CosmicArchiveManager.Instance;
            if (buttonRoot != null && archive != null)
            {
                buttonRoot.SetActive(archive.IsRevealed);
            }
        }
    }
}
