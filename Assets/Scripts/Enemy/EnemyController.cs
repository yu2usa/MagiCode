using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    // デフォルト値（EnemyData未設定時のフォールバック）
    private const int DEFAULT_ATTACK_DAMAGE = 3;
    private const int DEFAULT_MAX_HEALTH = 10;
    private const float MOVE_DURATION = 0.8f;
    private const float DEFENSE_DELAY = 0.2f;
    private const float PRE_MOVE_DELAY = 1.0f;
    private const float ACTION_TIMEOUT = 10.0f;

    // DOTween識別用ID
    public const string ENEMY_TWEEN_ID = "EnemyTween";

    [Header("Enemy Data (ScriptableObject)")]
    public EnemyData enemyData;

    [Header("EnemyControlMonitor")]
    public int enemyIndex;
    public string enemyType;
    public List<string> enemyList;

    [Header("Stage Config")]
    [Tooltip("ステージ設定（未設定時は従来のareaPosを使用）")]
    public StageConfig stageConfig;

    [Header("EnemyStatus")]
    [Tooltip("Legacy: StageConfig未設定時のみ使用")]
    public List<Vector2> areaPos;
    public int enemyCurrentPos;
    public int enemyHealth;
    // 敵本体の属性（EnemyData.element から初期化。コードから enemy.element で参照可能）
    public DamageType element = DamageType.None;

    // 属性フェーズメカニクス（EnemyData.elementPhases が設定されている場合に有効）
    private List<DamageType> _elementPhases = new List<DamageType>();

    // 属性フェーズが有効かどうか（EnemyManager から参照）
    public bool HasElementPhases => _elementPhases.Count > 0;

    // StageConfig から取得したエリア位置
    private List<Vector2> _enemyAreas;

    // 現在有効なエリア位置（StageConfig優先、フォールバックでareaPos）
    public List<Vector2> EnemyAreas => _enemyAreas ?? areaPos;

    [Header("UI")]
    [Tooltip("HPバーのPrefab（自動生成用）")]
    public GameObject healthBarPrefab;
    [Tooltip("HPバーを配置するCanvas（未設定時は自動検索）")]
    public Canvas canvas;
    [Tooltip("HPバーのY軸オフセット（追従モード時）")]
    public float healthBarOffsetY = -10f;
    [Tooltip("ダメージ表示のPrefab（未設定時は自動生成）")]
    public GameObject damageIndicatorPrefab;
    [Tooltip("属性ガード時の「GUARD」テキスト表示Prefab（未設定時はスキップ）")]
    public GameObject guardIndicatorPrefab;
    [Tooltip("HP全回復時のテキスト表示Prefab（damageIndicatorPrefabと同じものを設定可）")]
    public GameObject healIndicatorPrefab;
    [Tooltip("HP全回復インジケーターの色")]
    public Color healIndicatorColor = new Color(0.3f, 1f, 0.4f);

    [Header("HP Bar Display Mode")]
    [Tooltip("true=画面上部固定, false=敵に追従")]
    public bool useFixedPosition = true;
    [Tooltip("画面上部からのオフセット")]
    public float fixedPositionOffsetY = -50f;
    [Tooltip("HPバー間の縦間隔")]
    public float healthBarSpacing = 60f;

    [Header("HP Bar Size")]
    [Tooltip("HPバーの幅（0でPrefabのデフォルト幅を使用）")]
    public float healthBarWidth = 0f;
    [Tooltip("HPバーの高さ（0でPrefabのデフォルト高さを使用）")]
    public float healthBarHeight = 0f;
    [Tooltip("HP数値テキストのフォントサイズ")]
    public float healthBarFontSize = 20f;
    [Tooltip("敵名テキストのフォントサイズ（固定位置モード時）")]
    public float enemyNameFontSize = 20f;

    // 生成されたHPバーインスタンス
    private GameObject _healthBarInstance;
    private TextMeshProUGUI _healthTextDisplay;
    private TextMeshProUGUI _enemyNameDisplay;

    [Header("Action Label")]
    [Tooltip("行動ラベルのスタイル設定（ActionLabelSettings アセット）")]
    [SerializeField] private ActionLabelSettings actionLabelSettings;

    [Header("References")]
    public GameObject turnController;
    public GameObject player;

    [Header("EnemySkills")]
    public GameObject enemySlash;
    public GameObject enemyShockwave;

    // チャージ倍率（ChargeAction用）
    private float _chargeMultiplier = 1.0f;

    // オーバーキル判定用：受けた累積ダメージ（punishOverkill が有効な敵のみ使用）
    private int _totalDamageReceived = 0;

    // ソートボスステージ用の受け取りダメージ倍率（SortBossManagerが設定）
    public float incomingDamageMultiplier = 1.0f;

    // KeyGolemメカニクス等でダメージを無効化するフラグ（true時は0ダメージ＋コールバック）
    public bool blockIncomingDamage = false;
    // blockIncomingDamage有効時にヒットが発生したら呼ばれるコールバック
    public System.Action onHitWhileBlocked;

    // CoreBossメカニクス用: 正の値を設定すると与えるダメージを固定値に上書き（0 = 無効）
    public int forceDamage = 0;

    // CoreTotem用の割り当て番号（EnemyData.assignedNumber から初期化、StageConfigで上書き可能）
    public int assignedNumber = 0;

    // 防御フェーズ中に事前選択した次の攻撃アクション（予告と実行を一致させるため）
    private EnemyAction _pendingAttackAction;

    // アクション完了フラグ（EnemyManager用）
    public bool IsActionComplete { get; private set; }

    // 最後に受けたダメージの属性（外部から検出用）
    public DamageType LastDamageType { get; private set; }

    // ステージ単位のHP上書き（StageConfig.EnemySpawnData.overrideMaxHealth から設定）
    private int _overrideMaxHealth = 0;

    // 最大HP（StageConfig 上書きがあればそれを優先）
    public int MaxHealth => _overrideMaxHealth > 0 ? _overrideMaxHealth
                          : enemyData != null ? enemyData.maxHealth
                          : DEFAULT_MAX_HEALTH;

    // キャッシュされたコンポーネント
    private RectTransform _healthBarRect;
    private Slider _healthBarSlider;
    private Animator _slashAnimator;
    private Animator _enemyAnimator;
    private SpriteRenderer _spriteRenderer;
    private SpriteFlashEffect _flashEffect;
    private PlayerController _playerController;
    private TurnController _turnController;
    private EnemyActionContext _actionContext;
    private TextMeshPro _actionLabel;

    // 敵キャラのAnimatorへの公開アクセス
    public Animator EnemyAnimator => _enemyAnimator;

    void Awake()
    {
        // EnemyManager の検出より先に enemyHealth を初期化
        if (enemyData != null)
        {
            enemyHealth = enemyData.maxHealth;
            enemyType = enemyData.enemyName;
            element = enemyData.element;
        }
        else
        {
            enemyHealth = DEFAULT_MAX_HEALTH;
            enemyType = gameObject.name;
        }
    }

    /// <summary>
    /// シーン内オブジェクトの参照を自動検索
    /// Prefabからはシーン内オブジェクトを参照できないため、実行時に検索する
    /// </summary>
    private void AutoFindSceneReferences()
    {
        // TurnController の自動検索
        if (turnController == null)
        {
            _turnController = FindObjectOfType<TurnController>();
            if (_turnController != null)
                turnController = _turnController.gameObject;
        }
        else
        {
            _turnController = turnController.GetComponent<TurnController>();
        }

        // Player の自動検索
        if (player == null)
        {
            // 複数のPlayerControllerがないか確認
            var allPlayers = FindObjectsOfType<PlayerController>();
            if (allPlayers.Length > 1)
            {
                Debug.LogWarning($"[{gameObject.name}] 複数のPlayerControllerが検出されました: {allPlayers.Length}体");
                foreach (var p in allPlayers)
                {
                    Debug.LogWarning($"  - {p.name} (InstanceID={p.GetInstanceID()}, pos={p.playerCurrentPos})");
                }
            }

            _playerController = FindObjectOfType<PlayerController>();
            if (_playerController != null)
            {
                player = _playerController.gameObject;
                Debug.Log($"[{gameObject.name}] PlayerController検出: {_playerController.name} (InstanceID={_playerController.GetInstanceID()})");
            }
        }
        else
        {
            _playerController = player.GetComponent<PlayerController>();
        }

        // Canvas の自動検索（"Canvas"という名前のものを優先）
        if (canvas == null)
        {
            // まず"Canvas"という名前のオブジェクトを探す
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }

            // 見つからなければ任意のCanvasを使用
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
        }

        // StageConfig の自動検索（TurnControllerから取得）
        if (stageConfig == null && _turnController != null)
        {
            stageConfig = _turnController.stageConfig;
        }
    }

    void Start()
    {
        Debug.Log($"[EnemyController] {gameObject.name} Start() 開始");

        // シーン内オブジェクトの自動検索（Prefabから参照できないため）
        AutoFindSceneReferences();

        // 必須参照のチェック
        if (enemySlash == null)
            Debug.LogError($"[{gameObject.name}] enemySlash が未設定です");
        if (_playerController == null)
            Debug.LogError($"[{gameObject.name}] PlayerController が見つかりません");
        if (_turnController == null)
            Debug.LogError($"[{gameObject.name}] TurnController が見つかりません");
        if (healthBarPrefab == null)
            Debug.LogWarning($"[{gameObject.name}] healthBarPrefab が未設定です");

        // コンポーネントキャッシュ
        if (enemySlash != null)
            _slashAnimator = enemySlash.GetComponent<Animator>();
        _enemyAnimator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // フラッシュ効果コンポーネントを取得または追加
        _flashEffect = GetComponent<SpriteFlashEffect>();
        if (_flashEffect == null)
            _flashEffect = gameObject.AddComponent<SpriteFlashEffect>();

        // HPバーの生成（isInvincible が有効な場合はスキップ）
        if (enemyData == null || !enemyData.isInvincible)
            InitializeHealthBar();

        Debug.Log($"[EnemyController] {gameObject.name} HPバー生成完了, Canvas={canvas != null}");

        // EnemyDataがあればそこから初期化
        if (enemyData != null)
        {
            enemyHealth = enemyData.maxHealth;
            enemyType = enemyData.enemyName;
            element = enemyData.element;
            assignedNumber = enemyData.assignedNumber;

            // 属性フェーズリストが設定されていれば先頭フェーズで element を上書き
            if (enemyData.elementPhases.Count > 0)
            {
                _elementPhases = enemyData.elementPhases;
                element = _elementPhases[0];
            }

            // スプライト適用
            if (enemyData.enemySprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = enemyData.enemySprite;
            }

            // アニメーターコントローラー適用
            if (enemyData.animatorController != null && _enemyAnimator != null)
            {
                _enemyAnimator.runtimeAnimatorController = enemyData.animatorController;
            }

            // スケール適用（元の左右反転を保持）
            if (enemyData.scale != Vector3.zero)
            {
                float originalXSign = Mathf.Sign(transform.localScale.x);
                transform.localScale = new Vector3(
                    enemyData.scale.x * originalXSign,
                    enemyData.scale.y,
                    enemyData.scale.z
                );
            }

            // 斬撃エフェクト: Prefab アセット参照（シーン外）の場合は Instantiate してシーンに配置
            if (enemySlash != null && !enemySlash.scene.IsValid())
            {
                enemySlash = Instantiate(enemySlash);
                enemySlash.SetActive(false);
                _slashAnimator = enemySlash.GetComponent<Animator>();
            }
            // enemySlash 未設定かつ EnemyData に Prefab がある場合も同様に生成
            else if (enemySlash == null && enemyData.slashEffectPrefab != null)
            {
                enemySlash = Instantiate(enemyData.slashEffectPrefab);
                enemySlash.SetActive(false);
                _slashAnimator = enemySlash.GetComponent<Animator>();
            }

            // Shockwave エフェクト: Prefab アセット参照の場合は Instantiate
            if (enemyShockwave != null && !enemyShockwave.scene.IsValid())
            {
                enemyShockwave = Instantiate(enemyShockwave);
                enemyShockwave.SetActive(false);
            }

            // KeyGolemメカニクスを自動アタッチ（EnemyDataのフラグが有効な場合）
            if (enemyData.useKeyGolemMechanic && GetComponent<KeyGolemMechanic>() == null)
            {
                gameObject.AddComponent<KeyGolemMechanic>();
                Debug.Log($"[{gameObject.name}] KeyGolemMechanic を自動アタッチしました");
            }
        }
        else
        {
            // フォールバック: enemyData 未設定時のデフォルト値
            enemyHealth = DEFAULT_MAX_HEALTH;
            if (enemyList != null && enemyList.Count > enemyIndex && enemyIndex >= 0)
            {
                enemyType = enemyList[enemyIndex];
            }
            else
            {
                enemyType = gameObject.name;
            }
        }

        // StageConfig からエリア位置を取得（未設定時は従来のリストを使用）
        if (stageConfig != null)
        {
            _enemyAreas = stageConfig.enemyAreaPositions;
            // 敵のインデックスに基づいて初期エリアを取得
            int myIndex = GetEnemyIndex();
            enemyCurrentPos = stageConfig.GetEnemyStartArea(myIndex);

            // HP上書きが設定されている場合は適用
            if (stageConfig.enemySpawns != null && myIndex < stageConfig.enemySpawns.Count)
            {
                int overrideHp = stageConfig.enemySpawns[myIndex].overrideMaxHealth;
                if (overrideHp > 0)
                {
                    _overrideMaxHealth = overrideHp;
                    enemyHealth = _overrideMaxHealth;
                    Debug.Log($"[{gameObject.name}] HP上書き適用: {overrideHp}");
                }

                // CoreTotem用: StageConfigの割り当て番号上書き（0以外のとき有効）
                int overrideNum = stageConfig.enemySpawns[myIndex].overrideAssignedNumber;
                if (overrideNum != 0)
                {
                    assignedNumber = overrideNum;
                    Debug.Log($"[{gameObject.name}] 割り当て番号上書き適用: {overrideNum}");
                }
            }

            Debug.Log($"[{gameObject.name}] StageConfig使用: エリア数={_enemyAreas.Count}, 初期位置={enemyCurrentPos} (index={myIndex})");
        }
        else if (areaPos != null && areaPos.Count > 0)
        {
            _enemyAreas = areaPos;
            Debug.Log($"[{gameObject.name}] Legacy areaPos使用: エリア数={_enemyAreas.Count}, 初期位置={enemyCurrentPos}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] エリア設定がありません！StageConfigまたはareaPosを設定してください");
            _enemyAreas = new List<Vector2>();
            enemyCurrentPos = 2;
        }

        // 初期位置をエリア座標に設定（同じエリアの敵がいる場合はオフセット適用）
        if (_enemyAreas.Count > 0 && enemyCurrentPos < _enemyAreas.Count)
        {
            Vector2 startPos = _enemyAreas[enemyCurrentPos];
            float offsetX = GetSameAreaOffset(enemyCurrentPos);
            float offsetY = GetPositionOffsetY();
            transform.position = new Vector3(startPos.x + offsetX, startPos.y + offsetY, transform.position.z);
            Debug.Log($"[{gameObject.name}] 初期位置設定: ({startPos.x + offsetX}, {startPos.y + offsetY}) offsetX={offsetX}, offsetY={offsetY}");
        }

        // スライダーの範囲設定
        if (_healthBarSlider != null)
        {
            _healthBarSlider.minValue = 0;
            _healthBarSlider.maxValue = MaxHealth;
        }

        // UI初期化
        UpdateHealthUI();

        // 初期状態で左を向く（プレイヤー側）
        FaceLeft();

        // アクションコンテキスト作成
        _actionContext = new EnemyActionContext(this, _playerController, _turnController);

        // 頭上行動ラベルを初期化
        InitializeActionLabel();

        // CoreBossManager へ自己登録（sortRole が CoreTotem / CoreBoss の場合のみ）
        RegisterWithCoreBossManager();

        Debug.Log($"[{gameObject.name}] 初期化完了: WorldPos={transform.position}, enemyCurrentPos={enemyCurrentPos}, HP={enemyHealth}");
    }

    /// <summary>
    /// HPバーをPrefabから生成して初期化
    /// </summary>
    private void InitializeHealthBar()
    {
        if (healthBarPrefab == null || canvas == null)
        {
            Debug.LogWarning($"[{gameObject.name}] HPバーPrefabまたはCanvasが未設定です");
            return;
        }

        // Prefabからインスタンス生成
        _healthBarInstance = Instantiate(healthBarPrefab, canvas.transform);
        _healthBarInstance.name = $"HealthBar_{gameObject.name}";

        // コンポーネント取得
        _healthBarRect = _healthBarInstance.GetComponent<RectTransform>();
        _healthBarSlider = _healthBarInstance.GetComponent<Slider>();

        // テキストコンポーネントを取得（複数ある場合は最初のものをHP表示用）
        var textComponents = _healthBarInstance.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
            _healthTextDisplay = textComponents[0];

        // バーサイズの上書き（0以外が指定された場合のみ適用）
        Vector2 size = _healthBarRect.sizeDelta;
        if (healthBarWidth > 0) size.x = healthBarWidth;
        if (healthBarHeight > 0) size.y = healthBarHeight;
        _healthBarRect.sizeDelta = size;

        // フォントサイズ適用
        if (_healthTextDisplay != null)
            _healthTextDisplay.fontSize = healthBarFontSize;

        // 画面上部固定モードの場合、敵名表示を追加
        if (useFixedPosition)
        {
            SetupFixedPositionUI();
        }
    }

    /// <summary>
    /// 画面上部固定モード用のUI設定
    /// </summary>
    private void SetupFixedPositionUI()
    {
        // 敵名表示用のテキストを生成
        var nameObj = new GameObject("EnemyName");
        nameObj.transform.SetParent(_healthBarInstance.transform, false);

        // RectTransformを先に設定
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.localScale = Vector3.one;

        // TextMeshProUGUIを追加
        _enemyNameDisplay = nameObj.AddComponent<TextMeshProUGUI>();
        _enemyNameDisplay.text = enemyType;
        _enemyNameDisplay.fontSize = enemyNameFontSize;
        _enemyNameDisplay.fontStyle = FontStyles.Bold;
        _enemyNameDisplay.alignment = TextAlignmentOptions.Center;
        _enemyNameDisplay.color = Color.white;
        _enemyNameDisplay.enableAutoSizing = false;

        // 既存のTextMeshProUGUIからフォントをコピー
        if (_healthTextDisplay != null && _healthTextDisplay.font != null)
        {
            _enemyNameDisplay.font = _healthTextDisplay.font;
        }

        // 敵名をHPバーの上に配置
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0, 1f);
        nameRect.sizeDelta = new Vector2(200f, 1f);

        // HPバーを持つ敵の中での順番で位置を決定（無敵敵はスキップ）
        int enemyIndex = GetHealthBarIndex();
        PositionHealthBarAtTop(enemyIndex);
    }

    /// <summary>
    /// EnemyManager内での自分のインデックスを取得
    /// </summary>
    private int GetEnemyIndex()
    {
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) return 0;

        for (int i = 0; i < enemyManager.enemies.Count; i++)
        {
            if (enemyManager.enemies[i] == this)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// HPバーを持つ敵の中での自分の順番を返す（無敵敵はカウントしない）
    /// </summary>
    private int GetHealthBarIndex()
    {
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) return 0;

        int idx = 0;
        foreach (var e in enemyManager.enemies)
        {
            if (e == this) return idx;
            // isInvincible な敵はHPバーを持たないのでカウントしない
            if (e != null && (e.enemyData == null || !e.enemyData.isInvincible))
                idx++;
        }
        return 0;
    }

    /// <summary>
    /// sortRole に基づき CoreBossManager へ自己登録する
    /// StageConfig の EnemySpawnData.sortRole を優先し、None なら EnemyData.sortRole を使用
    /// </summary>
    private void RegisterWithCoreBossManager()
    {
        if (CoreBossManager.Instance == null) return;

        // StageConfig の sortRole を優先
        var role = EnemySortRole.None;
        if (stageConfig != null && stageConfig.enemySpawns != null)
        {
            int myIndex = GetEnemyIndex();
            if (myIndex < stageConfig.enemySpawns.Count)
                role = stageConfig.enemySpawns[myIndex].sortRole;
        }
        if (role == EnemySortRole.None && enemyData != null)
            role = enemyData.sortRole;

        if (role == EnemySortRole.CoreTotem)
            CoreBossManager.Instance.RegisterTotem(this);
        else if (role == EnemySortRole.CoreBoss)
            CoreBossManager.Instance.RegisterBoss(this);
    }

    /// <summary>
    /// HPバーを画面上部に配置（縦並び）
    /// </summary>
    private void PositionHealthBarAtTop(int index)
    {
        if (_healthBarRect == null) return;

        // 縦に並べる：インデックスに応じて下にずらす
        float yPos = fixedPositionOffsetY - (index * healthBarSpacing);

        // アンカーを上部中央に設定
        _healthBarRect.anchorMin = new Vector2(0.5f, 1f);
        _healthBarRect.anchorMax = new Vector2(0.5f, 1f);
        _healthBarRect.pivot = new Vector2(0.5f, 1f);
        _healthBarRect.anchoredPosition = new Vector2(0, yPos);
    }

    /// <summary>
    /// HealthのUIを更新
    /// </summary>
    private void UpdateHealthUI()
    {
        if (_healthBarSlider != null)
            _healthBarSlider.value = enemyHealth;
        if (_healthTextDisplay != null)
            _healthTextDisplay.text = $"{enemyHealth}/{MaxHealth}";
    }

    private void Update()
    {
        // 画面上部固定モードの場合は位置更新不要
        if (useFixedPosition)
            return;

        // HPバーの位置を敵に追従させる（追従モード）
        UpdateHealthBarFollowPosition();
    }

    /// <summary>
    /// HPバーを敵に追従させる（追従モード用）
    /// </summary>
    private void UpdateHealthBarFollowPosition()
    {
        if (_healthBarRect == null || canvas == null)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPoint);
        localPoint.y += healthBarOffsetY;

        _healthBarRect.anchoredPosition = localPoint;
    }

    /// <summary>
    /// カメラ調整後など、HPバーの位置を再計算する（SmoothCameraZoomから呼ばれる）
    /// </summary>
    public void RefreshHealthBarPosition()
    {
        if (_healthBarRect == null) return;
        if (useFixedPosition)
            PositionHealthBarAtTop(GetHealthBarIndex());
        else
            UpdateHealthBarFollowPosition();
    }

    public void HideHealthBar()
    {
        if (_healthBarInstance != null) _healthBarInstance.SetActive(false);
    }

    public void ShowHealthBar()
    {
        if (_healthBarInstance != null) _healthBarInstance.SetActive(true);
    }

    private void OnDestroy()
    {
        // HPバーインスタンスを削除
        if (_healthBarInstance != null)
        {
            Destroy(_healthBarInstance);
        }
    }

    public void GetDamaged(int damageNum, DamageType damageType = DamageType.None)
    {
        LastDamageType = damageType;

        // 無敵フラグが有効（CoreTotem等）：ダメージ・演出を完全スキップ
        if (enemyData != null && enemyData.isInvincible)
            return;

        // 属性フェーズメカニクス: 現在の属性（None以外）と一致しない攻撃はガード
        if (_elementPhases.Count > 0 && element != DamageType.None && damageType != element)
        {
            StartCoroutine(DamagedGuard());
            return;
        }

        StartCoroutine(Damaged(damageNum));
    }

    /// <summary>
    /// 属性不一致によるガード。フラッシュと「GUARD」テキストを表示し、プレイヤーへ反射ダメージを与える。
    /// </summary>
    private IEnumerator DamagedGuard()
    {
        if (_flashEffect != null) _flashEffect.Flash();
        DamageIndicator.SpawnTextFromPrefab(guardIndicatorPrefab, transform.position, "GUARD", Color.cyan);

        // 反射ダメージ: 属性不一致の攻撃はプレイヤーに跳ね返す
        int reflectDamage = enemyData != null ? enemyData.guardReflectDamage : 1;
        if (reflectDamage > 0 && _playerController != null)
        {
            _playerController.ShowReflectDamageIndicator(transform.position);
            _playerController.GetDamaged(reflectDamage);
        }

        yield return null;
    }

    /// <summary>
    /// 属性プールからランダムに属性を選択し、形態変化演出を再生する。
    /// EnemyManager と PlayerController から yield return で待機可能。
    /// </summary>
    public IEnumerator RandomizeElement()
    {
        if (_elementPhases.Count == 0) yield break;
        element = _elementPhases[Random.Range(0, _elementPhases.Count)];
        yield return StartCoroutine(PhaseChangeEffect());
    }

    /// <summary>
    /// 形態変化演出: 徐々に白く → 徐々に元の色に戻る + 頭上に属性名を表示
    /// </summary>
    private IEnumerator PhaseChangeEffect()
    {
        const float flashDuration = 0.6f;

        // 属性に対応する Animator トリガーを発火（見た目の変化）
        TriggerElementAnimation();

        // 徐々に白く → 徐々に元の色に戻る（SpriteFlashShader で補間）
        if (_flashEffect != null)
            _flashEffect.Flash(Color.white, flashDuration);

        // 頭上に変化後の属性を表示
        ShowActionLabel($"element: {element}");

        // フラッシュ完了を待ってからラベルを非表示
        yield return new WaitForSeconds(flashDuration);
        HideActionLabel();
    }

    /// <summary>
    /// 現在の element に対応する Animator トリガーを発火する
    /// EnemyData.elementAnimatorTriggers のマッピングを参照
    /// </summary>
    private void TriggerElementAnimation()
    {
        if (_enemyAnimator == null || enemyData == null) return;
        foreach (var mapping in enemyData.elementAnimatorTriggers)
        {
            if (mapping.element == element && !string.IsNullOrEmpty(mapping.triggerName))
            {
                // 他の属性トリガーが残留していると即座に逆遷移するため、先にリセット
                foreach (var m in enemyData.elementAnimatorTriggers)
                {
                    if (!string.IsNullOrEmpty(m.triggerName))
                        _enemyAnimator.ResetTrigger(m.triggerName);
                }
                _enemyAnimator.SetTrigger(mapping.triggerName);
                return;
            }
        }
    }

    public void AttackRoutine()
    {
        // ルーチン開始時にフラグをリセット（前のフェーズで残っている可能性があるため）
        IsActionComplete = false;
        StartCoroutine(ExecuteAttackPhase());
    }

    public void DefenseRoutine()
    {
        // ルーチン開始時にフラグをリセット（前のフェーズで残っている可能性があるため）
        IsActionComplete = false;
        StartCoroutine(ExecuteDefensePhase());
    }

    /// <summary>
    /// チャージ倍率を設定（ChargeAction用）
    /// </summary>
    public void SetChargeMultiplier(float multiplier)
    {
        _chargeMultiplier = multiplier;
    }

    /// <summary>
    /// チャージ倍率を消費して取得
    /// </summary>
    public float ConsumeChargeMultiplier()
    {
        float mult = _chargeMultiplier;
        _chargeMultiplier = 1.0f;
        return mult;
    }

    /// <summary>
    /// アクション完了フラグをリセット（EnemyManager用）
    /// </summary>
    public void ResetActionComplete()
    {
        IsActionComplete = false;
    }

    IEnumerator ExecuteAttackPhase()
    {
        Debug.Log($"[{gameObject.name}] 攻撃フェーズ開始");

        // CoreTotemはSwap()でのみ位置変更するため攻撃フェーズをスキップ
        if (CoreBossManager.Instance != null && CoreBossManager.Instance.coreTotems.Contains(this))
        {
            Debug.Log($"[{gameObject.name}] CoreTotem: 攻撃フェーズをスキップ");
            IsActionComplete = true;
            yield break;
        }

        bool actionExecuted = false;

        // PrepareNextAttack()で事前選択されたアクションを優先して使用
        EnemyAction action = _pendingAttackAction;
        _pendingAttackAction = null;

        // 事前選択がなければ通常選択
        if (action == null && enemyData != null && enemyData.attackPhaseActions.Count > 0)
            action = enemyData.SelectAction(enemyData.attackPhaseActions, _actionContext);

        if (action != null)
        {
            Debug.Log($"[{gameObject.name}] 攻撃アクション実行: {action.actionName}");
            ShowActionLabel(BuildActionLabelText(action));
            yield return action.Execute(_actionContext);
            HideActionLabel();
            actionExecuted = true;
        }

        // EnemyDataが未設定の場合のみフォールバック動作を実行
        // EnemyDataがあって攻撃フェーズ行動が空の場合は意図的に何もしない
        if (!actionExecuted && enemyData == null)
        {
            Debug.Log($"[{gameObject.name}] フォールバック攻撃動作を実行");
            ShowActionLabel($"エリア{enemyCurrentPos}で{DEFAULT_ATTACK_DAMAGE}の攻撃");
            yield return StartCoroutine(LegacyAttack());
            HideActionLabel();
        }

        Debug.Log($"[{gameObject.name}] 攻撃フェーズ完了");
        IsActionComplete = true;
    }

    /// <summary>
    /// 次の攻撃フェーズのアクションを事前選択し、予告メッセージを返す
    /// EnemyDefense中に呼び出し、EnemyAttackで_pendingAttackActionを使用する
    /// </summary>
    public string PrepareNextAttack()
    {
        if (enemyData == null || enemyData.attackPhaseActions.Count == 0)
            return "";

        _pendingAttackAction = enemyData.SelectAction(enemyData.attackPhaseActions, _actionContext);
        if (_pendingAttackAction == null)
            return "";

        // MagicAttackAction でプレイヤー追尾の場合、予告時点のプレイヤー位置を確定して保持
        if (_pendingAttackAction is MagicAttackAction pendingMagic && pendingMagic.targetArea < 0)
            _actionContext.LockedAttackArea = _actionContext.PlayerPosition;
        else
            _actionContext.LockedAttackArea = -1;

        string actionDesc = BuildAttackTelegraph(_pendingAttackAction);
        if (string.IsNullOrEmpty(actionDesc))
            return "";

        return $"{enemyType}が{actionDesc}をします";
    }

    /// <summary>
    /// PrepareNextAttack() で確定した次回攻撃エリアをリストで返す。
    /// 警告マーク表示など TurnController 側から防御フェーズに呼ぶ。
    /// </summary>
    public List<int> GetPendingAttackAreas()
    {
        if (_pendingAttackAction == null) return new List<int>();

        if (_pendingAttackAction is AttackAction atk)
            return new List<int> { atk.targetArea >= 0 ? atk.targetArea : enemyCurrentPos };

        if (_pendingAttackAction is MultiAttackAction multi)
            return new List<int>(multi.targetAreas);

        if (_pendingAttackAction is MagicAttackAction magic)
        {
            int area = magic.targetArea >= 0 ? magic.targetArea : _actionContext.LockedAttackArea;
            return new List<int> { area };
        }

        return new List<int>();
    }

    /// <summary>
    /// アクション内容から予告文の動詞部分を生成する
    /// AttackAction / MultiAttackAction / MagicAttackAction に対応。それ以外は "" を返す
    /// </summary>
    private string BuildAttackTelegraph(EnemyAction action)
    {
        if (action is AttackAction atk)
        {
            int area = atk.targetArea >= 0 ? atk.targetArea : enemyCurrentPos;
            return $"エリア{area}で{atk.damage}ダメージの攻撃";
        }

        if (action is MultiAttackAction multi)
        {
            string areas = string.Join(", ", multi.targetAreas);
            return $"エリア{areas}で{multi.damage}ダメージの攻撃";
        }

        if (action is MagicAttackAction magic)
        {
            // targetArea == -1 の場合は PrepareNextAttack() で確定済みの LockedAttackArea を使用
            int area = magic.targetArea >= 0 ? magic.targetArea : _actionContext.LockedAttackArea;
            return $"エリア{area}へ{magic.damageType}魔法攻撃({magic.damage}ダメージ)";
        }

        return "";
    }

    IEnumerator ExecuteDefensePhase()
    {
        Debug.Log($"[{gameObject.name}] 防御フェーズ開始");

        // ガードはバブルソートで並び替えられる専用の敵のため、防御フェーズでは動かない
        if (SortBossManager.Instance != null && SortBossManager.Instance.guards.Contains(this))
        {
            Debug.Log($"[{gameObject.name}] ガード: 防御フェーズをスキップ");
            IsActionComplete = true;
            yield break;
        }

        // CoreTotemはSwap()でのみ位置変更するため防御フェーズをスキップ
        if (CoreBossManager.Instance != null && CoreBossManager.Instance.coreTotems.Contains(this))
        {
            Debug.Log($"[{gameObject.name}] CoreTotem: 防御フェーズをスキップ");
            IsActionComplete = true;
            yield break;
        }

        bool actionExecuted = false;

        if (enemyData != null && enemyData.defensePhaseActions.Count > 0)
        {
            var action = enemyData.SelectAction(enemyData.defensePhaseActions, _actionContext);
            if (action != null)
            {
                Debug.Log($"[{gameObject.name}] 防御アクション実行: {action.actionName}");
                ShowActionLabel(BuildActionLabelText(action));
                yield return action.Execute(_actionContext);
                HideActionLabel();
                actionExecuted = true;
            }
        }

        // EnemyDataが未設定の場合のみフォールバック動作を実行
        // EnemyDataがあって防御フェーズ行動が空の場合は意図的に何もしない
        if (!actionExecuted && enemyData == null)
        {
            Debug.Log($"[{gameObject.name}] フォールバック防御動作を実行");
            ShowActionLabel("移動");
            yield return StartCoroutine(LegacyDefense());
            HideActionLabel();
        }

        Debug.Log($"[{gameObject.name}] 防御フェーズ完了");
        IsActionComplete = true;
    }

    /// <summary>
    /// プレイヤー攻撃フェーズ完了後にHPを全回復する（EnemyManager から呼び出す）。
    /// regenHealthOnTurnEnd が有効な場合のみ実行。ヘルスバーはスムーズにアニメーションする。
    /// </summary>
    public IEnumerator RegenHealthOnTurnEnd()
    {
        if (enemyHealth <= 0) yield break;
        if (enemyHealth >= MaxHealth) yield break;

        if (healIndicatorPrefab != null)
            DamageIndicator.SpawnTextFromPrefab(healIndicatorPrefab, transform.position, "HP全回復", healIndicatorColor);

        enemyHealth = MaxHealth;

        // ヘルスバーをスムーズにアニメーション
        if (_healthBarSlider != null)
            yield return _healthBarSlider.DOValue(MaxHealth, 0.6f).SetEase(Ease.OutQuad).WaitForCompletion();

        // テキストを最終値で更新
        if (_healthTextDisplay != null)
            _healthTextDisplay.text = $"{enemyHealth}/{MaxHealth}";

        Debug.Log($"[{gameObject.name}] HP全回復: {enemyHealth}/{MaxHealth}");
    }

    /// <summary>
    /// タイムアウト付きコルーチン実行
    /// </summary>
    IEnumerator ExecuteWithTimeout(IEnumerator coroutine, float timeout)
    {
        float startTime = Time.time;
        var routine = StartCoroutine(coroutine);

        while (routine != null && Time.time - startTime < timeout)
        {
            yield return null;
        }

        if (Time.time - startTime >= timeout)
        {
            Debug.LogWarning($"[Enemy] アクションがタイムアウト（{timeout}秒）");
            StopCoroutine(routine);
        }
    }

    /// <summary>
    /// 従来の攻撃処理（フォールバック用）
    /// EnemyDataが未設定の場合のみ使用
    /// </summary>
    IEnumerator LegacyAttack()
    {
        yield return null;

        // 攻撃アニメーション発火
        TriggerAttackAnimation();

        float multiplier = ConsumeChargeMultiplier();
        int finalDamage = Mathf.RoundToInt(DEFAULT_ATTACK_DAMAGE * multiplier);

        enemySlash.transform.position = _enemyAreas[enemyCurrentPos];

        Vector3 pos = enemySlash.transform.position;
        pos.y += 0.5f;
        enemySlash.transform.position = pos;

        enemySlash.SetActive(true);
        _slashAnimator.SetTrigger("Slash");
        CameraShake.Shake();
        yield return new WaitForSeconds(0.83f);

        if (enemyCurrentPos == _playerController.playerCurrentPos)
        {
            _playerController.playerDamaged(finalDamage);
        }

        enemySlash.SetActive(false);
    }

    /// <summary>
    /// 従来の防御処理（フォールバック用）
    /// 端にいる場合は中央へ移動
    /// </summary>
    IEnumerator LegacyDefense()
    {
        int center = (_enemyAreas.Count - 1) / 2;
        bool moved = false;

        // 端にいる場合は中央へ移動
        if (enemyCurrentPos < center)
        {
            yield return StartCoroutine(MoveForward());
            moved = true;
        }
        else if (enemyCurrentPos > center)
        {
            yield return StartCoroutine(MoveBackward());
            moved = true;
        }
        else
        {
            // 中央にいる場合はランダム移動
            if (Random.Range(0, 2) == 0 && enemyCurrentPos - 1 >= 0)
            {
                yield return StartCoroutine(MoveBackward());
                moved = true;
            }
            else if (enemyCurrentPos + 1 < _enemyAreas.Count)
            {
                yield return StartCoroutine(MoveForward());
                moved = true;
            }
        }

        if (!moved)
        {
            yield return new WaitForSeconds(DEFENSE_DELAY);
        }
    }

    IEnumerator MoveForward()
    {
        yield return new WaitForSeconds(PRE_MOVE_DELAY);
        int targetPos = enemyCurrentPos + 1;
        if (targetPos >= _enemyAreas.Count)
            yield break;

        // 右方向へ移動するので右を向く
        FaceRight();

        // 残像を生成
        CreateAfterImage();

        // 移動アニメーション開始
        TriggerMoveAnimation();

        // 移動先でのオフセットを計算
        float offsetX = GetMoveTargetOffset(targetPos);
        float offsetY = GetPositionOffsetY();
        Vector2 areaPos = _enemyAreas[targetPos];
        Vector3 finalPos = new Vector3(areaPos.x + offsetX, areaPos.y + offsetY, transform.position.z);

        yield return new WaitForSeconds(0.1f);
        yield return transform.DOMove(finalPos, GetMoveDuration())
            .SetId(ENEMY_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
        int oldPosFwd = enemyCurrentPos;
        enemyCurrentPos = targetPos;

        // 移動アニメーション終了
        TriggerMoveEndAnimation();

        // 移動後、左向き（アイドル状態）に戻す
        FaceLeft();

        // 旧エリアと新エリアの奥行き表現（色・X位置）を更新
        var emFwd = FindObjectOfType<EnemyManager>();
        if (emFwd != null)
        {
            emFwd.RefreshAreaVisuals(oldPosFwd);
            emFwd.RefreshAreaVisuals(targetPos);
        }
    }

    IEnumerator MoveBackward()
    {
        yield return new WaitForSeconds(PRE_MOVE_DELAY);
        int targetPos = enemyCurrentPos - 1;
        if (targetPos < 0)
            yield break;

        // 左方向へ移動するので左を向く（そのまま）
        FaceLeft();

        // 残像を生成
        CreateAfterImage();

        // 移動アニメーション開始
        TriggerMoveAnimation();

        // 移動先でのオフセットを計算
        float offsetX = GetMoveTargetOffset(targetPos);
        float offsetY = GetPositionOffsetY();
        Vector2 areaPos = _enemyAreas[targetPos];
        Vector3 finalPos = new Vector3(areaPos.x + offsetX, areaPos.y + offsetY, transform.position.z);

        yield return new WaitForSeconds(0.1f);
        yield return transform.DOMove(finalPos, GetMoveDuration())
            .SetId(ENEMY_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
        int oldPosBwd = enemyCurrentPos;
        enemyCurrentPos = targetPos;

        // 移動アニメーション終了
        TriggerMoveEndAnimation();

        // 旧エリアと新エリアの奥行き表現（色・X位置）を更新
        var emBwd = FindObjectOfType<EnemyManager>();
        if (emBwd != null)
        {
            emBwd.RefreshAreaVisuals(oldPosBwd);
            emBwd.RefreshAreaVisuals(targetPos);
        }
    }

    /// <summary>
    /// 移動時間を取得（EnemyDataから、なければデフォルト値）
    /// </summary>
    public float GetMoveDuration()
    {
        return enemyData != null ? enemyData.moveDuration : MOVE_DURATION;
    }

    /// <summary>
    /// Y座標オフセットを取得（EnemyDataから、なければ0）
    /// </summary>
    public float GetPositionOffsetY()
    {
        return enemyData != null ? enemyData.positionOffsetY : 0f;
    }

    /// <summary>
    /// 移動アニメーションを開始
    /// </summary>
    public void TriggerMoveAnimation()
    {
        if (_enemyAnimator == null) return;
        string trigger = enemyData != null ? enemyData.moveAnimTrigger : "Move";
        if (!string.IsNullOrEmpty(trigger))
            _enemyAnimator.SetTrigger(trigger);
    }

    /// <summary>
    /// 移動時の残像を生成
    /// </summary>
    public void CreateAfterImage()
    {
        if (_spriteRenderer == null) return;

        // 残像用のGameObjectを生成
        var afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        // SpriteRendererを追加して現在のスプライトをコピー
        var sr = afterImage.AddComponent<SpriteRenderer>();
        sr.sprite = _spriteRenderer.sprite;
        sr.sortingLayerID = _spriteRenderer.sortingLayerID;
        sr.sortingOrder = _spriteRenderer.sortingOrder - 1; // 本体の後ろに表示

        // アルファ0.5に設定
        Color color = _spriteRenderer.color;
        color.a = 0.5f;
        sr.color = color;

        // 0.5秒かけてフェードアウトして消える
        sr.DOFade(0f, 0.5f).OnComplete(() => Destroy(afterImage));
    }

    /// <summary>
    /// 移動アニメーションを終了
    /// </summary>
    public void TriggerMoveEndAnimation()
    {
        if (_enemyAnimator == null) return;
        string trigger = enemyData != null ? enemyData.moveEndAnimTrigger : "MoveEnd";
        if (!string.IsNullOrEmpty(trigger))
            _enemyAnimator.SetTrigger(trigger);
    }

    /// <summary>
    /// 攻撃アニメーションを発火
    /// </summary>
    public void TriggerAttackAnimation()
    {
        // 残像を生成
        CreateAfterImage();

        if (_enemyAnimator == null) return;
        string trigger = enemyData != null ? enemyData.attackAnimTrigger : "Action";
        if (!string.IsNullOrEmpty(trigger))
            _enemyAnimator.SetTrigger(trigger);
    }

    /// <summary>
    /// EnemyData に設定された攻撃 SE を再生する。AttackAction から呼ぶ。
    /// </summary>
    public void PlayAttackSE()
    {
        if (enemyData?.attackSE != null)
            AudioManager.Instance?.PlaySE(enemyData.attackSE);
    }

    /// <summary>
    /// 死亡アニメーションを発火
    /// </summary>
    public void TriggerDeathAnimation()
    {
        if (_enemyAnimator == null) return;
        string trigger = enemyData != null ? enemyData.deathAnimTrigger : "Death";
        if (!string.IsNullOrEmpty(trigger))
            _enemyAnimator.SetTrigger(trigger);
    }

    /// <summary>
    /// 左を向く（プレイヤー側、アイドル状態）
    /// 元のスプライトは右向きなので、localScale.x を負にして左向きにする
    /// </summary>
    public void FaceLeft()
    {
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    /// <summary>
    /// 右を向く（元のスプライトの向き）
    /// </summary>
    public void FaceRight()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    /// <summary>
    /// 移動方向に向きを変える
    /// </summary>
    /// <param name="targetPosition">移動先のエリア番号</param>
    public void FaceTowardPosition(int targetPosition)
    {
        if (targetPosition > enemyCurrentPos)
            FaceRight();  // 右へ移動
        else if (targetPosition < enemyCurrentPos)
            FaceLeft();   // 左へ移動
    }

    /// <summary>
    /// 同じエリアにいる敵のX座標オフセットを取得
    /// </summary>
    public float GetSameAreaOffset(int areaNum)
    {
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) return 0f;
        return enemyManager.GetAreaPositionOffset(this, areaNum);
    }

    /// <summary>
    /// 移動先でのX座標オフセットを取得
    /// </summary>
    public float GetMoveTargetOffset(int targetArea)
    {
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) return 0f;
        return enemyManager.GetMoveTargetOffset(this, targetArea);
    }

    IEnumerator Damaged(int damageAmount)
    {
        // ダメージ無効化フラグが有効な場合（KeyGolemメカニクス等）：0ダメージ表示してコールバック発火
        if (blockIncomingDamage)
        {
            DamageIndicator.SpawnFromPrefab(damageIndicatorPrefab, transform.position, 0);
            if (_flashEffect != null) _flashEffect.Flash();
            onHitWhileBlocked?.Invoke();
            yield break;
        }

        // forceDamageが設定されている場合は固定ダメージ（CoreBossメカニクス用）
        // それ以外はソートボス倍率を適用
        int finalDamage = forceDamage > 0
            ? forceDamage
            : Mathf.RoundToInt(damageAmount * incomingDamageMultiplier);
        Debug.Log($"[{gameObject.name}] Damaged: {finalDamage} (元={damageAmount}, 倍率={incomingDamageMultiplier:F2}), 現在位置: {transform.position}");

        // オーバーキル判定用に累積ダメージを記録
        if (enemyData != null && enemyData.punishOverkill)
            _totalDamageReceived += finalDamage;

        enemyHealth = Mathf.Max(0, enemyHealth - finalDamage);
        UpdateHealthUI();

        // ダメージインジケーター表示
        DamageIndicator.SpawnFromPrefab(damageIndicatorPrefab, transform.position, finalDamage);

        // 0ダメージ時は演出をスキップ
        if (finalDamage <= 0) yield break;

        // 白フラッシュ効果
        if (_flashEffect != null)
            _flashEffect.Flash();

        // ノックバック（右へ0.5移動）して元の位置に戻る
        float originalX = transform.position.x;
        yield return transform.DOMoveX(originalX + 0.5f, 0.15f)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
        yield return transform.DOMoveX(originalX, 0.15f)
            .SetEase(Ease.InQuad)
            .WaitForCompletion();

        Debug.Log($"[{gameObject.name}] ノックバック完了: 現在位置 {transform.position}");

        // 死亡判定
        if (enemyHealth <= 0)
        {
            yield return DeathSequence();
        }
    }

    /// <summary>
    /// 死亡演出: Deathトリガー発火 → 暗色適用 → その場に残す
    /// </summary>
    IEnumerator DeathSequence()
    {
        Debug.Log($"[Enemy] {enemyType} 死亡");

        // オーバーキルペナルティ: 累積ダメージが maxHealth を超えていたらプレイヤーへ反射
        if (enemyData != null && enemyData.punishOverkill)
        {
            int overkill = _totalDamageReceived - enemyData.maxHealth;
            if (overkill > 0)
            {
                int punish = enemyData.overkillPunishDamage > 0 ? enemyData.overkillPunishDamage : overkill;
                Debug.Log($"[Enemy] {enemyType} オーバーキル! 超過={overkill}, ペナルティ={punish}");
                _playerController?.GetDamaged(punish);
            }
        }

        // HPバーを非表示
        if (_healthBarInstance != null)
            _healthBarInstance.SetActive(false);

        // 死亡アニメーション発火
        TriggerDeathAnimation();

        // スプライトを暗く（死亡表現）
        _spriteRenderer.DOColor(new Color(0.35f, 0.35f, 0.35f, 1f), 0.4f);

        // 同エリアの残存敵の奥行き表現を更新（死亡敵が除外された状態でoffset/色を再計算）
        var em = FindObjectOfType<EnemyManager>();
        if (em != null)
            em.RefreshAreaVisuals(enemyCurrentPos);

        yield return null;
    }

    // ==============================================================
    // 頭上行動ラベル
    // ==============================================================

    /// <summary>
    /// 敵の子オブジェクトとして行動ラベルを生成
    /// </summary>
    private void InitializeActionLabel()
    {
        // 設定アセット未割り当て時はデフォルト値で動作
        float offsetY  = actionLabelSettings != null ? actionLabelSettings.offsetY  : 1.2f;
        float fontSize = actionLabelSettings != null ? actionLabelSettings.fontSize : 0.4f;
        Color color    = actionLabelSettings != null ? actionLabelSettings.color    : Color.white;

        var labelObj = new GameObject("ActionLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0f, offsetY, 0f);

        _actionLabel = labelObj.AddComponent<TextMeshPro>();
        _actionLabel.fontSize = fontSize;
        _actionLabel.alignment = TextAlignmentOptions.Center;
        _actionLabel.color = new Color(color.r, color.g, color.b, 0f);
        if (actionLabelSettings?.font != null)
            _actionLabel.font = actionLabelSettings.font;
        if (_spriteRenderer != null)
        {
            // sortingLayerID も合わせることでスプライトより前面に描画される
            _actionLabel.sortingLayerID = _spriteRenderer.sortingLayerID;
            _actionLabel.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
    }

    /// <summary>
    /// 行動ラベルをフェードインで表示
    /// </summary>
    private void ShowActionLabel(string text)
    {
        if (_actionLabel == null || string.IsNullOrEmpty(text)) return;
        // 親（敵）の localScale.x が負（左向き）のとき文字が反転するので打ち消す
        _actionLabel.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1f, 1f);
        _actionLabel.text = text;
        DOTween.To(() => _actionLabel.alpha, x => _actionLabel.alpha = x, 1f, 0.15f);
    }

    /// <summary>
    /// 行動ラベルをフェードアウトで非表示
    /// </summary>
    private void HideActionLabel()
    {
        if (_actionLabel == null) return;
        DOTween.To(() => _actionLabel.alpha, x => _actionLabel.alpha = x, 0f, 0.2f);
    }

    /// <summary>
    /// アクションの種類に応じた頭上表示テキストを生成
    /// AttackAction / MultiAttackAction / MoveAction に対応。それ以外は "" を返す
    /// </summary>
    private string BuildActionLabelText(EnemyAction action)
    {
        if (action is AttackAction atk)
        {
            int area = atk.targetArea >= 0 ? atk.targetArea : enemyCurrentPos;
            return $"エリア{area}で{atk.damage}ダメージの攻撃";
        }

        if (action is MultiAttackAction multi)
        {
            string areas = string.Join(", ", multi.targetAreas);
            return $"エリア{areas}で{multi.damage}ダメージの攻撃";
        }

        if (action is MoveAction move)
        {
            int target = move.CalculateTargetPosition(_actionContext);
            return target >= 0 ? $"エリア{target}へ移動" : "移動";
        }

        if (action is MagicAttackAction magic)
        {
            int area = magic.targetArea >= 0 ? magic.targetArea : _actionContext.LockedAttackArea;
            return $"エリア{area}へ{magic.damageType}魔法攻撃";
        }

        return "";
    }

    /// <summary>
    /// 奥行き表現用の色をアニメーション付きで設定（EnemyManagerから呼ばれる）
    /// </summary>
    public void SetDepthColor(Color color)
    {
        if (_spriteRenderer == null) return;
        _spriteRenderer.DOColor(color, 0.2f);
    }

    /// <summary>
    /// 現在のエリアに基づいてX座標を再計算し、位置をアニメーション更新（EnemyManagerから呼ばれる）
    /// 同エリアに複数いる場合、後ろの敵は向いている方向（前方）へずれる
    /// </summary>
    public void RefreshAreaPosition()
    {
        var areas = EnemyAreas;
        if (areas.Count == 0 || enemyCurrentPos >= areas.Count) return;

        var em = FindObjectOfType<EnemyManager>();
        int index = em != null ? em.GetEnemyIndexInArea(this, enemyCurrentPos) : 0;

        // localScale.x の符号で向きを判定（負 = 左向き、正 = 右向き）
        // 後ろ（index > 0）のキャラクターは向いている方向へずれる
        float facingSign = Mathf.Sign(transform.localScale.x);
        float offsetX = index * 0.5f * facingSign;
        float offsetY = GetPositionOffsetY();

        Vector2 areaCenter = areas[enemyCurrentPos];
        Vector3 targetPos = new Vector3(areaCenter.x + offsetX, areaCenter.y + offsetY, transform.position.z);
        transform.DOMove(targetPos, 0.3f).SetId(ENEMY_TWEEN_ID).SetEase(Ease.OutQuad);
    }
}
