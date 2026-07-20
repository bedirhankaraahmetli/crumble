using UnityEngine;

namespace Crumble.Core
{
    /// <summary>
    /// Minimal haptics wrapper. Uses the built-in vibration on device, no-op in the
    /// editor. The ship pass (Step 10) can swap in a nuanced haptics plugin (light/medium
    /// impacts) behind these same calls.
    /// </summary>
    public static class Haptics
    {
        public static void Impact()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
