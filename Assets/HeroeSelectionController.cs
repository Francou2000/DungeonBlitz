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

    [SerializeField] Image[] buttons_border;
    [SerializeField] ReadyHeroeSelection actual_heroe;

    public void UpdateHeroeData(HeroesList heroe_id)
    {
        for (int i = 0; i < buttons_border.Length; i++)
        {
            if (i == (int)heroe_id) { buttons_border[i].color = Color.green; }
            else { buttons_border[i].color = Color.red; }
        }
        actual_heroe.actual_unit = heroe_id;
    }
}
