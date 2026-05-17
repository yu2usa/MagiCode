using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// バブルソートボスステージの管理クラス（シングルトン）
/// ガードの並び替えと、ソート進行度に応じたボスの被ダメージ倍率を制御する
/// </summary>
public class SortBossManager : MonoBehaviour
{
    public static SortBossManager Instance { get; private set; }

    [Header("手動設定（StageConfig未使用時のフォールバック）")]
    public List<EnemyController> guards = new List<EnemyController>();
    public EnemyController boss;

    [Header("スワップアニメーション")]
    [SerializeField] private float swapDuration = 0.5f;
    [SerializeField] private Ease swapEase = Ease.OutQuad;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // StageConfig の useSortBossMechanic が有効なら EnemyData の sortRole から自動設定
        var tc = FindObjectOfType<TurnController>();
        if (tc != null && tc.stageConfig != null && tc.stageConfig.useSortBossMechanic)
        {
            AutoConfigureFromEnemyData();
        }

        // 初期状態のソート進行度でボスの倍率を設定
        RefreshBossDefense();
    }

    /// <summary>
    /// EnemyManager 内の全敵を走査し、各敵の役割を決定して guards / boss を自動設定する
    /// 優先順位: StageConfig の EnemySpawnData.sortRole → EnemyData.sortRole
    /// </summary>
    private void AutoConfigureFromEnemyData()
    {
        var em = FindObjectOfType<EnemyManager>();
        if (em == null)
        {
            Debug.LogWarning("[SortBossManager] EnemyManager が見つかりません。手動設定を使用します。");
            return;
        }

        var tc = FindObjectOfType<TurnController>();
        StageConfig config = tc?.stageConfig;
        bool hasSpawnConfig = config != null
                              && config.enemySpawns != null
                              && config.enemySpawns.Count > 0;

        guards.Clear();
        boss = null;

        foreach (var enemy in em.enemies)
        {
            if (enemy == null) continue;

            // EnemySpawnData.sortRole を優先し、None なら EnemyData.sortRole を使用
            EnemySortRole role = EnemySortRole.None;
            if (hasSpawnConfig && enemy.enemyIndex < config.enemySpawns.Count)
                role = config.enemySpawns[enemy.enemyIndex].sortRole;
            if (role == EnemySortRole.None && enemy.enemyData != null)
                role = enemy.enemyData.sortRole;

            if (role == EnemySortRole.Guard)
                guards.Add(enemy);
            else if (role == EnemySortRole.Boss)
                boss = enemy;
        }

        Debug.Log($"[SortBossManager] 自動設定完了: ガード {guards.Count} 体, ボス = {boss?.name ?? "未設定"}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// エリア番号の昇順にガードの MaxHP を並べたリストを返す
    /// LogsManager が "EnemiesHP = [...]" としてPythonコードに挿入する
    /// </summary>
    public List<int> GetGuardsHpInAreaOrder()
    {
        return guards
            .Where(g => g != null && g.enemyHealth > 0)
            .OrderBy(g => g.enemyCurrentPos)
            .Select(g => g.MaxHealth)
            .ToList();
    }

    /// <summary>
    /// エリア j のガードとエリア k のガードを入れ替える
    /// DOTween でスムーズに移動し、完了後にエリア番号とボスの防御倍率を更新する
    /// </summary>
    public IEnumerator SwapGuards(int j, int k)
    {
        var guardJ = guards.Find(g => g != null && g.enemyCurrentPos == j && g.enemyHealth > 0);
        var guardK = guards.Find(g => g != null && g.enemyCurrentPos == k && g.enemyHealth > 0);

        if (guardJ == null || guardK == null)
        {
            Debug.LogWarning($"[SortBossManager] Swap({j}, {k}): 指定エリアにガードが存在しません");
            yield break;
        }

        var areas = guardJ.EnemyAreas;
        Vector3 targetForJ = new Vector3(areas[k].x, areas[k].y, guardJ.transform.position.z);
        Vector3 targetForK = new Vector3(areas[j].x, areas[j].y, guardK.transform.position.z);

        // 既存のTweenをキャンセルしてスワップ移動を開始
        DOTween.Kill(guardJ.transform);
        DOTween.Kill(guardK.transform);
        guardJ.transform.DOMove(targetForJ, swapDuration).SetEase(swapEase);
        yield return guardK.transform.DOMove(targetForK, swapDuration).SetEase(swapEase).WaitForCompletion();

        // エリア番号を更新
        guardJ.enemyCurrentPos = k;
        guardK.enemyCurrentPos = j;

        // ボスの被ダメージ倍率を再計算
        RefreshBossDefense();
    }

    /// <summary>
    /// ソートの進行度（0.0〜1.0）を計算する
    /// エリア順に並べたガードの隣接ペアのうち MaxHP が昇順になっている割合
    /// </summary>
    private float CalculateSortedness()
    {
        var ordered = guards
            .Where(g => g != null && g.enemyHealth > 0)
            .OrderBy(g => g.enemyCurrentPos)
            .ToList();

        if (ordered.Count < 2)
            return 1f;

        int correctPairs = 0;
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].MaxHealth <= ordered[i + 1].MaxHealth)
                correctPairs++;
        }

        return (float)correctPairs / (ordered.Count - 1);
    }

    /// <summary>
    /// ソート進行度をボスの incomingDamageMultiplier に反映する
    /// 完全ソート時 1.0（通常ダメージ）、未ソート時 0.0（無効）
    /// </summary>
    public void RefreshBossDefense()
    {
        if (boss == null)
            return;

        float sortedness = CalculateSortedness();
        boss.incomingDamageMultiplier = sortedness;
        Debug.Log($"[SortBossManager] ボス被ダメージ倍率: {sortedness:F2} (正しいペア数 / 総ペア数)");
    }
}
