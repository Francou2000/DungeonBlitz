using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public Image[] turnStatusIcons;
    public Button endTurnButton;
    public Button pauseButton;

    public Image portraitImage;
    public Image hpBar;
    public Image apBar;
    public List<Button> abilityButtons;

    public void SetTimer(float secondsLeft)
    {
        timerText.text = TimeSpan.FromSeconds(secondsLeft).ToString(@"m\:ss");
    }

    public void SetPlayerTurnStatus(bool[] playerDone)
    {
        for (int i = 0; i < turnStatusIcons.Length; i++)
            turnStatusIcons[i].color = playerDone[i] ? Color.green : Color.red;
    }

    public void SetHPBar(int current, int max)
    {
        hpBar.fillAmount = current / (float)max;
    }

    public void SetAPBar(int current, int max)
    {
        apBar.fillAmount = current / (float)max;
    }
}
