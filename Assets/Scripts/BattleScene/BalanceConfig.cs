using UnityEngine;

/// <summary>
/// 戦闘の手触りを決める数値をまとめた設定。プレイ中にインスペクターで変えると、次の行動からすぐ反映される
/// （最大 HP・最大 AP だけは戦闘開始時に読む）
/// </summary>
[CreateAssetMenu(fileName = "BalanceConfig", menuName = "MagiCode/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("プレイヤー")]
    public int maxHealth = 10;
    [Tooltip("ターン開始時にここまで回復する")]
    public int maxAP = 50;

    [Header("近接攻撃 Attack()")]
    public int attackCost = 10;
    public int attackDamage = 2;

    [Header("魔法 Cast()")]
    public int lightningCost = 20;
    public int flameCost = 20;
    public int freezeCost = 20;
    public int spellDamage = 4;

    [Header("ループボーナス")]
    [Tooltip("for / while の中に書いた行動（Attack・Cast）にかかる AP の倍率（0.5 = 半分）")]
    [Range(0f, 1f)] public float loopBonusCostRate = 0.5f;

    [Header("ヒットストップ（60fps でのフレーム数）")]
    [Tooltip("ダメージ1あたり止めるフレーム数（Attack 2ダメージなら 2×この値）")]
    public int hitStopFramesPerDamage = 2;
    [Tooltip("通常の攻撃で止める最大フレーム数")]
    public int maxHitStopFrames = 10;
    [Tooltip("倒しきる一撃で止めるフレーム数")]
    public int finishingHitStopFrames = 14;
    [Tooltip("止まっている間に、受けた側が左右に揺れる幅（ワールド単位）")]
    public float hitStopShake = 0.08f;

    public int LoopCost(int cost)
    {
        return Mathf.RoundToInt(cost * loopBonusCostRate);
    }

    public int SpellCost(string spell)
    {
        return spell switch { "ライトニング" => lightningCost, "フレイム" => flameCost, _ => freezeCost };
    }
}
