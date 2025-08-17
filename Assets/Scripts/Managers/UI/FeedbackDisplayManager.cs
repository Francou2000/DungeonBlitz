using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackDisplayManager : MonoBehaviour
{
    public static FeedbackDisplayManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public Transform msgContainerFather;
    public GameObject msgContainerPrefab;
    List<GameObject> msgContainer_in_use = new List<GameObject>();
    List<GameObject> msgContainer_stanby = new List<GameObject>();

    public void showDmgFeedback(int dmg, Vector3 msg_pos)
    {
        if (msgContainer_stanby.Count <= 0) msgContainer_stanby.Add(spawnMsgContainer());

        msgContainer_stanby[0].GetComponent<RectTransform>().localPosition = msg_pos;
        msgContainer_stanby[0].GetComponent<TextMeshProUGUI>().text = dmg.ToString();

    }

    GameObject spawnMsgContainer()
    {
        return Instantiate(msgContainerPrefab, msgContainerFather);
    }

    public IEnumerator DmgFeedBack(int dmg, Vector3 msg_pos)
    {
        msgContainer_stanby[0].GetComponent<RectTransform>().localPosition = msg_pos;
        msgContainer_stanby[0].GetComponent<TextMeshProUGUI>().text = dmg.ToString();
        msgContainer_stanby[0].SetActive(true);

        msgContainer_in_use.Add(msgContainer_stanby[0]);
        msgContainer_stanby.RemoveAt(0);

        yield return new WaitForSeconds(2f);


        msgContainer_in_use[0].SetActive(false);
        msgContainer_stanby.Add(msgContainer_in_use[0]);
        msgContainer_in_use.RemoveAt(0);
    }
}
