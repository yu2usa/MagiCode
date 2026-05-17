using System.Collections;
using UnityEngine;

/// <summary>
/// チャージアクション（次ターンの強攻撃を予告）
/// </summary>
[CreateAssetMenu(fileName = "ChargeAction", menuName = "MagiCode/Enemy Actions/Charge")]
public class ChargeAction : EnemyAction
{
    [Header("チャージ設定")]
    public float chargeDuration = 1.0f;
    [Tooltip("チャージ完了後に付与されるバフ倍率")]
    public float damageMultiplier = 2.0f;

    [Header("エフェクト")]
    public Color chargeColor = Color.red;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        Debug.Log($"{context.Enemy.name} がチャージ開始！次の攻撃は{damageMultiplier}倍！");

        // 敵キャラのアニメーション発火
        context.TriggerEnemyAnimation(enemyAnimationTrigger);

        // チャージ中のビジュアルフィードバック
        var spriteRenderer = context.Enemy.GetComponent<SpriteRenderer>();
        Color originalColor = Color.white;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = chargeColor;
        }

        yield return new WaitForSeconds(chargeDuration);

        // 色を戻す
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // チャージ状態をEnemyControllerに設定
        context.Enemy.SetChargeMultiplier(damageMultiplier);
    }

    public override string GetDescription()
    {
        return $"チャージ (次攻撃{damageMultiplier}倍)";
    }
}
