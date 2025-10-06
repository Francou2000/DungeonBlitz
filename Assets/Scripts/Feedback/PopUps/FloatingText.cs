using UnityEngine;
using TMPro;

namespace SpatialUI
{
    [RequireComponent(typeof(Canvas))]
    public class FloatingText : MonoBehaviour
    {
        public TextMeshProUGUI label;
        public Canvas canvas;
        public RectTransform rect;
        public float bornTime;
        public float lifetime;
        public Vector3 worldStart;
        public Vector3 worldEnd;
        public AnimationCurve alphaCurve;
        public AnimationCurve verticalCurve;
        public AnimationCurve scaleCurve;

        Camera _cam;
        Transform _follow;

        void Awake()
        {
            if (!rect) rect = GetComponent<RectTransform>();
            if (!canvas) canvas = GetComponent<Canvas>();
            _cam = Camera.main;
        }

        public void Setup(Transform follow, Vector3 startWorld, Vector3 endWorld, float life,
                          TMP_FontAsset font, int fontSize, Color color, float outlineWidth, Color outlineColor,
                          AnimationCurve alpha, AnimationCurve vertical, AnimationCurve scale, bool useGradient, VertexGradient gradient)
        {
            _follow = follow;
            worldStart = startWorld;
            worldEnd = endWorld;
            lifetime = life;
            bornTime = Time.time;

            if (_cam == null) _cam = Camera.main;
            canvas.renderMode = RenderMode.WorldSpace;

            if (label == null)
            {
                var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                label = go.GetComponent<TextMeshProUGUI>();
            }

            label.font = font;
            label.fontSize = fontSize;
            label.color = color;
            label.outlineWidth = outlineWidth;
            label.outlineColor = outlineColor;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = TextAlignmentOptions.Center;
            label.enableVertexGradient = useGradient;
            if (useGradient) label.colorGradient = gradient;

            alphaCurve = alpha;
            verticalCurve = vertical;
            scaleCurve = scale;

            transform.localScale = Vector3.one;
        }

        public void SetText(string text) => label.text = text;

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            transform.forward = (_cam.transform.position - transform.position) * -1f;

            float t = Mathf.Clamp01((Time.time - bornTime) / lifetime);
            var pos = Vector3.Lerp(worldStart, worldEnd, verticalCurve.Evaluate(t));
            if (_follow) pos += (_follow.position - worldStart);
            transform.position = pos;

            float a = alphaCurve.Evaluate(t);
            var c = label.color; c.a = a; label.color = c;

            float s = scaleCurve.Evaluate(t);
            transform.localScale = new Vector3(s, s, s);

            if (t >= 1f) SpatialUIManager.Instance.Release(this);
        }
    }
}
