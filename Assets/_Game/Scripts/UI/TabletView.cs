using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using UnityEngine;

namespace Crumble.UI
{
    /// <summary>
    /// Visual for the current tablet: swaps between the 5 crack-state sprites
    /// (Full → Little Cracked → Cracked → Heavily Cracked → Shattered) from HP events.
    /// Subscribe-only: never calls into managers.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TabletView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private TabletMaterialSO _material;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            GameEvents.TabletChanged += OnTabletChanged;
            GameEvents.TabletHpChanged += OnHpChanged;
        }

        private void OnDisable()
        {
            GameEvents.TabletChanged -= OnTabletChanged;
            GameEvents.TabletHpChanged -= OnHpChanged;
        }

        private static readonly Color MilestoneTint = new Color(1f, 0.78f, 0.72f);

        private void OnTabletChanged(TabletMaterialSO material, int stage, bool isMilestone)
        {
            _material = material;
            spriteRenderer.color = isMilestone ? MilestoneTint : Color.white;
            SetCrackState(0);
        }

        private void OnHpChanged(BigDouble current, BigDouble max)
        {
            SetCrackState(CrackStateForHp(current, max));
        }

        /// <summary>0 Full, 1 Little Cracked, 2 Cracked, 3 Heavily Cracked, 4 Shattered.</summary>
        public static int CrackStateForHp(BigDouble current, BigDouble max)
        {
            if (max <= 0 || current <= 0)
            {
                return 4;
            }

            var pct = (current / max).ToDouble();
            if (pct > 0.75) return 0;
            if (pct > 0.5) return 1;
            if (pct > 0.25) return 2;
            return 3;
        }

        private void SetCrackState(int state)
        {
            if (_material == null || _material.CrackStates == null || _material.CrackStates.Length == 0)
            {
                return;
            }

            var index = Mathf.Clamp(state, 0, _material.CrackStates.Length - 1);
            var sprite = _material.CrackStates[index];
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}
