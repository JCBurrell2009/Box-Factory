using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(ClickDetection))]
[RequireComponent(typeof(TileNodeTransfer))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(TaskManager))]
public class FactoryTileManager : MonoBehaviour
{
    private ClickDetection click;

    public Camera factoryCam;

    public Tilemap factoryMachineMap;

    private TileNodeTransfer tnt;
    private UIManager uim;
    private TaskManager tm;

    public GameObject blankTile;
    private Dictionary<Vector3, GameObject> tiles = new();
    public Vector3 inputPos;
    public ButtonType inputDir;
    public Sprite inputTruck;
    public Vector3 outputPos;
    public ButtonType outputDir;
    public Sprite outputTruck;
    public Material truckMaterial;

    public List<GameObject> tileTypes = new();
    public int selectedType = 0;

    private GameObject highlightTile;
    private Renderer highlightRenderer;
    public Material highlightMaterial;

    private bool clickEnabled;
    private bool highLightEnabled;

    private readonly float tickTime = 1;
    private float tick = 0;
    private List<FactoryTile> toRunThisFrame = new();

    public Vector2 topLeftBound;
    public Vector2 bottomRightBound;

    public bool pause = false;

    void Awake()
    {
        InstantiateHighlightTile(Color.red, 0.5f);
        click = GetComponent<ClickDetection>();
        tnt = GetComponent<TileNodeTransfer>();
        uim = GetComponent<UIManager>();
        tm = GetComponent<TaskManager>();

        var input = Instantiate(new GameObject());
        var inputRend = input.AddComponent<SpriteRenderer>();
        inputRend.sprite = inputTruck;
        inputRend.material = truckMaterial;
        input.AddComponent<FactoryTile>();
        inputRend.transform.position = inputPos;
        input.layer = 3;
        tiles.Add(inputPos, input);

        var output = Instantiate(new GameObject());
        var outputRend = output.AddComponent<SpriteRenderer>();
        outputRend.sprite = outputTruck;
        outputRend.material = truckMaterial;
        output.AddComponent<FactoryTile>();
        outputRend.transform.position = outputPos;
        output.layer = 3;
        tiles.Add(outputPos, output);
    }

    void Update()
    {
        if (pause)
            return;
        if (tick >= tickTime)
        {
            StartCoroutine(PulseAnimation(tiles[inputPos].GetComponent<Renderer>(), tickTime));

            Dictionary<FactoryTile, Box> boxesToAttach = new();
            foreach (var tile in toRunThisFrame)
            {
                if (tile == null || tile.gameObject == null)
                    continue;

                var type = tile.machineType;
                var box = tnt.CarryOutNodes(tile.incomingDirection, type, tile.boxToProcess, tile.nextIndices, out var dir);
                var nextTile = tile.GetFactoryTileInDirection(dir, factoryMachineMap.layoutGrid.cellSize, out var tilePos);

                if (nextTile != null && nextTile.gameObject != null)
                {
                    if ((Vector3)tilePos == outputPos && FactoryTile.SwapDirection(dir) == outputDir)
                    {
                        tm.UpdateTaskRequirements(box, 1);
                        StartCoroutine(PulseAnimation(tiles[outputPos].GetComponent<Renderer>(), tickTime));
                    }
                    else
                    {
                        boxesToAttach.Add(nextTile, box);
                        nextTile.incomingDirection = FactoryTile.SwapDirection(dir);
                    }
                    StartCoroutine(PulseAnimation(tile.gameObject.GetComponent<Renderer>(), tickTime));
                }
            }

            if (tiles.TryGetValue(inputPos + (Vector3)(FactoryTile.ButtonTypeToDir(inputDir) * factoryMachineMap.layoutGrid.cellSize), out var inputTile))
            {
                var factTile = inputTile.GetComponent<FactoryTile>();
                boxesToAttach.TryAdd(factTile, new(State.Cardboard, Size.Medium));
                factTile.incomingDirection = FactoryTile.SwapDirection(inputDir);
            }

            toRunThisFrame.Clear();
            foreach (var kvp in boxesToAttach)
            {
                kvp.Key.boxToProcess = kvp.Value;
                toRunThisFrame.Add(kvp.Key);
            }
            tick = 0;
        }
        else
            tick += Time.deltaTime;

        Vector3 tileCoords = GetMouseTilePosition(factoryMachineMap);
        bool tileHere = tiles.ContainsKey(tileCoords);

        if (!clickEnabled)
            return;
        // Scroll = change type
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0)
            selectedType = (selectedType + 1) % tileTypes.Count;
        else if (scroll < 0)
            selectedType = (selectedType - 1 + tileTypes.Count) % tileTypes.Count;

        if (scroll != 0)
            uim.SetMachineNum(selectedType);

        var overUI = EventSystem.current.IsPointerOverGameObject();
        var outsideBounds = tileCoords.x <= topLeftBound.x || tileCoords.x >= bottomRightBound.x ||
                            tileCoords.y >= topLeftBound.y || tileCoords.y <= bottomRightBound.y;

        highlightTile.SetActive(!(overUI || outsideBounds || !highLightEnabled));

        if (overUI || outsideBounds)
            return;

        if (highLightEnabled)
        {
            highlightMaterial.SetFloat("_BitMap", tileTypes[selectedType].GetComponent<Renderer>().material.GetFloat("_BitMap"));
            highlightMaterial.SetFloat("_Transparency", tileHere ? 0.1f : 0.5f);
            highlightTile.transform.position = GetMouseTilePosition(factoryMachineMap);
        }


