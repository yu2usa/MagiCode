using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 複数の敵を管理し、ターン実行を調整する
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("Enemies")]
    [Tooltip("空の場合、シーン内のEnemyControllerを自動検出")]
    public List<EnemyController> enemies = new List<EnemyController>();

    [Header("Execution Mode")]
    [Tooltip("true=同時実行, false=順次実行")]
    public bool simultaneousExecution = false;

    [Header("Settings")]
    [SerializeField] private float sequentialDelay = 0.2f;
    [Tooltip("同じエリアにいる敵同士のX座標オフセット")]
    [SerializeField] private float sameAreaOffsetX = 1.0f;

    void Awake()
    {
        // 動的生成モードの場合は自動検出しない（TurnControllerが管理する）
        // シーン配置の敵がいる場合のみ自動検出
    }

    void Start()
    {
        // 敵リストが空の場合のみ自動検出（TurnControllerで追加されていない場合）
        if (enemies.Count == 0)
        {
            Debug.Log("[EnemyManager] 敵リストが空のため、自動検出を実行");
            AutoDetectAndMergeEnemies();
        }
        else
        {
            Debug.Log($"[EnemyManager] 既に{enemies.Count}体の敵が登録済み");
        }

        // 1フレーム後に初期奥行き表現を適用（全EnemyControllerのStart()完了後）
        StartCoroutine(RefreshVisualsNextFrame());
    }

    private IEnumerator RefreshVisualsNextFrame()
    {
        yield return null;
        var areas = new HashSet<int>(AliveEnemies.ConvertAll(e => e.enemyCurrentPos));
        foreach (int area in areas)
            RefreshAreaVisuals(area);
    }

    /// <summary>
    /// 敵リストの状態をログ出力
    /// </summary>
    public void LogEnemyStatus()
    {
        Debug.Log($"[EnemyManager] 敵リスト状態: 合計{enemies.Count}体");
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Debug.Log($"  - {enemy.gameObject.name}: HP={enemy.enemyHealth}, Pos={enemy.enemyCurrentPos}, Active={enemy.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.Log($"  - (null)");
            }
        }
    }

    /// <summary>
    /// シーン内の EnemyController を自動検出し、既存リストと統合
    /// 手動設定された敵 + 自動検出された敵 の両方を含める
    /// </summary>
    public void AutoDetectAndMergeEnemies()
    {
        var foundEnemies = FindObjectsOfType<EnemyController>();
        int addedCount = 0;

        foreach (var enemy in foundEnemies)
        {
            // 既にリストに含まれていなければ追加
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                addedCount++;
            }
        }

        Debug.Log($"[EnemyManager] 敵リスト統合完了: 合計{enemies.Count}体 (新規追加: {addedCount}体)");
        foreach (var enemy in enemies)
        {
            Debug.Log($"  - {enemy.gameObject.name}: HP={enemy.enemyHealth}, Active={enemy.gameObject.activeInHierarchy}");
        }

        if (enemies.Count == 0)
        {
            Debug.LogWarning("[EnemyManager] シーン内に敵が見つかりません");
        }
    }

    // 生存中の敵リスト
    public List<EnemyController> AliveEnemies =>
        enemies.FindAll(e => e != null && e.gameObject.activeInHierarchy && e.enemyHealth > 0);

    // 全敵死亡判定
    public bool AllEnemiesDead => AliveEnemies.Count == 0;

    /// <summary>
    /// 指定エリアにいる敵を取得
    /// </summary>
    public List<EnemyController> GetEnemiesInArea(int areaNum)
    {
        var result = AliveEnemies.FindAll(e => e.enemyCurrentPos == areaNum);
        Debug.Log($"[EnemyManager] GetEnemiesInArea({areaNum}): {result.Count}体 (全敵位置: {string.Join(", ", AliveEnemies.ConvertAll(e => $"{e.gameObject.name}@{e.enemyCurrentPos}"))})");
        return result;
    }

    /// <summary>
    /// 指定エリアでの敵のX座標オフセットを計算
    /// 同じエリアに複数の敵がいる場合、重ならないようにずらす
    /// </summary>
    /// <param name="enemy">オフセットを計算する敵</param>
    /// <param name="areaNum">エリア番号</param>
    /// <returns>X座標のオフセット値</returns>
    public float GetAreaPositionOffset(EnemyController enemy, int areaNum)
    {
        // 同じエリアにいる（または移動予定の）敵を取得
        var enemiesInArea = enemies.FindAll(e =>
            e != null &&
            e.gameObject.activeInHierarchy &&
            e.enemyHealth > 0 &&
            e.enemyCurrentPos == areaNum);

        int count = enemiesInArea.Count;
        if (count <= 1) return 0f;

        // この敵のインデックスを取得
        int index = enemiesInArea.IndexOf(enemy);
        if (index < 0) index = count; // 新しく移動してくる敵の場合

        // 中央を基準にオフセットを計算
        // 例: 2体の場合 → -0.5, +0.5
        // 例: 3体の場合 → -1, 0, +1
        float totalWidth = (count - 1) * sameAreaOffsetX;
        float startOffset = -totalWidth / 2f;
        return startOffset + index * sameAreaOffsetX;
    }

    /// <summary>
    /// 指定エリアに移動する際のX座標オフセットを計算
    /// （移動先に既にいる敵を考慮）
    /// </summary>
    public float GetMoveTargetOffset(EnemyController movingEnemy, int targetArea)
    {
        // 移動先エリアにいる敵（自分を除く）
        var enemiesInTargetArea = enemies.FindAll(e =>
            e != null &&
            e != movingEnemy &&
            e.gameObject.activeInHierarchy &&
            e.enemyHealth > 0 &&
            e.enemyCurrentPos == targetArea);

        int count = enemiesInTargetArea.Count + 1; // 自分を含める
        if (count <= 1) return 0f;

        // 自分は最後に追加される想定
        int index = count - 1;

        float totalWidth = (count - 1) * sameAreaOffsetX;
        float startOffset = -totalWidth / 2f;
        return startOffset + index * sameAreaOffsetX;
    }

    /// <summary>
    /// 全敵の攻撃フェーズを実行
    /// </summary>
    public IEnumerator ExecuteAllAttacks()
    {
        var aliveEnemies = AliveEnemies;
        Debug.Log($"[EnemyManager] ExecuteAllAttacks: 登録敵={enemies.Count}, 生存敵={aliveEnemies.Count}");
        LogEnemyList("攻撃フェーズ対象", aliveEnemies);

        if (aliveEnemies.Count == 0)
            yield break;

        if (simultaneousExecution)
        {
            yield return ExecuteSimultaneous(aliveEnemies, isAttack: true);
        }
        else
        {
            yield return ExecuteSequential(aliveEnemies, isAttack: true);
        }
    }

    /// <summary>
    /// 全敵の防御フェーズを実行
    /// </summary>
    public IEnumerator ExecuteAllDefenses()
    {
        var aliveEnemies = AliveEnemies;
        Debug.Log($"[EnemyManager] ExecuteAllDefenses: 登録敵={enemies.Count}, 生存敵={aliveEnemies.Count}");
        LogEnemyList("防御フェーズ対象", aliveEnemies);

        if (aliveEnemies.Count == 0)
            yield break;

        if (simultaneousExecution)
        {
            yield return ExecuteSimultaneous(aliveEnemies, isAttack: false);
        }
        else
        {
            yield return ExecuteSequential(aliveEnemies, isAttack: false);
        }
    }

    /// <summary>
    /// デバッグ用: 敵リストをログ出力
    /// </summary>
    private void LogEnemyList(string label, List<EnemyController> list)
    {
        foreach (var e in list)
        {
            Debug.Log($"  [{label}] {e.gameObject.name}: HP={e.enemyHealth}, Pos={e.enemyCurrentPos}, Active={e.gameObject.activeInHierarchy}");
        }
    }

    /// <summary>
    /// 順次実行: 1体ずつアクションを実行
    /// </summary>
    private IEnumerator ExecuteSequential(List<EnemyController> aliveEnemies, bool isAttack)
    {
        string phase = isAttack ? "攻撃" : "防御";
        Debug.Log($"[EnemyManager] ExecuteSequential開始: {phase}フェーズ, 対象={aliveEnemies.Count}体");

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            Debug.Log($"[EnemyManager] [{i + 1}/{aliveEnemies.Count}] {enemy.gameObject.name} の{phase}を開始");

            if (isAttack)
                enemy.AttackRoutine();
            else
                enemy.DefenseRoutine();

            yield return new WaitUntil(() => enemy.IsActionComplete);
            Debug.Log($"[EnemyManager] [{i + 1}/{aliveEnemies.Count}] {enemy.gameObject.name} の{phase}が完了");

            enemy.ResetActionComplete();
            yield return new WaitForSeconds(sequentialDelay);
        }

        Debug.Log($"[EnemyManager] ExecuteSequential完了: {phase}フェーズ");
    }

    /// <summary>
    /// 同時実行: 全敵のアクションを開始し、全完了を待つ
    /// </summary>
    private IEnumerator ExecuteSimultaneous(List<EnemyController> aliveEnemies, bool isAttack)
    {
        // 全敵のルーチンを開始
        foreach (var enemy in aliveEnemies)
        {
            if (isAttack)
                enemy.AttackRoutine();
            else
                enemy.DefenseRoutine();
        }

        // 全敵の完了を待つ
        yield return new WaitUntil(() => aliveEnemies.All(e => e.IsActionComplete));

        // フラグリセット
        foreach (var enemy in aliveEnemies)
        {
            enemy.ResetActionComplete();
        }
    }

    /// <summary>
    /// 指定エリアにいる生存中の敵リスト内でのインデックスを返す
    /// </summary>
    public int GetEnemyIndexInArea(EnemyController enemy, int areaNum)
    {
        var enemiesInArea = AliveEnemies.FindAll(e => e.enemyCurrentPos == areaNum);
        return enemiesInArea.IndexOf(enemy);
    }

    /// <summary>
    /// 指定エリアにいる生存中の全敵の奥行き表現（色・X位置）を更新
    /// インデックス0が最前面（通常色）、大きいほど暗く後方に配置
    /// </summary>
    public void RefreshAreaVisuals(int areaNum)
    {
        var enemiesInArea = AliveEnemies.FindAll(e => e.enemyCurrentPos == areaNum);
        for (int i = 0; i < enemiesInArea.Count; i++)
        {
            // index 0 = 通常色(1.0)、1 = 少し暗い(0.8)、2以降はさらに暗く(最低0.5)
            float brightness = Mathf.Max(0.5f, 1f - i * 0.2f);
            enemiesInArea[i].SetDepthColor(new Color(brightness, brightness, brightness, 1f));
            enemiesInArea[i].RefreshAreaPosition();
        }
    }

    /// <summary>
    /// 属性フェーズが有効な全敵の属性をランダム変化させ、全演出が完了するまで待機する。
    /// PlayerController の攻撃実行前に yield return で呼ぶ。
    /// </summary>
    public IEnumerator TriggerAllElementChanges()
    {
        var targets = AliveEnemies.FindAll(e => e.HasElementPhases);
        if (targets.Count == 0) yield break;

        // 全敵を並列で変化させ、全員の演出完了を待つ
        int remaining = targets.Count;
        foreach (var enemy in targets)
            StartCoroutine(WaitElementChange(enemy, () => remaining--));

        yield return new WaitUntil(() => remaining <= 0);
    }

    private IEnumerator WaitElementChange(EnemyController enemy, System.Action onComplete)
    {
        yield return StartCoroutine(enemy.RandomizeElement());
        onComplete();
    }

    /// <summary>
    /// regenHealthOnTurnEnd が有効な全敵のHP全回復を並列実行し、完了を待つ。
    /// TurnController の PlayerAttack() 完了後に yield return で呼ぶ。
    /// </summary>
    public IEnumerator ExecuteAllHealthRegens()
    {
        var targets = AliveEnemies.FindAll(e => e.enemyData != null && e.enemyData.regenHealthOnTurnEnd);
        if (targets.Count == 0) yield break;

        int remaining = targets.Count;
        foreach (var enemy in targets)
            StartCoroutine(WaitRegen(enemy, () => remaining--));

        yield return new WaitUntil(() => remaining <= 0);
    }

    private IEnumerator WaitRegen(EnemyController enemy, System.Action onComplete)
    {
        yield return StartCoroutine(enemy.RegenHealthOnTurnEnd());
        onComplete();
    }

    /// <summary>
    /// 実行モード切り替え
    /// </summary>
    public void ToggleExecutionMode()
    {
        simultaneousExecution = !simultaneousExecution;
        Debug.Log($"[EnemyManager] 実行モード変更: {(simultaneousExecution ? "同時実行" : "順次実行")}");
    }
}
