using UnityEngine;

/// <summary>
/// 重み付きアクション（条件付き）
/// </summary>
[System.Serializable]
public class WeightedAction
{
    public EnemyAction action;
    [Range(0f, 1f)]
    public float weight = 1f;
    public ActionCondition condition;

    /// <summary>
    /// 条件を満たしているかチェック
    /// </summary>
    public bool MeetsCondition(EnemyActionContext context)
    {
        if (condition == null) return true;
        return condition.IsMet(context);
    }

    /// <summary>
    /// アクションが実行可能かどうか
    /// </summary>
    public bool CanExecute(EnemyActionContext context)
    {
        if (action == null) return false;
        return MeetsCondition(context) && action.CanExecute(context);
    }
}
