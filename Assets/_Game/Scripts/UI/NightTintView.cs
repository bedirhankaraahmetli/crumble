using Crumble.Core;
using UnityEngine;

namespace Crumble.UI
{
    /// <summary>Darkens the camera background at night (GDD §6: reduced visibility).</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class NightTintView : MonoBehaviour
    {
        [SerializeField] private Color dayColor = new Color(0.10f, 0.09f, 0.08f);
        [SerializeField] private Color nightColor = new Color(0.03f, 0.04f, 0.10f);

        private Camera _camera;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable() => GameEvents.NightChanged += OnNightChanged;
        private void OnDisable() => GameEvents.NightChanged -= OnNightChanged;

        private void OnNightChanged(bool isNight)
        {
            _camera.backgroundColor = isNight ? nightColor : dayColor;
        }
    }
}
