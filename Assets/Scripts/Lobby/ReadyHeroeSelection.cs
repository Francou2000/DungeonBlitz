using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ReadyHeroeSelection : MonoBehaviourPunCallbacks
{
    Button my_button;
    public HeroesList actual_unit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(SelectionReady);
    }

    public void SelectionReady()
    {
        UnitLoaderController.Instance.photonView.RPC("AddHeroe", RpcTarget.All, actual_unit, PhotonNetwork.LocalPlayer.ActorNumber);
        AudioManager.Instance.PlayButtonSound();
        //UI Feedback
    }
}
