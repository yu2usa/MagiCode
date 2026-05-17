using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// コード実行時に現在実行中の行をハイライト表示するコンポーネント
/// TMP_InputField の Text Area 内にハイライト用 Image を動的生成し、行に追従させる
/// </summary>
public class CodeLineHighlighter : MonoBehaviour
{
    [SerializeField] private TMP_InputField targetInputField;

    [Tooltip("InGameCodeEditor の LineHighlight（実行中に非表示にする）")]
    [SerializeField] private GameObject editorLineHighlight;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.016f, 0.15f);
    [SerializeField] private float transitionDuration = 0.15f;
    [SerializeField] private float verticalPadding = 2f;

    private RectTransform _highlightRect;
    private Image _highlightImage;
    private RectTransform _textAreaRect;
    private TMP_Text _textComponent;
    private Transform _highlightParent;
    private bool _initialized;
    private int _currentLine = -1;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (_initialized) return;

        _textComponent = targetInputField.textComponent;

        // Text Area（テキストの親 = Viewport）を取得
        _textAreaRect = _textComponent.rectTransform.parent as RectTransform;

        // InputField の親（InGameCodeEditor）をハイライトの親にする
        // InputField 内部に入れると HighlightedText の位置がずれるため外に置く
        _highlightParent = targetInputField.transform.parent;

        var highlightObj = new GameObject("ExecutionHighlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        highlightObj.transform.SetParent(_highlightParent, false);

        _highlightRect = highlightObj.GetComponent<RectTransform>();
        _highlightImage = highlightObj.GetComponent<Image>();
        _highlightImage.color = highlightColor;
        _highlightImage.raycastTarget = false;

        // 横幅を Text Area に合わせる（高さは HighlightLine で設定）
        _highlightRect.pivot = new Vector2(0.5f, 0.5f);
        _highlightRect.sizeDelta = new Vector2(_textAreaRect.rect.width, 0);

        highlightObj.SetActive(false);
        _initialized = true;
    }

    /// <summary>
    /// 指定行をハイライト（1-based の行番号）
    /// </summary>
    public void HighlightLine(int lineNumber)
    {
        if (!_initialized) Initialize();
        if (targetInputField == null)
        {
            Debug.LogWarning("[CodeLineHighlighter] targetInputField が null");
            return;
        }
        if (lineNumber <= 0)
        {
            Debug.LogWarning($"[CodeLineHighlighter] 無効な行番号: {lineNumber}");
            return;
        }

        _textComponent.ForceMeshUpdate();

        string sourceText = targetInputField.text;
        int charIndex = GetCharIndexForSourceLine(sourceText, lineNumber);
        if (charIndex < 0)
        {
            Debug.LogWarning($"[CodeLineHighlighter] 行 {lineNumber} が見つからない (テキスト行数不足, textLength={sourceText.Length})");
            return;
        }

        TMP_TextInfo textInfo = _textComponent.textInfo;
        if (charIndex >= textInfo.characterCount)
        {
            Debug.LogWarning($"[CodeLineHighlighter] charIndex({charIndex}) >= characterCount({textInfo.characterCount})");
            return;
        }

        // 文字情報から行の Y 座標と高さを取得（テキストコンポーネントのローカル空間）
        int renderedLineIndex = textInfo.characterInfo[charIndex].lineNumber;
        TMP_LineInfo lineInfo = textInfo.lineInfo[renderedLineIndex];

        float lineHeight = lineInfo.ascender - lineInfo.descender + verticalPadding;
        float lineCenterY = (lineInfo.ascender + lineInfo.descender) / 2f;

        // テキストのローカル空間 → ワールド空間 → ハイライト親のローカル空間 に変換
        Vector3 worldPos = _textComponent.transform.TransformPoint(new Vector3(0, lineCenterY, 0));
        Vector3 localPos = _highlightParent.InverseTransformPoint(worldPos);
        float targetY = localPos.y;

        // テキストエリアの中心 X をハイライト親のローカル空間で求める
        Vector3 textAreaWorldCenter = _textAreaRect.TransformPoint(_textAreaRect.rect.center);
        float targetX = _highlightParent.InverseTransformPoint(textAreaWorldCenter).x;

        bool isFirstHighlight = !_highlightRect.gameObject.activeSelf;
        _highlightRect.sizeDelta = new Vector2(_textAreaRect.rect.width, lineHeight);

        if (isFirstHighlight)
        {
            // 再生中はエディタハイライトを非表示
            if (editorLineHighlight != null)
                editorLineHighlight.SetActive(false);

            // 初回はアニメーションなしで即座に配置してフェードイン
            _highlightRect.localPosition = new Vector3(targetX, targetY, 0);
            _highlightImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0);
            _highlightRect.gameObject.SetActive(true);
            _highlightImage.DOColor(highlightColor, transitionDuration);
        }
        else if (_currentLine != lineNumber)
        {
            // 行が変わったら滑らかに移動
            DOTween.Kill(_highlightRect);
            _highlightRect.DOLocalMoveY(targetY, transitionDuration).SetEase(Ease.OutCubic);
        }

        _currentLine = lineNumber;
    }

    /// <summary>
    /// ハイライトをフェードアウトして非表示にする
    /// </summary>
    public void ClearHighlight()
    {
        if (!_initialized || !_highlightRect.gameObject.activeSelf) return;

        DOTween.Kill(_highlightRect);
        _highlightImage.DOColor(
            new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0),
            transitionDuration
        ).OnComplete(() =>
        {
            _highlightRect.gameObject.SetActive(false);
            _currentLine = -1;
            if (editorLineHighlight != null)
                editorLineHighlight.SetActive(true);
        });
    }

    /// <summary>
    /// ソーステキストの行番号（1-based）から先頭文字のインデックスを返す
    /// </summary>
    static int GetCharIndexForSourceLine(string text, int sourceLine)
    {
        int currentLine = 1;
        if (sourceLine == 1) return 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                currentLine++;
                if (currentLine == sourceLine) return i + 1;
            }
        }
        return -1;
    }
}
