using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 攻略ブックウィンドウのタブ切り替えと表示を管理する。
///
/// Hierarchy:
///   StrategyBookWindow [このスクリプトをアタッチ + Button → Close()]
///     ├── Image              (背景パネル)
///     ├── GameSystemInst     [Button → ShowGameSystem()]
///     ├── ObtainedCode       [Button → ShowObtainedCodes()]
///     ├── Content            (TMP_Text — ゲームシステム説明用)
///     ├── KeywordContainer   (空の親オブジェクト — 習得コードアイテム生成先)
///     └── TooltipPanel       (GameObject — ホバー時に説明を表示)
///           └── TooltipText  (TMP_Text)
/// </summary>
public class StrategyBookWindowManager : MonoBehaviour
{
    [Tooltip("ゲームシステム説明用 TMP_Text（GameSystem タブで使用）")]
    [SerializeField] private TMP_Text contentText;

    [Tooltip("ゲームシステム説明文")]
    [TextArea(5, 30)]
    [SerializeField] private string gameSystemExplanation;

    [Tooltip("全ステージの StageConfig アセット（習得コード取得に使用）")]
    [SerializeField] private List<StageConfig> allStageConfigs = new List<StageConfig>();

    [Header("習得コードタブ")]
    [Tooltip("キーワードアイテムを縦に並べる親 Transform（VerticalLayoutGroup 推奨）")]
    [SerializeField] private Transform keywordContainer;

    [Tooltip("キーワード1行のPrefab（KeywordItem + TMP_Text 必須）")]
    [SerializeField] private GameObject keywordItemPrefab;

    [Tooltip("キーワード説明文のマスターデータ")]
    [SerializeField] private KeywordDatabase keywordDatabase;

    [Header("ツールチップ")]
    [Tooltip("ホバー時に表示するツールチップパネル")]
    [SerializeField] private GameObject tooltipPanel;

    [Tooltip("ツールチップ内の説明テキスト")]
    [SerializeField] private TMP_Text tooltipText;

    [Header("スライドアニメーション")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private Ease slideInEase  = Ease.OutCubic;
    [SerializeField] private Ease slideOutEase = Ease.InCubic;

    private RectTransform _rect;
    private float _originalY;
    private readonly List<GameObject> _spawnedItems = new List<GameObject>();

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalY = _rect.anchoredPosition.y;
        gameObject.SetActive(false);

        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void Open()
    {
        ShowGameSystem();
        gameObject.SetActive(true);

        float offscreenY = _originalY + _rect.rect.height;
        _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, offscreenY);

        _rect.DOKill();
        _rect.DOAnchorPosY(_originalY, slideDuration).SetEase(slideInEase);
    }

    public void Close()
    {
        HideTooltip();
        float offscreenY = _originalY + _rect.rect.height;

        _rect.DOKill();
        _rect.DOAnchorPosY(offscreenY, slideDuration)
            .SetEase(slideOutEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// ゲームシステム説明を表示する。GameSystemInst ボタンの onClick に登録する。
    /// </summary>
    public void ShowGameSystem()
    {
        ClearKeywordItems();
        HideTooltip();

        contentText.gameObject.SetActive(true);
        if (keywordContainer != null) keywordContainer.gameObject.SetActive(false);

        contentText.text = gameSystemExplanation;
    }

    /// <summary>
    /// 習得コード一覧を表示する。ObtainedCode ボタンの onClick に登録する。
    /// </summary>
    public void ShowObtainedCodes()
    {
        ClearKeywordItems();
        HideTooltip();

        contentText.gameObject.SetActive(false);
        if (keywordContainer != null) keywordContainer.gameObject.SetActive(true);

        bool any = false;
        foreach (var config in allStageConfigs)
        {
            if (config == null) continue;
            if (!ClearDataManager.IsStageClear(config.stageName) &&
                !ClearDataManager.IsKeywordsObtained(config.stageName)) continue;

            foreach (var keyword in config.obtainedKeywords)
            {
                SpawnKeywordItem(keyword);
                any = true;
            }
        }

        // 獲得コードがない場合は contentText にフォールバック
        if (!any)
        {
            contentText.gameObject.SetActive(true);
            if (keywordContainer != null) keywordContainer.gameObject.SetActive(false);
            contentText.text = "獲得したコードなし";
        }
    }

    // --- 内部メソッド ---

    private void SpawnKeywordItem(string keyword)
    {
        if (keywordItemPrefab == null || keywordContainer == null) return;

        var obj = Instantiate(keywordItemPrefab, keywordContainer);
        var item = obj.GetComponent<KeywordItem>();
        if (item != null)
        {
            string desc = keywordDatabase != null ? keywordDatabase.GetDescription(keyword) : "";
            item.Setup(keyword, desc, ShowTooltip, HideTooltip);
        }
        _spawnedItems.Add(obj);
    }

    private void ClearKeywordItems()
    {
        foreach (var obj in _spawnedItems)
            if (obj != null) Destroy(obj);
        _spawnedItems.Clear();
    }

    private void ShowTooltip(string description)
    {
        if (tooltipPanel == null) return;
        if (string.IsNullOrEmpty(description))
        {
            HideTooltip();
            return;
        }
        tooltipText.text = description;
        tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}
