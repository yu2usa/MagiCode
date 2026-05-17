using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallNodeSelect : MonoBehaviour
{
    public int emptyNodeNum;
    public GameObject nodeSelectWindow;

    public void CallNodeWindow()
    {
        nodeSelectWindow.GetComponent<NodeSelect>().NodeWindow(emptyNodeNum);
        nodeSelectWindow.GetComponent<NodeSelect>().selectedNodeNum = emptyNodeNum;
    }
}
