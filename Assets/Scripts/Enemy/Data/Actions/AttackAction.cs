using System.Collections;
using UnityEngine;

/// <summary>
/// 基本攻撃アクション
/// </summary>
[CreateAssetMenu(fileName = "AttackAction", menuName = "MagiCode/Enemy Actions/Attack")]
public class AttackAction : EnemyAction
{
    [Header("攻撃設定")]
    public int damage = 3;
    [Tooltip("-1で現在位置を攻撃")]
    public int targetArea = -1;

    [Header("エフェクトアニメーション")]
    [Tooltip("trueにするとenemySlashの代わりにenemyShockwaveを使用")]
    public bool useShockwave = false;
    [Tooltip("斬撃エフェクトのアニメーショントリガー")]
    public string slashAnimationTrigger = "Slash";

    [Header("タイミング")]
    public float preAttackDelay = 0f;
    public float animationDuration = 0.83f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        Debug.Log($"[AttackAction] 攻撃開始: {actionName}, Enemy={context.Enemy.name}");

        // 敵キャラの攻撃アニメーション発火 + 攻撃 SE
        context.Enemy.TriggerAttackAnimation();
        context.Enemy.PlayAttackSE();

        if (preAttackDelay > 0)
            yield return new WaitForSeconds(preAttackDelay);

        int attackArea = targetArea >= 0 ? targetArea : context.EnemyPosition;

        // プレイヤー位置を直接取得（キャッシュされた参照ではなく最新の状態を取得）
        var player = Object.FindObjectOfType<PlayerController>();
        int playerPos = player != null ? player.playerCurrentPos : context.PlayerPosition;

        Debug.Log($"[AttackAction] targetArea={targetArea}, EnemyPos={context.EnemyPosition}, 攻撃エリア={attackArea}, プレイヤー位置={playerPos} (context={context.PlayerPosition})");

        // スラッシュ or Shockwave エフェクト配置・再生
        var slash = useShockwave ? context.Enemy.enemyShockwave : context.Enemy.enemySlash;
        if (slash == null)
        {
            Debug.LogError($"[AttackAction] エフェクト({(useShockwave ? "enemyShockwave" : "enemySlash")}) が null です！ Enemy={context.Enemy.name}");
            yield break;
        }

        var areaList = context.Enemy.EnemyAreas;
        if (areaList == null || attackArea >= areaList.Count)
        {
            Debug.LogError($"[AttackAction] EnemyAreas が無効です！ attackArea={attackArea}, Count={areaList?.Count}");
            yield break;
        }

        Vector3 slashPos = areaList[attackArea];
        slashPos.y += 0.5f;
        slash.transform.position = slashPos;
        slash.SetActive(true);

        var animator = slash.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(slashAnimationTrigger);
        }
        CameraShake.Shake();

        yield return new WaitForSeconds(animationDuration);

        // ダメージ判定（直接取得したプレイヤー位置を使用）
        Debug.Log($"[AttackAction] ダメージ判定: attackArea={attackArea} == PlayerPos={playerPos} → {attackArea == playerPos}");
        if (attackArea == playerPos)
        {
            Debug.Log($"[AttackAction] ヒット！ {damage}ダメージ");
            if (player != null)
                player.playerDamaged(damage);
            else
                context.Player.playerDamaged(damage);
        }
        else
        {
            Debug.Log($"[AttackAction] ミス (攻撃エリア{attackArea} != プレイヤー位置{playerPos})");
        }

        slash.SetActive(false);
        Debug.Log("[AttackAction] 攻撃完了");
    }

    public override string GetDescription()
    {
        string target = targetArea >= 0 ? $"エリア{targetArea}" : "現在位置";
        return $"攻撃 ({target}, {damage}ダメージ)";
    }
}
