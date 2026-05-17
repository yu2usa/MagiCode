using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージの初期化と敵の動的生成を管理
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("Stage Config")]
    [Tooltip("現在のステージ設定")]
    public StageConfig currentStage;

    [Header("References")]
    [Tooltip("EnemyManager（未設定時は自動検索/作成）")]
    public EnemyManager enemyManager;
    [Tooltip("TurnController（未設定時は自動検索）")]
    public TurnController turnController;
    [Tooltip("PlayerController（未設定時は自動検索）")]
    public PlayerController playerController;

    [Header("Spawn Settings")]
    [Tooltip("敵の親オブジェクト（未設定時はこのオブジェクト）")]
    public Transform enemyParent;

    // 生成された敵のリスト
    private List<GameObject> _spawnedEnemies = new List<GameObject>();

    void Awake()
    {
        // 参照の自動検索
        if (enemyManager == null)
            enemyManager = FindObjectOfType<EnemyManager>();
        if (turnController == null)
            turnController = FindObjectOfType<TurnController>();
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (enemyParent == null)
            enemyParent = transform;
    }

    void Start()
    {
        // メニューシーンで選択されたステージがあればそちらを優先
        if (StageLoader.pendingStage != null)
        {
            currentStage = StageLoader.pendingStage;
            StageLoader.pendingStage = null;
        }

        if (currentStage != null)
            InitializeStage(currentStage);
    }

    /// <summary>
    /// ステージを初期化
    /// </summary>
    public void InitializeStage(StageConfig stage)
    {
        currentStage = stage;
        Debug.Log($"[StageManager] ステージ初期化: {stage.stageName}");

        // 設定の検証
        if (!stage.Validate())
        {
            Debug.LogError("[StageManager] ステージ設定が無効です");
            return;
        }

        // 既存の敵をクリア
        ClearSpawnedEnemies();

        // EnemyManagerの準備
        EnsureEnemyManager();

        // StageConfigをコンポーネントに伝播
        if (turnController != null)
            turnController.stageConfig = stage;
        if (playerController != null)
            playerController.stageConfig = stage;

        // 敵を生成（enemySpawnsが設定されている場合）
        if (stage.enemySpawns != null && stage.enemySpawns.Count > 0)
        {
            SpawnEnemies(stage);
        }

        Debug.Log($"[StageManager] ステージ初期化完了: 敵数={enemyManager.enemies.Count}");
    }

    /// <summary>
    /// EnemyManagerの存在を保証
    /// </summary>
    private void EnsureEnemyManager()
    {
        if (enemyManager != null) return;

        // StageConfigから取得
        if (currentStage != null && currentStage.enemyManager != null)
        {
            enemyManager = currentStage.enemyManager;
            return;
        }

        // シーン内を検索
        enemyManager = FindObjectOfType<EnemyManager>();

        // なければ作成
        if (enemyManager == null)
        {
            var obj = new GameObject("EnemyManager");
            enemyManager = obj.AddComponent<EnemyManager>();
            Debug.Log("[StageManager] EnemyManagerを自動作成");
        }
    }

    /// <summary>
    /// StageConfigの敵構成から敵を生成
    /// </summary>
    private void SpawnEnemies(StageConfig stage)
    {
        for (int i = 0; i < stage.enemySpawns.Count; i++)
        {
            var spawnData = stage.enemySpawns[i];
            if (spawnData.enemyPrefab == null)
            {
                Debug.LogWarning($"[StageManager] 敵Prefabが未設定: index={i}");
                continue;
            }

            // 敵を生成
            GameObject enemy = Instantiate(spawnData.enemyPrefab, enemyParent);
            enemy.name = $"Enemy_{i}_{spawnData.enemyData?.enemyName ?? "Unknown"}";

            // EnemyControllerの設定
            var controller = enemy.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.stageConfig = stage;
                controller.enemyIndex = i;

                // EnemyDataを設定（Prefabに設定されていない場合）
                if (controller.enemyData == null && spawnData.enemyData != null)
                {
                    controller.enemyData = spawnData.enemyData;
                }
            }

            // EnemyManagerに登録
            enemyManager.enemies.Add(controller);
            _spawnedEnemies.Add(enemy);

            Debug.Log($"[StageManager] 敵生成: {enemy.name} at area {spawnData.startArea}");
        }
    }

    /// <summary>
    /// 生成した敵をクリア
    /// </summary>
    public void ClearSpawnedEnemies()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        _spawnedEnemies.Clear();

        if (enemyManager != null)
            enemyManager.enemies.Clear();
    }

    /// <summary>
    /// 次のステージへ進む
    /// </summary>
    public void LoadNextStage(StageConfig nextStage)
    {
        StartCoroutine(TransitionToStage(nextStage));
    }

    /// <summary>
    /// ステージ遷移コルーチン
    /// </summary>
    private IEnumerator TransitionToStage(StageConfig nextStage)
    {
        Debug.Log($"[StageManager] ステージ遷移開始: {currentStage?.stageName} → {nextStage.stageName}");

        // TODO: フェードアウト演出を追加可能

        yield return new WaitForSeconds(0.5f);

        // 新ステージ初期化
        InitializeStage(nextStage);

        // TODO: フェードイン演出を追加可能

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[StageManager] ステージ遷移完了");
    }
}
