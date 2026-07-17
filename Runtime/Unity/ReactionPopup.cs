using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    [AddComponentMenu("Virtual Room/Reaction Popup")]
    public class ReactionPopup : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 0.7f;
        [SerializeField] private float lifetime = 1.4f;
        [SerializeField] private float popScale = 1.35f;

        private TextMesh _text;
        private Renderer _renderer;
        private Camera _camera;
        private float _age;
        private Vector3 _baseScale;
        private Color _color;

        public virtual void Play(ReactionType reaction, float intensity, Camera billboardTarget)
        {
            _camera = billboardTarget;
            _age = 0f;
            _color = ReactionPalette.ColorOf(reaction);

            _text = GetComponentInChildren<TextMesh>();
            _renderer = GetComponentInChildren<Renderer>();

            if (_text != null)
            {
                _text.text = Reactions.GlyphOf(reaction);
                _text.color = _color;
            }
            else if (_renderer != null)
            {
                _renderer.material.color = _color;
            }

            _baseScale = Vector3.one * Mathf.Lerp(0.7f, 1.15f, intensity);
            transform.localScale = _baseScale * popScale;

            gameObject.SetActive(true);
        }

        protected virtual void Update()
        {
            _age += Time.deltaTime;

            if (_age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _age / lifetime;

            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            float scale = Mathf.Lerp(popScale, 1f, Mathf.Clamp01(t * 6f));
            transform.localScale = _baseScale * scale;

            float alpha = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
            SetAlpha(alpha);

            Billboard();
        }

        private void SetAlpha(float alpha)
        {
            var c = new Color(_color.r, _color.g, _color.b, alpha);

            if (_text != null) _text.color = c;
            else if (_renderer != null) _renderer.material.color = c;
        }

        private void Billboard()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 toCamera = transform.position - _camera.transform.position;
            toCamera.y = 0f;

            if (toCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
            }
        }
    }

    public static class ReactionPalette
    {
        public static Color ColorOf(ReactionType reaction)
        {
            Rgb rgb = Reactions.ColorOf(reaction);
            return new Color(rgb.R, rgb.G, rgb.B);
        }
    }
}
