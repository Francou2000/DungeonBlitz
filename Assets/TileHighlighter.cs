using UnityEngine;

public class TileHighlighter : MonoBehaviour
{
    Vector2 tile_offset = new Vector3(0.5f, 0.5f);
    bool follow_mouse = false;

    [SerializeField] Color enemy;
    [SerializeField] Color ally;
    [SerializeField] Color zones;

    [SerializeField] bool is_enemy;
    [SerializeField] bool is_ally;
    [SerializeField] bool is_zone;

    SpriteRenderer sprite_renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite_renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = TileMapManager.Instance.Actual_tile + tile_offset;
        if (is_enemy) sprite_renderer.color = enemy;
        if (is_ally) sprite_renderer.color = ally;
        if (is_zone) sprite_renderer.color = zones;
    }
}
