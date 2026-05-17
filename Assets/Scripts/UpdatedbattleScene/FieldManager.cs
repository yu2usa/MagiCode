
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フィールド管理クラス
/// </summary>
public class FieldManager : MonoBehaviour
{
    // オブジェクト・コンポーネント参照
    private BattleManager battleManager; // 戦闘画面マネージャ

    // 初期化処理
    public void Init(BattleManager _battleManager)
    {
        // 参照取得
        battleManager = _battleManager;

        Debug.Log("FieldManager.cs : 初期化完了");
    }

    // Update
    void Update()
    {

    }

/*
    public void EndDragging()
    {
        // カードを元の位置に戻す
        draggingCard.BackToBasePos();

        // 後処理
        draggingCard = null;
    }*/
}