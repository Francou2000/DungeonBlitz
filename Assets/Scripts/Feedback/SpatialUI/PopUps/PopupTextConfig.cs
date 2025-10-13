using UnityEngine;
using TMPro;

namespace SpatialUI
{
    public enum DamageUiType { Physical, Magical, Fire, Frost, Electric, Heal, Miss }

    [CreateAssetMenu(fileName = "PopupTextConfig", menuName = "UI/Popup Text Config")]
    public class PopupTextConfig : ScriptableObject
    {
        [Header("Font")]
        public TMP_FontAsset font; // TMP Font Asset from your downloaded font.

        [Header("Colors")]
        public Color physical = new Color(1f, 0.55f, 0f);
        public Color magical  = new Color(0.66f, 0.33f, 1f);
        public Color fire     = new Color(1f, 0.25f, 0.1f);
        public Color frost    = new Color(0.5f, 0.85f, 1f);
        public Color electric = new Color(1f, 0.95f, 0.2f);
        public Color heal     = new Color(0.2f, 1f, 0.35f);
        public Color miss     = new Color(0.9f, 0.9f, 0.9f);

        [Header("Typeface")]
        [Min(8)] public int baseFontSize = 36;
        [Range(0f, 1f)] public float outlineWidth = 0.15f;
        public Color outlineColor = Color.black;
        public bool useGradient = false;
        public VertexGradient gradient = new VertexGradient(Color.white, Color.white, new Color(1,1,1,0.8f), new Color(1,1,1,0.8f));

        [Header("Animation")]
        [Min(0.1f)] public float lifetime = 0.9f;
        [Min(0.01f)] public float riseDistance = 1.2f;
        [Min(0f)] public float randomX = 0.25f;
        [Range(0f,1f)] public float popScale = 1.15f;
        [Range(0f,1f)] public float settleScale = 0.95f;
        public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0,1,1,0);
        public AnimationCurve verticalCurve = AnimationCurve.EaseInOut(0,0,1,1);
        public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0,1.15f,0,-0.6f), new Keyframe(0.15f,1.0f), new Keyframe(1,0.95f));

        [Header("Critical")]
        public bool enableCritStyle = true;
        public float critScale = 1.35f;
        public Color critColor = new Color(1f, 0.95f, 0.6f);

        [Header("Offsets")]
        public float verticalOffset = 1.6f;

        [Header("World Scale (URP 2D)")]
        [Min(0.0001f)] public float worldScale = 0.02f;
    }
}
