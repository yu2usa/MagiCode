using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public GameObject editorWindow;
   // public bool isTurnPlayer;
    public GameObject enemy;
    public GameObject player;
    public TextMeshProUGUI turnDispText;
    public GameObject turnDisp;
    public GameObject turnCountDisp;
    public int turnCount;
    public GameObject nodeWindow;
    private Vector2 playerInitPos;
    private Vector2 enemyInitPos;


    /// <summary>
    /// //////////////////////////
    /// </summary>
    public GameObject emptyNode1;
    public GameObject emptyNode2;
    public GameObject emptyNode3;


    void Start()
    {
        turnCount = 0;
        StartCoroutine(InitializeAnim());
        playerInitPos = player.transform.position;
        enemyInitPos = enemy.transform.position;

    }

    public void TurnChange(bool isToPlayer)
    {
        string turnCountStr = turnCount.ToString();
        if (isToPlayer == true)
        {
            turnCount++;
            turnCountDisp.GetComponent<TextMeshProUGUI>().text = "Turn: " + turnCount;
            editorWindow.SetActive(true);
            turnDispText.text = "YOUR TURN";
            TurnChangeAnimation(true);

        }
        else
        {
            turnDispText.text = "ENEMY TURN";
            editorWindow.SetActive(false);
            TurnChangeAnimation(false);
        }

    }
    public void TurnChangeAnimation(bool isPlayer)
    {
        StartCoroutine(TurnChangeAnim(isPlayer));
    }



    IEnumerator InitializeAnim()
    {
        turnDisp.SetActive(false);
        turnDisp.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        turnDisp.SetActive(true);
        turnDisp.SetActive(true);
        TurnChange(true);
    }



    IEnumerator TurnChangeAnim(bool isPlayer)
    {
        player.transform.DOMove((playerInitPos), 0.1f).SetEase(Ease.Linear);
        enemy.transform.DOMove((enemyInitPos), 0.1f).SetEase(Ease.Linear);

        turnDisp.SetActive(true);
        turnDisp.GetComponent<CanvasGroup>().DOFade(0.001f, 0.0f);
        turnDisp.GetComponent<CanvasGroup>().DOFade(0.5f, 1.0f);
        yield return new WaitForSeconds(0.7f);
        turnDisp.GetComponent<CanvasGroup>().DOFade(0.2f, 0.0f);
        turnDisp.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        if (isPlayer ==false)
        {
            NodeInit();
            enemy.GetComponent<KnightAnim>().AttackSeries();
        }
    }
   
    public void NodeInit()
    {
        Destroy(nodeWindow.GetComponent<NodeSelect>().currentNodes[0]);
        Destroy(nodeWindow.GetComponent<NodeSelect>().currentNodes[1]);
        Destroy(nodeWindow.GetComponent<NodeSelect>().currentNodes[2]);

        nodeWindow.GetComponent<NodeSelect>().currentNodes[0] = null;
        nodeWindow.GetComponent<NodeSelect>().currentNodes[1] = null;
        nodeWindow.GetComponent<NodeSelect>().currentNodes[2] = null;

        nodeWindow.GetComponent<NodeSelect>().selectedCodes.Clear();
        emptyNode1.SetActive(true);
        emptyNode2.SetActive(true);
        emptyNode3.SetActive(true);
    }

}
