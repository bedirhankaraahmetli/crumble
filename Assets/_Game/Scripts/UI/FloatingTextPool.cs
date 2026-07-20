using System.Collections.Generic;
using System.Globalization;
using Crumble.Core;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// Object pool for floating damage numbers (mobile hard rule: pool anything spawned
    /// per tap). Numbers spawn under the press position when known, otherwise over the
    /// tablet; crits get a bigger, hotter "CRIT xN DMG" popup. Pool exhausted = event
    /// skipped — never allocate mid-combat. Subscribe-only.
    /// </summary>
    public sealed class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private Transform worldAnchor; // the tablet (fallback position)
        [SerializeField] private int poolSize = 16;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private int critFontSize = 60;
        [SerializeField] private Color textColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField] private Color critColor = new Color(1f, 0.32f, 0.16f);
        [SerializeField] private float jitterRadius = 70f; // screen px (fallback spawns)
        [SerializeField] private float clickJitterRadius = 22f; // small wobble at the press point

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

        private void OnTabletDamaged(DamageInfo info)
        {
            if (!info.FromClick)
            {
                return; // DPS ticks land 4×/sec — numbers for those would flood the screen
            }

            if (_available.Count == 0)
            {
                return;
            }

            Vector2 screenPos;
            if (info.ScreenPosition != Vector2.zero)
            {
                screenPos = info.ScreenPosition + Random.insideUnitCircle * clickJitterRadius;
            }
            else
            {
                if (worldAnchor == null)
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

                screenPos = (Vector2)_camera.WorldToScreenPoint(worldAnchor.position)
                            + Random.insideUnitCircle * jitterRadius;
            }

            var instance = _available.Pop();
            if (info.IsCrit)
            {
                instance.Show(
                    "CRIT x" + info.CritMultiplier.ToString("0.#", CultureInfo.InvariantCulture)
                    + " DMG\n" + NumberFormatter.Format(info.Amount),
                    screenPos, critFontSize, critColor);
            }
            else
            {
                instance.Show("-" + NumberFormatter.Format(info.Amount), screenPos, fontSize, textColor);
            }
        }
    }
}
