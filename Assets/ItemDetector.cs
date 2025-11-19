using UnityEngine;

public class ItemDetector : MonoBehaviour
{
    HeroesShopManager heroesShopManager;
    public ItemData my_item;
    public int my_pedestal;
    [SerializeField] SpriteRenderer my_spriteRenderer;

    void Start()
    {
        heroesShopManager = HeroesShopManager.instance;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        heroesShopManager.ShowNewBuyUI(my_item, my_pedestal);
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        heroesShopManager.HideBuyUI();
    }

    public void SetItem(ItemData item, int pedestal)
    {
        my_spriteRenderer.sprite = item.sprite;
        my_item = item;
        my_pedestal = pedestal;
    }
}