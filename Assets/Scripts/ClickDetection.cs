using UnityEngine;
using UnityEngine.InputSystem;

public class ClickDetection : MonoBehaviour
{
    private Mouse mouse;

    [HideInInspector]
    public bool leftWasClickedThisFrame;
    [HideInInspector]
    public bool rightWasClickedThisFrame;
    [HideInInspector]
    public bool leftIsHeld;
    [HideInInspector]
    public bool rightIsHeld;
    [HideInInspector]
    public bool leftIsClicked;
    [HideInInspector]
    public bool rightIsClicked;

    public readonly float clickToHoldTime = 0.3f;
    public readonly float distanceToHold = 100f;
    private float lt = 0;
    private float rt = 0;
    private Vector2 lMouseClickPos = new();
    private Vector2 rMouseClickPos = new();

    void Awake()
    {
        mouse = Mouse.current;
    }

    void Update()
    {
        leftIsClicked = false;
        rightIsClicked = false;
        leftWasClickedThisFrame = false;
        rightWasClickedThisFrame = false;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            leftWasClickedThisFrame = true;
            lMouseClickPos = mouse.position.ReadValue();
        }
        else if (mouse.leftButton.isPressed)
        {
            var mouseDist = Vector2.Distance(mouse.position.ReadValue(), lMouseClickPos);
            if (lt >= clickToHoldTime || mouseDist >= distanceToHold)
            {
                leftIsHeld = true;
                lt = clickToHoldTime;
            }
            else
                lt += Time.deltaTime;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            leftIsHeld = false;
            if (lt < clickToHoldTime)
                leftIsClicked = true;
            lt = 0;
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            rightWasClickedThisFrame = true;
            rMouseClickPos = mouse.position.ReadValue();
        }
        else if (mouse.rightButton.isPressed)
        {
            var mouseDist = Vector2.Distance(mouse.position.ReadValue(), rMouseClickPos);
            if (rt >= clickToHoldTime || mouseDist >= distanceToHold)
            {
                rightIsHeld = true;
                rt = clickToHoldTime;
            }
            else
                rt += Time.deltaTime;
        }
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            rightIsHeld = false;
            rMouseClickPos = new();
            if (rt < clickToHoldTime)
                rightIsClicked = true;
            rt = 0;
        }

    }
}
