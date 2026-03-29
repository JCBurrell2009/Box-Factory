using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LineDrawer : MonoBehaviour
{
    [SerializeField]
    private Material lineMat;
    public float ppu = 32;

    void Awake()
    {
        lineMat = GetComponent<Renderer>().material;
    }

    public void DrawLine(Vector3 pointA, Vector3 pointB)
    {

        Vector3 start = ppu * pointA;
        start = new(Mathf.Floor(start.x) + 0.5f, Mathf.Floor(start.y) + 0.5f, 0f);
        start /= ppu;

        Vector3 end = ppu * pointB;
        end = new(Mathf.Floor(end.x) + 0.5f, Mathf.Floor(end.y) + 0.5f, 0f);
        end /= ppu;

        transform.position = (start + end) / 2;
        transform.localScale = new Vector3(
            Mathf.Abs(start.x - end.x) + 2 / ppu,
            Mathf.Abs(start.y - end.y) + 2 / ppu,
            1
        );

        lineMat.SetVector("_PointA", start);
        lineMat.SetVector("_PointB", end);
        lineMat.SetFloat("_PPU", ppu);
    }
}
