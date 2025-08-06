using UnityEngine;
using UnityEngine.UI;

public class MapReadyButton : MonoBehaviour
{
    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(MapReady);
    }

    void MapReady()
    {
        UnitLoaderController.Instance.photonView.RPC("CheckIfStart", Photon.Pun.RpcTarget.All);
    }
}
