
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘画面マネージャクラス
/// </summary>
public class BattleManager : MonoBehaviour
{
    // 管理下コンポーネント
    public FieldManager fieldManager; // フィールド管理クラス

    // Start
    void Start()
    {
        // 管理下コンポーネント初期化
        fieldManager.Init(this);

        Debug.Log("BattleManager.cs : 初期化完了");
    }

    // Update
    void Update()
    {

    }
}