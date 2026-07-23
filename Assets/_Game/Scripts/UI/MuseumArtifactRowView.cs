using Crumble.Data;
using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>One artifact entry: bright when owned, "???" silhouette when undiscovered.</summary>
    public sealed class MuseumArtifactRowView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descText;
        [SerializeField] private Text countText;

        private ArtifactSO _artifact;

        public void Init(ArtifactSO artifact)
        {
            _artifact = artifact;
            icon.sprite = artifact.Icon;
        }

        public void Refresh()
        {
            var mgr = MuseumManager.Instance;
            if (mgr == null || _artifact == null)
            {
                return;
            }

            var count = mgr.GetCount(_artifact);
            if (count > 0)
            {
                nameText.text = _artifact.DisplayName;
                descText.text = _artifact.Description;
                countText.text = count > 1 ? "x" + count : "";
                icon.color = Color.white;
                canvasGroup.alpha = 1f;
            }
            else
            {
                nameText.text = "???";
                descText.text = "Undiscovered.";
                countText.text = "";
                icon.color = new Color(0.15f, 0.13f, 0.11f);
                canvasGroup.alpha = 0.5f;
            }
        }
    }
}
