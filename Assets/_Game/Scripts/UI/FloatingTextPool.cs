using System.Collections.Generic;
using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Object pool for floating damage numbers (mobile hard rule: pool anything spawned
    /// per tap). Spawns a number over the tablet on every TabletDamaged event; when the
    /// pool is exhausted the event is simply skipped — never allocate mid-combat.
    /// Subscribe-only: reads GameEvents, never calls managers.
    /// </summary>
    public sealed class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private Transform worldAnchor; // the tablet
        [SerializeField] private int poolSize = 16;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private Color textColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField] private float jitterRadius = 70f; // screen px

        private readonly Stack<FloatingText> _available = new Stack<FloatingText>();
        private Camera _camera;

        private void Awake()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var i = 0; i < poolSize; i++)
            {
                var go = new GameObject("FloatingText", typeof(RectTransform), typeof(Text), typeof(FloatingText));
                go.transform.SetParent(transform, false);

                var text = go.GetComponent<Text>();
                text.font = font;
                text.fontSize = fontSize;
                text.fontStyle = FontStyle.Bold;
                text.color = textColor;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;

                var floating = go.GetComponent<FloatingText>();
                floating.Init(this);
                go.SetActive(false);
                _available.Push(floating);
            }
        }

        private void OnEnable() => GameEvents.TabletDamaged += OnTabletDamaged;
        private void OnDisable() => GameEvents.TabletDamaged -= OnTabletDamaged;

        public void Return(FloatingText instance)
        {
            _available.Push(instance);
        }

        private void OnTabletDamaged(BigDouble damage, bool fromClick)
        {
            if (_available.Count == 0 || worldAnchor == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            Vector2 screenPos = _camera.WorldToScreenPoint(worldAnchor.position);
            screenPos += Random.insideUnitCircle * jitterRadius;

            var instance = _available.Pop();
            instance.Show("-" + NumberFormatter.Format(damage), screenPos);
        }
    }
}
