using NUnit.Framework;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitLoaderController : MonoBehaviourPunCallbacks
{
    public static UnitLoaderController Instance;

    public bool[] players_ready = new bool[5];
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Playable_Map playable_Map;
    public UnitData[] playable_heroes;

    [SerializeField] UnitData[] heroes_data;

    [PunRPC]
    public void AddHeroe(HeroesList heroe, int client_id)
    {
        playable_heroes[client_id - 2] = heroes_data[(int)heroe];
        players_ready[client_id - 2] = true;
        CheckIfStart();
    }

    [PunRPC]
    public void AddMapDM(Playable_Map new_playable_Map)
    {
        playable_Map = new_playable_Map;
        players_ready[0] = true;
        CheckIfStart();
    }

    void CheckIfStart()
    {
        foreach (bool ready in players_ready)
        {
            if (!ready) return;
        }
        TimerAndLoadGame.instance.LoadGame();
    }
}

