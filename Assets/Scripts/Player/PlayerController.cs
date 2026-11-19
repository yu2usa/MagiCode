using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // 定数
    // 歩行アニメーションの足運びに合わせた移動速度（ワールド単位/秒）
    private const float WALK_SPEED = 4.5f;
    // とどめの一撃は、いつもの何倍の時間をかけて見せるか
    private const float FINISHING_BLOW_SLOWDOWN = 1.5f;

    [Tooltip("AP・ダメージなど戦闘の数値（プレイ中に変えるとすぐ反映される）")]
    [SerializeField] private BalanceConfig balance;

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
    [Tooltip("コード入力開始時のオーブ召喚アニメーションのトリガー名")]
    public string orbSummonAnimTrigger = "OrbSummon";
    [Tooltip("コード入力終了時のオーブ解除アニメーションのトリガー名")]
    public string orbDismissAnimTrigger = "OrbDismiss";
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
    public int MaxHealth => balance.maxHealth;
    public int MaxAP => balance.maxAP;


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
    [Tooltip("攻撃が当たったときに表示するヒットエフェクトのPrefab")]
    public GameObject hitEffectPrefab;
    [Tooltip("プレイヤーが受けたダメージの表示色")]
    public Color damageIndicatorColor = new Color(1f, 0.3f, 0.3f);
    [Tooltip("AP軽減ボーナス通知のPrefab")]
    public GameObject apReductionIndicatorPrefab;
    [Tooltip("AP軽減通知の色")]
    public Color apReductionColor = new Color(0.3f, 0.8f, 1f);
    [Tooltip("AP不足で行動できなかったときの通知・点滅の色")]
    public Color notEnoughAPColor = new Color(1f, 0.25f, 0.25f);
    [Tooltip("AP不足を知らせたあと、次の行動へ進むまでの間（秒）")]
    public float notEnoughAPPause = 0.6f;
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

        playerHealth = balance.maxHealth;
        playerAP = balance.maxAP;

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
            _healthBarSlider.maxValue = balance.maxHealth;
        }
        if (_apBarSlider != null)
        {
            _apBarSlider.minValue = 0;
            _apBarSlider.maxValue = balance.maxAP;
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
        playerAP = balance.maxAP;
        UpdateAPUI();
        Debug.Log($"[Player] AP全回復: {playerAP}/{balance.maxAP}");
    }

    /// <summary>
    /// for / while の中に書かれた行の番号（1始まり）を返す。ループボーナス（AP 割引）の対象になる。
    /// 字下げをさかのぼって、その行を囲むブロックの見出しに for か while があるかで判定する
    /// </summary>
    private static HashSet<int> FindLoopBodyLines(string code)
    {
        string[] lines = code.Replace("\t", "    ").Split('\n');
        var loopLines = new HashSet<int>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            int indent = IndentOf(lines[i]);
            for (int j = i - 1; j >= 0 && indent > 0; j--)
            {
                if (lines[j].Trim().Length == 0 || IndentOf(lines[j]) >= indent) continue;
                if (LoopHeaderPattern.IsMatch(lines[j]))
                {
                    loopLines.Add(i + 1);
                    break;
                }
                indent = IndentOf(lines[j]);
            }
        }
        return loopLines;
    }

    private static readonly Regex LoopHeaderPattern = new Regex(@"^\s*(for|while)\b");
    // サーバーは for の各周で、for の行に「i = 0」のようなログを出す
    private static readonly Regex LoopVariableLogPattern = new Regex(@"^(\w+) = (.+)$");

    /// <summary>
    /// 次に表示するループの周回数を決める。for なら「i = 0」、while なら「2周目」。
    /// ループの外の行に移ったら null（表示を消す）
    /// </summary>
    private string NextLoopCounter(ExecutionLogEntry log, string current, HashSet<int> loopLines, Dictionary<int, int> whileRounds)
    {
        bool isHeader = LoopHeaderPattern.IsMatch(GetCodeLine(log.line));
        if (!isHeader)
            return loopLines.Contains(log.line) ? current : null;

        var variable = LoopVariableLogPattern.Match(log.message);
        if (variable.Success)
            return $"{variable.Groups[1].Value} = {variable.Groups[2].Value}";

        // while は条件が成り立つたびに1周
        if (log.message.StartsWith("while ") && log.message.EndsWith("True"))
        {
            whileRounds[log.line] = whileRounds.GetValueOrDefault(log.line) + 1;
            return $"{whileRounds[log.line]}周目";
        }
        // 「for ... start」やループの終わりのログは、表示を変えない
        return current;
    }

    private static int IndentOf(string line)
    {
        return line.Length - line.TrimStart(' ').Length;
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
            _healthTextDisplay.text = $"{playerHealth}/{balance.maxHealth}";
    }

    /// <summary>
    /// APのUIを更新
    /// </summary>
    private void UpdateAPUI()
    {
        if (_apBarSlider != null)
            _apBarSlider.value = playerAP;
        if (_apTextDisplay != null)
            _apTextDisplay.text = $"{playerAP}/{balance.maxAP}";
    }



    // サーバー（server.py）がゲーム関数の呼び出しとして返すログの形式。
    // print の出力や変数名にコマンド名が含まれても誤動作しないよう、ログ全体が一致したときだけ行動とみなす
    private const string ATTACK_LOG = "Attacking";
    private const string MOVE_FORWARD_LOG = "Moving forward";
    private const string MOVE_BACKWARD_LOG = "Moving backward";
    private static readonly Regex CastLogPattern = new Regex(@"^Casting: (ライトニング|フレイム|フリーズ), (-?\d+)$");
    private static readonly Regex MoveToLogPattern = new Regex(@"^Moving to: (-?\d+)$");
    private static readonly Regex SwapLogPattern = new Regex(@"^Swap: (-?\d+), (-?\d+)$");

    /// <summary>
    /// 実行ログのエリア番号がステージの範囲内か確認し、範囲外があればその説明を、なければ null を返す。
    /// 範囲外のまま実行するとエリア座標の参照で例外が起き、ターンが進まなくなるため実行前に止める
    /// </summary>
    public string FindOutOfRangeAction(List<ExecutionLogEntry> logs)
    {
        foreach (var log in logs)
        {
            var cast = CastLogPattern.Match(log.message);
            if (cast.Success && !IsAreaInRange(cast.Groups[2].Value, _enemyAreas.Count))
                return $"{log.line}行目: Cast のエリア番号は 0〜{_enemyAreas.Count - 1} で指定してください";

            var moveTo = MoveToLogPattern.Match(log.message);
            if (moveTo.Success && !IsAreaInRange(moveTo.Groups[1].Value, _playerAreas.Count))
                return $"{log.line}行目: MoveTo のエリア番号は 0〜{_playerAreas.Count - 1} で指定してください";
        }
        return null;
    }

    private static bool IsAreaInRange(string number, int areaCount)
    {
        return int.TryParse(number, out int area) && area >= 0 && area < areaCount;
    }

    IEnumerator Execution()
    {
        Debug.Log("コマンド実行開始");
        List<ExecutionLogEntry> currentCode = _logsListManager.executionLogs;

        // ループの中に書かれた行動は、すべて AP が割引になる
        HashSet<int> loopLines = FindLoopBodyLines(_logsListManager.pythonCodeInputField.text);
        Debug.Log($"[LoopBonus] ループ内の行: [{string.Join(", ", loopLines)}]");
        // ホイールピッカーに出すループの周回数と、while の周回数の数え
        string loopCounter = null;
        var whileRounds = new Dictionary<int, int>();
        // 何回目の攻撃か（属性フェーズを持つ敵は、2回目以降の攻撃を受ける前に毎回形態変化する）
        int attackCount = 0;

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
            loopCounter = NextLoopCounter(currentCode[i], loopCounter, loopLines, whileRounds);
            _codeLineHighlighter.SetLoopCounter(loopCounter);

            string code = currentCode[i].message;
            bool isLoopBonus = loopLines.Contains(currentCode[i].line);
            var cast = CastLogPattern.Match(code);
            var moveTo = MoveToLogPattern.Match(code);
            var swap = SwapLogPattern.Match(code);
            bool isAttack = code == ATTACK_LOG;

            // アクションを伴う行のPythonコードを頭上に表示し、行動回数として数える
            if (IsActionLog(code))
            {
                ShowExecutionLabel(GetCodeLine(currentCode[i].line));
                _turnController.record.actions++;
            }

            // 攻撃を受ける前の形態変化。1回目の属性はフェーズ開始時に変化済み。
            // Python 側も Cast / Attack を呼ぶたびに同じ並びで enemy.element を進めている（LogsManager が注入）。
            // とどめの判定は変化後の属性で行うので、この後に置く
            if (cast.Success || isAttack)
            {
                if (attackCount > 0)
                    yield return _enemyManager.ChangeElementsBeforeAttack(attackCount);
                attackCount++;
            }

            // とどめの一撃: 戦闘UIを画面外へ退かせ、この行動全体をスローモーションで見せる
            bool isFinishingBlow = IsFinishingBlow(cast, isAttack, isLoopBonus);
            if (isFinishingBlow)
            {
                _turnController.SlideOutBattleUI();
                Time.timeScale = 1f / FINISHING_BLOW_SLOWDOWN;
            }

            // 魔法（エリア番号は実行前に FindOutOfRangeAction で検証済み）
            if (cast.Success)
            {
                int num = int.Parse(cast.Groups[2].Value);
                switch (cast.Groups[1].Value)
                {
                    case "ライトニング": yield return StartCoroutine(CastLightning(num, isLoopBonus)); break;
                    case "フレイム": yield return StartCoroutine(CastFlame(num, isLoopBonus)); break;
                    case "フリーズ": yield return StartCoroutine(CastFreeze(num, isLoopBonus)); break;
                }
            }

            if (isAttack)
                yield return StartCoroutine(Attack(isLoopBonus));

            // 移動コマンド（playerCurrentPos は各移動処理の中で更新される）
            if (moveTo.Success)
                yield return StartCoroutine(MoveTo(int.Parse(moveTo.Groups[1].Value)));

            if (code == MOVE_BACKWARD_LOG)
                yield return StartCoroutine(MoveBackward());

            if (code == MOVE_FORWARD_LOG)
                yield return StartCoroutine(MoveForward());

            // Swap コマンド（SortBossManager または CoreBossManager がある場合のみ有効）
            if (swap.Success)
            {
                int swapJ = int.Parse(swap.Groups[1].Value);
                int swapK = int.Parse(swap.Groups[2].Value);

                var sortBoss = SortBossManager.Instance;
                if (sortBoss != null)
                    yield return StartCoroutine(sortBoss.SwapGuards(swapJ, swapK));

                var coreBoss = CoreBossManager.Instance;
                if (coreBoss != null)
                    yield return StartCoroutine(coreBoss.SwapTotems(swapJ, swapK));
            }

            // 決着がついたら残りの行動は行わない
            if (isFinishingBlow)
            {
                Time.timeScale = 1f;
                break;
            }

            Debug.Log("コマンド実行完了");

            yield return new WaitForSeconds(0.1f);
        }
        // ハイライト解除・ラベル非表示
        if (_codeLineHighlighter != null)
            _codeLineHighlighter.ClearHighlight();
        HideExecutionLabel();

        Debug.Log("全コマンド実行完了");
        _turnController.isPlayerActionComplete = true;
        Debug.Log("Player Action Complete");
    }


    /// <summary>
    /// この行動で敵を倒しきり、勝利が決まるか（AP不足で発動しない場合は false）
    /// </summary>
    private bool IsFinishingBlow(Match cast, bool isAttack, bool halfCost)
    {
        if (cast.Success)
        {
            string spell = cast.Groups[1].Value;
            int baseCost = balance.SpellCost(spell);
            DamageType type = spell switch { "フレイム" => DamageType.Flame, "フリーズ" => DamageType.Ice, _ => DamageType.None };
            int cost = halfCost ? balance.LoopCost(baseCost) : baseCost;
            return playerAP >= cost && DefeatsLastEnemy(int.Parse(cast.Groups[2].Value), balance.spellDamage, type);
        }
        if (isAttack)
            return playerAP >= (halfCost ? balance.LoopCost(balance.attackCost) : balance.attackCost) && DefeatsLastEnemy(playerCurrentPos, balance.attackDamage, DamageType.None);
        return false;
    }

    private bool DefeatsLastEnemy(int areaNum, int damage, DamageType type)
    {
        var defeated = _enemyManager.GetEnemiesInArea(areaNum).FindAll(e => e.IsLethal(damage, type));
        return defeated.Count > 0 && _turnController.WouldWinAfter(defeated);
    }

    /// <summary>
    /// AP が足りない行動は発動しない。黙って飛ばすと「コードが無視された」としか見えないので、
    /// 頭上の文字・実行中の行・AP ゲージを赤く光らせて、少し間を置いてから次へ進む
    /// </summary>
    private IEnumerator NotEnoughAP()
    {
        DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, transform.position, "AP不足！", notEnoughAPColor);
        _codeLineHighlighter.FlashError(notEnoughAPColor);
        FlashAPGauge();
        yield return new WaitForSeconds(notEnoughAPPause);
    }

    /// <summary>
    /// AP ゲージの塗りを赤く2回点滅させ、元の色に戻す
    /// </summary>
    public void FlashAPGauge()
    {
        Image fill = _apBarSlider.fillRect.GetComponent<Image>();
        fill.DOKill(true);
        Color normal = fill.color;
        fill.DOColor(notEnoughAPColor, notEnoughAPPause / 4f)
            .SetLoops(4, LoopType.Yoyo)
            .OnComplete(() => fill.color = normal);
    }

    /// <summary>
    /// 実行する前に、今の AP でコードの行動を順にこなせるかを数える。
    /// 最初に AP が足りなくなる行動を返し、何 AP 足りないかを shortBy に入れる（足りるなら null）。
    /// 敵を倒しきった時点で実行は終わるが、そこまでは見ていないので、倒した後の行動も数えに入る
    /// </summary>
    public ExecutionLogEntry FindAPShortage(List<ExecutionLogEntry> logs, string code, out int shortBy)
    {
        HashSet<int> loopLines = FindLoopBodyLines(code);
        int ap = playerAP;
        foreach (var log in logs)
        {
            int cost = ActionCost(log.message);
            if (loopLines.Contains(log.line))
                cost = balance.LoopCost(cost);
            if (ap < cost)
            {
                shortBy = cost - ap;
                return log;
            }
            ap -= cost;
        }
        shortBy = 0;
        return null;
    }

    // AP を使わない行動（移動など）は 0
    private int ActionCost(string logMessage)
    {
        var cast = CastLogPattern.Match(logMessage);
        if (cast.Success) return balance.SpellCost(cast.Groups[1].Value);
        return logMessage == ATTACK_LOG ? balance.attackCost : 0;
    }

    IEnumerator CastLightning(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? balance.LoopCost(balance.lightningCost) : balance.lightningCost;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でライトニングを発動できない");
            yield return NotEnoughAP();
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] ライトニング AP半減: {balance.lightningCost} → {cost}");
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
            enemy.GetDamaged(balance.spellDamage);
        }

        yield return new WaitForSeconds(lightningEffectDuration);
        lightning.SetActive(false);
    }

    IEnumerator CastFlame(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? balance.LoopCost(balance.flameCost) : balance.flameCost;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でフレイムを発動できない");
            yield return NotEnoughAP();
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] フレイム AP半減: {balance.flameCost} → {cost}");
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
            enemy.GetDamaged(balance.spellDamage, DamageType.Flame);
        }

        yield return new WaitForSeconds(flameEffectDuration);
        flame.SetActive(false);
    }

    IEnumerator CastFreeze(int areaNum, bool halfCost = false)
    {
        int cost = halfCost ? balance.LoopCost(balance.freezeCost) : balance.freezeCost;
        if (playerAP < cost)
        {
            Debug.Log("AP不足でフリーズを発動できない");
            yield return NotEnoughAP();
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] フリーズ AP半減: {balance.freezeCost} → {cost}");
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
            enemy.GetDamaged(balance.spellDamage, DamageType.Ice);
        }

        yield return new WaitForSeconds(freezeEffectDuration);
        freeze.SetActive(false);
    }

    /// <summary>
    /// 近接攻撃: AP を消費して同エリアの敵にダメージを与える（ループの中なら AP 割引）
    /// </summary>
    IEnumerator Attack(bool halfCost = false)
    {
        int cost = halfCost ? balance.LoopCost(balance.attackCost) : balance.attackCost;
        if (playerAP < cost)
        {
            Debug.Log("AP不足で攻撃できない");
            yield return NotEnoughAP();
            yield break;
        }

        if (halfCost)
        {
            Debug.Log($"[LoopBonus] 攻撃 AP半減: {balance.attackCost} → {cost}");
            DamageIndicator.SpawnTextFromPrefab(apReductionIndicatorPrefab, transform.position, "AP軽減", apReductionColor);
        }
        playerAPVary(cost);

        // Attackアニメーション発火 (action2)
        AudioManager.Instance?.PlaySE(meleeSE);
        TriggerAttackAnimation();
        yield return new WaitForSeconds(attackAnimationDuration);
        CameraShake.Shake();

        // 同エリアの全敵にダメージ
        var targets = _enemyManager.GetEnemiesInArea(playerCurrentPos);
        foreach (var enemy in targets)
        {
            enemy.GetDamaged(balance.attackDamage);
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
        yield return WalkTo(_playerAreas[position]);

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
        yield return WalkTo(_playerAreas[targetPos]);

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
        yield return WalkTo(_playerAreas[targetPos]);

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
    /// 一定速度で指定座標へ歩く（加減速すると歩行アニメーションと足運びがずれるため等速）
    /// </summary>
    private IEnumerator WalkTo(Vector2 area)
    {
        return CharacterMotion.WalkTo(transform, new Vector3(area.x, area.y, transform.position.z), WALK_SPEED);
    }

    /// <summary>
    /// コード入力中のオーブ召喚アニメーションを開始
    /// </summary>
    public void StartOrbAnimation()
    {
        // 入力していない間に積まれた解除トリガーが残っていると、召喚直後に解除されてしまうためリセット
        playerAnimator.ResetTrigger(orbDismissAnimTrigger);
        playerAnimator.SetTrigger(orbSummonAnimTrigger);
    }

    /// <summary>
    /// コード入力終了時のオーブ解除アニメーションを開始
    /// </summary>
    public void EndOrbAnimation()
    {
        // プレイ終了・シーン遷移時は、入力欄のフォーカス解除が Animator の破棄後に届く
        if (playerAnimator == null) return;
        playerAnimator.ResetTrigger(orbSummonAnimTrigger);
        playerAnimator.SetTrigger(orbDismissAnimTrigger);
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
        // 当たった瞬間に止めて揺らし、止まり終わってからのけぞる（揺れとのけぞりの位置がずれないように）
        HitStop.Instance.Play(damageAmount, transform, damageAmount >= playerHealth);
        yield return HitStop.Instance.WaitUntilResumed();

        playerHealth = Mathf.Max(0, playerHealth - damageAmount);
        UpdateHealthUI();
        _turnController.record.damageTaken += damageAmount;

        // ダメージインジケーター表示
        DamageIndicator.SpawnFromPrefab(damageIndicatorPrefab, transform.position, damageAmount, damageIndicatorColor);
        HitEffect.Spawn(hitEffectPrefab, _spriteRenderer);

        // 白フラッシュ効果
        if (_flashEffect != null)
            _flashEffect.Flash();

        // ノックバック（左へ0.5移動）して元の位置に戻る
        Vector3 originalPos = transform.position;
        yield return CharacterMotion.MoveOver(transform, originalPos + Vector3.left * 0.5f, 0.15f, CharacterMotion.EaseOut);
        yield return CharacterMotion.MoveOver(transform, originalPos, 0.15f, CharacterMotion.EaseIn);

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
        return logMessage == ATTACK_LOG
            || logMessage == MOVE_FORWARD_LOG
            || logMessage == MOVE_BACKWARD_LOG
            || CastLogPattern.IsMatch(logMessage)
            || MoveToLogPattern.IsMatch(logMessage)
            || SwapLogPattern.IsMatch(logMessage);
    }
}
