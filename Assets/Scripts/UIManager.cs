using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameManager))]
public class UIManager : MonoBehaviour
{
    public GameObject factoryUI;
    public TMP_Text machineNum;
    public GameObject NodeUI;
    public GameObject taskPanel;
    public TMP_Text money;
    public GameObject debtTimer;
    public Image debtTimerBar;
    public float timeToGetOutOfDebt = 60;
    bool isInDebt = false;
    public TMP_Text timer;

    private GameManager gm;

    public GameObject notScreens;
    public GameObject loseScreen;

    void Awake()
    {
        gm = GetComponent<GameManager>();
    }

    public void EnableFactoryUI(bool enable) => factoryUI.SetActive(enable);
    public void EnableNodeUI(bool enable) => NodeUI.SetActive(enable);
    public void SetMachineNum(int machineNum) => this.machineNum.SetText((machineNum + 1).ToString());

    public void SetTaskToCanvas(GameObject task) => task.transform.SetParent(taskPanel.transform);

    public void SetMoneyCount(int money)
    {
        this.money.SetText("$" + money);
        if (money < 0)
        {
            this.money.color = Color.red;
            if (!isInDebt)
            {
                debtTimer.SetActive(true);
                StartCoroutine(LowerDebtBar());
            }
        }
        else if (isInDebt)
        {
            this.money.color = Color.white;
            StopAllCoroutines();
            isInDebt = false;
            debtTimer.SetActive(false);
        }
        else
            this.money.color = Color.white;
    }

    private IEnumerator LowerDebtBar()
    {
        isInDebt = true;
        float timeRemaining = timeToGetOutOfDebt;
        while (timeRemaining > 0)
        {
            debtTimerBar.fillAmount = timeRemaining / timeToGetOutOfDebt;
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        gm.Pause(true);
        ShowLoseScreen();
        debtTimerBar.fillAmount = 0;
        isInDebt = false;
    }

    public void SetTime(int seconds)
    {
        int minutes = seconds / 60;
        seconds %= 60;
        string extra0 = "";
        if (seconds < 10)
            extra0 = "0";
        if (minutes == 0)
            timer.SetText(extra0 + seconds.ToString());
        else
            timer.SetText($"{minutes}:{seconds}");
    }

    public void ShowLoseScreen()
    {
        notScreens.SetActive(false);
        loseScreen.SetActive(true);
    }
}
