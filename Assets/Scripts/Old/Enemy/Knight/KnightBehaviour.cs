using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class KnightAnim : MonoBehaviour
{

    public GameObject youwon;


    public Transform target;  // �ڕW�̃I�u�W�F�N�g
    public Animator knightAnim;
    public Animator slashAnim;
    public GameObject slash;
   // public int knightHealthInspector;
    public static int knightHealth;
    public TextMeshProUGUI knightHealthText;
  
    public RectTransform knighthealthBar; // UI�̃^�[�Q�b�g
    public Transform worldObject;   // 2D�I�u�W�F�N�g
    public Canvas canvas;
    public GameObject gameManager;
    public List<string> currentCodes = new List<string>();
    public bool canMove;

    void Awake()
    {
        slash.SetActive(false);
        //  Slash();
        knightHealth = 10;// knightHealthInspector;
        knightAnim.SetTrigger("Idle");
        Attack();
    }

    private void Update()
    {
        if (worldObject == null || knighthealthBar == null || canvas == null)
            return;

        // ���[���h���W���X�N���[�����W�ɕϊ�
        Vector3 screenPos = Camera.main.WorldToScreenPoint(this.transform.position);

        // �X�N���[�����W��UI�̃��[�J�����W�ɕϊ�
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPoint);
        localPoint.y -= 10.0f;

        // UI�v�f�̈ʒu���X�V
        knighthealthBar.anchoredPosition = localPoint;
    }

    public void AttackSeries()
    {
        StartCoroutine(Execution());
    }

    IEnumerator Execution()
    {
        for (int i = 0; i < currentCodes.Count; i++)
        {
            if (currentCodes[i] == "Slash")
            {
                Slash();
            }
            yield return new WaitUntil(() => canMove);
            yield return new WaitForSeconds(0.5f);
            DOTween.KillAll();
        }

        //gameManager.GetComponent<GameManager>().isTurnPlayer = false;
        gameManager.GetComponent<GameManager>().TurnChange(true);

    }



    public void Slash()
    {
       StartCoroutine(Attack());
    }

    public void KnightDamaged()
    {
        Debug.Log("KnightDamaged called. Current Health: " + knightHealth);
        StartCoroutine(Damaged());
        if (knightHealth > 0)
        {
            knightHealth--;
        }

        if (knightHealth <= 0)
        {
            knightAnim.SetTrigger("Die");
            Invoke("YouwonScreen", 1.5f);
        }

        knighthealthBar.GetComponent<Slider>().value = knightHealth;
        knightHealthText.text = knightHealth.ToString() + "/" + "10";

    }

    IEnumerator Attack()
    {
        knightAnim.SetTrigger("Idle");
        if (Mathf.Abs(this.transform.position.x - target.position.x) > 6.0f)
        {
            this.transform.DOMove(target.position, 2.0f).SetEase(Ease.Linear);
            yield return new WaitForSeconds(2.0f);
        }
        slash.SetActive(true);
       // slash.transform.position = this.transform.position;
        slashAnim.SetTrigger("Slash");
        knightAnim.SetTrigger("EndIdle");
        knightAnim.SetTrigger("Slash");
        yield return new WaitForSeconds(0.2f);
        slash.SetActive(false);
        knightAnim.SetTrigger("EndSlash");
        knightAnim.SetTrigger("Idle");
        canMove = true;
    }

    IEnumerator Damaged()
    {
         this.transform.DOMoveX(this.transform.position.x + 0.5f, 2.0f).SetEase(Ease.OutElastic);
        knightAnim.SetTrigger("Damaged");
         yield return new  WaitForSeconds(0.2f);
        knightAnim.SetTrigger("EndDamaged");
    }


    public void YouwonScreen()
    {
        youwon.SetActive(true);
        youwon.GetComponent<CanvasGroup>().DOFade(endValue: 0.0f, duration: 0.0f);
        youwon.GetComponent<CanvasGroup>().DOFade(endValue: 1.0f, duration: 1.0f);
    }
}