        // Left Button = Draw Tile
        if (click.leftIsClicked)
        {
            // Can only draw tile if there is not a tile there
            if (!tileHere)
            {
                GameObject newTile = Instantiate(blankTile);
                newTile.transform.position = tileCoords;
                newTile.layer = 3;

                Renderer newRenderer = newTile.GetComponent<Renderer>();
                newRenderer.material = tileTypes[selectedType].GetComponent<Renderer>().sharedMaterial;

                var factoryTile = newTile.AddComponent<FactoryTile>();
                Vector2 tileSize = factoryMachineMap.layoutGrid.cellSize;
                factoryTile.Initialize(selectedType, tiles, tileCoords, tileSize);
                tiles.Add(tileCoords, newTile);


                if (tiles.TryGetValue(tileCoords + Vector3.up * tileSize.y, out var temp))
                    temp.GetComponent<FactoryTile>().UpdateAdjacencies(tiles, tileSize);
                if (tiles.TryGetValue(tileCoords + Vector3.down * tileSize.y, out temp))
                    temp.GetComponent<FactoryTile>().UpdateAdjacencies(tiles, tileSize);
                if (tiles.TryGetValue(tileCoords + Vector3.left * tileSize.x, out temp))
                    temp.GetComponent<FactoryTile>().UpdateAdjacencies(tiles, tileSize);
                if (tiles.TryGetValue(tileCoords + Vector3.right * tileSize.x, out temp))
                    temp.GetComponent<FactoryTile>().UpdateAdjacencies(tiles, tileSize);
            }
        }

        // Right Button = Erase Tile
        else if (click.rightIsClicked)
        {
            // Can only erase tile if there is a tile there
            if (tileCoords != null && tileHere && tileCoords != inputPos && tileCoords != outputPos)
            {
                if (tiles.TryGetValue(tileCoords, out var tile))
                {
                    tiles.Remove(tileCoords);
                    Destroy(tile);
                }
            }
        }
    }

    private void InstantiateHighlightTile(Color color, float transparency)
    {
        highlightTile = Instantiate(blankTile);
        highlightTile.name = "HighLightTile";
        highlightTile.GetComponent<SpriteRenderer>().sortingOrder = 0;
        highlightRenderer = highlightTile.GetComponent<Renderer>();
        highlightRenderer.material = highlightMaterial;
        highlightMaterial.SetColor("_PathColor", color);
        highlightMaterial.SetFloat("_Transparency", transparency);
    }

    private GameObject CreateNewTileType()
    {
        GameObject tileType = new()
        {
            name = "Machine_Type_" + tileTypes.Count
        };
        tileType.SetActive(false);
        Renderer renderer = tileType.AddComponent<SpriteRenderer>();
        renderer.material = new(blankTile.GetComponent<Renderer>().sharedMaterial);
        renderer.material.SetFloat("_BitMap", 0);
        return tileType;
    }

    private Vector3 GetMouseTilePosition(Tilemap tilemap)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 screenMousePos = new(mousePos.x, mousePos.y, -factoryCam.transform.position.z);

        Vector3 worldPos = factoryCam.ScreenToWorldPoint(screenMousePos);

        Vector3Int cellCoords = tilemap.WorldToCell(worldPos);

        Vector3 tileCoords = tilemap.CellToWorld(cellCoords);
        tileCoords += tilemap.cellSize * 0.5f;

        return tileCoords;
    }

    private Vector3 GetTilePos(Vector3 pos, Tilemap tilemap)
    {
        Vector3Int cellCoords = tilemap.WorldToCell(pos);

        Vector3 tileCoords = tilemap.CellToWorld(cellCoords);
        tileCoords += tilemap.cellSize * 0.5f;

        return tileCoords;
    }

    public void EnableTileClicks(bool enable)
    {
        clickEnabled = enable;

        highlightTile.SetActive(enable);
        highLightEnabled = enable;
    }

    public void UpdateTileShader(TileNodeTransfer tnt)
    {
        for (int i = 0; i < tileTypes.Count; i++)
        {
            var type = tileTypes[i];
            var renderer = type.GetComponent<Renderer>();
            var material = renderer.material;

            var paths = tnt.GetPathsFromInputs(i);

            int combo = 0;

            foreach (var pair in paths)
            {
                if (pair.Count < 2)
                    continue;
                combo |= (pair[0], pair[^1]) switch
                {
                    (ButtonType.type2, ButtonType.type3) or (ButtonType.type3, ButtonType.type2) => 1, // left-down
                    (ButtonType.type1, ButtonType.type2) or (ButtonType.type2, ButtonType.type1) => 2, // up-left
                    (ButtonType.type4, ButtonType.type1) or (ButtonType.type1, ButtonType.type4) => 4, // right-up
                    (ButtonType.type3, ButtonType.type4) or (ButtonType.type4, ButtonType.type3) => 8, // down-right
                    (ButtonType.type1, ButtonType.type3) or (ButtonType.type3, ButtonType.type1) => 16, // up-down
                    (ButtonType.type2, ButtonType.type4) or (ButtonType.type4, ButtonType.type2) => 32, // left-right
                    (_, _) => 0
                };
            }
            material.SetFloat("_BitMap", combo);
        }
    }

    [ContextMenu("Create New Tile Type")]
    public void AddNewTileType() => tileTypes.Add(CreateNewTileType());

    IEnumerator PulseAnimation(Renderer pipeRenderer, float duration)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        pipeRenderer.GetPropertyBlock(block);
        float t = 0;
        while (t < duration)
        {
            if (pipeRenderer == null)
                yield break;
            t += Time.deltaTime;
            block.SetFloat("_Pulse", Mathf.Sin(t * Mathf.PI / duration));
            pipeRenderer.SetPropertyBlock(block);
            yield return null;
        }

        block.SetFloat("_Pulse", 0);
        pipeRenderer.SetPropertyBlock(block);
    }
}