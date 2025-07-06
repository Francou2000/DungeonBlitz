using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class HeroeSelectionController : MonoBehaviour
{
    public static HeroeSelectionController instance;
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

    [SerializeField] Image full_body;

    [SerializeField] Image heroe_portrait;
    [SerializeField] TextMeshProUGUI heroe_name;
    [SerializeField] TextMeshProUGUI heroe_stats;

    [SerializeField] UnitData[] heroes_list;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateHeroeData(HeroesList heroe_id)
    {
        UnitData heroe = heroes_list[(int)heroe_id];

        full_body.sprite = heroe.full_body_foto;

        heroe_portrait.sprite = heroe.portrait_foto;
        heroe_name.text = heroe.unitName;
        heroe_stats.text =  
            heroe.maxHP + "\n" +
            heroe.strength + "\n" +
            heroe.magicPower + "\n" +
            heroe.armor + "\n" +
            heroe.magicResistance;
        
    }
}
