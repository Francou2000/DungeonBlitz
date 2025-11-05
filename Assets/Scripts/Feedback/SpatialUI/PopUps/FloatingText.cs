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

        [HideInInspector] public float baseScale = 1f;

        Camera _cam;
        Transform _follow;
        Vector3 _initialAnchor; // Guardar el anclaje inicial para calcular el offset correctamente

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
            
            // Guardar el anclaje inicial para calcular el offset correctamente cuando el objeto se mueva
            _initialAnchor = follow ? SpatialUIManager.GetAnchor(follow) : startWorld;

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
            
            // Calcular posición base usando el punto de anclaje actualizado para mantener el texto centrado
            Vector3 currentAnchor = _follow ? SpatialUIManager.GetAnchor(_follow) : worldStart;
            // Calcular el offset vertical desde el anclaje inicial
            Vector3 offsetFromAnchor = worldStart - _initialAnchor;
            
            // Si el objeto seguido se movió, ajustar la posición manteniendo el offset relativo
            Vector3 basePos = currentAnchor + offsetFromAnchor;
            Vector3 endPos = currentAnchor + (worldEnd - _initialAnchor);
            var pos = Vector3.Lerp(basePos, endPos, verticalCurve.Evaluate(t));
            
            transform.position = pos;

            float a = alphaCurve.Evaluate(t);
            var c = label.color; c.a = a; label.color = c;

            float s = scaleCurve.Evaluate(t) * baseScale;
            transform.localScale = new Vector3(s, s, s);

            if (t >= 1f) SpatialUIManager.Instance.Release(this);
        }
    }
}
