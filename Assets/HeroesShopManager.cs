using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeroesShopManager : MonoBehaviourPunCallbacks
{
    const string LogTag = "[HeroesShop]";

    /// <summary>ActorNumber del jugador local → ViewID del avatar de tienda (evita múltiples Instantiate).</summary>
    static readonly Dictionary<int, int> s_shopAvatarViewIdByActor = new Dictionary<int, int>();

    [SerializeField] bool shopVerboseLogs = true;

    void ShopLog(string msg)
    {
        if (shopVerboseLogs)
            Debug.Log($"{LogTag} {msg}");
    }

    void ShopWarn(string msg) => Debug.LogWarning($"{LogTag} {msg}");
    void ShopErr(string msg) => Debug.LogError($"{LogTag} {msg}");

    public static HeroesShopManager instance;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
            instance = this;
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

    [Header("Shop avatar (networked)")]
    [SerializeField] string shopPlayerResourcesPrefab = "Player";
    [SerializeField] Transform networkShopPlayerSpawn;

    [Header("Shop bootstrap")]
    [Tooltip("Espera mínima tras considerar lista la escena / cola de red antes de spawns.")]
    [SerializeField] float shopReadyMinWaitSeconds = 0.35f;
    [Tooltip("Tiempo máximo esperando PhotonNetwork.LevelLoadingProgress.")]
    [SerializeField] float shopWaitLoadTimeoutSeconds = 12f;
    [Tooltip("Orden de capa para sprites de ítems (por encima de fondos de tienda).")]
    [SerializeField] int shopItemSortingOrder = 320;
    [Tooltip("Espera extra del master antes de enviar stock (héroes deben hacer ClearLocal + quitar placeholders primero).")]
    [SerializeField] float masterShopStockDelaySeconds = 0.65f;

    Vector3 _cachedShopAvatarWorldPos;
    Quaternion _cachedShopAvatarWorldRot;
    bool _shopAvatarPoseCached;

    public override void OnLeftRoom()
    {
        s_shopAvatarViewIdByActor.Clear();
        _shopAvatarPoseCached = false;
        ShopLog("OnLeftRoom: cleared shop avatar ViewID cache");
    }

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

        remaining_time.text = "Remaining time: " + ((int)unit_loader_controller.heroes_remaining_time).ToString() + " s";
        volatile_time_show.text = "Volatile time: " + volatile_seconds.ToString() + " s";

        StartCoroutine(CoShopStartupSequence());
    }

    IEnumerator WaitUntilShopNetworkAndSceneReady()
    {
        float t = 0f;
        while (t < shopWaitLoadTimeoutSeconds)
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
                PhotonNetwork.IsMessageQueueRunning = true;

            if (PhotonNetwork.InRoom && PhotonNetwork.IsMessageQueueRunning)
            {
                float lp = PhotonNetwork.LevelLoadingProgress;
                // Tras LoadLevel suele ir a ~1; si la escena ya está sin async de PUN, lp puede quedarse en 0.
                if (lp >= 0.99f || (t > 0.2f && lp <= 0.01f))
                    break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < 3; i++)
            yield return null;

        if (shopReadyMinWaitSeconds > 0f)
            yield return new WaitForSecondsRealtime(shopReadyMinWaitSeconds);

        int heroCount = PhotonNetwork.InRoom
            ? PhotonNetwork.PlayerList.Count(p => p != null && !p.IsMasterClient)
            : 0;
        ShopLog($"WaitUntilShopNetworkAndSceneReady done inRoom={PhotonNetwork.InRoom} msgQueue={PhotonNetwork.IsMessageQueueRunning} lvlProg={PhotonNetwork.LevelLoadingProgress:F2} heroesInRoom={heroCount}");
    }

    void CacheShopAvatarWorldSpawnPose()
    {
        if (networkShopPlayerSpawn != null)
        {
            _cachedShopAvatarWorldPos = networkShopPlayerSpawn.position;
            _cachedShopAvatarWorldRot = networkShopPlayerSpawn.rotation;
        }
        else
        {
            _cachedShopAvatarWorldPos = Vector3.zero;
            _cachedShopAvatarWorldRot = Quaternion.identity;
        }
        _shopAvatarPoseCached = true;
        ShopLog($"CacheShopAvatarWorldSpawnPose pos={_cachedShopAvatarWorldPos}");
    }

    /// <summary>
    /// El Player colocado en escena no es runtime: no recibe bien el sync de movimiento y deja "fantasmas" quietos.
    /// Quitarlo en todos los clientes tras cachear el spawn.
    /// </summary>
    void RemoveSceneShopPlayerPlaceholders()
    {
        int removed = 0;
        foreach (ShopPlayer sp in FindObjectsByType<ShopPlayer>(FindObjectsSortMode.None))
        {
            PhotonView pv = sp.GetComponent<PhotonView>();
            if (pv == null) continue;
            if (pv.isRuntimeInstantiated) continue;

            ShopLog($"RemoveSceneShopPlayerPlaceholders: Destroy scene ShopPlayer sceneViewId={pv.sceneViewId} ViewID={pv.ViewID}");
            Destroy(sp.gameObject);
            removed++;
        }
        ShopLog($"RemoveSceneShopPlayerPlaceholders totalRemoved={removed}");
    }

    IEnumerator CoShopStartupSequence()
    {
        ShopLog($"CoShopStartupSequence begin actor={PhotonNetwork.LocalPlayer?.ActorNumber} master={PhotonNetwork.IsMasterClient} lvl={unit_loader_controller?.lvl} msgQueue={PhotonNetwork.IsMessageQueueRunning}");

        yield return WaitUntilShopNetworkAndSceneReady();

        CacheShopAvatarWorldSpawnPose();
        RemoveSceneShopPlayerPlaceholders();

        if (!PhotonNetwork.IsMasterClient)
            ClearLocalSpawnedShopItemsAndResetCounter();

        if (PhotonNetwork.IsMasterClient)
        {
            yield return null;
            yield return null;
            if (masterShopStockDelaySeconds > 0f)
                yield return new WaitForSeconds(masterShopStockDelaySeconds);
            MasterSpawnAllShopItems();
            yield return new WaitForSeconds(0.2f);
            PruneDuplicateShopAvatars("master-post-spawn");
            yield return new WaitForSeconds(0.8f);
            PruneDuplicateShopAvatars("master-late");
        }
        else
        {
            SpawnNetworkShopAvatarIfNeeded();
            yield return new WaitForSeconds(0.15f);
            PruneDuplicateShopAvatars("post-avatar-spawn");
            yield return new WaitForSeconds(0.85f);
            PruneDuplicateShopAvatars("late-join-cleanup");
        }
    }

    void ClearLocalSpawnedShopItemsAndResetCounter()
    {
        for (int i = 0; i < pedestals.Length; i++)
        {
            if (pedestals[i] != null)
            {
                Destroy(pedestals[i]);
                pedestals[i] = null;
            }
        }
        _nextSpawnShelfSlot = 0;
        ShopLog("Cleared local shop item instances and reset spawn slot counter");
    }

    void PruneDuplicateShopAvatars(string reason)
    {
        if (!PhotonNetwork.InRoom) return;

        var byActor = new Dictionary<int, List<PhotonView>>();
        foreach (ShopPlayer sp in FindObjectsByType<ShopPlayer>(FindObjectsSortMode.None))
        {
            PhotonView pv = sp.GetComponent<PhotonView>();
            if (pv == null || !pv.isRuntimeInstantiated || pv.Owner == null) continue;
            if (pv.Owner.IsMasterClient) continue;

            int actor = pv.OwnerActorNr;
            if (!byActor.TryGetValue(actor, out var list))
            {
                list = new List<PhotonView>();
                byActor[actor] = list;
            }
            list.Add(pv);
        }

        int destroyed = 0;
        foreach (var kv in byActor)
        {
            var list = kv.Value;
            if (list.Count <= 1) continue;
            list.Sort((a, b) => a.ViewID.CompareTo(b.ViewID));
            for (int i = 1; i < list.Count; i++)
            {
                var dup = list[i];
                ShopWarn($"{reason}: duplicate runtime avatar actor={kv.Key} drop ViewID={dup.ViewID} keep={list[0].ViewID} isMine={dup.IsMine} isMaster={PhotonNetwork.IsMasterClient}");

                if (PhotonNetwork.IsMasterClient || dup.IsMine)
                {
                    PhotonNetwork.Destroy(dup.gameObject);
                    destroyed++;
                }
                else
                {
                    ShopLog($"{reason}: duplicate ViewID={dup.ViewID} esperando limpieza del Master (sin SetActive local)");
                }
            }
        }

        ShopLog($"{reason}: heroActorsWithAvatar={byActor.Count}, networkDestroyed={destroyed}");
    }

    void MasterSpawnAllShopItems()
    {
        if (!PhotonNetwork.IsMasterClient || photonView == null)
        {
            ShopErr($"MasterSpawnAllShopItems aborted: master={PhotonNetwork.IsMasterClient} photonViewNull={photonView == null}");
            return;
        }

        ShopLog($"Master spawning shop items ViewID={photonView.ViewID} heroesArrayLen={unit_loader_controller?.heroes?.Length} heroePedestals={heroe_pedestals?.Length} rPed={r_item_pedestals?.Length} cPed={c_item_pedestals?.Length}");

        try
        {
            SpawnHeroesItems();
            SpawnRandomItems();
            SpawnConsumableItems();
        }
        catch (Exception ex)
        {
            ShopErr($"MasterSpawnAllShopItems exception: {ex}");
        }
    }

    void SpawnNetworkShopAvatarIfNeeded()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
        if (unit_loader_controller == null || unit_loader_controller.lvl <= 1) return;

        int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

        if (s_shopAvatarViewIdByActor.TryGetValue(actor, out int cachedVid))
        {
            var existing = PhotonView.Find(cachedVid);
            if (existing != null && existing.IsMine)
            {
                ShopLog($"SpawnNetworkShopAvatarIfNeeded: reuse existing ViewID={cachedVid} actor={actor}");
                return;
            }

            s_shopAvatarViewIdByActor.Remove(actor);
            ShopWarn($"SpawnNetworkShopAvatarIfNeeded: stale ViewID={cachedVid} for actor={actor}, cache cleared");
        }

        foreach (ShopPlayer sp in FindObjectsByType<ShopPlayer>(FindObjectsSortMode.None))
        {
            PhotonView pv = sp.GetComponent<PhotonView>();
            if (pv != null && pv.isRuntimeInstantiated && pv.IsMine)
            {
                ShopLog($"SpawnNetworkShopAvatarIfNeeded: found existing runtime IsMine ViewID={pv.ViewID}");
                s_shopAvatarViewIdByActor[actor] = pv.ViewID;
                return;
            }
        }

        Vector3 pos = _shopAvatarPoseCached ? _cachedShopAvatarWorldPos : (networkShopPlayerSpawn != null ? networkShopPlayerSpawn.position : Vector3.zero);
        Quaternion rot = _shopAvatarPoseCached ? _cachedShopAvatarWorldRot : Quaternion.identity;
        ShopLog($"SpawnNetworkShopAvatarIfNeeded: PhotonNetwork.Instantiate prefab={shopPlayerResourcesPrefab} at {pos} actor={actor}");
        var go = PhotonNetwork.Instantiate(shopPlayerResourcesPrefab, pos, rot);
        var pvNew = go != null ? go.GetComponent<PhotonView>() : null;
        if (pvNew != null)
        {
            s_shopAvatarViewIdByActor[actor] = pvNew.ViewID;
            if (PhotonNetwork.LocalPlayer != null)
                PhotonNetwork.LocalPlayer.TagObject = pvNew.ViewID;
            ShopLog($"SpawnNetworkShopAvatarIfNeeded: created ViewID={pvNew.ViewID}");
        }
        else
            ShopErr("SpawnNetworkShopAvatarIfNeeded: Instantiate returned no PhotonView");
    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------Item Spawners------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------
    void SpawnHeroesItems()
    {
        var heroes = unit_loader_controller.heroes;
        if (heroes == null || heroe_pedestals == null)
        {
            ShopErr("SpawnHeroesItems: heroes or heroe_pedestals is null");
            return;
        }

        for (int i = 0; i < heroes.Length; i++)
        {
            if (i >= heroe_pedestals.Length)
            {
                ShopWarn($"SpawnHeroesItems: hero index {i} >= heroe_pedestals.Length {heroe_pedestals.Length}, stopping");
                break;
            }

            if (heroes[i].my_data == null)
            {
                ShopWarn($"SpawnHeroesItems: heroes[{i}].my_data is null, skipping RPC");
                continue;
            }

            HeroesList hid = heroes[i].my_data.heroe_id;
            int item = GetRandomItem(hid);
            ShopLog($"SpawnHeroesItems RPC heroSlot={i} heroeId={hid} itemIdx={item}");
            photonView.RPC(nameof(SpawnNewHeroeItem), RpcTarget.Others, i, hid, item);
        }
    }

    void SpawnRandomItems()
    {
        if (r_item_pedestals == null)
        {
            ShopErr("SpawnRandomItems: r_item_pedestals is null");
            return;
        }

        for (int i = 0; i < r_item_pedestals.Length; i++)
        {
            if (UnityEngine.Random.Range(0, 100) < r_empty_chance) continue;
            int item = GetRandomItem();
            ShopLog($"SpawnRandomItems RPC slot={i} itemIdx={item}");
            photonView.RPC(nameof(SpawnNewRandomItem), RpcTarget.Others, i, item);
        }
    }

    void SpawnConsumableItems()
    {
        if (c_item_pedestals == null || items_consumable == null || items_consumable.Length == 0)
        {
            ShopErr($"SpawnConsumableItems: invalid refs consumableLen={items_consumable?.Length ?? -1}");
            return;
        }

        for (int i = 0; i < c_item_pedestals.Length; i++)
        {
            if (UnityEngine.Random.Range(0, 100) < c_empty_chance) continue;
            int item = UnityEngine.Random.Range(0, items_consumable.Length);
            ShopLog($"SpawnConsumableItems RPC slot={i} itemIdx={item}");
            photonView.RPC(nameof(SpawnNewConsumableItem), RpcTarget.Others, i, item);
        }
    }

    int GetRandomItem(HeroesList item_pool = HeroesList.None)
    {
        switch (item_pool)
        {
            case HeroesList.Paladin:
                if (items_paladin == null || items_paladin.Length == 0) { ShopErr("GetRandomItem: items_paladin empty"); return 0; }
                return UnityEngine.Random.Range(0, items_paladin.Length);
            case HeroesList.Elementalist:
                if (items_elementalist == null || items_elementalist.Length == 0) { ShopErr("GetRandomItem: items_elementalist empty"); return 0; }
                return UnityEngine.Random.Range(0, items_elementalist.Length);
            case HeroesList.Sorcerer:
                if (items_sorcerer == null || items_sorcerer.Length == 0) { ShopErr("GetRandomItem: items_sorcerer empty"); return 0; }
                return UnityEngine.Random.Range(0, items_sorcerer.Length);
            case HeroesList.Rogue:
                if (items_rogue == null || items_rogue.Length == 0) { ShopErr("GetRandomItem: items_rogue empty"); return 0; }
                return UnityEngine.Random.Range(0, items_rogue.Length);
            case HeroesList.None:
                int list_id = UnityEngine.Random.Range(0, 5);
                if (list_id != 4) return GetRandomItem((HeroesList)list_id);
                if (items_for_all == null || items_for_all.Length == 0) { ShopErr("GetRandomItem: items_for_all empty"); return 0; }
                return UnityEngine.Random.Range(0, items_for_all.Length);

        }
        ShopErr("GetRandomItem: unhandled pool");
        return -1;

    }

    [PunRPC]
    public void SpawnNewHeroeItem(int spawn_idx, HeroesList heroeID, int item_idx)
    {
        ShopLog($"RPC SpawnNewHeroeItem recv spawn_idx={spawn_idx} heroeID={heroeID} item_idx={item_idx} master={PhotonNetwork.IsMasterClient}");
        if (heroe_pedestals == null || spawn_idx < 0 || spawn_idx >= heroe_pedestals.Length)
        {
            ShopErr($"SpawnNewHeroeItem: bad spawn_idx={spawn_idx} len={heroe_pedestals?.Length ?? -1}");
            return;
        }

        ItemData data = null;
        switch (heroeID)
        {
            case HeroesList.Paladin:
                if (items_paladin == null || item_idx < 0 || item_idx >= items_paladin.Length) break;
                data = items_paladin[item_idx];
                break;
            case HeroesList.Elementalist:
                if (items_elementalist == null || item_idx < 0 || item_idx >= items_elementalist.Length) break;
                data = items_elementalist[item_idx];
                break;
            case HeroesList.Sorcerer:
                if (items_sorcerer == null || item_idx < 0 || item_idx >= items_sorcerer.Length) break;
                data = items_sorcerer[item_idx];
                break;
            case HeroesList.Rogue:
                if (items_rogue == null || item_idx < 0 || item_idx >= items_rogue.Length) break;
                data = items_rogue[item_idx];
                break;
            case HeroesList.None:
                break;
        }

        if (data == null)
        {
            ShopErr($"SpawnNewHeroeItem: no ItemData for heroeID={heroeID} item_idx={item_idx}");
            return;
        }

        SpawnItem(heroe_pedestals[spawn_idx], data);
    }

    [PunRPC]
    public void SpawnNewRandomItem(int spawn_idx, int item_idx)
    {
        ShopLog($"RPC SpawnNewRandomItem recv spawn_idx={spawn_idx} item_idx={item_idx} master={PhotonNetwork.IsMasterClient}");
        if (r_item_pedestals == null || spawn_idx < 0 || spawn_idx >= r_item_pedestals.Length)
        {
            ShopErr($"SpawnNewRandomItem: bad spawn_idx={spawn_idx}");
            return;
        }
        if (items_for_all == null || item_idx < 0 || item_idx >= items_for_all.Length)
        {
            ShopErr($"SpawnNewRandomItem: bad item_idx={item_idx} len={items_for_all?.Length ?? -1}");
            return;
        }
        SpawnItem(r_item_pedestals[spawn_idx], items_for_all[item_idx]);
    }

    [PunRPC]
    public void SpawnNewConsumableItem(int spawn_idx, int item_idx)
    {
        ShopLog($"RPC SpawnNewConsumableItem recv spawn_idx={spawn_idx} item_idx={item_idx} master={PhotonNetwork.IsMasterClient}");
        if (c_item_pedestals == null || spawn_idx < 0 || spawn_idx >= c_item_pedestals.Length)
        {
            ShopErr($"SpawnNewConsumableItem: bad spawn_idx={spawn_idx}");
            return;
        }
        if (items_consumable == null || item_idx < 0 || item_idx >= items_consumable.Length)
        {
            ShopErr($"SpawnNewConsumableItem: bad item_idx={item_idx}");
            return;
        }
        SpawnItem(c_item_pedestals[spawn_idx], items_consumable[item_idx]);
    }


    public void SpawnItem(Transform spawn_pos, ItemData item)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ShopLog("SpawnItem skipped (IsMasterClient — solo héroes instancian ítems en mundo)");
            return;
        }

        if (spawn_pos == null || item == null || item_prefab == null)
        {
            ShopErr($"SpawnItem invalid spawn_pos={spawn_pos} item={item} prefab={item_prefab}");
            return;
        }

        if (_nextSpawnShelfSlot < 0 || _nextSpawnShelfSlot >= pedestals.Length)
        {
            ShopErr($"SpawnItem: shelf array full or bad index {_nextSpawnShelfSlot} (max {pedestals.Length})");
            return;
        }

        int slot = _nextSpawnShelfSlot++;
        bool parentChainActive = IsTransformHierarchyActive(spawn_pos);
        // Sin parent: si se destruye el placeholder de escena, los ítems no deben quedar hijos huérfanos ni ocultos.
        var go = Instantiate(item_prefab, spawn_pos.position, spawn_pos.rotation);
        go.SetActive(true);
        pedestals[slot] = go;
        var detector = go.GetComponent<ItemDetector>();
        if (detector == null)
        {
            ShopErr($"SpawnItem: ItemDetector missing on prefab at slot {slot}");
            return;
        }
        detector.SetItem(item, slot);

        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        int srWithSprite = 0;
        foreach (var sr in srs)
        {
            sr.sortingOrder = shopItemSortingOrder;
            var col = sr.color;
            if (col.a < 0.99f) { col.a = 1f; sr.color = col; }
            if (sr.sprite != null) srWithSprite++;
        }
        ShopLog($"SpawnItem OK slot={slot} item={item.name} anchor={spawn_pos.name} anchorChainActive={parentChainActive} goActiveInHierarchy={go.activeInHierarchy} pos={go.transform.position} lossyScale={go.transform.lossyScale} spriteRenderers={srs.Length} withSprite={srWithSprite} sortingOrder={shopItemSortingOrder}");
    }

    static bool IsTransformHierarchyActive(Transform t)
    {
        while (t != null)
        {
            if (!t.gameObject.activeSelf) return false;
            t = t.parent;
        }
        return true;
    }


    //-------------------------------------------------------------------------------------------------------------------------------------
    //---------------------------------------------------------Item Interaction/UI---------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------------

    ItemData actual_item;
    /// <summary>Siguiente índice libre en <see cref="pedestals"/> al instanciar ítems (no mezclar con UI).</summary>
    int _nextSpawnShelfSlot;
    /// <summary>Índice en <see cref="pedestals"/> del ítem abierto en la UI / compra.</summary>
    int uiSelectedPedestalShelfSlot;
    GameObject[] pedestals = new GameObject[24];
    bool is_requester = false;

    public void ShowNewBuyUI(ItemData item, int pedestal)
    {
        item_to_buy_ui.SetActive(true);
        actual_item = item;
        uiSelectedPedestalShelfSlot = pedestal;
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

        if (item.tailored_heroe != HeroesList.None && item.tailored_heroe != unit_loader_controller.my_heroe)
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
        
        // item_effect_description.text = "Unlock Action: " + actual_item.new_ability.name;


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

        // Si 2 o m�s jugadores negaron la petici�n
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
        bool only_volatile = !(volatile_seconds < 0);
        int extra_price = 0;
        if (!only_volatile){
            extra_price = -volatile_seconds;
            volatile_seconds = 0;
        } 
        volatile_time_show.text = "Volatile time: " + volatile_seconds.ToString() + " s";
        photonView.RPC("UpdateTimeCoins", RpcTarget.All, only_volatile,extra_price);
    }
    public void AddVolatileSeconds(int seconds) { volatile_seconds += seconds; }

    [PunRPC]
    public void UpdateTimeCoins(bool only_volatile,int extra_price=0)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (!only_volatile)
        {
            unit_loader_controller.SpendHeroeSeconds(extra_price);
            remaining_time.text = "Remaining time: " + ((int)unit_loader_controller.heroes_remaining_time).ToString() + " s";
        
        }
            
    }
    public void PurchaseItem()
    {
        unit_loader_controller.photonView.RPC("AddItemToHeroe", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, actual_item.ItemID);

        UseVolatileSeconds(actual_item.cost);
        if (actual_item.is_unique) photonView.RPC(nameof(RemoveItemFromShop), RpcTarget.All, uiSelectedPedestalShelfSlot);
        else RemoveItemFromShop(uiSelectedPedestalShelfSlot);
        StartCoroutine(PurchaseAccepted());
    }

    [PunRPC]
    public void RemoveItemFromShop(int pedestalID)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (pedestalID < 0 || pedestalID >= pedestals.Length || pedestals[pedestalID] == null)
        {
            ShopWarn($"RemoveItemFromShop: invalid pedestalID={pedestalID}");
            return;
        }
        pedestals[pedestalID].SetActive(false);

        //remaining_time.text = "Remaining time: " + unit_loader_controller.heroes_remaining_time.ToString(" s");
        remaining_time.text = "Remaining time: " + ((int)unit_loader_controller.heroes_remaining_time).ToString() + " s";
        
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