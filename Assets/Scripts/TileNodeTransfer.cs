using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TileNodeTransfer : MonoBehaviour
{
    public List<GameObject> nodes;
    public List<BoxAction> boxActions;
    public List<List<NodeReference>> factoryTileFunction = new();
    public List<List<int>> inputIds = new();
    public Dictionary<GameObject, BoxAction> objectAction = new();

    void Awake()
    {
        int distance = math.min(boxActions.Count, nodes.Count);

        for (int i = 0; i < distance; i++)
            objectAction.Add(nodes[i], boxActions[i]);
    }

    public List<List<ButtonType>> GetPathsFromInputs(int machineType)
    {
        if (machineType >= factoryTileFunction.Count)
            return new();

        var nodeLayout = factoryTileFunction[machineType];
        var machineInputIds = inputIds[machineType];
        var allPaths = new List<List<ButtonType>>();

        for (int i = 1; i < machineInputIds.Count; i++)
        {
            if (i >= nodeLayout.Count)
                break;
            var currentPath = new List<ButtonType>();
            TraversePath(machineInputIds[i], nodeLayout, currentPath, allPaths);
        }

        return allPaths;
    }

    private void TraversePath(int nodeIndex, List<NodeReference> nodeLayout,
                          List<ButtonType> currentPath, List<List<ButtonType>> allPaths)
    {
        var node = nodeLayout[nodeIndex];
        currentPath.Add(node.mode);

        if (node.action == BoxAction.Output || node.connectedNodeIds.Count == 0)
        {
            allPaths.Add(new List<ButtonType>(currentPath));
        }
        else
        {
            foreach (int nextId in node.connectedNodeIds)
                TraversePath(nextId, nodeLayout, currentPath, allPaths);
        }

        currentPath.RemoveAt(currentPath.Count - 1);
    }


    #region NodeFunc
    public void NodesToFunctionList(int functionIndex, List<NodeObject> nodes)
    {
        List<NodeReference> tileFunction = new();
        Dictionary<NodeObject, int> nodeToIds = new();

        bool hasInputs = false;
        bool hasOutputs = false;

        for (int i = 0; i < nodes.Count; i++)
            nodeToIds.Add(nodes[i], i);

        // ensure inputIds is large enough and reset the slot
        while (inputIds.Count - 1 < functionIndex)
        {
            inputIds.Add(new());
            inputIds[^1].Add(1);
        }
        inputIds[functionIndex].Clear();
        inputIds[functionIndex].Add(1); // reset tracker to 1

        foreach (var node in nodes)
        {
            List<int> nodeIds = new();

            foreach (var conNode in node.connectedNodes)
                nodeIds.Add(nodeToIds[conNode]);

            var action = objectAction[node.original];

            if (action == BoxAction.Input)
            {
                hasInputs = true;
                inputIds[functionIndex].Add(tileFunction.Count);
            }
            else if (action == BoxAction.Output)
                hasOutputs = true;

            NodeReference reference = new(node.mode, action, nodeIds);
            tileFunction.Add(reference);
        }

        while (factoryTileFunction.Count - 1 < functionIndex)
            factoryTileFunction.Add(new());

        if (!hasInputs || !hasOutputs)
        {
            factoryTileFunction[functionIndex] = new();
            return;
        }

        factoryTileFunction[functionIndex] = tileFunction;
    }

    public Box CarryOutNodes(ButtonType inputDir, int machineType, Box box, Dictionary<int, int> nextIndices, out ButtonType outputDir, int maxDepth = 10)
    {
        Box originalBox = box;
        outputDir = ButtonType.type5;
        if (machineType >= factoryTileFunction.Count)
            return box;

        var nodesForMachine = factoryTileFunction[machineType];

        if (nodesForMachine.Count == 0)
            return box;

        var machineInputIds = inputIds[machineType];

        List<int> correctInputs = new();
        List<int> positions = new();
        for (int i = 1; i < machineInputIds.Count; i++)
        {
            var inputId = machineInputIds[i];
            if (nodesForMachine[inputId].mode == inputDir)
            {
                correctInputs.Add(inputId);
                positions.Add(i);
            }
        }

        int chosenInput;
        int chosenInputPos;

        if (correctInputs.Count == 0)
        {
            outputDir = ButtonType.type5;
            return new(State.Cardboard, Size.Medium);
        }
        else if (correctInputs.Count == 1)
        {
            chosenInput = correctInputs[0];
            chosenInputPos = positions[0];
        }
        else
        {
            int lastUse = machineInputIds[0];
            chosenInput = correctInputs[0];
            chosenInputPos = positions[0];

            bool found = false;
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] > lastUse)
                {
                    chosenInput = correctInputs[i];
                    chosenInputPos = positions[i];
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                chosenInput = correctInputs[0];
                chosenInputPos = positions[0];
            }
        }

        box = FollowNodePath(nodesForMachine[chosenInput], chosenInput, nodesForMachine, box, maxDepth, nextIndices, out outputDir);

        if (machineInputIds.Count > 2)
            machineInputIds[0] = chosenInputPos;

        if (outputDir == ButtonType.type5 || box.state == State.DeleteThis)
            return originalBox;
        return box;
    }

    private Box FollowNodePath(NodeReference node, int nodeID, List<NodeReference> nodeLayout, Box box, int depth, Dictionary<int, int> nextIndices, out ButtonType outputDir)
    {
        outputDir = ButtonType.type5;
        box = CarryOutFunction(node.action, node.mode, box, out bool filtered);
        if (box.state == State.DeleteThis)
            return box;
        var connectedNodeIds = node.connectedNodeIds;
        if (connectedNodeIds.Count != 0 && depth > 0)
        {
            if (!nextIndices.ContainsKey(nodeID))
                nextIndices[nodeID] = 0;
            int indexToUse;
            if (filtered)
                indexToUse = 1; // no-match port
            else if (node.action == BoxAction.FilterSize || node.action == BoxAction.FilterState || node.action == BoxAction.FilterLabel)
                indexToUse = 0; // match port, don't cycle
            else
            {
                int nextIndex = nextIndices[nodeID];
                indexToUse = nextIndex % connectedNodeIds.Count;
                nextIndices[nodeID] = (indexToUse + 1) % connectedNodeIds.Count;
            }
            nextIndices[nodeID] = (indexToUse + 1) % connectedNodeIds.Count;
            box = FollowNodePath(nodeLayout[connectedNodeIds[indexToUse]], indexToUse, nodeLayout, box, depth - 1, nextIndices, out outputDir);
        }
        else if (node.action == BoxAction.Output)
            outputDir = node.mode;
        return box;
    }

    public Box CarryOutFunction(BoxAction function, ButtonType mode, Box box, out bool match)
    {
        match = false;
        return function switch
        {
            BoxAction.Input => InputAction(mode, box),
            BoxAction.Output => OutputAction(mode, box),
            BoxAction.Cut => CutAction(mode, box),
            BoxAction.Fold => FoldAction(mode, box),
            BoxAction.Tape => TapeAction(mode, box),
            BoxAction.FilterState => FilterStateAction(mode, box, out match),
            BoxAction.FilterSize => FilterSizeAction(mode, box, out match),
            BoxAction.FilterLabel => FilterLabelAction(mode, box, out match),
            BoxAction.Label => LabelAction(mode, box),
            _ => UnassignedAction(mode, box)
        };
    }

    #region Actions
    private Box InputAction(ButtonType mode, Box box) => box;

    private Box OutputAction(ButtonType mode, Box box) => box;

    private Box CutAction(ButtonType mode, Box box)
    {
        if (box.state != State.Cardboard)
            return new(State.DeleteThis, Size.Medium);

        return mode switch
        {
            ButtonType.type1 => new(State.Cut, Size.Smallbag),
            ButtonType.type2 => new(State.Cut, Size.Mediumbag),
            ButtonType.type3 => new(State.Cut, Size.Bigbag),
            ButtonType.type4 => new(State.Cut, Size.Small),
            ButtonType.type5 => new(State.Cut, Size.Medium),
            ButtonType.type6 => new(State.Cut, Size.Big),
            _ => new(State.DeleteThis, Size.Medium)
        };
    }

    private Box FoldAction(ButtonType mode, Box box)
    {
        if (box.state != State.Cut)
            return new(State.DeleteThis, box.size);

        return box.Fold();
    }

    private Box TapeAction(ButtonType mode, Box box)
    {
        if (box.state != State.Folded)
            return new(State.DeleteThis, box.size);

        return box.Tape();
    }

    private Box FilterStateAction(ButtonType mode, Box box, out bool match)
    {
        match = mode switch
        {
            ButtonType.type1 => box.state == State.Cardboard,
            ButtonType.type2 => box.state == State.Cut,
            ButtonType.type3 => box.state == State.Folded,
            ButtonType.type4 => box.state == State.Taped,
            _ => false
        };
        return box;
    }

    private Box FilterSizeAction(ButtonType mode, Box box, out bool match)
    {
        if (box.state == State.Cardboard)
            match = false;
        else
            match = mode switch
            {
                ButtonType.type1 => box.size == Size.Smallbag,
                ButtonType.type2 => box.size == Size.Mediumbag,
                ButtonType.type3 => box.size == Size.Bigbag,
                ButtonType.type4 => box.size == Size.Small,
                ButtonType.type5 => box.size == Size.Medium,
                ButtonType.type6 => box.size == Size.Big,
                _ => false
            };
        return box;
    }
    private Box FilterLabelAction(ButtonType mode, Box box, out bool match)
    {
        if (box.state != State.Taped)
            match = false;
        else
            match = mode switch
            {
                ButtonType.type1 => box.label == LabelType.Blank,
                ButtonType.type2 => box.label == LabelType.Bullseye,
                ButtonType.type3 => box.label == LabelType.Croaker,
                ButtonType.type4 => box.label == LabelType.Exfed,
                ButtonType.type5 => box.label == LabelType.Greenwalls,
                ButtonType.type6 => box.label == LabelType.Priceinc,
                ButtonType.type7 => box.label == LabelType.Sammysinging,
                ButtonType.type8 => box.label == LabelType.Sammysclub,
                ButtonType.type9 => box.label == LabelType.Slopify,
                ButtonType.type10 => box.label == LabelType.Suncash,
                ButtonType.type11 => box.label == LabelType.Youngmarine,
                _ => false
            };
        return box;
    }

    private Box LabelAction(ButtonType mode, Box box)
    {
        if (box.state != State.Taped)
        {
            box = new(State.DeleteThis, box.size);
            return box;
        }

        return mode switch
        {
            ButtonType.type1 => new Box(LabelType.Bullseye, box.size),
            ButtonType.type2 => new Box(LabelType.Croaker, box.size),
            ButtonType.type3 => new Box(LabelType.Exfed, box.size),
            ButtonType.type4 => new Box(LabelType.Greenwalls, box.size),
            ButtonType.type5 => new Box(LabelType.Priceinc, box.size),
            ButtonType.type6 => new Box(LabelType.Sammysinging, box.size),
            ButtonType.type7 => new Box(LabelType.Sammysclub, box.size),
            ButtonType.type8 => new Box(LabelType.Slopify, box.size),
            ButtonType.type9 => new(LabelType.Suncash, box.size),
            ButtonType.type10 => new(LabelType.Youngmarine, box.size),
            _ => new(State.DeleteThis, Size.Medium)
        };
    }

    private Box UnassignedAction(ButtonType mode, Box box)
    {
        Debug.Log("Anassigned" + mode);
        return box;
    }
    #endregion

    #endregion
}

[Serializable]
public enum BoxAction { Input, Output, Cut, Fold, Tape, FilterState, FilterSize, FilterLabel, Label }

public class NodeReference
{
    public ButtonType mode;
    public BoxAction action;
    public List<int> connectedNodeIds;
    public int nextIndex;

    public NodeReference(ButtonType mode, BoxAction action, List<int> connectedNodeIds)
    {
        this.action = action;
        this.connectedNodeIds = connectedNodeIds;
        this.mode = mode;
        nextIndex = connectedNodeIds.Count != 0 ? 0 : -1;
    }
}