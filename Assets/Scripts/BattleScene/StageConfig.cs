using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の配置情報
/// </summary>
[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("敵のデータ（EnemyData ScriptableObject）")]
    public EnemyData enemyData;
    [Tooltip("敵のPrefab")]
    public GameObject enemyPrefab;
    [Tooltip("初期配置エリア")]
    public int startArea;
    [Tooltip("最大HPの上書き（0 = EnemyData のデフォルト値を使用）")]
    public int overrideMaxHealth = 0;
    [Tooltip("ソートボスメカニクスでの役割（None = EnemyData の設定を使用）")]
    public EnemySortRole sortRole = EnemySortRole.None;
    [Tooltip("CoreTotemの割り当て番号の上書き（0 = EnemyData のデフォルト値を使用）")]
    public int overrideAssignedNumber = 0;

    [Header("会話演出")]
    [Tooltip("バトル前会話で表示するスプライト（未設定の場合は EnemyData のスプライトを使用）")]
    public Sprite dialogueSprite;
    [Tooltip("会話演出でスプライトを左右反転させるか")]
    public bool flipDialogueSprite;
}

/// <summary>
/// ステージのエリア設定を一元管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewStageConfig", menuName = "MagiCode/Stage Config")]
public class StageConfig : ScriptableObject
{
    [Header("ステージ情報")]
    [Tooltip("ステージ名")]
    public string stageName;
    [Tooltip("ステージの説明")]
    [TextArea(2, 4)]
    public string stageDescription;

    [Header("エリア設定")]
    [Tooltip("プレイヤー側のエリア位置（左から右へ）")]
    public List<Vector2> playerAreaPositions = new List<Vector2>();

    [Tooltip("敵側のエリア位置（左から右へ）")]
    public List<Vector2> enemyAreaPositions = new List<Vector2>();

    [Header("プレイヤー初期配置")]
    [Tooltip("プレイヤーの初期エリア（0始まり）")]
    public int playerStartArea = 0;

    [Header("敵の構成（動的生成用）")]
    [Tooltip("このステージに出現する敵のリスト")]
    public List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

    [Header("Legacy: 既存シーン配置用")]
    [Tooltip("敵の初期エリア（0始まり）- 単体敵用またはデフォルト値")]
    public int enemyStartArea = 2;

    [Tooltip("複数敵の初期エリア（インデックス順に適用、未設定の敵はenemyStartAreaを使用）")]
    public List<int> enemyStartAreas = new List<int>();

    [Header("BGM")]
    [Tooltip("このステージで流す BGM（未設定の場合は現在再生中の BGM を継続）")]
    public AudioClip bgmClip;

    [Header("バトル前演出")]
    [Tooltip("バトル開始前に表示する会話リスト（空の場合はスキップ）")]
    public List<DialogueEntry> introDialogue = new List<DialogueEntry>();
    [Tooltip("会話演出で表示する敵のスプライト画像")]
    public Sprite enemyDialogueSprite;

    [Header("コード制限")]
    [Tooltip("このステージで使用を禁止するキーワードリスト（例: for, while, import）")]
    public List<string> restrictedKeywords = new List<string>();

    [Tooltip("このリストが空でない場合、記載のキーワードのみ使用可能（他の制御構文は自動的に禁止）")]
    public List<string> allowedKeywords = new List<string>();

    [Header("クリア報酬")]
    [Tooltip("このステージをクリアすることで習得できるコード・キーワード（メニュー画面で表示される）")]
    public List<string> obtainedKeywords = new List<string>();

    [Header("敗北アドバイス")]
    [Tooltip("敗北後にメニュー画面で表示するアドバイス文（空白の場合はウィンドウを表示しない）")]
    [TextArea(2, 6)]
    public string defeatAdvice;

    [Header("チュートリアルヒント")]
    [Tooltip("フェーズ開始時にアシスタントが発言するヒントのリスト")]
    public List<TutorialHint> tutorialHints = new List<TutorialHint>();

    [Header("コードエディタ初期設定")]
    [Tooltip("コードエディタ上部に固定表示するコード（プレイヤーが削除不可）。空白の場合は適用なし。")]
    [TextArea(2, 6)]
    public string lockedCode = "";

    [Header("ソートボスメカニクス")]
    [Tooltip("バブルソートによるガード並び替えメカニクスを有効にする")]
    public bool useSortBossMechanic = false;

    [Header("マネージャー参照")]
    [Tooltip("EnemyManager（未設定時は自動検索）")]
    public EnemyManager enemyManager;

    // エリア数
    public int PlayerAreaCount => playerAreaPositions.Count;
    public int EnemyAreaCount => enemyAreaPositions.Count;

    /// <summary>
    /// 指定インデックスの敵の初期エリアを取得
    /// 優先順位: enemySpawns > enemyStartAreas > enemyStartArea
    /// </summary>
    /// <param name="enemyIndex">敵のインデックス（EnemyManager内での順番）</param>
    /// <returns>初期エリア番号</returns>
    public int GetEnemyStartArea(int enemyIndex)
    {
        // 1. enemySpawns に設定があればそれを使用（動的生成用）
        if (enemySpawns != null && enemyIndex < enemySpawns.Count)
        {
            return Mathf.Clamp(enemySpawns[enemyIndex].startArea, 0, enemyAreaPositions.Count - 1);
        }

        // 2. enemyStartAreas に設定があればそれを使用（既存シーン配置用）
        if (enemyStartAreas != null && enemyIndex < enemyStartAreas.Count)
        {
            return Mathf.Clamp(enemyStartAreas[enemyIndex], 0, enemyAreaPositions.Count - 1);
        }

        // 3. なければデフォルトの enemyStartArea を使用
        return Mathf.Clamp(enemyStartArea, 0, enemyAreaPositions.Count - 1);
    }

    /// <summary>
    /// 設定の検証
    /// </summary>
    public bool Validate()
    {
        if (playerAreaPositions.Count == 0)
        {
            Debug.LogError("[StageConfig] プレイヤーエリアが設定されていません");
            return false;
        }

        if (enemyAreaPositions.Count == 0)
        {
            Debug.LogError("[StageConfig] 敵エリアが設定されていません");
            return false;
        }

        if (playerStartArea < 0 || playerStartArea >= playerAreaPositions.Count)
        {
            Debug.LogError($"[StageConfig] プレイヤー初期エリアが範囲外です: {playerStartArea}");
            return false;
        }

        if (enemyStartArea < 0 || enemyStartArea >= enemyAreaPositions.Count)
        {
            Debug.LogError($"[StageConfig] 敵初期エリアが範囲外です: {enemyStartArea}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// デフォルト設定を生成（エディタ用）
    /// </summary>
    public void GenerateDefaultPositions(int areaCount, float startX, float spacing, float playerY, float enemyY)
    {
        playerAreaPositions.Clear();
        enemyAreaPositions.Clear();

        for (int i = 0; i < areaCount; i++)
        {
            float x = startX + (i * spacing);
            playerAreaPositions.Add(new Vector2(x, playerY));
            enemyAreaPositions.Add(new Vector2(x, enemyY));
        }

        playerStartArea = 0;
        enemyStartArea = areaCount - 1;
    }

    /// <summary>
    /// 敵の総数を取得（enemySpawnsが設定されている場合）
    /// </summary>
    public int EnemyCount => enemySpawns != null ? enemySpawns.Count : 0;

    /// <summary>
    /// 動的生成を使用するかどうか
    /// </summary>
    public bool UseDynamicSpawn => enemySpawns != null && enemySpawns.Count > 0;
}
