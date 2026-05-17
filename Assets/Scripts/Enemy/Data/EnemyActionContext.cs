using UnityEngine;

/// <summary>
/// 敵アクション実行時のコンテキスト情報
/// </summary>
public class EnemyActionContext
{
    public EnemyController Enemy { get; }
    public PlayerController Player { get; }
    public TurnController TurnController { get; }

    public int EnemyPosition => Enemy.enemyCurrentPos;
    public int PlayerPosition => Player.playerCurrentPos;
    public int EnemyHealth => Enemy.enemyHealth;
    public int EnemyMaxHealth => Enemy.MaxHealth;
    public int PlayerHealth => Player.playerHealth;
    public int PlayerMaxHealth => Player.MaxHealth;

    // 敵キャラのAnimator（EnemyControllerで設定済み）
    public Animator EnemyAnimator => Enemy.EnemyAnimator;

    // PrepareNextAttack() で確定したターゲットエリア（-1 = 未確定）
    // MagicAttackAction が予告時点のプレイヤー位置を攻撃時に参照するために使用
    public int LockedAttackArea { get; set; } = -1;

    public EnemyActionContext(EnemyController enemy, PlayerController player, TurnController turnController)
    {
        Enemy = enemy;
        Player = player;
        TurnController = turnController;
    }

    /// <summary>
    /// 敵キャラのアニメーショントリガーを発火
    /// </summary>
    public void TriggerEnemyAnimation(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;
        if (EnemyAnimator == null) return;
        EnemyAnimator.SetTrigger(triggerName);
    }
}
