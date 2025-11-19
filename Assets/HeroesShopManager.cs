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
    public int volatile_seconds;
    [SerializeField] TextMeshProUGUI remaining_time;
    [SerializeField] TextMeshProUGUI volatile_time_show;

    [Header("Items")]
    [SerializeField] ItemRarityWeigt[] rarity_weigts;
    Dictionary<Rarity, int> rarity_weigt = new Dictionary<Rarity, int>();
    [SerializeField] GameObject item_prefab;
    [SerializeField] ItemData[] items_paladin;
    [SerializeField] ItemData[] items_sorcerer;
    [SerializeField] ItemData[] items_rogue;
    [SerializeField] ItemData[] items_elementalist;
    [SerializeField] ItemData[] items_for_all;
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
    [SerializeField] GameObject purchase_accepted;

    [Header("Votes settings")]
    [SerializeField] GameObject votes;
    [SerializeField] Image[] vote_list;
    [SerializeField] Color default_color;
    [SerializeField] Color positive_color;
    [SerializeField] Color negative_color;
    int actual_vote = 1;
    int positive_votes = 1;

    UnitLoaderController unit_loader_controller;

    [Header("Votes settings")]
    UnitData[] unidades;
    void Start()
    {
        unit_loader_controller = UnitLoaderController.Instance;
        if (unit_loader_controller.lvl == 2)
        {
            foreach (ItemRarityWeigt rar in rarity_weigts)
            {
                rarity_weigt[rar.rarity] = rar.weigt_shop_1;
            }
        }
        if (unit_loader_controller.lvl == 3)
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

        remaining_time.text = "Remaining time: " + unit_loader_controller.heroes_remaining_time.ToString(" s");
        volatile_time_show.text = "Volatile time: " + volatile_seconds.ToString(" s");
    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------Item Spawners------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------
    void SpawnHeroesItems()
    {
        var heroes = unit_loader_controller.heroes;
        for (int i = 0; i < heroes.Length; i++)
        {
            int item = GetRandomItem(heroes[i].my_data.heroe_id);

            // SpawnItem(heroe_pedestals[i], item);

            photonView.RPC("SpawnNewHeroeItem", RpcTarget.Others, i, heroes[i].my_data.heroe_id, item);


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
            int item = GetRandomItem();
            //SpawnItem(r_item_pedestals[i], item);

            photonView.RPC("SpawnNewRandomItem", RpcTarget.Others, i, item);

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
            int item = UnityEngine.Random.Range(0, items_consumable.Length);
            //SpawnItem(c_item_pedestals[i], item);

            photonView.RPC("SpawnNewConsumableItem", RpcTarget.Others, i, item);

            //GameObject new_item = Instantiate(item_prefab, c_item_pedestals[i]);
            //new_item.GetComponent<SpriteRenderer>().sprite = item.sprite;
            //TODO: Add SetShopValues() logic
        }
    }

    int GetRandomItem(HeroesList item_pool = HeroesList.None)
    {
        int id;
        switch (item_pool)
        {
            case HeroesList.Paladin:
                return UnityEngine.Random.Range(0, items_paladin.Length);
            case HeroesList.Elementalist:
                return UnityEngine.Random.Range(0, items_elementalist.Length);
            case HeroesList.Sorcerer:
                return UnityEngine.Random.Range(0, items_sorcerer.Length);
            case HeroesList.Rogue:
                return UnityEngine.Random.Range(0, items_rogue.Length);
            case HeroesList.None:
                int list_id = UnityEngine.Random.Range(0, 5);
                if (list_id != 4) return GetRandomItem((HeroesList)list_id);
                return UnityEngine.Random.Range(0, items_for_all.Length);

        }
        Debug.LogError("Error al generar item random");
        return -1;

    }

    [PunRPC]
    public void SpawnNewHeroeItem(int spawn_idx, HeroesList heroeID, int item_idx)
    {
        switch (heroeID)
        {
            case HeroesList.Paladin:
                SpawnItem(heroe_pedestals[spawn_idx], items_paladin[item_idx]);
                break;
            case HeroesList.Elementalist:
                SpawnItem(heroe_pedestals[spawn_idx], items_elementalist[item_idx]);
                break;
            case HeroesList.Sorcerer:
                SpawnItem(heroe_pedestals[spawn_idx], items_sorcerer[item_idx]);
                break;
            case HeroesList.Rogue:
                SpawnItem(heroe_pedestals[spawn_idx], items_rogue[item_idx]);
                break;
            case HeroesList.None:
                break;
        }
    }

    [PunRPC]
    public void SpawnNewRandomItem(int spawn_idx, int item_idx)
    {
        SpawnItem(r_item_pedestals[spawn_idx], items_for_all[item_idx]);
    }

    [PunRPC]
    public void SpawnNewConsumableItem(int spawn_idx, int item_idx)
    {
        SpawnItem(c_item_pedestals[spawn_idx], items_consumable[item_idx]);
    }


    public void SpawnItem(Transform spawn_pos, ItemData item)
    {
        if (PhotonNetwork.IsMasterClient) return;
        pedestals[actual_pedestal] = Instantiate(item_prefab, spawn_pos);
        pedestals[actual_pedestal].GetComponent<ItemDetector>().SetItem(item, actual_pedestal);
        //pedestals[actual_pedestal].GetComponent<SpriteRenderer>().sprite = item.sprite;
        ////TODO: Add SetShopValues() logic

        //pedestals[actual_pedestal].GetComponent<ItemDetector>().my_item = item;
        //pedestals[actual_pedestal].GetComponent<ItemDetector>().my_pedestal = actual_pedestal;

        actual_pedestal++;
    }


    //-------------------------------------------------------------------------------------------------------------------------------------
    //---------------------------------------------------------Item Interaction/UI---------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------

    ItemData actual_item;
    int actual_pedestal = 0;
    GameObject[] pedestals = new GameObject[12];
    bool is_requester = false;

    public void ShowNewBuyUI(ItemData item, int pedestal)
    {
        item_to_buy_ui.SetActive(true);
        actual_item = item;
        actual_pedestal = pedestal;
        item_name.text = actual_item.name;

        //actual_item_cost = item.cost;
        item_cost.text = "Cost: " + actual_item.cost.ToString() + "s - " + volatile_seconds + "vs available";
        if (actual_item.cost <= volatile_seconds)
        {
            purchase_button.SetActive(true);
            purchase_button.GetComponent<Button>().interactable = true;
            try_purchase_button.SetActive(false);
        }
        else if (actual_item.cost <= volatile_seconds + unit_loader_controller.heroes_remaining_time)
        {
            try_purchase_button.SetActive(true);
            purchase_button.SetActive(false);
        }
        else
        {
            purchase_button.SetActive(true);
            purchase_button.GetComponent<Button>().interactable = false;
            try_purchase_button.SetActive(false);
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
        vote_list[0].color = positive_color;
        actual_vote = 1;
        positive_votes = 1;
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
            if (is_requester) PurchaseItem();
            StartCoroutine(PurchaseAccepted());
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
    IEnumerator PurchaseAccepted()
    {
        HideAskUI();
        purchase_accepted.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        purchase_accepted.SetActive(false);
    }

    public void UseVolatileSeconds(int seconds) 
    {
        volatile_seconds -= seconds;
        if (volatile_seconds < 0)
        {
            unit_loader_controller.photonView.RPC("SpendHeroeSeconds", RpcTarget.All,-volatile_seconds);
            volatile_seconds = 0;
        }
    }
    public void AddVolatileSeconds(int seconds) { volatile_seconds += seconds; }


    public void PurchaseItem()
    {
        unit_loader_controller.photonView.RPC("AddItemToHeroe", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, actual_item);

        UseVolatileSeconds(actual_item.cost);
        if (actual_item.is_unique) photonView.RPC("RemoveItemFromShop", RpcTarget.All, actual_pedestal);
        else RemoveItemFromShop(actual_pedestal);
    }

    [PunRPC]
    public void RemoveItemFromShop(int pedestalID)
    {
        if (PhotonNetwork.IsMasterClient) return;
        pedestals[pedestalID].SetActive(false);

        remaining_time.text = "Remaining time: " + unit_loader_controller.heroes_remaining_time.ToString(" s");
        volatile_time_show.text = "Volatile time: " + volatile_seconds.ToString(" s");
    }

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