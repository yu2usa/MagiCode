using UnityEngine;

/// <summary>
/// 位置に基づく条件
/// </summary>
[CreateAssetMenu(fileName = "PositionCondition", menuName = "MagiCode/Conditions/Position")]
public class PositionCondition : ActionCondition
{
    public enum ConditionType
    {
        EnemyAtPosition,
        PlayerAtPosition,
        SamePosition,
        DifferentPosition
    }

    public ConditionType conditionType;
    public int targetPosition;

    public override bool IsMet(EnemyActionContext context)
    {
        return conditionType switch
        {
            ConditionType.EnemyAtPosition => context.EnemyPosition == targetPosition,
            ConditionType.PlayerAtPosition => context.PlayerPosition == targetPosition,
            ConditionType.SamePosition => context.EnemyPosition == context.PlayerPosition,
            ConditionType.DifferentPosition => context.EnemyPosition != context.PlayerPosition,
            _ => true
        };
    }
}
