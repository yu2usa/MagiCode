using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using JetBrains.Annotations;

public class NodeSelect : MonoBehaviour
{
    public TextMeshProUGUI outputArea;
    public GameObject[] nodes;
    public GameObject[] nodesForGenerate;
    public GameObject[] emptyNodes;
    public List<GameObject> currentNodes = new List<GameObject>();
    public List<string> selectedCodes = new List<string>();
    public Vector2[] nodePosition;
    public Transform nodeParent;
    public GameObject player;
    public int selectedNodeNum;
    public GameObject editorManager;


    public void Execute()
    {
        editorManager.GetComponent<EditorWindowMovement>().CloseEditor();
        selectedCodes.Clear(); // �N���A���Ă���ǉ�

        foreach (GameObject node in currentNodes)
        {
            string nodeName = node.name;
            string processedName = ProcessName(nodeName);
            selectedCodes.Add(processedName);
        }

        string ProcessName(string originalName)
        {
            if (originalName.Contains("(Clone)"))
            {
                originalName = originalName.Replace("(Clone)", "").Trim();
            }
            return originalName;
        }
        player.GetComponent<PlayerMovement>().Execute(); 
    }


    private void NodeGenerate(int targetNode)
    {
        GameObject nodeClone = Instantiate(nodesForGenerate[targetNode], nodePosition[selectedNodeNum] / 70, Quaternion.identity, nodeParent);
        this.gameObject.SetActive(false);
        emptyNodes[selectedNodeNum].SetActive(false);
        currentNodes[selectedNodeNum] = nodeClone;
    }

    private void Start()
    {
        this.gameObject.SetActive(false);

    }

    public void NodeWindow(int targetNode)
    {
        this.gameObject.SetActive(true);
        this.gameObject.transform.position = nodePosition[targetNode] / 70;
        this.GetComponent<CanvasGroup>().DOFade(endValue: 0.0f, duration: 0.0f);
        this.GetComponent<CanvasGroup>().DOFade(endValue: 1.0f, duration: 0.1f);
        this.transform.DOScale(new Vector2(0.6f, 0.6f), 0.0f);
        this.transform.DOScale(new Vector3(1.0f, 1.0f, 1.0f), 0.3f).SetEase(Ease.OutBack);
    }
    public void OnNodeClicked(int nodeCount)
    {
        NodeGenerate(nodeCount);
        CloseNodeWindow();
    }

    public void CloseNodeWindow()
    {
        this.gameObject.SetActive(false);
    }

    public void NodeDelete(int targetNode)
    {
        Destroy(currentNodes[targetNode]);
        emptyNodes[targetNode].SetActive(true);
    }

    private void Update()
    {
        NodeOutput();
    }

    public void NodeOutput()
    {
        Debug.Log("NodeOutput");
    }
}
