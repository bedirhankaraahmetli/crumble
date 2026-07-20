using Crumble.Core;
using Crumble.Gameplay;
using UnityEngine;

namespace Crumble.UI
{
    /// <summary>
    /// Positional camera shake on shatters (bigger for milestone bosses) and fever start.
    /// Subscribe-only; decaying random offset, restores the exact base position.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private float shatterAmplitude = 0.12f;
        [SerializeField] private float shatterDuration = 0.18f;
        [SerializeField] private float milestoneAmplitude = 0.32f;
        [SerializeField] private float milestoneDuration = 0.4f;
        [SerializeField] private float feverAmplitude = 0.2f;
        [SerializeField] private float feverDuration = 0.3f;

        private Vector3 _basePosition;
        private float _remaining;
        private float _duration;
        private float _amplitude;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            GameEvents.TabletShattered += OnTabletShattered;
            GameEvents.FeverStarted += OnFeverStarted;
        }

        private void OnDisable()
        {
            GameEvents.TabletShattered -= OnTabletShattered;
            GameEvents.FeverStarted -= OnFeverStarted;
        }

        private void OnTabletShattered(string materialId, int stage)
        {
            // event fires before the next tablet spawns, so IsMilestone is still accurate
            var milestone = TabletManager.Instance != null && TabletManager.Instance.IsMilestone;
            Shake(milestone ? milestoneAmplitude : shatterAmplitude,
                milestone ? milestoneDuration : shatterDuration);
        }

        private void OnFeverStarted(double duration)
        {
            Shake(feverAmplitude, feverDuration);
        }

        private void Shake(float amplitude, float duration)
        {
            _amplitude = Mathf.Max(_amplitude, amplitude);
            _duration = duration;
            _remaining = duration;
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                transform.localPosition = _basePosition;
                _amplitude = 0f;
                return;
            }

            var falloff = _remaining / _duration;
            var offset = Random.insideUnitCircle * (_amplitude * falloff);
            transform.localPosition = _basePosition + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
