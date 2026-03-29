using System.Collections.Generic;
using UnityEngine;

public class FactoryTile : MonoBehaviour
{
    public int machineType;
    public Vector2 position;
    public Box boxToProcess;
    public ButtonType incomingDirection;
    public Dictionary<int, int> nextIndices = new();

    public FactoryTile neighborUp = null;
    public FactoryTile neighborDown = null;
    public FactoryTile neighborLeft = null;
    public FactoryTile neighborRight = null;

    public void Initialize(int machineType, Dictionary<Vector3, GameObject> tilePositions, Vector2 position, Vector2 tileSize)
    {
        this.machineType = machineType;
        this.position = position;
        UpdateAdjacencies(tilePositions, tileSize);
    }

    public void UpdateAdjacencies(Dictionary<Vector3, GameObject> tilePositions, Vector2 tileSize)
    {
        if (tilePositions.TryGetValue(position + Vector2.up * tileSize.y, out var temp))
            neighborUp = temp.GetComponent<FactoryTile>();
        else
            neighborUp = null;

        if (tilePositions.TryGetValue(position + Vector2.down * tileSize.y, out temp))
            neighborDown = temp.GetComponent<FactoryTile>();
        else
            neighborDown = null;

        if (tilePositions.TryGetValue(position + Vector2.left * tileSize.x, out temp))
            neighborLeft = temp.GetComponent<FactoryTile>();
        else
            neighborLeft = null;

        if (tilePositions.TryGetValue(position + Vector2.right * tileSize.x, out temp))
            neighborRight = temp.GetComponent<FactoryTile>();
        else
            neighborRight = null;

    }

    public FactoryTile GetFactoryTileInDirection(ButtonType direction)
    {
        return direction switch
        {
            ButtonType.type1 => neighborUp,
            ButtonType.type2 => neighborLeft,
            ButtonType.type3 => neighborDown,
            ButtonType.type4 => neighborRight,
            _ => null
        };
    }
    public FactoryTile GetFactoryTileInDirection(ButtonType direction, Vector2 tileSize, out Vector2 tilePos)
    {
        tilePos = position + ButtonTypeToDir(direction);
        return direction switch
        {
            ButtonType.type1 => neighborUp,
            ButtonType.type2 => neighborLeft,
            ButtonType.type3 => neighborDown,
            ButtonType.type4 => neighborRight,
            _ => null
        };
    }

    public static ButtonType SwapDirection(ButtonType direction)
    {
        return direction switch
        {
            ButtonType.type1 => ButtonType.type3,
            ButtonType.type2 => ButtonType.type4,
            ButtonType.type3 => ButtonType.type1,
            ButtonType.type4 => ButtonType.type2,
            _ => ButtonType.type5
        };
    }

    public static Vector2 ButtonTypeToDir(ButtonType type)
    {
        return type switch
        {
            ButtonType.type1 => Vector2.up,
            ButtonType.type2 => Vector2.down,
            ButtonType.type3 => Vector2.left,
            ButtonType.type4 => Vector2.right,
            _ => new()
        };
    }
}