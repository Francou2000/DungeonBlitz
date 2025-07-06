using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitLoaderController : MonoBehaviour
{
    public static UnitLoaderController Instance;

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
    public int[] playable_heroes;

    

    public void AddHeoroe(int heroe, int client_id)
    {
        playable_heroes[client_id] = heroe;
    }

    public void AddMapDM(Playable_Map new_playable_Map)
    {
        playable_Map = new_playable_Map;
    }
}

public enum HeroesList
{
    Paladin,
    Rogue,
    Elementalist,
    Sorcerer,
}