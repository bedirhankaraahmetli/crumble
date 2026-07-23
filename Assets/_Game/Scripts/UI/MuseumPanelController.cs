using System.Collections.Generic;
using Crumble.Core;
using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Full-screen museum overlay: a section per set (header + its artifacts), built once
    /// from templates on first open and refreshed when the museum changes.
    /// </summary>
    public sealed class MuseumPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject setHeaderTemplate;
        [SerializeField] private GameObject artifactRowTemplate;
        [SerializeField] private Button closeButton;

        private readonly List<MuseumSetHeaderView> _headers = new List<MuseumSetHeaderView>();
        private readonly List<MuseumArtifactRowView> _rows = new List<MuseumArtifactRowView>();
        private bool _built;

        private void OnEnable()
        {
            GameEvents.MuseumChanged += OnMuseumChanged;
            if (_built)
            {
                RefreshAll();
            }
        }

        private void OnDisable() => GameEvents.MuseumChanged -= OnMuseumChanged;

        private void Start()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            Build();
            RefreshAll();
        }

        private void Build()
        {
            var mgr = MuseumManager.Instance;
            if (_built || mgr == null)
            {
                return;
            }

            foreach (var set in mgr.Sets)
            {
                var header = Instantiate(setHeaderTemplate, content).GetComponent<MuseumSetHeaderView>();
                header.Init(set);
                header.gameObject.SetActive(true);
                _headers.Add(header);

                foreach (var artifact in set.Artifacts)
                {
                    var row = Instantiate(artifactRowTemplate, content).GetComponent<MuseumArtifactRowView>();
                    row.Init(artifact);
                    row.gameObject.SetActive(true);
                    _rows.Add(row);
                }
            }

            _built = true;
        }

        private void RefreshAll()
        {
            foreach (var header in _headers)
            {
                header.Refresh();
            }

            foreach (var row in _rows)
            {
                row.Refresh();
            }
        }

        private void OnMuseumChanged()
        {
            if (_built)
            {
                RefreshAll();
            }
        }
    }
}
