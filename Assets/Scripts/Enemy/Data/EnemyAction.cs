using System.Collections;
using UnityEngine;

/// <summary>
/// 敵アクションの基底クラス
/// </summary>
public abstract class EnemyAction : ScriptableObject
{
    [Header("基本情報")]
    public string actionName;
    [TextArea(2, 4)]
    public string telegraphMessage;

    [Header("敵キャラアニメーション")]
    [Tooltip("このアクション実行時に敵キャラに設定するアニメーショントリガー（空欄で無視）")]
    public string enemyAnimationTrigger;

    /// <summary>
    /// アクションを実行する
    /// </summary>
    public abstract IEnumerator Execute(EnemyActionContext context);

    /// <summary>
    /// アクションが実行可能かどうかを判定
    /// </summary>
    public virtual bool CanExecute(EnemyActionContext context) => true;

    /// <summary>
    /// エディタ表示用の説明文を取得
    /// </summary>
    public virtual string GetDescription() => actionName;
}
