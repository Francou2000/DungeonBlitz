using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;

public class HeroesShopManager : MonoBehaviour
{
    public static HeroesShopManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Shop time (seconds)")]
    [SerializeField] float time_limit;
    float time = 0;

    [Header("Items")]
    [SerializeField] GameObject item_prefab;
    [SerializeField] ItemData[] items_paladin;
    [SerializeField] ItemData[] items_sorcerer;
    [SerializeField] ItemData[] items_rogue;
    [SerializeField] ItemData[] items_elementalist;
    [SerializeField] ItemData[] items_consumable;

    [Header("Items pedestals")]
    [SerializeField] Transform[] heroe_pedestals;

    [SerializeField] Transform[] r_item_pedestals;
    [UnityEngine.Range(0, 100)]
    [SerializeField] int r_empty_chance;
    
    [SerializeField] Transform[] c_item_pedestals;
    [UnityEngine.Range(0, 100)]
    [SerializeField] int c_empty_chance;

    void Start()
    {
        SpawnHeroesItems();
        SpawnRandomItems();
        SpawnConsumableItems();
    }


    void SpawnHeroesItems()
    {
        var heroes = UnitLoaderController.Instance.playable_heroes;
        for (int i = 0; i < heroes.Length; i++)
        {
            ItemData item = GetRandomItem(heroes[i].heroe_id);
            GameObject new_item = Instantiate(item_prefab, heroe_pedestals[i]);
            new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }

    }

    void SpawnRandomItems()
    {
        for (int i = 0; i < r_item_pedestals.Length; i++)
        {
            if (Random.Range(0, 100) < r_empty_chance) continue;
            ItemData item = GetRandomItem();
            GameObject new_item = Instantiate(item_prefab, r_item_pedestals[i]);
            new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }
    }

    void SpawnConsumableItems()
    {
        for (int i = 0; i < c_item_pedestals.Length; i++)
        {
            if (Random.Range(0, 10) < c_empty_chance) continue;
            ItemData item = items_consumable[Random.Range(0, items_consumable.Length)];
            GameObject new_item = Instantiate(item_prefab, c_item_pedestals[i]);
            new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }
    }

    ItemData GetRandomItem(HeroesList item_pool = HeroesList.None)
    {
        int id;
        switch (item_pool)
        {
            case HeroesList.Paladin:
                id =  Random.Range(0, items_paladin.Length);
                return items_paladin[id];
            case HeroesList.Elementalist:
                id = Random.Range(0, items_elementalist.Length);
                return items_elementalist[id];
            case HeroesList.Sorcerer:
                id = Random.Range(0, items_sorcerer.Length);
                return items_sorcerer[id];
            case HeroesList.Rogue:
                id = Random.Range(0, items_rogue.Length);
                return items_rogue[id];
            case HeroesList.None:
                return GetRandomItem((HeroesList)Random.Range(1, 5));
        }
        Debug.LogError("Error al generar item random");
        return null;

    }
}
