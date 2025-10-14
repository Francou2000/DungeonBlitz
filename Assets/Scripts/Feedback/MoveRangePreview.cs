using UnityEngine;

public class MoveRangePreview : MonoBehaviour
{
    public static MoveRangePreview Instance;

    [SerializeField] Transform ringRoot;   // Assign: the RangeRing child (the GameObject that holds the LineRenderer)
    [SerializeField] float defaultRadius = 3f;

    void Awake()
    {
        Instance = this;
        if (ringRoot) ringRoot.gameObject.SetActive(false);
    }

    public static void Show(Transform origin, float radius)
    {
        if (!Instance || !Instance.ringRoot) return;
        Instance.ringRoot.position = new Vector3(origin.position.x, origin.position.y, 0f);

        // If your ring is a unit circle drawn with LineRenderer, scale by diameter
        Instance.ringRoot.localScale = Vector3.one * (radius * 2f);

        Instance.ringRoot.gameObject.SetActive(true);
    }

    public static void HideStatic()
    {
        if (Instance && Instance.ringRoot)
            Instance.ringRoot.gameObject.SetActive(false);
    }
}
