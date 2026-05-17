using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

public class CardContainer : MonoBehaviour
{
    public List<string> cardTypes = new List<string>();
    private List<Transform> trackedChildren = new List<Transform>();

    void Update()
    {
       

        // 子オブジェクトのチェック - 外れたオブジェクトの処理
        for (int i = trackedChildren.Count - 1; i >= 0; i--)
        {
            Transform child = trackedChildren[i];
            if (child == null || child.parent != this.transform)
            {
                // 子が外れた場合、該当するリスト要素も削除する
                cardTypes.RemoveAt(i);
                trackedChildren.RemoveAt(i);
            }
        }

        // 新たに追加された子オブジェクトの確認
        foreach (Transform child in transform)
        {
            if (!trackedChildren.Contains(child))
            {
                CardDiscrimination card = child.GetComponent<CardDiscrimination>();
                if (card != null)
                {
                    cardTypes.Add(card.cardType);
                    trackedChildren.Add(child);
                }
            }
        }
    }




}
