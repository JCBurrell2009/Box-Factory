using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(MoneyManager))]
public class TaskManager : MonoBehaviour
{
    List<Task> taskList = new();
    public GameObject refTaskTracker;
    private UIManager uim;
    private MoneyManager mm;
    public BoxSpriteData spriteData;
    private readonly List<int> childList = new() { 0, 1, 2, 3, 4 };

    public float timeBetweenQuota = 30;
    public float waitTimeDecreasePerLevel = 5;
    private float timeSinceLastQuota = 0;
    public bool pause = false;

    public float totalTimePassed = 0;
    public float timeBetweenDifficultyIncrease = 60;
    public int difficulty = 0;

    void Awake()
    {
        uim = GetComponent<UIManager>();
        mm = GetComponent<MoneyManager>();
    }

    public void Update()
    {
        if (pause)
            return;
        float timePassed = Time.deltaTime;
        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            task.timePassed += timePassed;
            if (task.timePassed >= task.timeToComplete)
            {
                taskList.RemoveAt(i);
                mm.AddFunds(-task.punishment);
                uim.SetMoneyCount(mm.money);
                Destroy(task.taskTracker);
                i--;

                if (taskList.Count == 0)
                    GenerateRandomTask(difficulty);
            }
            else
                task.UpdateTaskTracker();
        }
        totalTimePassed += timePassed;
        uim.SetTime((int)totalTimePassed);
        if (totalTimePassed >= timeBetweenDifficultyIncrease * (difficulty + 1))
            difficulty++;

        timeSinceLastQuota += timePassed;
        if (timeSinceLastQuota >= (timeBetweenQuota - difficulty * waitTimeDecreasePerLevel))
            GenerateRandomTask(difficulty);
    }

    public void CreateTask(Box requiredBox, int numOfBox, float timeToComplete, int reward, int punishment)
    {
        foreach (var task in taskList)
        {
            if (task.requiredBox.Equals(requiredBox))
                return;
        }

        taskList.Add(new(requiredBox, numOfBox, timeToComplete, reward, punishment, refTaskTracker, childList, spriteData));
        uim.SetTaskToCanvas(taskList[^1].taskTracker);
        timeSinceLastQuota = 0;
    }

    public void UpdateTaskRequirements(Box boxInput, int numOfBoxInput)
    {
        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            if (task.requiredBox.Equals(boxInput))
            {
                task.numOfBox -= numOfBoxInput;
                if (task.numOfBox <= 0)
                {
                    taskList.RemoveAt(i);
                    mm.AddFunds(task.reward);
                    uim.SetMoneyCount(mm.money);
                    Destroy(task.taskTracker);
                }
                else
                    task.UpdateTaskTracker();
                return;
            }
        }
    }

    public void GenerateRandomTask(int difficulty = 0)
    {
        difficulty = Math.Min(difficulty, 4);


        int minNumOfBoxes = 3;
        int boxRange = 1;
        int boxesAddedPerDifficulty = 2;

        int numOfBoxes = minNumOfBoxes + UnityEngine.Random.Range(-boxRange, boxRange + 1) +
                     difficulty * boxesAddedPerDifficulty;


        Box box = Box.GenerateRandomBox(difficulty > 0);


        int minTime = 30;
        int individualModificationTime = 10;
        int labelAdditionTime = 15;
        int difficultySubtractedTime = 10;
        int timePerBox = 2;

        int time = minTime + individualModificationTime * BoxSpriteData.StateToInt(box.state) +
                   (((box.label != LabelType.Blank) && box.state == State.Taped) ? labelAdditionTime : 0)
                   - difficulty * difficultySubtractedTime + timePerBox * numOfBoxes;


        int minMoneyVal = 4;
        int moneyRange = 2;
        int difficultyMoneyAdded = 3;
        float moneyPerBox = 0.7f;

        int reward = minMoneyVal + UnityEngine.Random.Range(-moneyRange, moneyRange + 1) +
                     difficulty * difficultyMoneyAdded + (int)(moneyPerBox * numOfBoxes);


        int minPunishValue = 2;
        int punishRange = 1;
        int difficultyPunishAdded = 2;
        float punishmentPerBox = 0.3f;

        int punishment = minPunishValue + UnityEngine.Random.Range(-punishRange, punishRange + 1) +
                     difficulty * difficultyPunishAdded + (int)(punishmentPerBox * numOfBoxes);

        CreateTask(box, numOfBoxes, time, reward, punishment);
    }
}

