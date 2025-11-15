using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
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
    public int volatile_seconds;

    [Header("Items")]
    [SerializeField] ItemRarityWeigt[] rarity_weigts;
    Dictionary<Rarity, int> rarity_weigt;
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

    [Header("Item panel")]
    [SerializeField] GameObject item_to_buy_ui;
    [SerializeField] TextMeshProUGUI item_name;
    [SerializeField] TextMeshProUGUI item_cost;
    [SerializeField] TextMeshProUGUI item_stat_description;
    [SerializeField] TextMeshProUGUI item_effect_description;
    [SerializeField] GameObject purchase_button;
    [SerializeField] GameObject try_purchase_button;

    [Header("Voting panel 1")]
    [SerializeField] GameObject voting_panel_1;
    [SerializeField] GameObject voting_panel_2;
    [SerializeField] TextMeshProUGUI player_who_ask;
    [SerializeField] TextMeshProUGUI item_wanted;
    [SerializeField] TextMeshProUGUI left_cost;
    [SerializeField] GameObject waiting_txt;
    [SerializeField] GameObject yes_button;
    [SerializeField] GameObject nay_button;
    [SerializeField] GameObject purchase_canceled;
    [SerializeField] GameObject purchase_denied;

    [Header("Votes settings")]
    [SerializeField] GameObject votes;
    [SerializeField] Image[] vote_list;
    [SerializeField] Color default_color;
    [SerializeField] Color positive_color;
    [SerializeField] Color negative_color;
    int actual_vote = 0;
    int positive_votes = 0;



    void Start()
    {
        if (UnitLoaderController.Instance.lvl == 2)
        {
            foreach (ItemRarityWeigt rar in rarity_weigts)
            {
                rarity_weigt[rar.rarity] = rar.weigt_shop_1;
            }
        }
        if (UnitLoaderController.Instance.lvl == 3)
        {
            foreach (ItemRarityWeigt rar in rarity_weigts)
            {
                rarity_weigt[rar.rarity] = rar.weigt_shop_2;
            }
        }
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
            if (UnityEngine.Random.Range(0, 100) < r_empty_chance) continue;
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
            if (UnityEngine.Random.Range(0, 10) < c_empty_chance) continue;
            ItemData item = items_consumable[UnityEngine.Random.Range(0, items_consumable.Length)];
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
                id =  UnityEngine.Random.Range(0, items_paladin.Length);
                return items_paladin[id];
            case HeroesList.Elementalist:
                id = UnityEngine.Random.Range(0, items_elementalist.Length);
                return items_elementalist[id];
            case HeroesList.Sorcerer:
                id = UnityEngine.Random.Range(0, items_sorcerer.Length);
                return items_sorcerer[id];
            case HeroesList.Rogue:
                id = UnityEngine.Random.Range(0, items_rogue.Length);
                return items_rogue[id];
            case HeroesList.None:
                return GetRandomItem((HeroesList)UnityEngine.Random.Range(1, 5));
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

    ItemData actual_item;
    public void ShowNewBuyUI(ItemData item)
    {
        item_to_buy_ui.SetActive(true);
        actual_item = item;
        item_name.text = actual_item.name;

        //actual_item_cost = item.cost;
        item_cost.text = "Cost: " + actual_item.cost.ToString() + "s - " + volatile_seconds + "vs available";
        if (actual_item.cost <= volatile_seconds)
        {
            purchase_button.SetActive(true);
            try_purchase_button.SetActive(false);
        }
        else
        {
            try_purchase_button.SetActive(true);
            purchase_button.SetActive(false);
        }

        string hp              = actual_item.maxHP           > 0 ? " +" + actual_item.maxHP.ToString()           + " HP"              : actual_item.maxHP           < 0 ? " -" + actual_item.maxHP.ToString()           + " HP"              : "";
        string performance     = actual_item.performance     > 0 ? " +" + actual_item.performance.ToString()     + " performance"     : actual_item.performance     < 0 ? " -" + actual_item.performance.ToString()     + " performance"     : "";
        string affinity        = actual_item.maxHP           > 0 ? " +" + actual_item.affinity.ToString()        + " affinity"        : actual_item.affinity        < 0 ? " -" + actual_item.affinity.ToString()        + " affinity"        : "";
        string armor           = actual_item.armor           > 0 ? " +" + actual_item.armor.ToString()           + " armor"           : actual_item.armor           < 0 ? " -" + actual_item.armor.ToString()           + " armor"           : "";
        string magicResistance = actual_item.magicResistance > 0 ? " +" + actual_item.magicResistance.ToString() + " magicResistance" : actual_item.magicResistance < 0 ? " -" + actual_item.magicResistance.ToString() + " magicResistance" : "";
        string strength        = actual_item.strength        > 0 ? " +" + actual_item.strength.ToString()        + " strength"        : actual_item.strength        < 0 ? " -" + actual_item.strength.ToString()        + " strength"        : "";
        string magicPower      = actual_item.magicPower      > 0 ? " +" + actual_item.magicPower.ToString()      + " magicPower"      : actual_item.magicPower      < 0 ? " -" + actual_item.magicPower.ToString()      + " magicPower"      : "";
        item_stat_description.text = hp + performance + affinity + armor + magicResistance + strength + magicPower;
        
        item_effect_description.text = "Unlock Action: " + actual_item.new_ability.name;


    }

    public void HideBuyUI()
    {
        item_to_buy_ui.SetActive(false);
    }
    public void ShowBuyUI()
    {
        item_to_buy_ui.SetActive(true);
    }

    public void TryPurchase()
    {
        int diference = actual_item.cost - volatile_seconds;
        if (diference > 0) photonView.RPC("AskForPurchase", RpcTarget.Others, diference, PhotonNetwork.NickName, actual_item.name);
        WaitForAnswer();
    }

    public void WaitForAnswer()
    {
        voting_panel_1.SetActive(true);
        votes.SetActive(true);
        HideBuyUI();
    }

    public void HideAskUI()
    {
        voting_panel_1.SetActive(false);
        voting_panel_2.SetActive(false);
        
        foreach (Image image in vote_list)
        {
            image.color = default_color;
        }

        votes.SetActive(false);       
    }

    [PunRPC]
    public void AskForPurchase(int time_asked, string player_name, string item_name)
    {
        if (PhotonNetwork.IsMasterClient) return;

        voting_panel_2.SetActive(true);
        votes.SetActive(true);
        player_who_ask.text = player_name + " is trying to purchase";
        item_wanted.text = item_name;
        left_cost.text = "This wwould cost the team " + time_asked + "s";

        yes_button.SetActive(true);
        nay_button.SetActive(true);
        waiting_txt.SetActive(false);
    }

    public void AcceptPurchase()
    {
        photonView.RPC("UpdateVotingList", RpcTarget.All, true);
        yes_button.SetActive(false);
        nay_button.SetActive(false);
        waiting_txt.SetActive(true);
    }

    public void DenyPurchase()
    {
        photonView.RPC("UpdateVotingList", RpcTarget.All, false);
        yes_button.SetActive(false);
        nay_button.SetActive(false);
        waiting_txt.SetActive(true);
    }

    public void CancelPurchase()
    {
        photonView.RPC("StopPurchase", RpcTarget.All, true);
    }

    [PunRPC]
    public void UpdateVotingList(bool accept)
    {
        if (PhotonNetwork.IsMasterClient) return;

        if (accept)
        {
            vote_list[actual_vote].color = positive_color;
            positive_votes++;
        }
        else
        {
            vote_list[actual_vote].color = negative_color;
        }
        actual_vote++;

        // Si 2 o más jugadores negaron la petición
        if (actual_vote - positive_votes >= 2)
        {
            photonView.RPC("StopPurchase", RpcTarget.All, false);
        }

        // Si todos votaron o hubo tres votos positivos
        if (actual_vote >= 4 | positive_votes >= 3)
        {

        }

    }

    [PunRPC]
    public void StopPurchase(bool canceled)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (canceled)
        {
            StartCoroutine(PurchaseCancel());
        }
        else
        {
            StartCoroutine(PurchaseDenied());
        }

    }

    IEnumerator PurchaseCancel()
    {
        HideAskUI();
        purchase_canceled.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        purchase_canceled.SetActive(false);
    }

    IEnumerator PurchaseDenied()
    {
        HideAskUI();
        purchase_denied.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        purchase_denied.SetActive(false);
    }

    public void UseVolatileSeconds(int seconds) { volatile_seconds -= seconds; }
    public void AddVolatileSeconds(int seconds) { volatile_seconds += seconds; }
}

[Serializable]
public struct ItemRarityWeigt
{
    public Rarity rarity;
    [Range(0, 100)]
    public int weigt_shop_1;
    [Range(0, 100)]
    public int weigt_shop_2;
}