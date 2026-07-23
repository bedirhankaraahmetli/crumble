using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// The Expedition Tent overlay: one row per mission, refreshed every second while open
    /// so countdowns tick visibly.
    /// </summary>
    public sealed class ExpeditionPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Button closeButton;

        private readonly List<ExpeditionRowView> _rows = new List<ExpeditionRowView>();
        private bool _built;
        private float _refreshTimer;

        private void OnEnable()
        {
            GameEvents.ExpeditionStarted += OnExpeditionStarted;
            GameEvents.ExpeditionCollected += OnExpeditionCollected;
            if (_built)
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            GameEvents.ExpeditionStarted -= OnExpeditionStarted;
            GameEvents.ExpeditionCollected -= OnExpeditionCollected;
        }

        private void Start()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            Build();
            RefreshAll();
        }

        private void Update()
        {
            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= 1f)
            {
                _refreshTimer = 0f;
                RefreshAll();
            }
        }

        private void Build()
        {
            var mgr = ExpeditionManager.Instance;
            if (_built || mgr == null)
            {
                return;
            }

            foreach (var expedition in mgr.Expeditions)
            {
                var row = Instantiate(rowTemplate, content).GetComponent<ExpeditionRowView>();
                row.Init(expedition);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }

            _built = true;
        }

        private void RefreshAll()
        {
            foreach (var row in _rows)
            {
                row.Refresh();
            }
        }

        private void OnExpeditionStarted(ExpeditionSO expedition, long endUnixUtc)
        {
            if (_built)
            {
                RefreshAll();
            }
        }

        private void OnExpeditionCollected(BigDouble coins, ArtifactSO artifact)
        {
            if (_built)
            {
                RefreshAll();
            }
        }
    }
}
