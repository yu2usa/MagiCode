using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class BattleSceneManager : MonoBehaviour

{
    public int turnCount;
    public bool isPlayerTurn;
    public bool isGuardPhase;
    public GameObject guardPhaseUI;
    public GameObject attackPhaseUI;

    //public bool doStartWithEnemyGuardPhase;

    public GameObject turnStart;
    public GameObject turnEnd;

    public GameObject codeInputField;

    public GameObject player;
    public GameObject enemy;

    void Start()
    {
        codeInputField.SetActive(false);
        TurnStart();
    }

    public void TurnStart()
    {
        StartCoroutine(PlayerTurnBegin());
    }


    IEnumerator PlayerTurnBegin()
    {

        turnStart.SetActive(true);
        turnEnd.SetActive(false);

        turnStart.GetComponent<CanvasGroup>().alpha = 1;
        turnStart.GetComponent<CanvasGroup>().DOFade(0, 0.2f);
        yield return new WaitForSeconds(0.5f);


        codeInputField.SetActive(true);
        codeInputField.GetComponent<CanvasGroup>().alpha = 0;
        codeInputField.GetComponent<CanvasGroup>().DOFade(1, 0.5f);

        turnStart.GetComponent<CanvasGroup>().DOFade(1, 0.2f);
        yield return new WaitForSeconds(0.2f);
        turnStart.SetActive(false);
    }

    public void PlayerTurnEnd()
    {
      //  if(this)
      //  {
            
     //   }
        StartCoroutine(TurnFinishAnim());



    }

    IEnumerator TurnFinishAnim()
    {
        yield return new();
    }

}
