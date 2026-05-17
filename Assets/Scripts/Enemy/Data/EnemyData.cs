using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ソートボスメカニクスにおける敵の役割
/// </summary>
public enum EnemySortRole
{
    None,       // 通常の敵（メカニクスに関与しない）
    Guard,      // バブルソートで並び替えられるガード
    Boss,       // ガードの並び順に応じて被ダメージ倍率が変わるボス
    CoreTotem,  // CoreBossステージで並び替えられるトーテム
    CoreBoss    // CoreBossステージのボス（ソート完了で一撃撃破可能）
}

/// <summary>
/// 死亡アニメーションの種類
/// </summary>
public enum DeathAnimationType
{
    FadeOut,           // フェードアウト（デフォルト）
    UseAnimatorTrigger, // Animatorのトリガーを使用
    ScaleDown,         // 縮小して消える
    FallDown           // 倒れる
}

/// <summary>
/// 属性変化時に発火する Animator トリガーのマッピング（属性ごとに1つ設定）
/// </summary>
[System.Serializable]
public class ElementAnimatorTrigger
{
    public DamageType element;
    [Tooltip("この属性になったときに発火する Animator トリガー名")]
    public string triggerName;
}

/// <summary>
/// 敵の基本データと行動パターンを定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "MagiCode/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    public string enemyName;
    public Sprite enemySprite;
    [Tooltip("敵本体の属性（None = 無属性）")]
    public DamageType element = DamageType.None;

    [Header("属性フェーズメカニクス")]
    [Tooltip("攻撃を受けるたびに順番に変化する属性リスト。空の場合はメカニクス無効。\n各フェーズで対応する属性の攻撃のみダメージが通る（None = 無防備）。")]
    public List<DamageType> elementPhases = new List<DamageType>();
    [Tooltip("属性不一致の攻撃を受けたとき、プレイヤーに与える反射ダメージ（0 = ダメージなし）")]
    public int guardReflectDamage = 1;
    [Tooltip("属性変化時に発火する Animator トリガーのマッピング（例: Ice → ChangeToIceMode）")]
    public List<ElementAnimatorTrigger> elementAnimatorTriggers = new List<ElementAnimatorTrigger>();

    [Header("見た目")]
    [Tooltip("バトル前会話でスプライトを左右反転する（右向きスプライトを左向きにしたい場合にチェック）")]
    public bool flipSpriteInDialogue = false;
    [Tooltip("敵の大きさ（1.0が標準サイズ）")]
    public Vector3 scale = Vector3.one;
    [Tooltip("Y座標のオフセット（スプライトの基準点調整用）")]
    public float positionOffsetY = 0f;

    [Header("アニメーション")]
    [Tooltip("敵キャラのAnimatorController")]
    public RuntimeAnimatorController animatorController;

    [Header("アニメーショントリガー")]
    [Tooltip("攻撃アニメーションのトリガー名")]
    public string attackAnimTrigger = "Action";
    [Tooltip("移動開始アニメーションのトリガー名")]
    public string moveAnimTrigger = "Move";
    [Tooltip("移動終了アニメーションのトリガー名")]
    public string moveEndAnimTrigger = "MoveEnd";
    [Tooltip("死亡アニメーションのトリガー名")]
    public string deathAnimTrigger = "Death";

    [Header("死亡演出")]
    [Tooltip("死亡アニメーションの種類")]
    public DeathAnimationType deathAnimationType = DeathAnimationType.UseAnimatorTrigger;
    [Tooltip("死亡アニメーションのトリガー名（UseAnimatorTrigger時）- deathAnimTriggerを使用")]
    [HideInInspector]
    public string deathAnimatorTrigger = "Death";
    [Tooltip("死亡アニメーションの時間")]
    public float deathAnimationDuration = 0.5f;

    [Header("効果音")]
    [Tooltip("攻撃時に再生する効果音")]
    public AudioClip attackSE;

    [Header("エフェクト")]
    [Tooltip("攻撃エフェクトのプレハブ（未設定時はEnemyControllerのデフォルトを使用）")]
    public GameObject slashEffectPrefab;

    [Header("ステータス")]
    public int maxHealth = 10;
    [Tooltip("移動速度（秒）。小さいほど速い")]
    public float moveDuration = 0.8f;
    [Tooltip("ターン終了時（敵攻撃フェーズ完了後）にHPを全回復する")]
    public bool regenHealthOnTurnEnd = false;

    [Header("オーバーキルペナルティ")]
    [Tooltip("ちょうどHPを削りきる必要がある敵。受けた累積ダメージが maxHealth を超えると超過分をプレイヤーへ反射する")]
    public bool punishOverkill = false;
    [Tooltip("オーバーキル時にプレイヤーが受けるダメージ（0 = 超過ダメージと同量）")]
    public int overkillPunishDamage = 3;

    [Header("KeyGolemメカニクス")]
    [Tooltip("プレイヤー攻撃フェーズ中に指定回数ぴったり攻撃するとダメージが入るメカニクスを有効化")]
    public bool useKeyGolemMechanic = false;

    [Header("ソートボスメカニクス")]
    [Tooltip("バブルソートボスステージでの役割（Noneは通常の敵）")]
    public EnemySortRole sortRole = EnemySortRole.None;

    [Header("CoreBossメカニクス")]
    [Tooltip("CoreTotemに割り当てる番号（昇順に並べるとCoreに30ダメージ）")]
    public int assignedNumber = 0;
    [Tooltip("攻撃を受けてもダメージ0・HPバー非表示（CoreTotem用）")]
    public bool isInvincible = false;

    [Header("攻撃フェーズ行動")]
    public List<WeightedAction> attackPhaseActions = new List<WeightedAction>();

    [Header("防御フェーズ行動")]
    public List<WeightedAction> defensePhaseActions = new List<WeightedAction>();

    [Header("予告表示")]
    public bool showTelegraph = true;

    /// <summary>
    /// 重み付きランダムでアクションを選択
    /// </summary>
    public EnemyAction SelectAction(List<WeightedAction> actions, EnemyActionContext context)
    {
        // 実行可能なアクションをフィルタリング
        var validActions = new List<WeightedAction>();
        float totalWeight = 0f;

        foreach (var wa in actions)
        {
            if (wa.CanExecute(context))
            {
                validActions.Add(wa);
                totalWeight += wa.weight;
            }
        }

        if (validActions.Count == 0 || totalWeight <= 0f)
            return null;

        // 重み付きランダム選択
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var wa in validActions)
        {
            cumulative += wa.weight;
            if (random <= cumulative)
            {
                return wa.action;
            }
        }

        // フォールバック
        return validActions[validActions.Count - 1].action;
    }
}
