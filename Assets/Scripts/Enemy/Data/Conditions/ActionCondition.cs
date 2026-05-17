using UnityEngine;

/// <summary>
/// アクション実行条件の基底クラス
/// </summary>
public abstract class ActionCondition : ScriptableObject
{
    public abstract bool IsMet(EnemyActionContext context);
}
