using Crumble.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Crumble.Gameplay
{
    /// <summary>
    /// Reads pointer presses (mouse + touch via Pointer.current) and forwards taps that
    /// hit the tablet's collider to TabletManager. Multi-touch Fever tapping lands in
    /// Step 7; a single pointer is enough for the core loop.
    /// </summary>
    public sealed class TapInputController : MonoBehaviour
    {
        [Tooltip("Physics2D layers considered tappable (the tablet).")]
        [SerializeField] private LayerMask tappableLayers = ~0;

        private Camera _camera;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            // Presses on UI (upgrade panel, buttons) must not also hit the tablet.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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

            Vector2 worldPoint = _camera.ScreenToWorldPoint(pointer.position.ReadValue());
            var hit = Physics2D.OverlapPoint(worldPoint, tappableLayers);
            if (hit != null)
            {
                TabletManager.Instance.Tap();
            }
        }
    }
}
