using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// KeyGolem専用メカニクス。
/// プレイヤー攻撃フェーズ開始時にランダムな必要攻撃回数を決定し、
/// フェーズ中は全ての攻撃を0ダメージにする。
/// フェーズ終了時、攻撃回数がちょうど requiredCount と一致していれば successDamage を与える。
/// 必要回数は行動ラベルと同じ方式で頭上に表示される。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class KeyGolemMechanic : MonoBehaviour
{
    [Header("カウント設定")]
    [Tooltip("1フェーズあたりの必要攻撃回数（最小値）")]
    [SerializeField] private int countMin = 1;
    [Tooltip("1フェーズあたりの必要攻撃回数（最大値）")]
    [SerializeField] private int countMax = 5;

    [Header("ダメージ設定")]
    [Tooltip("攻撃回数がぴったり一致したときに与えるダメージ")]
    [SerializeField] private int successDamage = 4;

    [Header("カウントラベル")]
    [Tooltip("ラベルのY軸オフセット（アクションラベルより上に表示）")]
    [SerializeField] private float labelOffsetY = 1.8f;
    [Tooltip("フォントサイズ（ワールドスペース単位）")]
    [SerializeField] private float labelFontSize = 0.4f;
    [Tooltip("ラベルの色")]
    [SerializeField] private Color labelColor = Color.yellow;
    [Tooltip("フォント（未設定時はTMPデフォルト）")]
    [SerializeField] private TMP_FontAsset labelFont;

    private EnemyController _enemy;
    private TurnController _tc;
    private TextMeshPro _label;

    private int _requiredCount;
    private int _currentHitCount;
    private bool _inPlayerAttackPhase;

    // LogsManager から参照
    public int RequiredCount => _requiredCount;
    public bool IsActive => _inPlayerAttackPhase;

    void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        _enemy.onHitWhileBlocked += OnHitReceived;
    }

    void Start()
    {
        InitializeLabel();
    }

    void OnDestroy()
    {
        if (_enemy != null)
            _enemy.onHitWhileBlocked -= OnHitReceived;
    }

    /// <summary>
    /// TurnController.PlayerAttack() 開始時に呼ぶ
    /// </summary>
    public void NotifyPlayerAttackStart()
    {
        BeginPhase();
    }

    /// <summary>
    /// TurnController.PlayerAttack() 終了時に呼ぶ
    /// </summary>
    public void NotifyPlayerAttackEnd()
    {
        EndPhase();
    }

    private void BeginPhase()
    {
        _requiredCount = Random.Range(countMin, countMax + 1);
        _currentHitCount = 0;
        _enemy.blockIncomingDamage = true;
        _inPlayerAttackPhase = true;
        ShowLabel($"0 / {_requiredCount}");
    }

    private void EndPhase()
    {
        _enemy.blockIncomingDamage = false;
        _inPlayerAttackPhase = false;
        HideLabel();

        if (_currentHitCount == _requiredCount)
            StartCoroutine(ApplyDamage());
    }

    private void OnHitReceived()
    {
        if (!_inPlayerAttackPhase) return;
        _currentHitCount++;
        UpdateLabel($"{_currentHitCount} / {_requiredCount}");
    }

    private IEnumerator ApplyDamage()
    {
        yield return new WaitForSeconds(0.3f);
        _enemy.GetDamaged(successDamage);
    }

    // --- ラベル管理 ---

    /// <summary>
    /// アクションラベルと同じ方式で頭上に TextMeshPro を生成
    /// </summary>
    private void InitializeLabel()
    {
        var labelObj = new GameObject("CountLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0f, labelOffsetY, 0f);

        _label = labelObj.AddComponent<TextMeshPro>();
        _label.fontSize = labelFontSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(labelColor.r, labelColor.g, labelColor.b, 0f);
        if (labelFont != null)
            _label.font = labelFont;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            _label.sortingOrder = spriteRenderer.sortingOrder + 1;
    }

    private void ShowLabel(string text)
    {
        if (_label == null) return;
        _label.text = text;
        DOTween.To(() => _label.alpha, x => _label.alpha = x, 1f, 0.15f);
    }

    private void UpdateLabel(string text)
    {
        if (_label == null) return;
        _label.text = text;
    }

    private void HideLabel()
    {
        if (_label == null) return;
        DOTween.To(() => _label.alpha, x => _label.alpha = x, 0f, 0.2f);
    }
}
