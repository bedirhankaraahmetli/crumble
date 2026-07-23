using Crumble.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Crumble.UI
{
    /// <summary>
    /// Sits on the sandstorm overlay's raycastable image and feeds drag distance into
    /// SandstormManager. UI calling a manager method — allowed by the architecture rules.
    /// </summary>
    public sealed class SandstormSwipeArea : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            if (SandstormManager.Instance != null)
            {
                SandstormManager.Instance.RegisterSwipe(eventData.delta.magnitude);
            }
        }
    }
}
