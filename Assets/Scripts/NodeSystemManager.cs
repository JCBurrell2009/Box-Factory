using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

enum NodeMode { Disabled, DrawNode, DrawConnection }

[RequireComponent(typeof(ClickDetection))]
public class NodeSystemManager : MonoBehaviour
{
    #region Variables
    private ClickDetection click;

    public Camera nodeCam;

    private NodeMode currentMode;
    public Tilemap nodeEditorMap;
    List<Dictionary<Vector3Int, NodeObject>> totalOccupancy = new();
    public int selectedType = 0;
    public List<HashSet<NodeObject>> nodeList = new();

    public List<GameObject> references;
    private int selectedRef = 0;

    private bool clickEnabled;
    private bool highlightEnabled;

    private GameObject highlight;
    private SpriteRenderer highlightRenderer;
    public Material highlightMaterial;

    private Vector3 prevPosition = new(Mathf.Infinity, Mathf.Infinity);
    private Vector3 lineNodeBase = new(Mathf.Infinity, Mathf.Infinity);
    private NodeObject baseNode = null;
    private bool basePortOutput = false;

    public GameObject lineDrawer;
    private List<GameObject> lines = new();
    public bool pause = false;
    #endregion
    void Awake()
    {
        InstantiateHighlightNode(Color.green, 0.7f);
        click = GetComponent<ClickDetection>();
    }
    void Update()
    {
        if (pause)
            return;
        if (EventSystem.current.IsPointerOverGameObject())
        {
            highlightMaterial.SetFloat("_Alpha", 0);
            prevPosition = new(Mathf.Infinity, Mathf.Infinity);
            return;
        }

        var position = GetMouseTilePosition(nodeEditorMap);
        var tileCoord = nodeEditorMap.WorldToCell(position);
        bool canDraw = true;
        NodeObject contactNode = null;

        bool isOverPort = false;
        bool isOverButton = false;
        if (clickEnabled)
        {
            // Scroll = change type
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
                prevPosition = new(Mathf.Infinity, Mathf.Infinity);

            if (scroll > 0)
                selectedRef = (selectedRef + 1) % references.Count;
            else if (scroll < 0)
                selectedRef = (selectedRef - 1 + references.Count) % references.Count;

            while (totalOccupancy.Count < selectedType + 1)
                totalOccupancy.Add(new());
        }


        // precomputes occupancy
        if ((click.leftIsClicked || position != prevPosition) && (highlightEnabled || clickEnabled))
        {
            var occupancy = totalOccupancy[selectedType];
            // multi-cell check for placement validity
            NodeObject highlightNode = new(position, highlight, references[selectedRef], nodeEditorMap, false);
            int xSize = highlightNode.size.x;
            int ySize = highlightNode.size.y;

            for (int x = -xSize / 2; x < xSize / 2; x++)
            {
                for (int y = -ySize / 2; y < ySize / 2; y++)
                {
                    if (occupancy.ContainsKey(new Vector3Int(x, y) + tileCoord))
                    {
                        canDraw = false;
                        break;
                    }
                }
                if (!canDraw) break;
            }

            // single cell check for port/button detection
            if (occupancy.TryGetValue(tileCoord, out contactNode))
            {
                Vector2Int offset = (Vector2Int)(tileCoord - contactNode.tilePosition);

                if (contactNode.nodeTypes.TryGetValue(offset, out var port) && port != PortType.None)
                    isOverPort = true;
                else if (contactNode.buttonTypes.ContainsKey(offset))
                    isOverButton = true;
            }
        }

        if (currentMode == NodeMode.DrawNode)
            DrawNode(position, canDraw, contactNode, isOverPort, isOverButton);
        else if (currentMode == NodeMode.DrawConnection && lines.Count > 0)
            DrawConnection(lineNodeBase, position, lines[^1], isOverPort, contactNode, basePortOutput);
    }

