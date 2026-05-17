using UnityEngine;
public class LineManager : MonoBehaviour
{
    [SerializeField] private LineRenderer rend;
    private void Start()
    {
        rend.enabled = false;
        rend.positionCount = 1;
        rend.SetPosition(0, transform.position);
    }
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            rend.enabled = true;
            rend.positionCount = 2;
            rend.SetPosition(1, mousePos);
        }
    }
}