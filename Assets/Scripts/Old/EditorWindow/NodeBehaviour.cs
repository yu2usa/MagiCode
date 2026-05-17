using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeBehaviour : MonoBehaviour
{
    public int nodeNum;
    [SerializeField] private GameObject nodeSelectWindow;

    public void deleteNode()
    {
        nodeSelectWindow.GetComponent<NodeSelect>().NodeDelete(nodeNum);
    }
}
