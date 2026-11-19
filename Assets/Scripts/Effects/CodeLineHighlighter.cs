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

    [Header("Focus Settings")]
    [Tooltip("実行中の行以外を薄くする文字（色付きコードと行番号）")]
    [SerializeField] private TMP_Text[] focusTexts;
    [Tooltip("実行中の行以外の文字の濃さ（0〜1）")]
    [Range(0f, 1f)]
    [SerializeField] private float unfocusedAlpha = 0.3f;

    [Tooltip("画面下部で実行中のコードを回して見せるホイールピッカー")]
    [SerializeField] private CodeWheelPicker wheelPicker;

    private RectTransform _highlightRect;
    private Image _highlightImage;
    private RectTransform _textAreaRect;
    private TMP_Text _textComponent;
    private Transform _highlightParent;
    private bool _initialized;
    private int _currentLine = -1;

    // 実行中の行の中心の高さ（ワールド座標）と、その行の半分の高さ
    private float _focusCenterY;
    private float _focusHalfHeight;

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
        wheelPicker.ShowLine(lineNumber);

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
        _focusCenterY = worldPos.y;
        _focusHalfHeight = Mathf.Abs(_textComponent.transform.TransformVector(new Vector3(0, lineHeight * 0.5f, 0)).y);
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
    /// ループの周回数をホイールピッカーに出す（null で消す）
    /// </summary>
    public void SetLoopCounter(string text)
    {
        wheelPicker.SetLoopCounter(text);
    }

    /// <summary>
    /// 実行中の行が失敗した（AP不足など）ことを、ハイライトとホイールピッカーの赤い点滅で知らせる
    /// </summary>
    public void FlashError(Color color)
    {
        wheelPicker.FlashCenterRow(color);
        _highlightImage.DOKill(true);
        _highlightImage.DOColor(new Color(color.r, color.g, color.b, 0.4f), 0.15f)
            .SetLoops(4, LoopType.Yoyo)
            .OnComplete(() => _highlightImage.color = highlightColor);
    }

    /// <summary>
    /// ハイライトをフェードアウトして非表示にする
    /// </summary>
    public void ClearHighlight()
    {
        wheelPicker.Hide();
        if (!_initialized || !_highlightRect.gameObject.activeSelf) return;

        DOTween.Kill(_highlightRect);
        _highlightImage.DOColor(
            new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0),
            transitionDuration
        ).OnComplete(() =>
        {
            _highlightRect.gameObject.SetActive(false);
            _currentLine = -1;
            // 薄くしていた文字を元の色に戻す
            foreach (var text in focusTexts)
                text.ForceMeshUpdate();
            if (editorLineHighlight != null)
                editorLineHighlight.SetActive(true);
        });
    }

    // 実行中は、実行中の行以外の文字と行番号を薄くして、その行に目が行くようにする。
    // エディタ側が文字を描き直しても効くよう、毎フレーム頂点の色を付け直す
    void LateUpdate()
    {
        if (_currentLine < 0) return;

        foreach (var text in focusTexts)
        {
            text.ForceMeshUpdate();
            DimOtherLines(text);
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    private void DimOtherLines(TMP_Text text)
    {
        TMP_TextInfo info = text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            TMP_LineInfo line = info.lineInfo[ch.lineNumber];
            float centerY = text.transform.TransformPoint(new Vector3(0, (line.ascender + line.descender) * 0.5f, 0)).y;
            if (Mathf.Abs(centerY - _focusCenterY) < _focusHalfHeight) continue;

            Color32[] colors = info.meshInfo[ch.materialReferenceIndex].colors32;
            for (int v = 0; v < 4; v++)
                colors[ch.vertexIndex + v].a = (byte)(colors[ch.vertexIndex + v].a * unfocusedAlpha);
        }
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
