using System;
using UnityEngine;

public enum Size { Big, Medium, Small, Bigbag, Mediumbag, Smallbag }
public enum State { Cardboard, Cut, Folded, Taped, DeleteThis }
public enum LabelType { Blank, Bullseye, Croaker, Exfed, Greenwalls, Priceinc, Sammysinging, Sammysclub, Slopify, Suncash, Youngmarine }

[CreateAssetMenu(fileName = "BoxSprites", menuName = "Box Factory/Box Sprites")]
public class BoxSpriteData : ScriptableObject
{
    public Sprite[] boxStateSprites;
    public Sprite[] labelSprites;

    public Sprite GetBoxSprite(Box box)
    {
        if (box.state == State.Taped)
            return labelSprites[6 * LabelToInt(box.label) + SizeToInt(box.size)];
        else if (box.state == State.Cardboard)
            return boxStateSprites[0];
        else
            return boxStateSprites[6 * StateToInt(box.state) + SizeToInt(box.size) + 1];
    }

    public static int SizeToInt(Size size)
    {
        return size switch
        {
            Size.Small => 0,
            Size.Medium => 1,
            Size.Big => 2,
            Size.Smallbag => 3,
            Size.Mediumbag => 4,
            Size.Bigbag => 5,
            _ => -1
        };
    }

    public static int StateToInt(State state)
    {
        return state switch
        {
            State.Cut => 0,
            State.Folded => 1,
            State.Taped => 2,
            _ => -1
        };
    }

    public static int LabelToInt(LabelType label)
    {
        return label switch
        {
            LabelType.Blank => 0,
            LabelType.Bullseye => 1,
            LabelType.Croaker => 2,
            LabelType.Exfed => 3,
            LabelType.Greenwalls => 4,
            LabelType.Priceinc => 5,
            LabelType.Sammysinging => 6,
            LabelType.Sammysclub => 7,
            LabelType.Slopify => 8,
            LabelType.Suncash => 9,
            LabelType.Youngmarine => 10,
            _ => -1
        };
    }
}
public readonly struct Box
{
    public readonly Size size;
    public readonly State state;
    public readonly LabelType label;
    public Box(State state, Size size)
    {
        this.state = state;
        this.size = size;
        label = LabelType.Blank;
    }

    public Box(LabelType label, Size size)
    {
        state = State.Taped;
        this.size = size;
        this.label = label;
    }

    #region Normal Actions
    public readonly Box Cut(Size size)
    {
        if (state != State.Cardboard)
            return this;

        return new(State.Cut, size);
    }

    public readonly Box Fold()
    {

        if (state != State.Cut)
            return this;

        return new(State.Folded, size);
    }

    public readonly Box Tape()
    {
        if (state != State.Folded)
            return this;

        return new(LabelType.Blank, size);
    }

    public readonly Box Label(LabelType label)
    {
        if (state != State.Taped)
            return this;

        return new(label, size);
    }
    #endregion

    #region Return Status Actions
    public readonly Box Cut(Size size, out bool status)
    {
        status = false;
        if (state != State.Cardboard)
            return this;

        status = true;
        return new(State.Cut, size);
    }

    public readonly Box Fold(out bool status)
    {
        status = false;
        if (state != State.Cut)
            return this;

        status = true;
        return new(State.Folded, size);
    }

    public readonly Box Tape(out bool status)
    {
        status = false;
        if (state != State.Folded)
            return this;

        status = true;
        return new(LabelType.Blank, size);
    }

    public readonly Box Label(LabelType label, out bool status)
    {
        status = false;
        if (state != State.Taped)
            return this;

        status = true;
        return new(label, size);
    }
    #endregion

    public override bool Equals(object obj)
    {
        if (obj is not Box other)
            return false;

        return Equals(other);



    }

    private bool Equals(Box box)
    {
        if (box.state == State.Cardboard && state == State.Cardboard)
            return true;
        if (state != box.state || size != box.size || label != box.label)
            return false;

        return true;
    }


    // Don't forget to override GetHashCode whenever you override Equals
    public override int GetHashCode()
    {
        // Combine hash codes of properties that define equality
        return HashCode.Combine(state, size, label);
    }

    public static Box GenerateRandomBox(bool onlyGenerateFullBoxes)
    {
        int boxID = UnityEngine.Random.Range(onlyGenerateFullBoxes ? 3 : 0, 14);
        int boxSize = UnityEngine.Random.Range(0, 6);
        var size = boxSize switch
        {
            0 => Size.Small,
            2 => Size.Big,
            3 => Size.Smallbag,
            4 => Size.Mediumbag,
            5 => Size.Bigbag,
            _ => Size.Medium
        };
        if (boxID < 3)
        {
            var state = boxID switch
            {
                1 => State.Cut,
                2 => State.Folded,
                _ => State.Cardboard
            };
            return new(state, size);
        }
        else
        {
            var label = boxID - 3 switch
            {
                1 => LabelType.Bullseye,
                2 => LabelType.Croaker,
                3 => LabelType.Exfed,
                4 => LabelType.Greenwalls,
                5 => LabelType.Priceinc,
                6 => LabelType.Sammysinging,
                7 => LabelType.Sammysclub,
                8 => LabelType.Slopify,
                9 => LabelType.Suncash,
                10 => LabelType.Youngmarine,
                _ => LabelType.Blank
            };
            return new(label, size);
        }
    }
}
