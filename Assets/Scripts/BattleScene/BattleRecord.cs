/// <summary>
/// 1回の戦闘の戦績。勝利画面に表示する
/// </summary>
public class BattleRecord
{
    public int turns;        // プレイヤーの攻撃ターンを迎えた回数
    public int cleanRuns;    // エラーなくコードを実行できた回数
    public int actions;      // 実行した行動（攻撃・魔法・移動など）の数
    public int damageDealt;  // 敵に与えたダメージの合計
    public int damageTaken;  // 受けたダメージの合計
}
