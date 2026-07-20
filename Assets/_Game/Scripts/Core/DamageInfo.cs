using BreakInfinity;
using UnityEngine;

namespace Crumble.Core
{
    /// <summary>
    /// One damage application, as broadcast on GameEvents.TabletDamaged. A struct so the
    /// per-tap event stays allocation-free. ScreenPosition is where the press landed
    /// (floating numbers spawn there) — zero when unknown (DPS ticks, scripted taps).
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly BigDouble Amount;
        public readonly bool FromClick;
        public readonly bool IsCrit;
        public readonly double CritMultiplier;
        public readonly Vector2 ScreenPosition;

        public DamageInfo(BigDouble amount, bool fromClick, bool isCrit, double critMultiplier, Vector2 screenPosition)
        {
            Amount = amount;
            FromClick = fromClick;
            IsCrit = isCrit;
            CritMultiplier = critMultiplier;
            ScreenPosition = screenPosition;
        }
    }
}
