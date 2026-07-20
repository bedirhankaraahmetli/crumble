using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One pooled floating damage number. Rises and fades, then returns itself to the
    /// pool — never destroyed (mobile rule: no per-tap allocations). Style (size, color)
    /// is set per show so the same instance serves normal hits and crits.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class FloatingText : MonoBehaviour
    {
        private const float LifetimeSeconds = 0.7f;
        private const float RiseSpeed = 220f; // screen px/sec

        private Text _text;
        private FloatingTextPool _pool;
        private float _life;
        private Color _baseColor;

        public void Init(FloatingTextPool pool)
        {
            _pool = pool;
            _text = GetComponent<Text>();
        }

        public void Show(string value, Vector2 screenPosition, int fontSize, Color color)
        {
            _text.text = value;
            _text.fontSize = fontSize;
            _baseColor = color;
            _text.color = color;
            transform.position = screenPosition;
            _life = LifetimeSeconds;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                gameObject.SetActive(false);
                _pool.Return(this);
                return;
            }

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            var color = _baseColor;
            color.a = Mathf.Clamp01(_life / (LifetimeSeconds * 0.5f)); // fade out in last half
            _text.color = color;
        }
    }
}