    #region Draw Node
    private void DrawNode(Vector3 position, bool canDraw, NodeObject contactNode, bool isOverPort = false, bool isOverButton = false)
    {
        var tileCoord = nodeEditorMap.WorldToCell(position);
        if (highlightEnabled && position != prevPosition)
        {
            if (isOverPort || isOverButton)
                highlightMaterial.SetFloat("_Alpha", 0);
            else
                ConfigureHighlight(position, canDraw, 0.7f);
        }

        prevPosition = position;


        if (!clickEnabled)
            return;

        if (canDraw)
            DrawFunction(position, canDraw);
        else if (isOverPort && click.leftIsClicked && contactNode != null)
        {
            currentMode = NodeMode.DrawConnection;
            Vector2Int portOffset = contactNode.reference.GetComponent<NodeLayout>().GetPortCenter((Vector2Int)(tileCoord - contactNode.tilePosition), out var type);
            basePortOutput = type == PortType.Output;
            lineNodeBase = contactNode.position
            + new Vector3(portOffset.x * nodeEditorMap.cellSize.x, portOffset.y * nodeEditorMap.cellSize.y)
            + new Vector3(nodeEditorMap.cellSize.x * 0.5f, nodeEditorMap.cellSize.y * 0.5f);

            lines.Add(Instantiate(lineDrawer));

            baseNode = contactNode;
        }
        else if (isOverButton && click.leftIsClicked && contactNode != null)
        {
            Vector2Int offset = (Vector2Int)(tileCoord - contactNode.tilePosition);

            if (!contactNode.buttonTypes.TryGetValue(offset, out var type))
                return;

            contactNode.SetToMode(type);
        }
    }

    private void DrawFunction(Vector3 position, bool canDraw)
    {
        var occupancy = totalOccupancy[selectedType];
        var tileCoord = nodeEditorMap.WorldToCell(position);
        // Left Button = Draw Tile
        if (click.leftIsClicked)
        {

            var nodeObject = Instantiate(references[selectedRef]);
            nodeObject.layer = 7;
            nodeObject.transform.position = position;
            NodeObject node = new(position, nodeObject, references[selectedRef], nodeEditorMap);

            int xSize = node.size.x;
            int ySize = node.size.y;

            if (!canDraw)
            {
                Destroy(nodeObject);
                return;
            }

            for (int x = -xSize / 2; x < xSize / 2; x++)
                for (int y = -ySize / 2; y < ySize / 2; y++)
                    occupancy[new Vector3Int(x, y) + tileCoord] = node;

            while (nodeList.Count < selectedType + 1)
                nodeList.Add(new());
            nodeList[selectedType].Add(node);

            prevPosition = new(Mathf.Infinity, Mathf.Infinity);

        }
        else if (click.rightIsClicked)
        {
            if (!occupancy.ContainsKey(tileCoord))
                return;

            var node = occupancy[tileCoord];
            var nodeCenter = nodeEditorMap.WorldToCell(node.position);

            int xSize = node.size.x;
            int ySize = node.size.y;
            nodeList[selectedType].Remove(node);

            for (int x = -xSize / 2; x < xSize / 2; x++)
                for (int y = -ySize / 2; y < ySize / 2; y++)
                    occupancy.Remove(new Vector3Int(x, y) + nodeCenter);
            foreach (var line in node.connectedLines.ToList())
                Destroy(line);
            foreach (var connected in node.nodesConnected.ToList())
                connected.connectedNodes.Remove(node);

            foreach (var connected in node.connectedNodes.ToList())
                connected.nodesConnected.Remove(node);

            Destroy(node.reference);

            prevPosition = new(Mathf.Infinity, Mathf.Infinity);
        }
    }
    #endregion

    #region Draw Connection
    private void DrawConnection(Vector3 nodePosition, Vector3 position, GameObject line, bool isOverPort, NodeObject contactNode = null, bool output = false)
    {
        var tileCoord = nodeEditorMap.WorldToCell(position);
        var drawer = line.GetComponent<LineDrawer>();
        drawer.DrawLine(nodePosition, position);
        if (click.leftIsClicked)
        {
            if (!isOverPort)
            {
                lines.RemoveAt(lines.Count - 1);
                Destroy(line);
            }
            else
            {
                var layout = contactNode.reference.GetComponent<NodeLayout>();

                Vector2Int portOffset = layout.GetPortCenter((Vector2Int)(tileCoord - contactNode.tilePosition), out var type);

                if ((output && type == PortType.Output) ||
                    (!output && type == PortType.Input))
                {
                    lines.RemoveAt(lines.Count - 1);
                    Destroy(line);

                    baseNode = null;
                    basePortOutput = false;
                    currentMode = NodeMode.DrawNode;
                    return;
                }


                var newPosition = contactNode.position
                + new Vector3(portOffset.x * nodeEditorMap.cellSize.x, portOffset.y * nodeEditorMap.cellSize.y)
                + new Vector3(nodeEditorMap.cellSize.x * 0.5f, nodeEditorMap.cellSize.y * 0.5f);
                drawer.DrawLine(nodePosition, newPosition);
                lineNodeBase = new(Mathf.Infinity, Mathf.Infinity);
                baseNode.connectedLines.Add(line);
                contactNode.connectedLines.Add(line);

                if (basePortOutput)
                {
                    baseNode.connectedNodes.Add(contactNode);
                    contactNode.nodesConnected.Add(baseNode);
                }
                else
                {
                    contactNode.connectedNodes.Add(baseNode);
                    baseNode.nodesConnected.Add(contactNode);
                }
            }
            baseNode = null;
            currentMode = NodeMode.DrawNode;
        }
    }
    #endregion

