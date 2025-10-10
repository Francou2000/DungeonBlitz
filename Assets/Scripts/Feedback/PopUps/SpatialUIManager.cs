using UnityEngine;
using TMPro;

namespace SpatialUI
{
    public class SpatialUIManager : MonoBehaviour
    {
        public static SpatialUIManager Instance { get; private set; }
        [Header("Config & Pool")]
        public PopupTextConfig config;
        public FloatingTextPool pool;
        public FloatingText textPrefab;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (pool == null) pool = gameObject.AddComponent<FloatingTextPool>();
            if (pool.prefab == null) pool.prefab = textPrefab;
            pool.Init();
        }

        public void ShowDamage(Transform target, float amount, DamageUiType type, bool crit = false)
        {
            var col = GetColor(type);
            var text = Mathf.RoundToInt(amount).ToString();
            if (crit && config.enableCritStyle)
            {
                text = "<size=110%><b>" + text + "</b></size>";
                col = Color.Lerp(col, config.critColor, 0.45f);
            }
            Spawn(target, text, col, crit);
        }

        public void ShowMiss(Transform target)
        {
            Spawn(target, "<b>MISS!</b>", config.miss, false);
        }

        public void ShowHeal(Transform target, float amount)
        {
            var text = "+" + Mathf.RoundToInt(amount);
            Spawn(target, text, config.heal, false);
        }

        void Spawn(Transform target, string text, Color color, bool crit)
        {
            Vector3 basePos = GetAnchor(target) + Vector3.up * config.verticalOffset;
            Vector3 endPos = basePos + new Vector3(Random.Range(-config.randomX, config.randomX), config.riseDistance, 0f);

            var ft = pool.Get();
            ft.transform.position = basePos;
            ft.baseScale = config ? Mathf.Max(0.0001f, config.worldScale) : 0.02f;       

            var fontSize = config.baseFontSize;
            if (crit && config.enableCritStyle) fontSize = Mathf.RoundToInt(config.baseFontSize * config.critScale);

            ft.Setup(
                follow: target,
                startWorld: basePos,
                endWorld: endPos,
                life: config.lifetime,
                font: config.font,
                fontSize: fontSize,
                color: color,
                outlineWidth: config.outlineWidth,
                outlineColor: config.outlineColor,
                alpha: config.alphaCurve,
                vertical: config.verticalCurve,
                scale: config.scaleCurve,
                useGradient: config.useGradient,
                gradient: config.gradient
            );
            ft.SetText(text);
        }

        public void Release(FloatingText ft) => pool.Release(ft);

        Color GetColor(DamageUiType type)
        {
            switch (type)
            {
                case DamageUiType.Physical: return config.physical;
                case DamageUiType.Magical:  return config.magical;
                case DamageUiType.Fire:     return config.fire;
                case DamageUiType.Frost:    return config.frost;
                case DamageUiType.Electric: return config.electric;
                case DamageUiType.Heal:     return config.heal;
                case DamageUiType.Miss:     return config.miss;
                default: return Color.white;
            }
        }

        Vector3 GetAnchor(Transform t)
        {
            var rend = t.GetComponentInChildren<Renderer>();
            if (rend) return rend.bounds.center + Vector3.up * (rend.bounds.extents.y * 0.8f);
            return t.position;
        }
    }
}
