using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public float longPressThreshold = 1.0f;  // 長押し判定までの秒数
    private float pressTime = 0f;
    private bool isPressing = false;

    public Transform cardParent;

    public RectTransform targetRectTransform;

    public void OnBeginDrag(PointerEventData eventData) // ドラッグを始めるときに行う処理
    {
        cardParent = transform.parent;
        transform.SetParent(cardParent.parent, false);
        GetComponent<CanvasGroup>().blocksRaycasts = false; // blocksRaycastsをオフにする
    }

    public void OnDrag(PointerEventData eventData) // ドラッグした時に起こす処理
    {
     //   targetRectTransform = GetComponent<RectTransform>();

        /*Vector2 localPoint;
        Vector2 mousePos = Input.mousePosition;

        // RectTransformの中のローカル座標に変換
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRectTransform,
            mousePos,
            null,  // カメラ (CanvasがScreen Space - Overlayならnull)
            out localPoint
        );*/

        //transform.position = new Vector2(eventData.position.x - 55002, eventData.position.y -10580);
        transform.position = eventData.position;　
    }

    public void OnEndDrag(PointerEventData eventData) // カードを離したときに行う処理
    {
        transform.SetParent(cardParent, false);
        GetComponent<CanvasGroup>().blocksRaycasts = true; // blocksRaycastsをオンにする


        //独自の追加処理
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // サイズリセット（GridLayoutGroupのCellSizeに合わせたい場合など）
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170, 220); // ← お好みのサイズに
    }

    public void OnClicked()
    {
        // マウス左クリック or スペースキーの押下開始を検知
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            isPressing = true;
            pressTime = 0f;
        }

        while (isPressing == true)
        {
            // 押し続けている間
            if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) && isPressing)
            {
                pressTime += Time.deltaTime;

                if (pressTime >= longPressThreshold)
                {
                    Debug.Log("長押し判定発動！");
                    isPressing = false;  // 一度発動したらリセット（連続判定したいならここをコメントアウト）
                }
            }
        }

        // 押し離したときにリセット
        if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space))
        {
            isPressing = false;
            pressTime = 0f;
        }
    }

    void Update()
    {

    }
}