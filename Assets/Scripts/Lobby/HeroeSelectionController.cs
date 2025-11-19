using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class HeroeSelectionController : MonoBehaviourPunCallbacks
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
    [SerializeField] Color selected;
    [SerializeField] Color non_selected;
    [SerializeField] ReadyHeroeSelection actual_heroe;

    public void UpdateHeroeData(HeroesList heroe_id)
    {
        for (int i = 0; i < buttons_border.Length; i++)
        {
            if (i == (int)heroe_id) { buttons_border[i].color = selected; }
            else { buttons_border[i].color = non_selected; }
        }
        actual_heroe.actual_unit = heroe_id;
        UnitLoaderController.Instance.photonView.RPC("AddHeroe", RpcTarget.All, heroe_id, PhotonNetwork.LocalPlayer.ActorNumber);
        AudioManager.Instance.PlayButtonSound();
    }
}
