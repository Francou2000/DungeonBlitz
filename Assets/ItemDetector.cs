using UnityEngine;

public class ItemDetector : MonoBehaviour
{
    HeroesShopManager heroesShopManager;
    public ItemData my_item;
    public int my_pedestal;
    [SerializeField] SpriteRenderer my_spriteRenderer;
    [SerializeField] Animator my_animator;

    void Start()
    {
        heroesShopManager = HeroesShopManager.instance;
    }

    public void ShowUI()
    {
        Debug.Log("C");
        heroesShopManager.ShowNewBuyUI(my_item, my_pedestal);
    }

    public void HideUI()
    {
        heroesShopManager.HideBuyUI();
    }

    public void SetItem(ItemData item, int pedestal)
    {
        my_animator.SetTrigger("Highlight");
        my_spriteRenderer.sprite = item.sprite;
        my_item = item;
        my_pedestal = pedestal;
    }
}