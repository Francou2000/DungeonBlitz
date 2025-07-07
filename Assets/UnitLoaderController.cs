using NUnit.Framework;
using Photon.Pun;
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

    

    public void AddHeroe(UnitData heroe, int client_id)
    {
        playable_heroes[client_id] = heroe;
        players_ready[client_id] = true;
        CheckIfStart();
    }

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

public enum HeroesList
{
    Paladin,
    Rogue,
    Elementalist,
    Sorcerer,
}