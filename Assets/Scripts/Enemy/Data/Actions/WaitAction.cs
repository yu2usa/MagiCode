using System.Collections;
using UnityEngine;

/// <summary>
/// 待機アクション（何もしない）
/// </summary>
[CreateAssetMenu(fileName = "WaitAction", menuName = "MagiCode/Enemy Actions/Wait")]
public class WaitAction : EnemyAction
{
    [Header("待機設定")]
    public float waitDuration = 0.5f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        // 敵キャラのアニメーション発火
        context.TriggerEnemyAnimation(enemyAnimationTrigger);

        yield return new WaitForSeconds(waitDuration);
    }

    public override string GetDescription()
    {
        return "待機";
    }
}
