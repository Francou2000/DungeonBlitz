using UnityEngine;

public class MoveOrderIndicator : MonoBehaviour
{
    private static Sprite whiteSprite;

    [SerializeField] private float lifetime = 0.45f;
    [SerializeField] private float startScale = 0.35f;
    [SerializeField] private float endScale = 0.95f;
    [SerializeField] private float barWidth = 0.08f;
    [SerializeField] private float barLength = 0.6f;
    [SerializeField] private Color color = new Color(0.2f, 0.9f, 1f, 0.9f);

    private SpriteRenderer[] parts;
    private float elapsed;

    public static void Spawn(Vector3 worldPosition, float zOffset = -0.01f)
    {
        var go = new GameObject("MoveOrderIndicator");
        go.transform.position = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z + zOffset);

        var indicator = go.AddComponent<MoveOrderIndicator>();
        indicator.BuildVisual();
    }

    private void BuildVisual()
    {
        EnsureSprite();

        parts = new SpriteRenderer[2];
        parts[0] = CreateBarPart("BarA", 45f);
        parts[1] = CreateBarPart("BarB", -45f);

        transform.localScale = Vector3.one * startScale;
    }

    private SpriteRenderer CreateBarPart(string name, float zRotation)
    {
        var child = new GameObject(name).transform;
        child.SetParent(transform, false);
        child.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        var sr = child.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = 150;

        child.localScale = new Vector3(barLength, barWidth, 1f);
        return sr;
    }

    private static void EnsureSprite()
    {
        if (whiteSprite != null) return;

        whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        float alpha = Mathf.Lerp(color.a, 0f, t);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null) continue;
            var c = color;
            c.a = alpha;
            parts[i].color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
