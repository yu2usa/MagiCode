using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 攻略ブックウィンドウのタブ切り替えと表示を管理する。
///
/// 開くときは背景をその場でグレーアウトしながら、本のパネルだけを上からスライドさせる
/// （戦闘中の「メニューに戻る」確認ウィンドウと同じ作り）。
///
/// Hierarchy:
///   StrategyBookWindow [このスクリプトをアタッチ]
///     ├── Outside            (画面全体の暗幕 + Button → Close())
///     └── Window             (スライドする本のパネル)
///           ├── Image              (背景パネル)
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

    [Tooltip("クリックしたコードの例を貼り付けるコードエディタ（戦闘シーンだけ設定する。メニューの戦術書は空のまま）")]
    [SerializeField] private InGameCodeEditor.CodeEditor codeEditor;

    [Tooltip("説明文と習得コード一覧を入れたスクロール領域（本に収まらない分はスクロールで読む）")]
    [SerializeField] private ScrollRect scroll;

    [Header("ツールチップ")]
    [Tooltip("ホバー時に表示するツールチップパネル")]
    [SerializeField] private GameObject tooltipPanel;

    [Tooltip("ツールチップ内の説明テキスト")]
    [SerializeField] private TMP_Text tooltipText;

    [Header("スライドアニメーション")]
    [Tooltip("上からスライドする本のパネル")]
    [SerializeField] private RectTransform window;
    [Tooltip("背景をグレーアウトする全画面の暗幕（その場でフェードする）")]
    [SerializeField] private Image dim;
    // 開いた直後のフレームは表示の準備で重くなりやすいので、動き出しがゆっくりなイージングにする
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private Ease slideInEase = Ease.InOutCubic;
    [SerializeField] private float slideOutDuration = 0.25f;
    [SerializeField] private Ease slideOutEase = Ease.InQuad;

    private float _shownY;
    private float _dimAlpha;
    private readonly List<GameObject> _spawnedItems = new List<GameObject>();

    // シーンには非表示で置いておく。Awake は初めて Open() で表示したときに走るので、ここで自分を非表示にしてはいけない
    // （以前はここで SetActive(false) していたため、1回目に開いた瞬間に閉じていた）
    private void Awake()
    {
        _shownY = window.anchoredPosition.y;
        _dimAlpha = dim.color.a;

        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void Open()
    {
        // タブは「獲得したコード」が上なので、開いたときもそちらを見せる
        ShowObtainedCodes();
        gameObject.SetActive(true);
        KillTweens();

        // 本は画面の上の外から定位置へ、背景はその場でグレーアウト
        window.anchoredPosition = new Vector2(window.anchoredPosition.x, HiddenY());
        window.DOAnchorPosY(_shownY, slideInDuration).SetEase(slideInEase);
        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
        dim.DOFade(_dimAlpha, slideInDuration);
    }

    public void Close()
    {
        HideTooltip();
        KillTweens();
        dim.DOFade(0f, slideOutDuration);
        window.DOAnchorPosY(HiddenY(), slideOutDuration)
            .SetEase(slideOutEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // このウィンドウは画面いっぱいの大きさなので、その高さぶん上げれば画面外になる
    private float HiddenY()
    {
        return _shownY + ((RectTransform)transform).rect.height;
    }

    private void KillTweens()
    {
        window.DOKill();
        dim.DOKill();
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
        ScrollToTop();
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
        ScrollToTop();
    }

    // タブを切り替えたとき・開き直したときは、前に読んでいた位置ではなく先頭から見せる
    private void ScrollToTop()
    {
        scroll.StopMovement();
        scroll.content.anchoredPosition = Vector2.zero;
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
            string snippet = keywordDatabase != null ? keywordDatabase.GetSnippet(keyword) : "";

            // 戦闘中の戦術書で、貼り付ける例があるものだけクリックで貼り付けられる
            bool canPaste = codeEditor != null && !string.IsNullOrEmpty(snippet);
            if (canPaste)
                desc += "\n\n<color=#FFD966>クリックで入力欄に貼り付け</color>";
            item.Setup(keyword, desc, ShowTooltip, HideTooltip, canPaste ? () => PasteSnippet(snippet) : null);
        }
        _spawnedItems.Add(obj);
    }

    /// <summary>
    /// コードの例を入力欄の最後に足して、本を閉じる。
    /// コードを実行している最中など入力できないときは、貼り付けずにそのことを知らせる
    /// </summary>
    private void PasteSnippet(string snippet)
    {
        if (!codeEditor.InputField.IsInteractable())
        {
            ShowTooltip("今はコードを入力できません。\n自分の番でコードを書くときに貼り付けられます");
            return;
        }

        string code = codeEditor.Text.TrimEnd('\n', ' ');
        codeEditor.Text = code.Length == 0 ? snippet : code + "\n" + snippet;
        Close();
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
