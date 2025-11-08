using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroesShopManager : MonoBehaviourPunCallbacks
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
    [Range(0, 100)]
    [SerializeField] int r_empty_chance;
    
    [SerializeField] Transform[] c_item_pedestals;
    [Range(0, 100)]
    [SerializeField] int c_empty_chance;

    [Header("UI panels")]
    [SerializeField] GameObject item_to_buy_ui;
    [SerializeField] TextMeshProUGUI item_name;
    [SerializeField] TextMeshProUGUI item_cost;
    [SerializeField] TextMeshProUGUI item_stat_description;
    [SerializeField] TextMeshProUGUI item_effect_description;
    [SerializeField] GameObject purchase_button;
    [SerializeField] GameObject try_purchase_button;

    public int sec;

    void Start()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        SpawnHeroesItems();
        SpawnRandomItems();
        SpawnConsumableItems();
    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------Item Spawners------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------
    void SpawnHeroesItems()
    {
        var heroes = UnitLoaderController.Instance.playable_heroes;
        for (int i = 0; i < heroes.Length; i++)
        {
            ItemData item = GetRandomItem(heroes[i].heroe_id);

            SpawnItem(heroe_pedestals[i], item);

            photonView.RPC(nameof(SpawnItem), RpcTarget.Others);


            //GameObject new_item = Instantiate(item_prefab, heroe_pedestals[i]);
            //new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }

    }

    void SpawnRandomItems()
    {
        for (int i = 0; i < r_item_pedestals.Length; i++)
        {
            if (Random.Range(0, 100) < r_empty_chance) continue;
            ItemData item = GetRandomItem();
            SpawnItem(r_item_pedestals[i], item);



            //GameObject new_item = Instantiate(item_prefab, r_item_pedestals[i]);
            //new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }
    }

    void SpawnConsumableItems()
    {
        for (int i = 0; i < c_item_pedestals.Length; i++)
        {
            if (Random.Range(0, 10) < c_empty_chance) continue;
            ItemData item = items_consumable[Random.Range(0, items_consumable.Length)];
            SpawnItem(c_item_pedestals[i], item);



            //GameObject new_item = Instantiate(item_prefab, c_item_pedestals[i]);
            //new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
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

    [PunRPC]
    public void SpawnItem(Transform spawn_pos, ItemData item)
    {
        if (PhotonNetwork.IsMasterClient) return;
        GameObject new_item = Instantiate(item_prefab, spawn_pos);
        new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
        //TODO: Add SetShopValues() logic

        new_item.GetComponent<ItemDetector>().my_item = item;
    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    //---------------------------------------------------------Item Interaction/UI---------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------

    public void ShowBuyUI(ItemData item)
    {
        item_to_buy_ui.SetActive(true);
        item_name.text = item.name;

        item_cost.text = "Cost: " + item.cost.ToString() + "s - " + sec + "vs available";
        if (item.cost <= sec)
        {
            purchase_button.SetActive(true);
            try_purchase_button.SetActive(false);
        }
        else
        {
            try_purchase_button.SetActive(true);
            purchase_button.SetActive(false);
        }

            string hp = item.maxHP > 0 ? " +" + item.maxHP.ToString() + " HP" : item.maxHP < 0 ? " -" + item.maxHP.ToString() + " HP" : "";
        string performance     = item.performance     > 0 ? " +" + item.performance.ToString()     + " performance"     : item.performance     < 0 ? " -" + item.performance.ToString()     + " performance"     : "";
        string affinity        = item.maxHP           > 0 ? " +" + item.affinity.ToString()        + " affinity"        : item.affinity        < 0 ? " -" + item.affinity.ToString()        + " affinity"        : "";
        string armor           = item.armor           > 0 ? " +" + item.armor.ToString()           + " armor"           : item.armor           < 0 ? " -" + item.armor.ToString()           + " armor"           : "";
        string magicResistance = item.magicResistance > 0 ? " +" + item.magicResistance.ToString() + " magicResistance" : item.magicResistance < 0 ? " -" + item.magicResistance.ToString() + " magicResistance" : "";
        string strength        = item.strength        > 0 ? " +" + item.strength.ToString()        + " strength"        : item.strength        < 0 ? " -" + item.strength.ToString()        + " strength"        : "";
        string magicPower      = item.magicPower      > 0 ? " +" + item.magicPower.ToString()      + " magicPower"      : item.magicPower      < 0 ? " -" + item.magicPower.ToString()      + " magicPower"      : "";
        item_stat_description.text = hp + performance + affinity + armor + magicResistance + strength + magicPower;
        
        item_effect_description.text = "Unlock Action: " + item.new_ability.name;


    }

    public void HideBuyUI(ItemData item)
    {
        item_to_buy_ui.SetActive(false);
    }
}
