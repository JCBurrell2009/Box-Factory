using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Factory, Node }

[RequireComponent(typeof(FactoryTileManager))]
[RequireComponent(typeof(NodeSystemManager))]
[RequireComponent(typeof(TileNodeTransfer))]
[RequireComponent(typeof(MapExplorer))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(TaskManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameState currentState;

    private FactoryTileManager ftm;
    private NodeSystemManager nsm;
    private TileNodeTransfer tnt;
    private MapExplorer me;
    private UIManager uim;
    private TaskManager tm;
    private MoneyManager mm;

    public GameObject nodeCamera;
    public GameObject factoryCamera;
    public GameObject backgroundCamera;


    public Material factoryTint;
    private readonly float tint = 0.77f;

    void Awake()
    {
        ftm = GetComponent<FactoryTileManager>();

        nsm = GetComponent<NodeSystemManager>();

        tnt = GetComponent<TileNodeTransfer>();

        me = GetComponent<MapExplorer>();

        uim = GetComponent<UIManager>();

        tm = GetComponent<TaskManager>();

        mm = GetComponent<MoneyManager>();
    }

    void Start()
    {

        SetStartState();
    }

    void Update()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (currentState == GameState.Factory)
                SetState(GameState.Node);
            else if (currentState == GameState.Node)
                SetState(GameState.Factory);
        }
    }

    public void SetStartState()
    {
        factoryTint.SetFloat("_Alpha", 0);
        nsm.EnableNodeClicks(false);
        nodeCamera.SetActive(false);


        ftm.EnableTileClicks(true);
        me.UpdateCamera(new());

        uim.EnableFactoryUI(true);
        uim.EnableNodeUI(false);

        ftm.selectedType = 0;
        uim.SetMachineNum(0);
        nsm.SetVisibleNodes(0);

        for (int i = 0; i < 10; i++)
            ftm.AddNewTileType();

        mm.SetFunds(0);
        uim.SetMoneyCount(0);

        tm.GenerateRandomTask();
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        // disabling portion
        if (currentState == GameState.Factory)
        {
            ftm.EnableTileClicks(false);
            uim.EnableFactoryUI(false);
        }
        else if (currentState == GameState.Node)
        {
            factoryTint.SetFloat("_Alpha", 0);
            nsm.EnableNodeClicks(false);
            nodeCamera.SetActive(false);
            uim.EnableNodeUI(false);
        }

        // enabling portion
        if (newState == GameState.Factory)
        {
            ftm.EnableTileClicks(true);
            for (int i = 0; i < nsm.nodeList.Count; i++)
            {
                tnt.NodesToFunctionList(i, nsm.nodeList[i].ToList());
            }
            ftm.UpdateTileShader(tnt);

            me.UpdateCamera(new());
            uim.EnableFactoryUI(true);
        }
        else if (newState == GameState.Node)
        {
            factoryTint.SetFloat("_Alpha", tint);
            nsm.EnableNodeClicks(true);
            nodeCamera.SetActive(true);

            List<Camera> cameras = new()
            {
                nodeCamera.GetComponent<Camera>()
            };
            me.UpdateCamera(cameras);
            uim.EnableNodeUI(true);

            nsm.SetVisibleNodes(ftm.selectedType);
        }

        currentState = newState;
    }

    public void SetFactoryState() => SetState(GameState.Factory);
    public void SetNodeState() => SetState(GameState.Node);

    public void Pause(bool pause)
    {
        ftm.pause = pause;
        nsm.pause = pause;
        tm.pause = pause;
    }

    public void ReloadCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void QuitGame() => Application.Quit();
    public void ToTitle() => SceneManager.LoadScene("Title", LoadSceneMode.Single);
}
