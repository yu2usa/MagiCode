using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数の行動を順番に実行するアクション
/// </summary>
[CreateAssetMenu(fileName = "SequenceAction", menuName = "MagiCode/Enemy Actions/Sequence")]
public class SequenceAction : EnemyAction
{
    [Header("連続行動設定")]
    public List<EnemyAction> actions = new List<EnemyAction>();

    [Tooltip("各行動間の待機時間（秒）")]
    public float delayBetweenActions = 0.2f;

    [Tooltip("最初の行動前の待機時間（秒）")]
    public float initialDelay = 0f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        if (initialDelay > 0)
            yield return new WaitForSeconds(initialDelay);

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action == null) continue;

            // 行動が実行可能かチェック
            if (!action.CanExecute(context))
            {
                Debug.Log($"SequenceAction: {action.actionName} はスキップ（実行条件を満たさない）");
                continue;
            }

            yield return action.Execute(context);

            // 最後の行動でなければ待機
            if (i < actions.Count - 1 && delayBetweenActions > 0)
            {
                yield return new WaitForSeconds(delayBetweenActions);
            }
        }
    }

    public override bool CanExecute(EnemyActionContext context)
    {
        // 少なくとも1つの行動が実行可能ならtrue
        foreach (var action in actions)
        {
            if (action != null && action.CanExecute(context))
                return true;
        }
        return false;
    }

    public override string GetDescription()
    {
        if (actions.Count == 0)
            return "連続行動 (空)";

        var names = new List<string>();
        foreach (var action in actions)
        {
            if (action != null)
                names.Add(action.actionName ?? action.name);
        }
        return $"連続: {string.Join(" → ", names)}";
    }
}
