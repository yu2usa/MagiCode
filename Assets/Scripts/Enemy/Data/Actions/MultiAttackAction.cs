using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数エリア同時攻撃アクション
/// </summary>
[CreateAssetMenu(fileName = "MultiAttackAction", menuName = "MagiCode/Enemy Actions/Multi Attack")]
public class MultiAttackAction : EnemyAction
{
    [Header("攻撃設定")]
    public int damage = 3;
    public List<int> targetAreas = new List<int>();

    [Header("エフェクトアニメーション")]
    [Tooltip("斬撃エフェクトのアニメーショントリガー")]
    public string slashAnimationTrigger = "Slash";

    [Header("タイミング")]
    public float preAttackDelay = 0.5f;
    public float delayBetweenAttacks = 0.2f;
    public float animationDuration = 0.5f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        // 敵キャラのアニメーション発火
        context.TriggerEnemyAnimation(enemyAnimationTrigger);

        if (preAttackDelay > 0)
            yield return new WaitForSeconds(preAttackDelay);

        bool playerHit = false;

        var areas = context.Enemy.EnemyAreas;
        foreach (int area in targetAreas)
        {
            if (areas == null || area < 0 || area >= areas.Count)
                continue;

            // エフェクト再生
            var slash = context.Enemy.enemySlash;
            Vector3 slashPos = areas[area];
            slashPos.y += 0.5f;
            slash.transform.position = slashPos;
            slash.SetActive(true);
            slash.GetComponent<Animator>().SetTrigger(slashAnimationTrigger);
            CameraShake.Shake();

            // ダメージ判定（1回のみ）
            if (!playerHit && area == context.PlayerPosition)
            {
                context.Player.playerDamaged(damage);
                playerHit = true;
            }

            yield return new WaitForSeconds(delayBetweenAttacks);
        }

        yield return new WaitForSeconds(animationDuration);
        context.Enemy.enemySlash.SetActive(false);
    }

    public override string GetDescription()
    {
        string areas = string.Join(",", targetAreas);
        return $"複数攻撃 (エリア[{areas}], {damage}ダメージ)";
    }
}
