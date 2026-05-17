using UnityEngine;

/// <summary>
/// HP状態に基づく条件
/// </summary>
[CreateAssetMenu(fileName = "HealthCondition", menuName = "MagiCode/Conditions/Health")]
public class HealthCondition : ActionCondition
{
    public enum Target { Enemy, Player }
    public enum ComparisonType { LessThan, LessOrEqual, Equal, GreaterOrEqual, GreaterThan }

    public Target target;
    public ComparisonType comparison;
    [Range(0, 100)]
    public int thresholdPercent = 50;

    public override bool IsMet(EnemyActionContext context)
    {
        int currentHealth = target == Target.Enemy ? context.EnemyHealth : context.PlayerHealth;
        int maxHealth = target == Target.Enemy ? context.EnemyMaxHealth : context.PlayerMaxHealth;

        float healthPercent = (float)currentHealth / maxHealth * 100f;

        return comparison switch
        {
            ComparisonType.LessThan => healthPercent < thresholdPercent,
            ComparisonType.LessOrEqual => healthPercent <= thresholdPercent,
            ComparisonType.Equal => Mathf.Approximately(healthPercent, thresholdPercent),
            ComparisonType.GreaterOrEqual => healthPercent >= thresholdPercent,
            ComparisonType.GreaterThan => healthPercent > thresholdPercent,
            _ => true
        };
    }
}
