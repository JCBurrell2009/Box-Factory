using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ClickDetection))]
public class MapExplorer : MonoBehaviour
{
    private ClickDetection click;
    private List<Camera> cams;
    private bool dragging;
    private Vector3 prevPosition;

    void Awake()
    {
        click = GetComponent<ClickDetection>();
    }

    void Update()
    {
        if (click.leftIsHeld && cams.Count > 0)
        {
            Vector3 currentPosition = Mouse.current.position.ReadValue();

            if (!dragging)
            {
                prevPosition = currentPosition;
                dragging = true;
                return;
            }

            if (currentPosition == prevPosition)
                return;

            float depth = -cams[0].transform.position.z;
            Vector3 moveWorld = cams[0].ScreenToWorldPoint(new Vector3(currentPosition.x, currentPosition.y, depth))
                              - cams[0].ScreenToWorldPoint(new Vector3(prevPosition.x, prevPosition.y, depth));
            moveWorld.z = 0;
            foreach (var cam in cams)
                cam.transform.position -= moveWorld;
            prevPosition = currentPosition;
        }
        else
            dragging = false;
    }

    public void UpdateCamera(List<Camera> cams) => this.cams = cams;
}