    #region Highlight
    private void ConfigureHighlight(Vector3 position, bool canDraw, float transparency)
    {
        highlightRenderer.sprite = references[selectedRef].GetComponent<SpriteRenderer>().sprite;
        highlightMaterial.SetTexture("_Texture", highlightRenderer.sprite.texture);
        highlight.transform.position = position;

        highlightMaterial.SetColor("_Color", canDraw ? Color.green : Color.red);
        highlightMaterial.SetFloat("_Alpha", transparency);
    }

    private void InstantiateHighlightNode(Color color, float transparency)
    {
        highlight = new GameObject();
        highlightRenderer = highlight.AddComponent<SpriteRenderer>();
        highlight.AddComponent<NodeLayout>();

        highlightRenderer.material = highlightMaterial;
        highlightMaterial.SetFloat("_Alpha", transparency);
        highlightMaterial.SetColor("_Color", color);
        highlight.layer = 7;
    }
    #endregion

    #region Clicks
    private Vector3 GetMouseTilePosition(Tilemap tilemap)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 screenMousePos = new(mousePos.x, mousePos.y, -nodeCam.transform.position.z);

        Vector3 worldPos = nodeCam.ScreenToWorldPoint(screenMousePos);

        Vector3Int cellCoords = tilemap.WorldToCell(worldPos);

        Vector3 tileCoords = tilemap.CellToWorld(cellCoords);
        tileCoords += tilemap.cellSize * 0.5f;

        return tileCoords;
    }

    public void EnableNodeClicks(bool enable)
    {
        clickEnabled = enable;

        highlightEnabled = enable;
        highlight.SetActive(enable);

        if (enable)
            currentMode = NodeMode.DrawNode;
        else
            currentMode = NodeMode.Disabled;

    }

    public void SetVisibleNodes(int newType)
    {

        while (totalOccupancy.Count < selectedType + 1)
            totalOccupancy.Add(new());
        foreach (var node in totalOccupancy[selectedType].Values)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null)
                    lines.RemoveAt(i);
                else
                    line.SetActive(false);
            }
            node.reference.SetActive(false);
        }

        while (totalOccupancy.Count < newType + 1)
            totalOccupancy.Add(new());

        foreach (var node in totalOccupancy[newType].Values)
        {
            for (int i = 0; i < node.connectedLines.Count; i++)
            {
                var line = node.connectedLines[i];
                if (line == null)
                    node.connectedLines.RemoveAt(i);
                else
                    line.SetActive(true);
            }
            node.reference.SetActive(true);
        }

        selectedType = newType;
    }
    #endregion
}

#region NodeObjects

public enum ButtonType { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10, type11 }

public enum PortType { Input, Output, None }

public class NodeObject
{
    public ButtonType mode;
    public Vector3 position;
    public Vector3Int tilePosition;
    public Vector2Int size;
    public readonly GameObject reference;
    public readonly GameObject original;
    public readonly Dictionary<Vector2Int, PortType> nodeTypes;
    public readonly Dictionary<Vector2Int, ButtonType> buttonTypes;
    public List<GameObject> connectedLines;
    public List<NodeObject> connectedNodes = new();
    public List<NodeObject> nodesConnected = new();

    public NodeObject(Vector3 position, GameObject reference, GameObject original, Tilemap nodeEditorMap, bool setType = true)
    {
        this.position = position;
        tilePosition = nodeEditorMap.WorldToCell(position);
        this.reference = reference;
        this.original = original;
        size = Vector2Int.RoundToInt(reference.GetComponent<SpriteRenderer>().size * 32);
        var nodeLayout = reference.GetComponent<NodeLayout>();
        nodeTypes = nodeLayout.GetPorts();
        buttonTypes = nodeLayout.GetButtons();
        connectedLines = new();
        if (setType)
            SetToMode(ButtonType.type1);
    }

    public void SetToMode(ButtonType type)
    {
        Sprite modeSprite;
        if (reference.GetComponent<NodeLayout>().typeToButton.TryGetValue(type, out var value))
            modeSprite = value.pressedSprite;
        else
            modeSprite = reference.GetComponent<SpriteRenderer>().sprite;

        mode = type;

        reference.GetComponent<SpriteRenderer>().sprite = modeSprite;
    }
}
#endregion