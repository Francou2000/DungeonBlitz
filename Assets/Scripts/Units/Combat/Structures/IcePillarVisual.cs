using UnityEngine;

public class IcePillarVisual : MonoBehaviour
{
    public StructureBase bound;              // set by manager
    public Color fullHP = Color.white;
    public Color lowHP = new Color(1f, .6f, .6f);
    SpriteRenderer _sr;

    void Awake() { _sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (!bound || !_sr || bound.MaxHP <= 0) return;
        float t = Mathf.Clamp01(1f - (bound.HP / (float)bound.MaxHP));
        _sr.color = Color.Lerp(fullHP, lowHP, t);  // tint as it takes damage
        // small idle bob
        transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 1.5f) * 0.02f, 0f);
    }
}
