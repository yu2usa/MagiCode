using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeClickhandler : MonoBehaviour
{
    public int nodeNum;
    public GameObject nodeWindow;

    public void OnNodeClicked()
    {
        nodeWindow.GetComponent<NodeSelect>().OnNodeClicked(nodeNum);
        nodeWindow.GetComponent<NodeSelect>().CloseNodeWindow();
    }
}
