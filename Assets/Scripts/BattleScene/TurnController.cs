using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public enum Phase { Attack, Defense }
public enum Actor { Player, Enemy }

public class TurnController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool enemyStartsFirst = true;  //  インスペクター先攻後攻切り替え
    [SerializeField] float phaseDelay = 0.6f;

    [Header("Runtime Info (ReadOnly)")]
    // デフォルトを Enemy+Defense に設定（Player+Attack がデフォルト値0と一致するため、
    // KeyGolemMechanic 等のポーリング検出が誤作動しないよう初期値を明示する）
    public Actor activeActor = Actor.Enemy;
    public Phase currentPhase = Phase.Defense;
    public int playerTurn = 0;
    public int enemyTurn = 0;

    // 敵の攻撃内容（予告表示用）
    private string nextEnemyAttackInfo = "";

    //通常インスペクター項木
    [Header("InspectorItems")]
    public int turnCount;
    public bool isPlayerTurn;
    public bool isGuardPhase;
    public GameObject guardPhaseUI;
    public GameObject attackPhaseUI;
    public GameObject guardIcon;
    public GameObject attackIcon;


    public GameObject turnStartUI;

    [Header("TurnChangeUI")]
    public GameObject playerTurnUI;
    public GameObject enemyTurnUI;
    [SerializeField] float turnChangeDisplayTime = 1.0f;

    [Header("PlayerSync")]
    public bool isCodeInputReady;
    public bool isPlayerActionComplete;


    [Header("Stage Config")]
    [Tooltip("ステージ設定（これだけ設定すればステージが構築される）")]
    public StageConfig stageConfig;
    [Tooltip("stageConfigが未設定のときに使用するデフォルトステージ（S_Config_1を設定しておく）")]
    [SerializeField] private StageConfig defaultStageConfig;

    [Header("Enemy Settings")]
    [Tooltip("EnemyManagerへの直接参照（未設定時は自動作成）")]
    public EnemyManager enemyManager;
    [Tooltip("敵の親オブジェクト（未設定時は自動作成）")]
    public Transform enemyParent;

    [Header("Player Reference")]
    [Tooltip("PlayerController（未設定時は自動検索）")]
    public PlayerController playerController;

    // 生成された敵のリスト
    private List<GameObject> _spawnedEnemies = new List<GameObject>();

    // 生成されたエリアラベルのリスト
    private List<GameObject> _areaLabels = new List<GameObject>();

    [Header("CodeInputWindow")]
    public GameObject codeWindow;
    [Tooltip("InGameCodeEditor の LineHighlight（敵ターン中に非表示にする）")]
    public GameObject editorLineHighlight;
    [Tooltip("コード入力欄（InputFieldにフォーカスしたときパーティクルを表示する）")]
    [SerializeField] TMP_InputField codeInputField;

    [Header("Coding Particle")]
    [Tooltip("コード入力中にプレイヤー位置に表示するパーティクル（PlayerCodeEffect）")]
    [SerializeField] GameObject codingParticle;

    [Header("LeftWindowUI")]
    public GameObject leftWindow;

    [Header("Telegraph UI")]
    [Tooltip("プレイヤー防御フェーズ中に敵の次の行動を表示するテキスト")]
    [SerializeField] TMP_Text telegraphText;

    [Header("Area Labels")]
    [Tooltip("エリア番号テキストのフォントサイズ（ワールドスペース単位）")]
    [SerializeField] private float areaLabelFontSize = 1f;
    [Tooltip("エリア番号テキストの色")]
    [SerializeField] private Color areaLabelColor = Color.white;
    [Tooltip("エリア番号テキストのフォント（未設定時はTMPデフォルト）")]
    [SerializeField] private TMP_FontAsset areaLabelFont;
    [Tooltip("スライドイン開始位置のオフセット（目標Y座標からどれだけ下から来るか）")]
    [SerializeField] private float areaLabelSlideDistance = 2f;
    [Tooltip("スライドインのアニメーション時間（秒）")]
    [SerializeField] private float areaLabelSlideDuration = 0.5f;
    [Tooltip("各エリアラベルの出現遅延（秒）。0だと同時に出てくる")]
    [SerializeField] private float areaLabelStaggerDelay = 0.08f;

    [Header("Window Slide In")]
    [Tooltip("ゲーム開始時のウィンドウスライドイン時間（秒）")]
    [SerializeField] private float windowSlideInDuration = 0.45f;
    [Tooltip("スライドインのイージング")]
    [SerializeField] private Ease windowSlideInEase = Ease.OutQuart;

    [Header("Enemy Attack Notification")]
    [Tooltip("敵行動予告の各項目プレハブ（EAN_Content）")]
    [SerializeField] private GameObject eanContentPrefab;
    [Tooltip("EAN_Contentを配置する親Transform（EnemyAttackNotificationWindow>Content）")]
    [SerializeField] private Transform eanContentParent;
    [Tooltip("攻撃エリアに警告マークを表示するシステム")]
    [SerializeField] private AttackWarningSystem attackWarningSystem;

    [Header("Tutorial Hint")]
    [Tooltip("フェーズ開始時にアドバイスを表示するウィンドウ（未設定時はスキップ）")]
    [SerializeField] private AIAAWindow tutorialWindow;

    [Header("Pre-Battle Dialogue")]
    [Tooltip("バトル前会話演出マネージャー（未設定時はスキップ）")]
    [SerializeField] private PreBattleDialogueManager preBattleDialogue;
    [Tooltip("演出中に非表示にするプレイヤーHPゲージ")]
    [SerializeField] private GameObject playerHealthGuage;
    [Tooltip("演出中に非表示にするプレイヤーMPゲージ")]
    [SerializeField] private GameObject playerMPGuage;

    [Header("Stage Opening")]
    [Tooltip("暗転フェードイン用の黒い全画面オーバーレイ（CanvasGroup付き）")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [Tooltip("フェードインにかかる時間（秒）")]
    [SerializeField] private float fadeInDuration = 2f;
    [Tooltip("戦闘開始演出UI（dialogue後・バトル開始前に表示）")]
    [SerializeField] private GameObject battleStartUI;
    [Tooltip("戦闘開始演出の表示時間（秒）")]
    [SerializeField] private float battleStartDisplayTime = 1.0f;

    [Header("Game Result UI")]
    [Tooltip("全敵撃破時に表示するVictory演出UI")]
    public GameObject victoryUI;
    [Tooltip("プレイヤー死亡時に表示するDefeat演出UI")]
    public GameObject defeatUI;

    [Header("BehaviourPattern")]
    public List<string> enemyAttackBehaviour;

    public List<string> enemyGuardBehaviour;


    [Header("EnemyParametors")]
    public int enemyHP;
    public Vector2[] areaPositions;

    // キャッシュされたコンポーネント
    private CanvasGroup _codeWindowCanvasGroup;
    private CanvasGroup _leftWindowCanvasGroup;
    private RectTransform _codeWindowRect;
    private RectTransform _leftWindowRect;
    private CanvasGroup _turnStartUICanvasGroup;
    private CanvasGroup _playerTurnUICanvasGroup;
    private CanvasGroup _enemyTurnUICanvasGroup;
    private List<GameObject> _eanInstances = new List<GameObject>();
    private List<string> _nextEnemyAttackMessages = new List<string>();
    // 敵が次フェーズで攻撃するエリア番号リスト（警告マーク表示用）
    private List<int> _nextEnemyAttackAreas = new List<int>();

    // 前回のアクター（ターン切り替え検出用）
    private Actor _previousActor = Actor.Enemy;

    // バトル終了フラグ（二重呼び出し防止）
    private bool _battleEnded = false;

    void Start()
    {
        // コンポーネントキャッシュ
        _codeWindowCanvasGroup = codeWindow.GetComponent<CanvasGroup>();
        _leftWindowCanvasGroup = leftWindow.GetComponent<CanvasGroup>();
        _codeWindowRect = codeWindow.GetComponent<RectTransform>();
        _leftWindowRect = leftWindow.GetComponent<RectTransform>();
        _turnStartUICanvasGroup = turnStartUI.GetComponent<CanvasGroup>();

        if (playerTurnUI != null)
            _playerTurnUICanvasGroup = playerTurnUI.GetComponent<CanvasGroup>();
        if (enemyTurnUI != null)
            _enemyTurnUICanvasGroup = enemyTurnUI.GetComponent<CanvasGroup>();

        // PlayerControllerの自動検索
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        // コード入力欄のフォーカスでパーティクルを制御
        if (codeInputField != null)
        {
            codeInputField.onSelect.AddListener(_ => ShowCodingParticle());
            codeInputField.onDeselect.AddListener(_ => HideCodingParticle());
        }

        // メニューシーンで選択されたステージがあればインスペクター設定を上書き
        if (StageLoader.pendingStage != null)
        {
            stageConfig = StageLoader.pendingStage;
            StageLoader.pendingStage = null;
        }

        // stageConfigが未設定のときはデフォルトにフォールバック
        if (stageConfig == null && defaultStageConfig != null)
        {
            stageConfig = defaultStageConfig;
            Debug.Log("[TurnController] stageConfig未設定のため defaultStageConfig を使用します");
        }

        // ステージ初期化
        InitializeStage();

        // Start() の最初のフレームからオーバーレイで黒く塗りつぶす
        // （この後に各コンポーネントのStart()でUIが生成されても見えない）
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 1f;
        }

        // 1フレーム待ってからバトル開始（描画の安定化）
        StartCoroutine(DelayedBattleStart());
    }

    IEnumerator DelayedBattleStart()
    {
        // 1フレーム待機：この間に全コンポーネントのStart()が完了しUIが生成される
        // オーバーレイはStart()内で既にalpha=1なので、生成されたUIは見えない
        yield return null;

        // 全Start()完了後に要素を非表示にし、オーバーレイを最前面へ
        // （新規生成UIがオーバーレイより上になっていても、ここで確実に隠す）
        HidePreBattleElements();

        // フェードイン
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            yield return fadeOverlay.DOFade(0f, fadeInDuration).SetEase(Ease.InQuad).WaitForCompletion();
            fadeOverlay.gameObject.SetActive(false);
        }

        // バトル前会話演出（introDialogueが設定されている場合のみ）
        if (preBattleDialogue != null
            && stageConfig != null
            && stageConfig.introDialogue != null
            && stageConfig.introDialogue.Count > 0)
        {
            // 最初の敵の名前・スプライトを取得して反映
            if (enemyManager.enemies.Count > 0)
            {
                var firstEnemy = enemyManager.enemies[0];
                string resolvedName = firstEnemy.enemyData?.enemyName;
                if (string.IsNullOrEmpty(resolvedName))
                    resolvedName = firstEnemy.enemyType;
                if (!string.IsNullOrEmpty(resolvedName))
                    preBattleDialogue.SetEnemyName(resolvedName);
            }

            // ステージに登場する全敵のスプライトをリストで渡す（DialogueEntry.enemyIndex で切り替え）
            // 優先順位: enemyDialogueSprite（ステージ単位の明示指定）> EnemySpawnData.dialogueSprite > EnemyData.enemySprite
            if (stageConfig.enemyDialogueSprite != null)
            {
                // StageConfig で明示指定されている場合はそれ1枚のみ使用
                preBattleDialogue.SetEnemySprite(stageConfig.enemyDialogueSprite);
            }
            else
            {
                var dialogueSprites = new List<Sprite>();
                var dialogueFlips   = new List<bool>();
                for (int i = 0; i < enemyManager.enemies.Count; i++)
                {
                    var enemy = enemyManager.enemies[i];
                    var spawn = (stageConfig.enemySpawns != null && i < stageConfig.enemySpawns.Count)
                        ? stageConfig.enemySpawns[i] : null;

                    if (spawn?.dialogueSprite != null)
                    {
                        dialogueSprites.Add(spawn.dialogueSprite);
                        dialogueFlips.Add(spawn.flipDialogueSprite);
                    }
                    else if (enemy.enemyData?.enemySprite != null)
                    {
                        dialogueSprites.Add(enemy.enemyData.enemySprite);
                        dialogueFlips.Add(enemy.enemyData.flipSpriteInDialogue);
                    }
                }
                if (dialogueSprites.Count > 0)
                    preBattleDialogue.SetEnemySprites(dialogueSprites, dialogueFlips);
            }

            // dialogue開始直前に改めてバーを非表示（フェード中に再表示された場合の保険）
            if (playerController != null) playerController.HideStatusUI();
            foreach (var enemy in enemyManager.enemies) enemy.HideHealthBar();

            // dialogue末尾に表示するキーワードをセット（obtainedKeywordsが設定されている場合のみ）
            if (stageConfig.obtainedKeywords != null && stageConfig.obtainedKeywords.Count > 0)
                preBattleDialogue.SetKeywords(stageConfig.obtainedKeywords);

            yield return StartCoroutine(preBattleDialogue.Play(stageConfig.introDialogue));

            // キーワード取得を永続保存（スキップ有無に関わらず入場した時点で記録）
            if (stageConfig.obtainedKeywords != null && stageConfig.obtainedKeywords.Count > 0)
                ClearDataManager.MarkKeywordsObtained(stageConfig.stageName);
        }

        // dialogue終了後（またはdialogue不使用時）にキャラクター・ゲージを再表示
        // codeWindow・leftWindow は BattleFlow() がアニメーション付きで表示するため戻さない
        ShowPreBattleElements();

        // 戦闘開始演出
        if (battleStartUI != null)
            yield return StartCoroutine(ShowBattleStartEffect());

        isPlayerActionComplete = false;
        StartCoroutine(BattleFlow());
    }

    /// <summary>
    /// フェードイン・dialogue演出中に隠すキャラクター・UIを一括非表示
    /// </summary>
    private void HidePreBattleElements()
    {
        if (playerController != null)
        {
            playerController.gameObject.SetActive(false);
            playerController.HideStatusUI();
        }
        foreach (var enemy in enemyManager.enemies)
        {
            enemy.gameObject.SetActive(false);
            enemy.HideHealthBar();
        }
        codeWindow.SetActive(false);
        leftWindow.SetActive(false);
        if (playerHealthGuage != null) playerHealthGuage.SetActive(false);
        if (playerMPGuage != null)     playerMPGuage.SetActive(false);
    }

    /// <summary>
    /// dialogue演出終了後にキャラクター・UIを一括再表示
    /// </summary>
    private void ShowPreBattleElements()
    {
        if (playerController != null)
        {
            playerController.gameObject.SetActive(true);
            playerController.ShowStatusUI();
        }
        foreach (var enemy in enemyManager.enemies)
        {
            enemy.gameObject.SetActive(true);
            enemy.ShowHealthBar();
        }
        if (playerHealthGuage != null) playerHealthGuage.SetActive(true);
        if (playerMPGuage != null)     playerMPGuage.SetActive(true);
        // codeWindow・leftWindow は BattleFlow() 側で管理
    }

    /// <summary>
    /// 戦闘開始演出：スケールアップ＋フェードイン → 一定時間表示 → フェードアウト
    /// </summary>
    IEnumerator ShowBattleStartEffect()
    {
        var cg = battleStartUI.GetComponent<CanvasGroup>();
        battleStartUI.SetActive(true);

        if (cg != null)
        {
            cg.alpha = 0f;
            battleStartUI.transform.localScale = Vector3.one * 0.7f;
            cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            yield return battleStartUI.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).WaitForCompletion();
            yield return new WaitForSeconds(battleStartDisplayTime);
            yield return cg.DOFade(0f, 0.2f).WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(battleStartDisplayTime);
        }

        battleStartUI.SetActive(false);
    }

    /// <summary>
    /// StageConfigからステージを初期化
    /// </summary>
    private void InitializeStage()
    {
        Debug.Log($"[TurnController] InitializeStage開始: stageConfig={stageConfig != null}");

        if (stageConfig == null)
        {
            Debug.LogWarning("[TurnController] StageConfigが設定されていません");
            EnsureEnemyManager();
            return;
        }

        Debug.Log($"[TurnController] ステージ初期化: {stageConfig.stageName}");
        Debug.Log($"[TurnController] enemySpawns数: {stageConfig.enemySpawns?.Count ?? 0}");

        // ステージ固有の BGM を再生
        if (stageConfig.bgmClip != null)
            AudioManager.Instance?.PlayBGM(stageConfig.bgmClip);

        // 設定の検証
        if (!stageConfig.Validate())
        {
            Debug.LogError("[TurnController] StageConfig の設定が無効です");
        }

        // EnemyManager の取得/作成
        EnsureEnemyManager();

        // PlayerControllerにStageConfigを設定
        if (playerController != null)
        {
            playerController.stageConfig = stageConfig;
        }

        // StageConfigからの敵生成（enemySpawnsが設定されている場合）
        if (stageConfig.enemySpawns != null && stageConfig.enemySpawns.Count > 0)
        {
            Debug.Log($"[TurnController] 敵の動的生成を開始");
            SpawnEnemiesFromConfig();
        }
        else
        {
            Debug.Log("[TurnController] enemySpawnsが空のため、シーン内の敵を検出します");
            // シーンに既に配置されている敵を検出
            enemyManager.AutoDetectAndMergeEnemies();
        }

        // 敵リストの状態を出力
        enemyManager.LogEnemyStatus();

        // コードエディタのロックコードを設定（stageConfig に lockedCode が設定されている場合のみ）
        if (!string.IsNullOrEmpty(stageConfig.lockedCode))
        {
            var logsManager = FindObjectOfType<LogsListManager>();
            logsManager?.SetLockedCode(stageConfig.lockedCode);
        }

        // SortBossManager の確保（useSortBossMechanic が有効な場合のみ自動生成）
        if (stageConfig.useSortBossMechanic)
            EnsureSortBossManager();

        Debug.Log($"[TurnController] ステージ初期化完了: 敵数={enemyManager?.enemies.Count ?? 0}");
        // エリアラベルはBattleFlow()でウィンドウ演出と同時に生成する
    }

    /// <summary>
    /// SortBossManager の存在を保証（シーンになければ自動生成）
    /// </summary>
    private void EnsureSortBossManager()
    {
        if (FindObjectOfType<SortBossManager>() != null) return;

        var managerObj = new GameObject("SortBossManager");
        managerObj.AddComponent<SortBossManager>();
        Debug.Log("[TurnController] SortBossManager を自動生成しました");
    }

    /// <summary>
    /// EnemyManagerの存在を保証
    /// </summary>
    private void EnsureEnemyManager()
    {
        if (enemyManager != null) return;

        // StageConfigから取得を試みる
        if (stageConfig != null && stageConfig.enemyManager != null)
        {
            enemyManager = stageConfig.enemyManager;
            Debug.Log("[TurnController] StageConfig から EnemyManager を取得");
            return;
        }

        // シーン内を検索
        enemyManager = FindObjectOfType<EnemyManager>();

        // シーンに EnemyManager がない場合は作成
        if (enemyManager == null)
        {
            var managerObj = new GameObject("EnemyManager");
            enemyManager = managerObj.AddComponent<EnemyManager>();
            Debug.Log("[TurnController] EnemyManager を自動作成しました");
        }
    }

    /// <summary>
    /// StageConfigから敵を生成
    /// </summary>
    private void SpawnEnemiesFromConfig()
    {
        // 敵の親オブジェクトを確保
        if (enemyParent == null)
        {
            var parentObj = new GameObject("Enemies");
            enemyParent = parentObj.transform;
        }

        Debug.Log($"[TurnController] SpawnEnemiesFromConfig: {stageConfig.enemySpawns.Count}体を生成予定");

        int spawnedCount = 0;
        for (int i = 0; i < stageConfig.enemySpawns.Count; i++)
        {
            var spawnData = stageConfig.enemySpawns[i];

            // デバッグ: spawnDataの内容を確認
            Debug.Log($"[TurnController] spawnData[{i}]: prefab={spawnData.enemyPrefab != null}, data={spawnData.enemyData != null}, area={spawnData.startArea}");

            if (spawnData.enemyPrefab == null)
            {
                Debug.LogError($"[TurnController] 敵Prefabが未設定: index={i}");
                continue;
            }

            // 敵を生成（初期位置を設定してから生成）
            // PrefabのZ座標を保持
            float prefabZ = spawnData.enemyPrefab.transform.position.z;
            Vector3 spawnPosition = new Vector3(0, 0, prefabZ);

            if (stageConfig.enemyAreaPositions != null && spawnData.startArea < stageConfig.enemyAreaPositions.Count)
            {
                Vector2 areaPos = stageConfig.enemyAreaPositions[spawnData.startArea];
                spawnPosition = new Vector3(areaPos.x, areaPos.y, prefabZ);
            }

            GameObject enemyObj = Instantiate(spawnData.enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
            string enemyName = spawnData.enemyData?.enemyName ?? "Unknown";
            enemyObj.name = $"Enemy_{i}_{enemyName}";

            // EnemyControllerの設定
            var controller = enemyObj.GetComponent<EnemyController>();
            if (controller == null)
            {
                Debug.LogError($"[TurnController] EnemyControllerが見つかりません: {enemyObj.name}");
                continue;
            }

            // StageConfigとインデックスを設定（Start()より先に設定）
            controller.stageConfig = stageConfig;
            controller.enemyIndex = i;

            // EnemyDataを設定（Prefabに設定されていない場合）
            if (controller.enemyData == null && spawnData.enemyData != null)
            {
                controller.enemyData = spawnData.enemyData;
            }

            // EnemyManagerに登録
            enemyManager.enemies.Add(controller);
            _spawnedEnemies.Add(enemyObj);
            spawnedCount++;

            Debug.Log($"[TurnController] 敵生成成功: {enemyObj.name}, area={spawnData.startArea}, HP={controller.enemyHealth}");
        }

        Debug.Log($"[TurnController] 敵生成完了: {spawnedCount}/{stageConfig.enemySpawns.Count}体");
    }

    /// <summary>
    /// 生成した敵をクリア（ステージ切り替え用）
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

        ClearAreaLabels();
    }

    /// <summary>
    /// 新しいステージをロード
    /// </summary>
    public void LoadStage(StageConfig newStage)
    {
        ClearSpawnedEnemies();
        stageConfig = newStage;
        InitializeStage();
    }

    /// <summary>
    /// 各エリアの中央下にエリア番号ラベルを生成する
    /// X = プレイヤーエリアX と 敵エリアX の中点、Y = -1.2 固定
    /// </summary>
    private void SpawnAreaLabels()
    {
        if (stageConfig == null) return;

        int areaCount = Mathf.Min(
            stageConfig.playerAreaPositions.Count,
            stageConfig.enemyAreaPositions.Count
        );

        for (int i = 0; i < areaCount; i++)
        {
            float centerX = (stageConfig.playerAreaPositions[i].x + stageConfig.enemyAreaPositions[i].x) / 2f;

            var labelObj = new GameObject($"AreaLabel_{i}");
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = i.ToString();
            tmp.fontSize = areaLabelFontSize;
            tmp.color = areaLabelColor;
            tmp.alignment = TextAlignmentOptions.Center;
            if (areaLabelFont != null)
                tmp.font = areaLabelFont;

            // スライドイン開始位置（目標Y座標より下）に配置
            float targetY = -0.1f;
            labelObj.transform.position = new Vector3(centerX, targetY - areaLabelSlideDistance, 0f);

            // エリアインデックスに応じた遅延で目標位置へスライドイン
            labelObj.transform.DOMoveY(targetY, areaLabelSlideDuration)
                .SetDelay(i * areaLabelStaggerDelay)
                .SetEase(Ease.OutBack);

            _areaLabels.Add(labelObj);
        }

        Debug.Log($"[TurnController] エリアラベル生成完了: {areaCount}個");
    }

    /// <summary>
    /// 生成したエリアラベルを破棄（ステージ切り替え用）
    /// </summary>
    private void ClearAreaLabels()
    {
        foreach (var label in _areaLabels)
        {
            if (label != null)
                Destroy(label);
        }
        _areaLabels.Clear();
    }

    IEnumerator BattleFlow()
    {
        isCodeInputReady = false;
        isPlayerActionComplete = false;

        // バトルシーン初期化（ターン開始前は入力無効）
        if (editorLineHighlight != null)
            editorLineHighlight.SetActive(false);
        _codeWindowCanvasGroup.interactable = false;
        codeWindow.SetActive(true);
        leftWindow.SetActive(true);
        _codeWindowCanvasGroup.alpha = 0.0f;
        _leftWindowCanvasGroup.alpha = 0.0f;

        // 1フレーム待ってCanvas Layout を確定させてから目標位置を記録
        yield return null;
        Vector2 codeWindowTarget = _codeWindowRect.anchoredPosition;
        Vector2 leftWindowTarget  = _leftWindowRect.anchoredPosition;

        // 画面外（右・左）にオフセットしてからスライドイン
        _codeWindowRect.anchoredPosition = new Vector2(codeWindowTarget.x + Screen.width, codeWindowTarget.y);
        _leftWindowRect.anchoredPosition  = new Vector2(leftWindowTarget.x  - Screen.width, leftWindowTarget.y);

        // エリアラベル生成（ウィンドウと同時にスライドイン開始）
        SpawnAreaLabels();

        _codeWindowCanvasGroup.DOFade(1.0f, windowSlideInDuration);
        _leftWindowCanvasGroup.DOFade(1.0f, windowSlideInDuration);
        _codeWindowRect.DOAnchorPos(codeWindowTarget, windowSlideInDuration).SetEase(windowSlideInEase);
        _leftWindowRect.DOAnchorPos(leftWindowTarget,  windowSlideInDuration).SetEase(windowSlideInEase);

        // ラベルのスタガー込み完了時間 = duration + (ラベル数-1) * stagger
        float labelsTotalTime = _areaLabels.Count > 0
            ? areaLabelSlideDuration + (_areaLabels.Count - 1) * areaLabelStaggerDelay
            : 0f;
        // ウィンドウとラベル、遅い方が終わるまで待ってからバトル開始
        yield return new WaitForSeconds(Mathf.Max(windowSlideInDuration, labelsTotalTime));




        Debug.Log("=== Battle Start ===");

        // 開始順の設定（最初のターン切り替え演出のため、開始アクターと逆を設定）
        if (enemyStartsFirst)
        {
            _previousActor = Actor.Player;  // 敵先攻なので、初期値はプレイヤー
            yield return StartCoroutine(EnemyFirstStart());
        }
        else
        {
            _previousActor = Actor.Enemy;   // プレイヤー先攻なので、初期値は敵
            yield return StartCoroutine(PlayerFirstStart());
        }
    }

    // ==============================================================
    //敵が先行, 敵防御→プレイヤー攻撃防御→敵攻防...
    // ==============================================================
    IEnumerator EnemyFirstStart()
    {
        // --- 敵 1ターン目：防御のみ ---
        enemyTurn = 1;
        yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Defense));

        // --- プレイヤー 1ターン目：攻撃→防御 ---
        playerTurn = 1;
        yield return StartCoroutine(RunPhase(Actor.Player, Phase.Attack));
        yield return StartCoroutine(RunPhase(Actor.Player, Phase.Defense));

        //以降ループ
        while (true)
        {
            // 敵ターン：攻撃→防御
            enemyTurn++;
            yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Attack));
            yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Defense));

            // プレイヤーターン：攻撃→防御
            playerTurn++;
            yield return StartCoroutine(RunPhase(Actor.Player, Phase.Attack));
            yield return StartCoroutine(RunPhase(Actor.Player, Phase.Defense));
        }
    }

    // ==============================================================
    //プレイヤーが先行, プレイヤー防御→敵攻防→プレイヤー攻防...
    // ==============================================================
    IEnumerator PlayerFirstStart()
    {
        // --- プレイヤー 1ターン目：防御のみ ---
        playerTurn = 1;
        yield return StartCoroutine(RunPhase(Actor.Player, Phase.Defense));

        // --- 敵 1ターン目：攻撃→防御 ---
        enemyTurn = 1;
        yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Attack));
        yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Defense));

        // --- 以降ループ ---
        while (true)
        {
            // プレイヤー：攻撃→防御
            playerTurn++;
            yield return StartCoroutine(RunPhase(Actor.Player, Phase.Attack));
            yield return StartCoroutine(RunPhase(Actor.Player, Phase.Defense));

            // 敵：攻撃→防御
            enemyTurn++;
            yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Attack));
            yield return StartCoroutine(RunPhase(Actor.Enemy, Phase.Defense));
        }
    }

    // ==============================================================
    // フェーズ実行
    // ==============================================================
    IEnumerator RunPhase(Actor actor, Phase phase)
    {
        // ターン切り替え演出（アクターが変わった時のみ）
        if (actor != _previousActor)
        {
            yield return StartCoroutine(ShowTurnChangeEffect(actor));
            _previousActor = actor;
        }

        activeActor = actor;
        currentPhase = phase;
        Debug.Log($"[TurnController] {actor} {phase} 開始");

        // フェーズ開始時はコード入力とエディタハイライトを無効化
        _codeWindowCanvasGroup.interactable = false;
        if (editorLineHighlight != null)
            editorLineHighlight.SetActive(false);

        // --- 実処理 ---
        if (actor == Actor.Enemy)
        {
            if (phase == Phase.Attack)
                yield return StartCoroutine(EnemyAttack());
            else
                yield return StartCoroutine(EnemyDefense());
        }
        else
        {
            if (phase == Phase.Attack)
                yield return StartCoroutine(PlayerAttack());
            else
                yield return StartCoroutine(PlayerDefense());
        }

        // フェーズ終了後に勝敗チェック
        if (!_battleEnded && IsBattleOver())
        {
            EndBattle(playerWon: IsPlayerVictory());
            yield break;
        }

        Debug.Log($"[TurnController] {actor} {phase} 終了");
        yield return new WaitForSeconds(phaseDelay);
    }

    /// <summary>
    /// 勝敗条件を判定する
    /// </summary>
    private bool IsBattleOver() => IsPlayerVictory() || IsPlayerDefeat();

    /// <summary>プレイヤー勝利判定（全敵死亡 または CoreBoss撃破）</summary>
    private bool IsPlayerVictory()
    {
        if (enemyManager.AllEnemiesDead) return true;
        // CoreBossステージ: Coreが倒されたらトーテムが残っていても勝利
        var mgr = CoreBossManager.Instance;
        if (mgr?.coreBoss != null)
        {
            var boss = mgr.coreBoss;
            if (!boss.gameObject.activeInHierarchy || boss.enemyHealth <= 0) return true;
        }
        return false;
    }

    /// <summary>プレイヤー敗北判定（プレイヤーHP0）</summary>
    private bool IsPlayerDefeat() => playerController != null && playerController.playerHealth <= 0;

    /// <summary>
    /// バトルを終了させる。全コルーチンを停止してから結果UIを表示する
    /// </summary>
    private void EndBattle(bool playerWon)
    {
        _battleEnded = true;
        StopAllCoroutines();

        // 勝利時にクリアデータを保存。初回クリア時のみ justClearedStageName をセット（報酬ウィンドウ・解放アニメ用）
        if (playerWon && stageConfig != null && !string.IsNullOrEmpty(stageConfig.stageName))
        {
            bool wasAlreadyCleared = ClearDataManager.IsStageClear(stageConfig.stageName);
            ClearDataManager.MarkStageClear(stageConfig.stageName);
            if (!wasAlreadyCleared)
                StageLoader.justClearedStageName = stageConfig.stageName;
            Debug.Log($"[TurnController] クリアデータ保存: stageName={stageConfig.stageName}, 初回={!wasAlreadyCleared}");
        }
        else if (playerWon)
        {
            Debug.LogWarning($"[TurnController] 勝利しましたがクリアデータを保存できません: stageConfig={stageConfig?.stageName ?? "null"}");
        }

        // 敗北時にアドバイス表示用のステージ名をセット（defeatAdvice が設定されているときのみ）
        if (!playerWon && stageConfig != null && !string.IsNullOrEmpty(stageConfig.defeatAdvice))
        {
            StageLoader.justDefeatedStageName = stageConfig.stageName;
        }

        // バトル関連UIを全て非表示
        codeWindow.SetActive(false);
        leftWindow.SetActive(false);
        guardPhaseUI.SetActive(false);
        attackPhaseUI.SetActive(false);
        turnStartUI.SetActive(false);
        if (playerTurnUI != null)  playerTurnUI.SetActive(false);
        if (enemyTurnUI != null)   enemyTurnUI.SetActive(false);
        if (telegraphText != null) telegraphText.gameObject.SetActive(false);
        if (codingParticle != null) codingParticle.SetActive(false);
        if (editorLineHighlight != null) editorLineHighlight.SetActive(false);
        ClearEnemyAttackNotification();
        if (playerController != null) playerController.HideStatusUI();

        GameObject resultUI = playerWon ? victoryUI : defeatUI;
        if (resultUI != null)
            StartCoroutine(ShowResultUI(resultUI));

        Debug.Log($"[TurnController] バトル終了: {(playerWon ? "Victory" : "Defeat")}");
    }

    /// <summary>
    /// 結果UIをフェードイン＋スケールアップで表示する
    /// </summary>
    IEnumerator ShowResultUI(GameObject resultUI)
    {
        yield return new WaitForSeconds(1f);
        resultUI.SetActive(true);

        var cg = resultUI.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
        }

        resultUI.transform.localScale = Vector3.one * 0.7f;
        yield return resultUI.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    // ==============================================================
    // 各アクションルーチン
    // ==============================================================

    IEnumerator EnemyAttack()
    {
        // 全敵死亡チェック
        if (enemyManager.AllEnemiesDead)
        {
            Debug.Log("[EnemyAttack] 全敵死亡のためスキップ");
            yield break;
        }

        Debug.Log($"[EnemyAttack] 生存敵数: {enemyManager.AliveEnemies.Count}");
        yield return PhaseUIEffect(attackPhaseUI);
        yield return enemyManager.ExecuteAllAttacks();

        Debug.Log($"[Enemy Turn {enemyTurn}] 攻撃フェーズ完了");
        yield return new WaitForSeconds(0.8f);
        Debug.Log("敵が攻撃した！");
    }

    IEnumerator EnemyDefense()
    {
        // 全敵死亡チェック
        if (enemyManager.AllEnemiesDead)
        {
            Debug.Log("[EnemyDefense] 全敵死亡のためスキップ");
            yield break;
        }

        Debug.Log($"[EnemyDefense] 生存敵数: {enemyManager.AliveEnemies.Count}");
        yield return PhaseUIEffect(guardPhaseUI);
        Debug.Log($"[Enemy Turn {enemyTurn}] 防御フェーズ開始");

        yield return enemyManager.ExecuteAllDefenses();
        yield return new WaitForSeconds(0.6f);

        // 次の敵攻撃内容を決定, 予告(Telegraph)
        nextEnemyAttackInfo = TelegraphNextAttack();

        Debug.Log($"敵は次の攻撃を準備中... ({nextEnemyAttackInfo})");
    }

    IEnumerator PlayerAttack()
    {
        // KeyGolemメカニクスにプレイヤー攻撃フェーズ開始を通知
        FindObjectOfType<KeyGolemMechanic>()?.NotifyPlayerAttackStart();

        // ターン開始時にAPを全回復（上限50まで）
        playerController?.RecoverAP();

        yield return PhaseUIEffect(attackPhaseUI);

        // チュートリアルヒントを表示
        ShowTutorialHint(Phase.Attack);

        // プレイヤーの入力待ち：コード入力とエディタハイライトを有効化
        _codeWindowCanvasGroup.interactable = true;
        if (editorLineHighlight != null)
            editorLineHighlight.SetActive(true);

        yield return new WaitUntil(() => isCodeInputReady);

        // コード実行開始 → ヒントを非表示
        HideTutorialHint();

        // コード実行中は入力を無効化・パーティクルも確実に消す
        _codeWindowCanvasGroup.interactable = false;
        HideCodingParticle();

        yield return new WaitUntil(() => isPlayerActionComplete);
        yield return new WaitForSeconds(0.1f);

        // KeyGolemメカニクスにプレイヤー攻撃フェーズ終了を通知（ダメージ判定はここで発生）
        FindObjectOfType<KeyGolemMechanic>()?.NotifyPlayerAttackEnd();

        // プレイヤー攻撃フェーズ完了後、ターン終了HP全回復（対象敵のみ）
        yield return StartCoroutine(enemyManager.ExecuteAllHealthRegens());

        // CoreBossメカニクス: ソート完了なら30ダメージ自動適用、未ソートなら初期位置リセット
        var coreBossMgr = CoreBossManager.Instance;
        if (coreBossMgr != null)
        {
            if (coreBossMgr.IsCurrentlySorted)
                yield return StartCoroutine(coreBossMgr.ApplySortedDamage());
            else
                yield return StartCoroutine(coreBossMgr.ResetToInitialState());
        }

        Debug.Log("プレイヤーの攻撃が完了！");
        isCodeInputReady = false;
        isPlayerActionComplete = false;
    }

    IEnumerator PlayerDefense()
    {
        yield return PhaseUIEffect(guardPhaseUI);

        // チュートリアルヒントを表示
        ShowTutorialHint(Phase.Defense);

        // 敵の次の行動を予告テキストに表示
        if (telegraphText != null)
        {
            telegraphText.text = !string.IsNullOrEmpty(nextEnemyAttackInfo)
                ? nextEnemyAttackInfo
                : "";
            telegraphText.gameObject.SetActive(!string.IsNullOrEmpty(nextEnemyAttackInfo));
        }

        // 敵行動予告をEANウィンドウにリスト表示
        ShowEnemyAttackNotification();
        // 攻撃予定エリアに警告マークを表示（エリアラベルの X 座標に合わせて生成）
        attackWarningSystem?.ShowWarningsForAreas(_nextEnemyAttackAreas, _areaLabels);

        // プレイヤーの入力待ち：コード入力とエディタハイライトを有効化
        _codeWindowCanvasGroup.interactable = true;
        if (editorLineHighlight != null)
            editorLineHighlight.SetActive(true);

        yield return new WaitUntil(() => isCodeInputReady);

        // コード実行開始 → ヒントを非表示
        HideTutorialHint();

        // コード実行中は入力を無効化・パーティクルも確実に消す
        _codeWindowCanvasGroup.interactable = false;
        HideCodingParticle();

        // アクション（アニメーション）完了まで待機
        yield return new WaitUntil(() => isPlayerActionComplete);
        yield return new WaitForSeconds(0.1f);

        // 予告テキストを非表示・EANウィンドウと警告マークをクリア
        if (telegraphText != null)
            telegraphText.gameObject.SetActive(false);
        ClearEnemyAttackNotification();
        attackWarningSystem?.HideAllWarnings();

        Debug.Log("プレイヤーが防御体勢に入った！");
        isCodeInputReady = false;
        isPlayerActionComplete = false;
    }

    IEnumerator TurnStartUIEffect(bool isEnemyTurn)
    {
        turnStartUI.SetActive(true);
        turnStartUI.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        yield return turnStartUI.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(0.2f);
        yield return _turnStartUICanvasGroup.DOFade(0.0f, 0.1f);
        turnStartUI.SetActive(false);
    }

    /// <summary>
    /// ターン切り替え演出
    /// </summary>
    IEnumerator ShowTurnChangeEffect(Actor newActor)
    {
        GameObject turnUI = newActor == Actor.Player ? playerTurnUI : enemyTurnUI;
        CanvasGroup canvasGroup = newActor == Actor.Player ? _playerTurnUICanvasGroup : _enemyTurnUICanvasGroup;

        // UIが設定されていない場合はスキップ
        if (turnUI == null || canvasGroup == null)
        {
            Debug.Log($"[TurnController] ターン切り替え: {newActor} (UI未設定)");
            yield break;
        }

        Debug.Log($"[TurnController] ターン切り替え演出: {newActor}");

        // 初期状態
        turnUI.SetActive(true);
        canvasGroup.alpha = 0f;
        turnUI.transform.localScale = Vector3.one * 0.7f;

        // フェードイン + スケールアップ
        canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        yield return turnUI.transform.DOScale(1f, 0.3f).SetEase(Ease.InOutQuint).WaitForCompletion();

        // 表示維持
        yield return new WaitForSeconds(turnChangeDisplayTime);

        // フェードアウト
        yield return canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.OutSine).WaitForCompletion();

        turnUI.SetActive(false);
    }

    //フェーズUI演出


    IEnumerator  PhaseUIEffect(GameObject phaseUI)
    {
        phaseUI.SetActive(true);
        phaseUI.GetComponent<CanvasGroup>().alpha = 0;
        yield return phaseUI.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);
        yield return new WaitForSeconds(0.6f);
        yield return phaseUI.GetComponent<CanvasGroup>().DOFade(0.0f, 0.2f).WaitForCompletion();
        phaseUI.SetActive(false);
    }

    // ==============================================================
    // コード入力パーティクル制御
    // ==============================================================

    /// <summary>
    /// InputFieldにフォーカスしたときパーティクルをプレイヤー位置（+0.6上）に表示
    /// </summary>
    private void ShowCodingParticle()
    {
        if (codingParticle == null) return;
        codingParticle.transform.position = playerController.transform.position;// + new Vector3(0f, 0.6f, 0f); これは、パーティクルが円のちゅうしんに収束するような演出に付けるときは必要
        codingParticle.SetActive(true);
    }

    /// <summary>
    /// コード実行開始時にパーティクルを非表示
    /// </summary>
    private void HideCodingParticle()
    {
        if (codingParticle == null) return;
        codingParticle.SetActive(false);
    }

    // ==============================================================
    // チュートリアルヒント表示
    // ==============================================================

    /// <summary>
    /// 現在のプレイヤーターン・フェーズに対応するヒントを検索してアシスタントに表示する。
    /// 対応するヒントがない場合は吹き出しを非表示にする。
    /// </summary>
    private void ShowTutorialHint(Phase phase)
    {
        if (tutorialWindow == null || stageConfig == null || stageConfig.tutorialHints == null) return;

        // playerTurn==0 は全ターン共通、それ以外は現在のターンと一致するものを選ぶ
        var hint = stageConfig.tutorialHints.Find(h =>
            h.phase == phase && (h.playerTurn == 0 || h.playerTurn == playerTurn));

        if (hint != null && !string.IsNullOrEmpty(hint.message))
            tutorialWindow.ShowHint(hint.message);
        else
            tutorialWindow.HideHint();
    }

    private void HideTutorialHint()
    {
        tutorialWindow?.HideHint();
    }

    /// <summary>
    /// コード実行ボタンが押された瞬間に LogsListManager から呼ばれる。
    /// INTERPRETING... UI が出る前にヒントを消す。
    /// </summary>
    public void NotifyCodeExecutionStarted()
    {
        HideTutorialHint();
    }

    // ==============================================================
    // 敵の次攻撃を事前選択・予告文を生成
    // ==============================================================
    string TelegraphNextAttack()
    {
        if (enemyManager == null) return "";

        var messages = new List<string>();
        var areas = new List<int>();
        foreach (var enemy in enemyManager.AliveEnemies)
        {
            string msg = enemy.PrepareNextAttack();
            if (!string.IsNullOrEmpty(msg))
                messages.Add(msg);
            // PrepareNextAttack() 後に確定した攻撃エリアを収集
            areas.AddRange(enemy.GetPendingAttackAreas());
        }

        // EAN表示・警告マーク表示用にリストを保存
        _nextEnemyAttackMessages = messages;
        _nextEnemyAttackAreas = areas;
        return string.Join("\n", messages);
    }

    // 敵行動予告をEAN_Contentプレハブとしてウィンドウに一覧表示
    void ShowEnemyAttackNotification()
    {
        if (eanContentPrefab == null || eanContentParent == null) return;
        foreach (string msg in _nextEnemyAttackMessages)
        {
            GameObject instance = Instantiate(eanContentPrefab, eanContentParent);
            instance.GetComponentInChildren<TMP_Text>().text = msg;
            _eanInstances.Add(instance);
        }
    }

    // 表示中のEAN_Contentを全て破棄
    void ClearEnemyAttackNotification()
    {
        foreach (GameObject instance in _eanInstances)
            Destroy(instance);
        _eanInstances.Clear();
    }
}