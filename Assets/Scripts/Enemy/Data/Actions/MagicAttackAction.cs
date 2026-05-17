using System.Collections;
using UnityEngine;

/// <summary>
/// 魔法攻撃アクション。プレイヤー側エリアに魔法エフェクトを表示してダメージを与える。
/// MP消費なし。AttackAction と同様に WeightedAction に設定して使用する。
/// </summary>
[CreateAssetMenu(fileName = "MagicAttackAction", menuName = "MagiCode/Enemy Actions/Magic Attack")]
public class MagicAttackAction : EnemyAction
{
    [Header("魔法設定")]
    [Tooltip("ダメージ量")]
    public int damage = 3;
    [Tooltip("魔法の属性")]
    public DamageType damageType = DamageType.None;
    [Tooltip("攻撃対象エリア（-1 でプレイヤーの現在エリアを追尾）")]
    public int targetArea = -1;

    [Header("エフェクト")]
    [Tooltip("魔法エフェクトの Prefab（Instantiate して使用）")]
    public GameObject magicEffectPrefab;
    [Tooltip("エフェクト起動時に Animator に送るトリガー名（空欄でスキップ）")]
    public string animatorTrigger = "Strike";

    [Header("タイミング")]
    [Tooltip("詠唱モーション後、エフェクト発動までの待機時間（秒）")]
    public float preAttackDelay = 0.5f;
    [Tooltip("エフェクトの表示時間（秒）")]
    public float effectDuration = 0.7f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        // 敵の詠唱アニメーション + 攻撃 SE
        context.TriggerEnemyAnimation(enemyAnimationTrigger);
        context.Enemy.PlayAttackSE();

        if (preAttackDelay > 0)
            yield return new WaitForSeconds(preAttackDelay);

        // targetArea == -1 の場合は PrepareNextAttack() で確定済みのエリアを使用（予告と一致させる）
        // LockedAttackArea が未設定（-1）の場合のみ現在位置にフォールバック
        int attackArea = targetArea >= 0 ? targetArea
                       : context.LockedAttackArea >= 0 ? context.LockedAttackArea
                       : context.PlayerPosition;

        // エフェクトをプレイヤー側エリア座標に生成
        Vector2 areaPos = context.Player.GetPlayerArea(attackArea);
        Vector3 effectPos = new Vector3(areaPos.x, areaPos.y + 0.5f, 0f);

        GameObject effect = null;
        if (magicEffectPrefab != null)
        {
            effect = Object.Instantiate(magicEffectPrefab, effectPos, Quaternion.identity);
            // 非アクティブな Prefab から生成した場合も有効化してから Animator を操作する
            effect.SetActive(true);
            var animator = effect.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
                animator.SetTrigger(animatorTrigger);
        }

        CameraShake.Shake();
        yield return new WaitForSeconds(effectDuration);

        // プレイヤーが攻撃エリアにいればダメージ
        if (context.PlayerPosition == attackArea)
            context.Player.GetDamaged(damage);

        if (effect != null)
            Object.Destroy(effect);
    }

    public override string GetDescription()
    {
        string target = targetArea >= 0 ? $"エリア{targetArea}" : "プレイヤーの現在エリア";
        return $"魔法攻撃 ({damageType}, {target}, {damage}ダメージ)";
    }
}
