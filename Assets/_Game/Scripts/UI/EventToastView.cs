using BreakInfinity;
using Crumble.Core;
using Crumble.Data;
using Crumble.Numerics;
using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// A single HUD toast line for rare happy moments: artifact finds, expedition returns,
    /// cleared sandstorms. New toasts overwrite the current one; fades out on a timer.
    /// </summary>
    public sealed class EventToastView : MonoBehaviour
    {
        [SerializeField] private Text toastText;
        [SerializeField] private float showSeconds = 2.5f;

        private float _life;
        private Color _baseColor;

        private void Awake()
        {
            if (toastText != null)
            {
                _baseColor = toastText.color;
                toastText.text = "";
            }
        }

        private void OnEnable()
        {
            GameEvents.ArtifactDropped += OnArtifactDropped;
            GameEvents.ExpeditionCollected += OnExpeditionCollected;
            GameEvents.SandstormCleared += OnSandstormCleared;
        }

        private void OnDisable()
        {
            GameEvents.ArtifactDropped -= OnArtifactDropped;
            GameEvents.ExpeditionCollected -= OnExpeditionCollected;
            GameEvents.SandstormCleared -= OnSandstormCleared;
        }

        private void OnArtifactDropped(ArtifactSO artifact)
        {
            Show("FOUND: " + artifact.DisplayName + "!");
        }

        private void OnExpeditionCollected(BigDouble coins, ArtifactSO artifact)
        {
            Show("EXPEDITION RETURNED  +" + NumberFormatter.Format(coins) + " coins");
        }

        private void OnSandstormCleared(BigDouble reward)
        {
            Show("SANDSTORM CLEARED  +" + NumberFormatter.Format(reward) + " coins");
        }

        private void Show(string message)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastText.color = _baseColor;
            _life = showSeconds;
        }

        private void Update()
        {
            if (_life <= 0f || toastText == null)
            {
                return;
            }

            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                toastText.text = "";
                return;
            }

            if (_life < 1f)
            {
                var color = _baseColor;
                color.a = _life;
                toastText.color = color;
            }
        }
    }
}
