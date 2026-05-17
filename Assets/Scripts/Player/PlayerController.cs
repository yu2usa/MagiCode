using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // 定数
    private const int AP_COST_LIGHTNING = 20;
    private const int AP_COST_FLAME = 20;
    private const int AP_COST_FREEZE = 20;
    private const int AP_COST_ATTACK = 10;
    private const int SKILL_DAMAGE = 4;
    private const int ATTACK_DAMAGE = 2;
    private const float MOVE_DURATION = 0.8f;
    private const float MOVE_TO_DURATION = 0.5f;
    private const int MAX_HEALTH = 10;
    private const int MAX_AP = 50;

    [Header("Animation Settings")]
    [Tooltip("Cast系スキルのアニメーショントリガー名")]
    public string castAnimTrigger = "action1";
    [Tooltip("Attack系スキルのアニメーショントリガー名")]
    public string attackAnimTrigger = "action2";
    [Tooltip("移動開始アニメーションのトリガー名")]
    public string moveAnimTrigger = "Move";
    [Tooltip("移動終了アニメーションのトリガー名")]
    public string moveEndAnimTrigger = "MoveEnd";
    [Tooltip("死亡アニメーションのトリガー名")]
    public string deathAnimTrigger = "Death";
    [Tooltip("キャスト動作が終わってから魔法エフェクトが出るまでの待機時間")]
    public float attackAnimationDuration = 0.5f;

    [Header("効果音")]
    [Tooltip("近接攻撃の効果音")]
    public AudioClip meleeSE;
    [Tooltip("ライトニング詠唱の効果音")]
    public AudioClip lightningSE;
    [Tooltip("フレイム詠唱の効果音")]
    public AudioClip flameSE;
    [Tooltip("フリーズ詠唱の効果音")]
    public AudioClip freezeSE;

    [Header("Magic Effect Duration")]
    [Tooltip("ライトニングエフェクトの表示時間（アニメーションクリップ長に合わせる）")]
    public float lightningEffectDuration = 0.4f;
    [Tooltip("フレイムエフェクトの表示時間")]
    public float flameEffectDuration = 0.7f;
    [Tooltip("フリーズエフェクトの表示時間")]
    public float freezeEffectDuration = 0.7f;

    // 外部参照用プロパティ
    public int MaxHealth => MAX_HEALTH;
    public int MaxAP => MAX_AP;

    // DOTween識別用ID
    private const string PLAYER_TWEEN_ID = "PlayerTween";

    public GameObject logsManager;

    public GameObject lightning;
    public GameObject flame;
    public GameObject freeze;
    public GameObject shield;

    public Animator playerAnimator;

    [Header("UI Prefabs")]
    [Tooltip("HPバーのPrefab")]
    public GameObject healthBarPrefab;
    [Tooltip("APバーのPrefab")]
    public GameObject apBarPrefab;
    [Tooltip("ダメージ表示のPrefab（未設定時は自動生成）")]
    public GameObject damageIndicatorPrefab;
    [Tooltip("AP軽減ボーナス通知のPrefab")]
    public GameObject apReductionIndicatorPrefab;
    [Tooltip("AP軽減通知の色")]
    public Color apReductionColor = new Color(0.3f, 0.8f, 1f);
    [Tooltip("UIを配置するCanvas（未設定時は自動検索）")]
    public Canvas canvas;
    [Tooltip("HPバーのY軸オフセット")]
    public float healthBarOffsetY = -10f;
    [Tooltip("APバーのY軸オフセット（HPバーからの相対位置）")]
    public float apBarOffsetY = -17f;

    // 生成されたUIインスタンス
    private GameObject _healthBarInstance;
    private GameObject _apBarInstance;
    private TextMeshProUGUI _healthTextDisplay;
    private TextMeshProUGUI _apTextDisplay;

    public GameObject turnFlowManager;

    [Header("Stage Config")]
    [Tooltip("ステージ設定（未設定時は従来のareaPosを使用）")]
    public StageConfig stageConfig;

    [Header("Legacy Area Settings（StageConfig未設定時のみ使用）")]
    public List<Vector2> areaPos;
    public List<Vector2> enemAreaPos;

    // プレイヤーが現在いるarea
    public int playerCurrentPos;

    // StageConfig から取得したエリア位置
    private List<Vector2> _playerAreas;
    private List<Vector2> _enemyAreas;
    [Header("PlayerStatus")]
    public int playerHealth;
    public int playerAP;

    [Header("Execution Label")]
    [Tooltip("実行中コードを表示するテキストの頭上オフセット（Y方向）")]
    [SerializeField] private float executionLabelOffsetY = 1.2f;
    [Tooltip("実行ラベルのフォントサイズ（ワールドスペース単位）")]
    [SerializeField] private float executionLabelFontSize = 0.4f;
    [Tooltip("実行ラベルのテキスト色")]
    [SerializeField] private Color executionLabelColor = Color.white;
    [Tooltip("実行ラベルのフォント（未設定時はTMPデフォルト）")]
    [SerializeField] private TMP_FontAsset executionLabelFont;

    // キャッシュされたコンポーネント
    private RectTransform _healthBarRect;
    private RectTransform _apBarRect;
    private Slider _healthBarSlider;
    private Slider _apBarSlider;
    private Animator _lightningAnimator;
    private Animator _flameAnimator;
    private Animator _freezeAnimator;
    private EnemyManager _enemyManager;
    private TurnController _turnController;
    private LogsListManager _logsListManager;
    private CodeLineHighlighter _codeLineHighlighter;
    private SpriteRenderer _spriteRenderer;
    private SpriteFlashEffect _flashEffect;
    private TextMeshPro _executionLabel;

    private void Start()
    {
        // StageConfig からエリア位置を取得（未設定時は従来のリストを使用）
        if (stageConfig != null)
        {
            _playerAreas = stageConfig.playerAreaPositions;
            _enemyAreas = stageConfig.enemyAreaPositions;
            playerCurrentPos = stageConfig.playerStartArea;

            // StageConfig が設定されている場合、初期エリア座標にテレポート
            if (_playerAreas.Count > playerCurrentPos)
            {
                Vector2 startPos = _playerAreas[playerCurrentPos];
                transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
            }
        }
        else
        {
            _playerAreas = areaPos;
            _enemyAreas = enemAreaPos;
            playerCurrentPos = 0;
        }

        playerHealth = MAX_HEALTH;
        playerAP = MAX_AP;

        // Canvas の自動検索
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        // HP/APバーの生成
        InitializeUIBars();

        // コンポーネントキャッシュ
        _lightningAnimator = lightning.GetComponent<Animator>();
        _flameAnimator = flame.GetComponent<Animator>();
        _freezeAnimator = freeze.GetComponent<Animator>();
        _enemyManager = FindObjectOfType<EnemyManager>();
        _turnController = turnFlowManager.GetComponent<TurnController>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _logsListManager = logsManager.GetComponent<LogsListManager>();
        _codeLineHighlighter = _logsListManager.codeLineHighlighter;

        // フラッシュ効果コンポーネントを取得または追加
        _flashEffect = GetComponent<SpriteFlashEffect>();
        if (_flashEffect == null)
            _flashEffect = gameObject.AddComponent<SpriteFlashEffect>();

        // 頭上コード表示ラベルを初期化
        InitializeExecutionLabel();

        // スライダーの範囲設定
        if (_healthBarSlider != null)
        {
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = MAX_HEALTH;
        }
        if (_apBarSlider != null)
        {
            _apBarSlider.minValue = 0;
            _apBarSlider.maxValue = MAX_AP;
        }

        // UI初期化
        UpdateHealthUI();
        UpdateAPUI();

        Debug.Log($"[Player] 初期化完了: playerCurrentPos={playerCurrentPos}, WorldPos={transform.position}");
    }

    /// <summary>
    /// HP/APバーをPrefabから生成して初期化
    /// </summary>
    private void InitializeUIBars()
    {
        if (canvas == null)
        {
            Debug.LogWarning("[PlayerController] Canvasが見つかりません");
            return;
        }

        // HPバー生成（最初は非表示。TurnControllerのShowStatusUI()で表示する）
        if (healthBarPrefab != null)
        {
            _healthBarInstance = Instantiate(healthBarPrefab, canvas.transform);
            _healthBarInstance.name = "HealthBar_Player";
            _healthBarInstance.SetActive(false);
            _healthBarRect = _healthBarInstance.GetComponent<RectTransform>();
            _healthBarSlider = _healthBarInstance.GetComponent<Slider>();
            _healthTextDisplay = _healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
        }

        // APバー生成（最初は非表示。TurnControllerのShowStatusUI()で表示する）
        if (apBarPrefab != null)
        {
            _apBarInstance = Instantiate(apBarPrefab, canvas.transform);
            _apBarInstance.name = "APBar_Player";
            _apBarInstance.SetActive(false);
            _apBarRect = _apBarInstance.GetComponent<RectTransform>();
            _apBarSlider = _apBarInstance.GetComponent<Slider>();
            _apTextDisplay = _apBarInstance.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

  /*  private void Update()
    {
        // HP/APバーの位置をプレイヤーに追従させる
        if (_healthBarRect == null || canvas == null)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPoint);

        // HPバーの位置
        Vector2 healthPos = localPoint;
        healthPos.y += healthBarOffsetY;
        _healthBarRect.anchoredPosition = healthPos;

        // APバーの位置
        if (_apBarRect != null)
        {
            Vector2 apPos = localPoint;
            apPos.y += apBarOffsetY;
            _apBarRect.anchoredPosition = apPos;
        }
    } */

    public void HideStatusUI()
    {
        if (_healthBarInstance != null) _healthBarInstance.SetActive(false);
        if (_apBarInstance != null)     _apBarInstance.SetActive(false);
    }

    public void ShowStatusUI()
    {
        if (_healthBarInstance != null) _healthBarInstance.SetActive(true);
        if (_apBarInstance != null)     _apBarInstance.SetActive(true);
    }

    private void OnDestroy()
    {
        // UIインスタンスを削除
        if (_healthBarInstance != null)
            Destroy(_healthBarInstance);
        if (_apBarInstance != null)
            Destroy(_apBarInstance);
    }

    public void Execute()
    {
        StartCoroutine(Execution());
    }

    /// <summary>
    /// ターン開始時にAPを上限まで全回復する。TurnControllerのPlayerAttack()から呼ぶ。
    /// </summary>
    public void RecoverAP()
    {
        playerAP = MAX_AP;
        UpdateAPUI();
        Debug.Log($"[Player] AP全回復: {playerAP}/{MAX_AP}");
    }

    /// <summary>
    /// ループボーナスが適用されるエントリのインデックスを返す
    /// 同一行番号が2回以上出現する最初のエントリ = ループ内の初回実行（回数 >= 2 のみ）
    /// 各ターン1回限り（最初のループの最初の1回のみ）
    /// </summary>
    private int FindLoopBonusEntryIndex(List<ExecutionLogEntry> logs)
    {
        // 各行番号の出現回数をカウント
        var lineCounts = new Dictionary<int, int>();
        foreach (var entry in logs)
        {
            if (!lineCounts.ContainsKey(entry.line))
                lineCounts[entry.line] = 0;
            lineCounts[entry.line]++;
        }

        // AP消費コマンド（Casting）のエントリの中で、同一行が2回以上出現する最初のものを返す
        // ループ制御ログ（"for ... start", "i = N" 等）は除外し、実際の詠唱ログのみ対象にする
        for (int i = 0; i < logs.Count; i++)
        {
            if (lineCounts[logs[i].line] >= 2 && logs[i].message.Contains("Casting: "))
                return i;
        }

        return -1; // ループなし
    }

    public void playerDamaged(int damageNum)
    {
        StartCoroutine(Damaged(damageNum));
    }

    /// <summary>
    /// 指定インデックスのプレイヤー側エリア座標を返す（MagicAttackAction から参照）
    /// </summary>
    public Vector2 GetPlayerArea(int index)
    {
        var areas = _playerAreas ?? areaPos;
        if (areas == null || index < 0 || index >= areas.Count)
            return (Vector2)transform.position;
        return areas[index];
    }

    public void playerAPVary(int varyAP)
    {
        StartCoroutine(APVary(varyAP));
    }

    /// <summary>
    /// HealthのUIを更新
    /// </summary>
    private void UpdateHealthUI()
    {
        if (_healthBarSlider != null)
            _healthBarSlider.value = playerHealth;
        if (_healthTextDisplay != null)
            _healthTextDisplay.text = $"{playerHealth}/{MAX_HEALTH}";
    }

    /// <summary>
    /// APのUIを更新
    /// </summary>
    private void UpdateAPUI()
    {
        if (_apBarSlider != null)
            _apBarSlider.value = playerAP;
        if (_apTextDisplay != null)
            _apTextDisplay.text = $"{playerAP}/{MAX_AP}";
    }



    IEnumerator Execution()
    {
        Debug.Log("コマンド実行開始");
        List<ExecutionLogEntry> currentCode = _logsListManager.executionLogs;

        // ループボーナスエントリを特定（各ターン1回限り: 同一行が2回以上出現 = ループ実行）
        int bonusEntryIndex = FindLoopBonusEntryIndex(currentCode);
        if (bonusEntryIndex >= 0)
            Debug.Log($"[LoopBonus] index={bonusEntryIndex}, line={currentCode[bonusEntryIndex].line} でAP半減ボーナス適用");

        for (int i = 0; i < currentCode.Count; i++)
        {
            // 実行中の行をハイライト
            if (_codeLineHighlighter != null)
            {
                Debug.Log($"[Execution] HighlightLine呼び出し: line={currentCode[i].line}");
                _codeLineHighlighter.HighlightLine(currentCode[i].line);
            }
            else
            {
                Debug.LogWarning("[Execution] _codeLineHighlighter が null");
            }

           string code =  currentCode[i].message;
           bool isLoopBonus = (i == bonusEntryIndex);

            // アクションを伴う行のPythonコードを頭上に表示
            if (IsActionLog(code))
                ShowExecutionLabel(GetCodeLine(currentCode[i].line));

            // 攻撃アクション直前に全敵の属性をランダム変化させる（属性フェーズが有効な敵のみ）
            if ((code.Contains("Casting: ") || code.Contains("Attack")) && _enemyManager != null)
                yield return StartCoroutine(_enemyManager.TriggerAllElementChanges());

            //コードコマンド実行の場合:
            if (code.Contains("Casting: "))
            {
                if (code.Contains("ライトニング"))
                {
                    string code2 = code.Replace("Casting: ライトニング, ", "");

                        int num = int.Parse(code2);
                        yield return StartCoroutine(CastLightning(num, isLoopBonus));
                }

                if (code.Contains("フレイム"))
                {
                    string code2 = code.Replace("Casting: フレイム, ", "");

                    int num = int.Parse(code2);
                    yield return StartCoroutine(CastFlame(num, isLoopBonus));
                }

                if (code.Contains("フリーズ"))
                {
                    string code2 = code.Replace("Casting: フリーズ, ", "");
                    int num = int.Parse(code2);
                    yield return StartCoroutine(CastFreeze(num, isLoopBonus));
                }
            }

            if (code.Contains("Attack"))
            {
                yield return StartCoroutine(Attack());
            }


                // 移動コマンド
                if (code.Contains("Moving to: "))
            {
                string code2 = code.Replace("Moving to: ", "");
                int num = int.Parse(code2);
                yield return StartCoroutine(MoveTo(num));
                // playerCurrentPosはMoveTo()内で更新される
            }

            //forward, backwardコマンド関係
            if (code.Contains("Moving backward"))
            {
               // Debug.Log("バックワードコマンド実行");
                yield return StartCoroutine(MoveBackward());

            }

            if (code.Contains("Moving forward"))
            {
                //Debug.Log("フォワードコマンド実行");
             yield return StartCoroutine(MoveForward());
            }

            // Swap コマンド（SortBossManager または CoreBossManager がある場合のみ有効）
            if (code.Contains("Swap: "))
            {
                string swapData = code.Replace("Swap: ", "");
                string[] parts = swapData.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int swapJ) &&
                    int.TryParse(parts[1].Trim(), out int swapK))
                {
                    var sortBoss = SortBossManager.Instance;
                    if (sortBoss != null)
                        yield return StartCoroutine(sortBoss.SwapGuards(swapJ, swapK));

                    var coreBoss = CoreBossManager.Instance;
                    if (coreBoss != null)
                        yield return StartCoroutine(coreBoss.SwapTotems(swapJ, swapK));
                }
            }

            Debug.Log("コマンド実行完了");

            yield return new WaitForSeconds(0.1f);
            // プレイヤーのTweenのみ停止（敵のTweenは停止しない）
            DOTween.Kill(PLAYER_TWEEN_ID);
        }
        // ハイライト解除・ラベル非表示
        if (_codeLineHighlighter != null)
            _codeLineHighlighter.ClearHighlight();
        HideExecutionLabel();

        Debug.Log("全コマンド実行完了");
        _turnController.isPlayerActionComplete = true;
        Debug.Log("Player Action Complete");
    }


    IEnumerator CastLightning(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? AP_COST_LIGHTNING / 2 : AP_COST_LIGHTNING;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でライトニングを発動できない");
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] ライトニング AP半減: {AP_COST_LIGHTNING} → {cost}");
            DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, transform.position, "AP軽減", apReductionColor);
        }
        playerAPVary(cost);

        // Castアニメーション発火 (action1)
        TriggerCastAnimation();

        // アニメーション完了を待つ
        yield return new WaitForSeconds(attackAnimationDuration);

        // 魔法発動
        AudioManager.Instance?.PlaySE(lightningSE);
        lightning.transform.position = _enemyAreas[areaNum];
        lightning.SetActive(true);
        _lightningAnimator.SetTrigger("Strike");
        CameraShake.Shake();

        // エリア内の全敵にダメージ付与
        var targets = _enemyManager.GetEnemiesInArea(areaNum);
        foreach (var enemy in targets)
        {
            enemy.GetDamaged(SKILL_DAMAGE);
        }

        yield return new WaitForSeconds(lightningEffectDuration);
        lightning.SetActive(false);
    }

    IEnumerator CastFlame(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? AP_COST_FLAME / 2 : AP_COST_FLAME;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でフレイムを発動できない");
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] フレイム AP半減: {AP_COST_FLAME} → {cost}");
            DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, transform.position, "AP軽減", apReductionColor);
        }
        playerAPVary(cost);

        // Castアニメーション発火 (action1)
        TriggerCastAnimation();

        // アニメーション完了を待つ
        yield return new WaitForSeconds(attackAnimationDuration);

        // 魔法発動
        AudioManager.Instance?.PlaySE(flameSE);
        flame.transform.position = _enemyAreas[areaNum];
        flame.SetActive(true);
        _flameAnimator.SetTrigger("Strike");
        CameraShake.Shake();

        // エリア内の全敵にダメージ付与（フレイム属性）
        var targets = _enemyManager.GetEnemiesInArea(areaNum);
        foreach (var enemy in targets)
        {
            enemy.GetDamaged(SKILL_DAMAGE, DamageType.Flame);
        }

        yield return new WaitForSeconds(flameEffectDuration);
        flame.SetActive(false);
    }

    IEnumerator CastFreeze(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? AP_COST_FREEZE / 2 : AP_COST_FREEZE;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でフリーズを発動できない");
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] フリーズ AP半減: {AP_COST_FREEZE} → {cost}");
            DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, transform.position, "AP軽減", apReductionColor);
        }
        playerAPVary(cost);

        // Castアニメーション発火 (action1)
        TriggerCastAnimation();

        // アニメーション完了を待つ
        yield return new WaitForSeconds(attackAnimationDuration);

        // 魔法発動
        AudioManager.Instance?.PlaySE(freezeSE);
        freeze.transform.position = _enemyAreas[areaNum];
        freeze.SetActive(true);
        _freezeAnimator.SetTrigger("Strike");
        CameraShake.Shake();

        // エリア内の全敵にダメージ付与（アイス属性）
        var targets = _enemyManager.GetEnemiesInArea(areaNum);
        foreach (var enemy in targets)
        {
            enemy.GetDamaged(SKILL_DAMAGE, DamageType.Ice);
        }

        yield return new WaitForSeconds(freezeEffectDuration);
        freeze.SetActive(false);
    }

    /// <summary>
    /// 近接攻撃: 10APを消費して同エリアの敵にダメージを与える
    /// </summary>
    IEnumerator Attack()
    {
        if (playerAP < AP_COST_ATTACK)
        {
            Debug.Log("AP不足で攻撃できない");
            yield break;
        }
        playerAPVary(AP_COST_ATTACK);

        // Attackアニメーション発火 (action2)
        AudioManager.Instance?.PlaySE(meleeSE);
        TriggerAttackAnimation();
        yield return new WaitForSeconds(attackAnimationDuration);
        CameraShake.Shake();

        // 同エリアの全敵にダメージ
        var targets = _enemyManager.GetEnemiesInArea(playerCurrentPos);
        foreach (var enemy in targets)
        {
            enemy.GetDamaged(ATTACK_DAMAGE);
        }

        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator MoveTo(int position)
    {
        Debug.Log($"[Player] MoveTo開始: {playerCurrentPos} → {position}");

        // 移動方向に向きを変える
        if (position > playerCurrentPos)
            FaceDirection(forward: true);
        else if (position < playerCurrentPos)
            FaceDirection(forward: false);

        // 残像を生成
        CreateAfterImage();

        // 移動アニメーション開始
        TriggerMoveAnimation();

        yield return new WaitForSeconds(0.1f);
        yield return transform.DOMove(_playerAreas[position], MOVE_TO_DURATION)
            .SetId(PLAYER_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        // 位置を更新（MoveForward/MoveBackwardと同様に内部で更新）
        playerCurrentPos = position;
        Debug.Log($"[Player] MoveTo完了: playerCurrentPos={playerCurrentPos}");

        // 移動アニメーション終了
        TriggerMoveEndAnimation();

        // 移動後、前方（敵側）に戻す
        FaceDirection(forward: true);
    }

    IEnumerator MoveForward()
    {
        Debug.Log($"[Player] MoveForward開始: playerCurrentPos={playerCurrentPos}");

        if (playerCurrentPos + 1 >= _playerAreas.Count)
        {
            Debug.Log($"[Player] MoveForward中止: 境界外 (playerCurrentPos+1={playerCurrentPos + 1} >= Count={_playerAreas.Count})");
            yield break;
        }

        // 前進方向（敵側）を向く
        FaceDirection(forward: true);

        // 残像を生成
        CreateAfterImage();

        // 移動アニメーション開始
        TriggerMoveAnimation();

        int targetPos = playerCurrentPos + 1;
        Debug.Log($"[Player] MoveForward移動中: {playerCurrentPos} → {targetPos}");

        yield return new WaitForSeconds(0.1f);
        yield return transform.DOMove(_playerAreas[targetPos], MOVE_DURATION)
            .SetId(PLAYER_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        playerCurrentPos = targetPos;
        Debug.Log($"[Player] MoveForward完了: playerCurrentPos={playerCurrentPos}");

        // 移動アニメーション終了
        TriggerMoveEndAnimation();
    }

    IEnumerator MoveBackward()
    {
        Debug.Log($"[Player] MoveBackward開始: playerCurrentPos={playerCurrentPos}");

        if (playerCurrentPos - 1 < 0)
        {
            Debug.Log($"[Player] MoveBackward中止: 境界外 (playerCurrentPos-1={playerCurrentPos - 1} < 0)");
            yield break;
        }

        // 後退方向を向く
        FaceDirection(forward: false);

        // 残像を生成
        CreateAfterImage();

        // 移動アニメーション開始
        TriggerMoveAnimation();

        int targetPos = playerCurrentPos - 1;
        Debug.Log($"[Player] MoveBackward移動中: {playerCurrentPos} → {targetPos}");

        yield return new WaitForSeconds(0.1f);
        yield return transform.DOMove(_playerAreas[targetPos], MOVE_DURATION)
            .SetId(PLAYER_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        playerCurrentPos = targetPos;
        Debug.Log($"[Player] MoveBackward完了: playerCurrentPos={playerCurrentPos}");

        // 移動アニメーション終了
        TriggerMoveEndAnimation();

        // 移動後、前方（敵側）に戻す
        FaceDirection(forward: true);
    }

    /// <summary>
    /// 進行方向に体を向ける
    /// </summary>
    /// <param name="forward">true=前進方向（敵側）, false=後退方向</param>
    private void FaceDirection(bool forward)
    {
        // プレイヤーはデフォルトで右向き（敵側）と仮定
        // forward=true → flipX=false（右向き）, forward=false → flipX=true（左向き）
        _spriteRenderer.flipX = !forward;
    }

    /// <summary>
    /// 攻撃アニメーションを発火
    /// </summary>
    private void TriggerCastAnimation()
    {
        CreateAfterImage();
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(castAnimTrigger))
            playerAnimator.SetTrigger(castAnimTrigger);
    }

    private void TriggerAttackAnimation()
    {
        CreateAfterImage();
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(attackAnimTrigger))
            playerAnimator.SetTrigger(attackAnimTrigger);
    }

    /// <summary>
    /// 移動アニメーションを開始
    /// </summary>
    private void TriggerMoveAnimation()
    {
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(moveAnimTrigger))
            playerAnimator.SetTrigger(moveAnimTrigger);
    }

    /// <summary>
    /// 移動アニメーションを終了
    /// </summary>
    private void TriggerMoveEndAnimation()
    {
        if (playerAnimator == null) return;
        if (!string.IsNullOrEmpty(moveEndAnimTrigger))
            playerAnimator.SetTrigger(moveEndAnimTrigger);
    }

    /// <summary>
    /// 移動時の残像を生成
    /// </summary>
    private void CreateAfterImage()
    {
        if (_spriteRenderer == null) return;

        // 残像用のGameObjectを生成
        var afterImage = new GameObject("PlayerAfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        // SpriteRendererを追加して現在のスプライトをコピー
        var sr = afterImage.AddComponent<SpriteRenderer>();
        sr.sprite = _spriteRenderer.sprite;
        sr.flipX = _spriteRenderer.flipX;
        sr.sortingLayerID = _spriteRenderer.sortingLayerID;
        sr.sortingOrder = _spriteRenderer.sortingOrder - 1;

        // アルファ0.5に設定
        Color color = _spriteRenderer.color;
        color.a = 0.5f;
        sr.color = color;

        // 0.5秒かけてフェードアウトして消える
        sr.DOFade(0f, 0.5f).OnComplete(() => Destroy(afterImage));
    }

    IEnumerator Shield()
    {
        yield return null;
    }



    /// <summary>
    /// 反射ダメージを示すインジケーターを指定位置に表示する（EnemyController から敵の位置を渡す）
    /// </summary>
    public void ShowReflectDamageIndicator(Vector3 spawnPosition)
    {
        DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, spawnPosition, "ダメージ反射", new Color(1f, 0.4f, 0.1f));
    }

    /// <summary>
    /// 外部（EnemyController など）からプレイヤーへダメージを与える
    /// </summary>
    public void GetDamaged(int damageAmount)
    {
        StartCoroutine(Damaged(damageAmount));
    }

    IEnumerator Damaged(int damageAmount)
    {
        playerHealth = Mathf.Max(0, playerHealth - damageAmount);
        UpdateHealthUI();

        // ダメージインジケーター表示
        DamageIndicator.SpawnFromPrefab(damageIndicatorPrefab, transform.position, damageAmount);

        // 白フラッシュ効果
        if (_flashEffect != null)
            _flashEffect.Flash();

        // ノックバック（左へ0.5移動）して元の位置に戻る
        float originalX = transform.position.x;
        yield return transform.DOMoveX(originalX - 0.5f, 0.15f)
            .SetId(PLAYER_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
        yield return transform.DOMoveX(originalX, 0.15f)
            .SetId(PLAYER_TWEEN_ID)
            .SetEase(Ease.InQuad)
            .WaitForCompletion();

        // 死亡判定: Deathトリガー発火 → 暗色適用 → その場に残す
        if (playerHealth <= 0)
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimTrigger))
                playerAnimator.SetTrigger(deathAnimTrigger);
            _spriteRenderer.DOColor(new Color(0.35f, 0.35f, 0.35f, 1f), 0.4f);
        }
    }

    IEnumerator APVary(int consumeAP)
    {
        playerAP = Mathf.Max(0, playerAP - consumeAP);
        UpdateAPUI();
        yield return null;
    }

    // ==============================================================
    // 頭上コード表示ラベル
    // ==============================================================

    /// <summary>
    /// プレイヤーの子オブジェクトとして実行ラベルを生成
    /// 親に追従するため位置更新処理は不要
    /// </summary>
    private void InitializeExecutionLabel()
    {
        var labelObj = new GameObject("ExecutionLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0f, executionLabelOffsetY, 0f);

        _executionLabel = labelObj.AddComponent<TextMeshPro>();
        _executionLabel.fontSize = executionLabelFontSize;
        _executionLabel.alignment = TextAlignmentOptions.Center;
        // 初期アルファは0（非表示状態）
        _executionLabel.color = new Color(executionLabelColor.r, executionLabelColor.g, executionLabelColor.b, 0f);
        // フォント指定があれば適用
        if (executionLabelFont != null)
            _executionLabel.font = executionLabelFont;
        // プレイヤースプライトより前面に表示
        _executionLabel.sortingOrder = _spriteRenderer.sortingOrder + 1;
    }

    /// <summary>
    /// 指定テキストをフェードインで表示する
    /// すでに表示中の場合はテキストだけ差し替える
    /// </summary>
    private void ShowExecutionLabel(string text)
    {
        if (_executionLabel == null || string.IsNullOrEmpty(text)) return;
        _executionLabel.text = text;
        DOTween.To(() => _executionLabel.alpha, x => _executionLabel.alpha = x, 1f, 0.15f);
    }

    /// <summary>
    /// ラベルをフェードアウトで非表示にする
    /// </summary>
    private void HideExecutionLabel()
    {
        if (_executionLabel == null) return;
        DOTween.To(() => _executionLabel.alpha, x => _executionLabel.alpha = x, 0f, 0.2f);
    }

    /// <summary>
    /// executionLogsの行番号（1始まり）からInputFieldの該当行を取得
    /// </summary>
    private string GetCodeLine(int lineNumber)
    {
        if (_logsListManager?.pythonCodeInputField == null) return "";
        string[] lines = _logsListManager.pythonCodeInputField.text.Split('\n');
        int index = lineNumber - 1;
        return (index >= 0 && index < lines.Length) ? lines[index].Trim() : "";
    }

    /// <summary>
    /// プレイヤーの実際のアクション（詠唱・攻撃・移動）を伴うログか判定
    /// ループ制御ログ（"for ... start", "i = N"等）を除外するために使用
    /// </summary>
    private bool IsActionLog(string logMessage)
    {
        return logMessage.Contains("Casting: ")
            || logMessage.Contains("Attack")
            || logMessage.Contains("Moving to: ")
            || logMessage.Contains("Moving forward")
            || logMessage.Contains("Moving backward")
            || logMessage.Contains("Swap: ");
    }
}