public class Task
{
    public Box requiredBox;
    public int numOfBox;
    public float timeToComplete;
    public float timePassed;
    public int reward;
    public int punishment;
    public GameObject taskTracker;
    private TMP_Text timeText;
    private TMP_Text rewardText;
    private TMP_Text punishmentText;
    private TMP_Text achievedText;
    private Image goalImage;
    private BoxSpriteData spriteData;

    public Task(Box requiredBox, int numOfBox, float timeToComplete, int reward, int punishment, GameObject referenceTracker, List<int> children, BoxSpriteData spriteData)
    {
        this.requiredBox = requiredBox;
        this.numOfBox = numOfBox;
        this.timeToComplete = timeToComplete;
        timePassed = 0;
        this.reward = reward;
        this.punishment = punishment;
        this.spriteData = spriteData;
        MakeTaskTracker(referenceTracker, children);
    }

    public void MakeTaskTracker(GameObject reference, List<int> children)
    {
        taskTracker = UnityEngine.Object.Instantiate(reference);
        var taskTransform = taskTracker.transform;
        timeText = taskTransform.GetChild(children[0]).GetComponent<TMP_Text>();
        rewardText = taskTransform.GetChild(children[1]).GetComponent<TMP_Text>();
        punishmentText = taskTransform.GetChild(children[2]).GetComponent<TMP_Text>();
        achievedText = taskTransform.GetChild(children[3]).GetComponent<TMP_Text>();
        goalImage = taskTransform.GetChild(children[4]).GetComponent<Image>();
        if (requiredBox.state == State.Cardboard)
            taskTracker.GetComponent<TooltipTrigger>().tooltipText = $"{numOfBox} Cardboard";
        else
        {
            string boxOrBoxes = numOfBox > 1 ? "Boxes" : "Box";

            if (requiredBox.size == Size.Bigbag ||
                requiredBox.size == Size.Mediumbag ||
                requiredBox.size == Size.Smallbag)
                boxOrBoxes = numOfBox > 1 ? "Bags" : "Bag";

            string boxSize = requiredBox.size switch
            {
                Size.Bigbag => "Big",
                Size.Mediumbag => "Medium",
                Size.Smallbag => "Small",
                _ => requiredBox.size.ToString()
            };

            if (requiredBox.state != State.Taped)
                taskTracker.GetComponent<TooltipTrigger>().tooltipText = $"{numOfBox} {boxSize} {requiredBox.state} {boxOrBoxes}";
            else
            {
                string boxLabel = requiredBox.label switch
                {
                    LabelType.Greenwalls => "Green Walls",
                    LabelType.Priceinc => "Price Inc.",
                    LabelType.Sammysinging => "Sammy Singing",
                    LabelType.Sammysclub => "Sammy's Club",
                    LabelType.Youngmarine => "Yong Marine",
                    LabelType.Blank => "",
                    _ => requiredBox.label.ToString()
                };
                taskTracker.GetComponent<TooltipTrigger>().tooltipText = $"{numOfBox} {boxSize} {boxLabel} {boxOrBoxes}";
            }
        }

        UpdateTaskTracker();
    }

    public void UpdateTaskTracker()
    {
        timeText.SetText(Mathf.RoundToInt(timeToComplete - timePassed).ToString());
        rewardText.SetText(reward.ToString());
        punishmentText.SetText(punishment.ToString());
        achievedText.SetText(numOfBox.ToString());
        goalImage.sprite = spriteData.GetBoxSprite(requiredBox);
    }

}