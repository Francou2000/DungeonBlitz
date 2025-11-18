using UnityEngine;

public class ItemDetector : MonoBehaviour
{
    HeroesShopManager heroesShopManager;
    public ItemData my_item;
    public int my_pedestal;

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
}