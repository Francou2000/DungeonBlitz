using UnityEngine;

public class ItemDetector : MonoBehaviour
{
    HeroesShopManager heroesShopManager;
    public ItemData my_item;

    void Start()
    {
        heroesShopManager = HeroesShopManager.instance;
    }


    public void OnCollisionEnter2D(Collision2D collision)
    {
        heroesShopManager.ShowNewBuyUI(my_item);
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        heroesShopManager.HideBuyUI();
    }
}