
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    void Awake()
    {
        instance = this;
        tooltipPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    void Update()
    {
        tooltipPanel.transform.position = (Vector3)Mouse.current.position.ReadValue() + new Vector3(15, -15);
    }

    public void Show(string text)
    {
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        Cursor.visible = false;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
        Cursor.visible = true;
    }
}