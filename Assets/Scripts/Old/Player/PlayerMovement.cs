using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;
using TMPro;
using UnityEngine.Assertions.Must;

public class PlayerMovement : MonoBehaviour
{
    public Transform target;  // 目標のオブジェクト
   // public float speed = 0.8f;  // 移動速度
    public float stopDistance = 0.1f; // 近づく距離
    public Animator playerAnim;
    public Animator slashAnim;
    public GameObject gameManager;
    public GameObject slash;
    public bool canMove;
    public bool evade;
    public List<string> currentCodes = new List<string>();
    public string nowCode;

    public GameObject nodeSelect;

    public RectTransform playerCode; // UIのターゲット
    public Transform worldObject;   // 2Dオブジェクト
    public Canvas canvas;
    public bool isDistant;

    public SpriteRenderer spriteRenderer;
    private void Start()
    {
        slash.SetActive(false);
    //     MoveTowards();
        evade = false;
     //   Dodge();
       playerAnim.SetTrigger("Idle");
    }

    public void Execute()
    {
        StartCoroutine(Execution());
    }

    public void MoveTowards()
    {
        StartCoroutine(Attack());
    }

    public void Slash()
    {
        StartCoroutine(Attack());
    }

    public void PlayerDamaged()
    {
        StartCoroutine(Damaged());
    }

    public void Dodge()
    {
        StartCoroutine(Evade());
    }

    IEnumerator Execution()
    {
        currentCodes = nodeSelect.GetComponent<NodeSelect>().selectedCodes;
        for (int i = 0; i < currentCodes.Count; i++)
        {
            if (currentCodes[i] == "MeleeAttack_Window")
            {
                nowCode = "MeleeAttack_Sword";
                MoveTowards();
                
            }
            if (currentCodes[i] == "Evade_Window")
            {
                nowCode = "Evade";
                Dodge();
            }
            if (currentCodes[i] == "Loop_Window")
            {

            }
            if (currentCodes[i] == "WaitForSeconds_Window")
            {
                nowCode = "WaitForSeconds";
            }
            yield return new WaitUntil(() => canMove);
            yield return new WaitForSeconds(0.5f);
            DOTween.KillAll();
        }

        //gameManager.GetComponent<GameManager>().isTurnPlayer = false;
        gameManager.GetComponent<GameManager>().TurnChange(false);

    }


    IEnumerator Attack()
    {
        canMove = false;
        playerAnim.SetTrigger("EndIdle");
        this.transform.DOMove(target.position, 1.5f).SetEase(Ease.Linear);
        
        if (Mathf.Abs(this.transform.position.x - target.position.x) > 6.0f)
        {
            playerAnim.SetTrigger("StartWalk");
            yield return new WaitForSeconds(2.0f);
        }
        slash.SetActive(true);
       // slash.transform.position = this.transform.position;
        slashAnim.SetTrigger("Slash");
        playerAnim.SetTrigger("EndWalk");

        target.GetComponent<KnightAnim>().KnightDamaged();

        yield return new WaitForSeconds(0.2f);
        slash.SetActive(false);
        playerAnim.SetTrigger("Idle");
        canMove = true;
        
    }
    IEnumerator Damaged()
    {
        if (evade == false)
        {
            this.transform.DOMoveX(this.transform.position.x - 0.5f, 2.0f).SetEase(Ease.OutElastic);
            yield return new WaitForSeconds(2.0f);
        }
    }   

    IEnumerator Evade()
    {

        canMove = false;
        spriteRenderer.GetComponent<SpriteRenderer>().DOFade(0.2f, 1f);
        evade = true;
        yield return new WaitForSeconds(3.0f);
        spriteRenderer.GetComponent<SpriteRenderer>().DOFade(1.0f, 1f);
        evade = false;
        canMove = true;
    }

    private void Update()
    {
        if (worldObject == null || playerCode == null || canvas == null)
            return;

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = Camera.main.WorldToScreenPoint(this.transform.position);

        // スクリーン座標をUIのローカル座標に変換
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPoint);
        localPoint.y += 100.0f;

        // UI要素の位置を更新
        playerCode.anchoredPosition = localPoint;

        playerCode.GetComponent<TextMeshProUGUI>().text = nowCode;
    }
}