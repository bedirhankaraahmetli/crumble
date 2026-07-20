using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using UnityEngine;

namespace Crumble.UI
{
    /// <summary>
    /// Visual for the current tablet: swaps between the 5 crack-state sprites, tints
    /// milestone bosses, and delivers the juice — a punch-scale on every tap, dust motes
    /// per click, and a colored particle burst on shatter. Subscribe-only; particles are
    /// emitted from pre-placed systems (never instantiated per event).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TabletView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ParticleSystem shatterBurst;
        [SerializeField] private ParticleSystem tapDust;

        private static readonly Color MilestoneTint = new Color(1f, 0.78f, 0.72f);

        private TabletMaterialSO _material;
        private Vector3 _baseScale;
        private float _punch; // 1 = full crush, recovers to 0

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            GameEvents.TabletChanged += OnTabletChanged;
            GameEvents.TabletHpChanged += OnHpChanged;
            GameEvents.TabletDamaged += OnTabletDamaged;
            GameEvents.TabletShattered += OnTabletShattered;
        }

        private void OnDisable()
        {
            GameEvents.TabletChanged -= OnTabletChanged;
            GameEvents.TabletHpChanged -= OnHpChanged;
            GameEvents.TabletDamaged -= OnTabletDamaged;
            GameEvents.TabletShattered -= OnTabletShattered;
        }

        private void OnTabletChanged(TabletMaterialSO material, int stage, bool isMilestone)
        {
            _material = material;
            spriteRenderer.color = isMilestone ? MilestoneTint : Color.white;
            SetCrackState(0);
            TintParticles(material.BaseColor);
        }

        private void OnHpChanged(BigDouble current, BigDouble max)
        {
            SetCrackState(CrackStateForHp(current, max));
        }

        private void OnTabletDamaged(DamageInfo info)
        {
            if (!info.FromClick)
            {
                return;
            }

            _punch = Mathf.Max(_punch, info.IsCrit ? 0.85f : 0.55f);
            if (tapDust != null)
            {
                tapDust.Emit(info.IsCrit ? 8 : 3);
            }
        }

        private void OnTabletShattered(string materialId, int stage)
        {
            _punch = 1f;
            if (shatterBurst != null)
            {
                shatterBurst.Emit(48);
            }
        }

        private void Update()
        {
            if (_punch <= 0f)
            {
                return;
            }

            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 6f);
            transform.localScale = _baseScale * (1f - 0.08f * _punch);
        }

        private void TintParticles(Color color)
        {
            var dark = color * 0.6f;
            dark.a = 1f;
            if (shatterBurst != null)
            {
                var main = shatterBurst.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color, dark);
            }

            if (tapDust != null)
            {
                var main = tapDust.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color, dark);
            }
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
