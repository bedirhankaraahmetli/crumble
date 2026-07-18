using UnityEngine;
using UnityEngine.UI;

namespace Crumble.UI
{
    /// <summary>
    /// One pooled floating damage number. Rises and fades, then returns itself to the
    /// pool — never destroyed (mobile rule: no per-tap allocations).
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
            _baseColor = _text.color;
        }

        public void Show(string value, Vector2 screenPosition)
        {
            _text.text = value;
            transform.position = screenPosition;
            _life = LifetimeSeconds;
            _text.color = _baseColor;
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
