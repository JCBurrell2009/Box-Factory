using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NodeLayout : MonoBehaviour
{
    [System.Serializable]
    public class PortEntry
    {
        public Vector2Int offset;
        public PortType type;
        public int radius;
        [HideInInspector]
        public Vector2Int sourceOffset;

        public PortEntry(Vector2Int offset, PortType type, int radius)
        {
            this.offset = offset;
            this.type = type;
            this.radius = radius;
            sourceOffset = offset;
        }

        public PortEntry(Vector2Int offset, PortType type, int radius, Vector2Int sourceOffset)
        {

            this.offset = offset;
            this.type = type;
            this.radius = radius;
            this.sourceOffset = sourceOffset;
        }
    }

    [System.Serializable]
    public struct TypeButton
    {
        public Vector2Int offset;
        public ButtonType type;
        public int radius;
        public Sprite pressedSprite;

        public TypeButton(Vector2Int offset, ButtonType type, int radius, Sprite pressedSprite)
        {
            this.offset = offset;
            this.type = type;
            this.radius = radius;
            this.pressedSprite = pressedSprite;
        }
    }

    public List<PortEntry> portEntries = new();
    public List<TypeButton> typeButtons = new();
    [HideInInspector]
    public Dictionary<ButtonType, TypeButton> typeToButton;

    void Awake()
    {
        for (int i = 0; i < portEntries.Count; i++)
            portEntries[i].sourceOffset = portEntries[i].offset;
        typeToButton = new();
        foreach (var button in typeButtons)
            typeToButton.TryAdd(button.type, button);
    }

    public List<PortEntry> ExpandPorts()
    {
        List<PortEntry> expandedPortEntries = new(portEntries);
        foreach (var entry in portEntries)
        {
            if (entry.radius == 0)
                continue;
            int size = entry.radius * 2 + 1;
            for (int x = -(size - 1) / 2; x <= (size - 1) / 2; x++)
            {
                for (int y = -(size - 1) / 2; y <= (size - 1) / 2; y++)
                {
                    PortEntry newEntry;
                    newEntry = new(new(entry.offset.x + x, entry.offset.y + y), entry.type, 0, entry.offset);
                    expandedPortEntries.Add(newEntry);
                }
            }
        }
        return expandedPortEntries;
    }

    public List<TypeButton> ExpandButtons()
    {
        List<TypeButton> expandedTypeButtons = new(typeButtons);
        foreach (var button in typeButtons)
        {
            if (button.radius == 0)
                continue;

            int size = button.radius * 2 + 1;
            for (int x = -(size - 1) / 2; x <= (size - 1) / 2; x++)
            {
                for (int y = -(size - 1) / 2; y <= (size - 1) / 2; y++)
                {
                    TypeButton newEntry = new(new(button.offset.x + x, button.offset.y + y), button.type, 0, button.pressedSprite);
                    expandedTypeButtons.Add(newEntry);
                }
            }
        }
        return expandedTypeButtons;
    }

    public Vector2Int GetPortCenter(Vector2Int offset)
    {
        var expandedPortEntries = ExpandPorts();
        foreach (var entry in expandedPortEntries)
            if (entry.offset == offset)
                return entry.sourceOffset;

        return new();
    }

    public Vector2Int GetPortCenter(Vector2Int offset, out PortType type)
    {
        var expandedPortEntries = ExpandPorts();
        foreach (var entry in expandedPortEntries)
            if (entry.offset == offset)
            {
                type = entry.type;
                return entry.sourceOffset;
            }

        type = PortType.None;
        Debug.Log("this is a bug");
        return new();
    }

    public Dictionary<Vector2Int, PortType> GetPorts()
    {
        var expandedPortEntries = ExpandPorts();
        var dict = new Dictionary<Vector2Int, PortType>();
        if (portEntries.Count == 0)
            return dict;
        foreach (var entry in expandedPortEntries)
            dict[entry.offset] = entry.type;
        return dict;
    }

    public Dictionary<Vector2Int, ButtonType> GetButtons()
    {
        var expandedButtonEntries = ExpandButtons();
        var dict = new Dictionary<Vector2Int, ButtonType>();
        if (typeButtons.Count == 0)
            return dict;
        foreach (var button in expandedButtonEntries)
            dict[button.offset] = button.type;
        return dict;
    }

}
