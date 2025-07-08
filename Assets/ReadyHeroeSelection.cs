using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ReadyHeroeSelection : MonoBehaviourPunCallbacks
{
    Button my_button;
    public UnitData actual_unit;

    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(SelectionReady);
    }

    public void SelectionReady()
    {
        UnitLoaderController.Instance.photonView.RPC("AddHeroe", RpcTarget.All, actual_unit.heroe_id, PhotonNetwork.LocalPlayer.ActorNumber);
        //UI Feedback
    }
}
