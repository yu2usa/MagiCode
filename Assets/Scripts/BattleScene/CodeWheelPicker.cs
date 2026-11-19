using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// コード実行中、画面下部にホイールピッカー（回転するドラム）のように実行中のコードを表示する。
/// 実行中の行が中央に来るようにドラムが回り、上下の行は奥へ倒れ込むように小さく薄くなる
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CodeWheelPicker : MonoBehaviour
{
    [Tooltip("表示するコードの入力欄（文字とフォントをここから取る）")]
    [SerializeField] private TMP_InputField inputField;
    [Tooltip("行を並べる親（ドラムの中心）")]
    [SerializeField] private RectTransform rowParent;
    [Tooltip("中央の行の後ろに敷く帯。幅はコードの長さに合わせる")]
    [SerializeField] private RectTransform centerBand;
    [SerializeField] private float bandPadding = 40f;
    [Tooltip("中央の帯の左に出す、ループの周回数（i = 0 など）")]
    [SerializeField] private TextMeshProUGUI loopCounter;
    [SerializeField] private float fontSize = 28f;
    [Tooltip("ドラムの半径（px）。行の間隔は 半径 × 1行ぶんの角度 になる")]
    [SerializeField] private float radius = 58f;
    [Tooltip("1行ぶんの回転角度（度）")]
    [SerializeField] private float degreesPerRow = 30f;
    [SerializeField] private Color centerColor = Color.white;
    [SerializeField] private Color sideColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [Tooltip("いちばん奥の行の濃さ（0〜1）")]
    [SerializeField] private float edgeAlpha = 0.1f;
    [SerializeField] private float spinDuration = 0.3f;
    [SerializeField] private Ease spinEase = Ease.OutCubic;
    [SerializeField] private float fadeDuration = 0.2f;

    private CanvasGroup _group;
    private readonly List<TextMeshProUGUI> _rows = new List<TextMeshProUGUI>();
    // 表示している行の元の行番号（空行は表示しないので、行番号と並び順がずれる）
    private readonly List<int> _lineNumbers = new List<int>();
    // ドラムの回転位置（中央に来ている行の並び順。途中の値は回転中）
    private float _position;
    // 中央の行を赤く点滅させる強さ（0〜1）と色
    private float _flash;
    private Color _flashColor;
    private Tween _flashTween;

    // シーンでは非表示にしておき、最初に表示したときに初期化される
    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 指定行（1始まり）が中央に来るようにドラムを回す。非表示のときは今のコードを並べてから表示する
    /// </summary>
    public void ShowLine(int lineNumber)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            _group.alpha = 0f;
            BuildRows();
            _position = _lineNumbers.IndexOf(lineNumber);
        }
        // 消えている途中でも表示し直す
        _group.DOKill();
        _group.DOFade(1f, fadeDuration);

        DOTween.Kill(this);
        DOTween.To(() => _position, x => _position = x, _lineNumbers.IndexOf(lineNumber), spinDuration)
            .SetEase(spinEase)
            .SetTarget(this);
    }

    /// <summary>
    /// 中央の行を指定色で2回点滅させる（AP不足などで行動できなかったとき）
    /// </summary>
    public void FlashCenterRow(Color color)
    {
        _flashColor = color;
        _flashTween?.Kill();
        _flash = 0f;
        _flashTween = DOTween.To(() => _flash, x => _flash = x, 1f, 0.15f).SetLoops(4, LoopType.Yoyo);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        _group.DOKill();
        _group.DOFade(0f, fadeDuration).OnComplete(() => gameObject.SetActive(false));
    }

    // 空行を除いたコードの各行を、左端をそろえて並べる（字下げでブロックの形がわかるように）
    private void BuildRows()
    {
        _lineNumbers.Clear();
        string[] lines = inputField.text.Replace("\t", "    ").Split('\n');
        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd();
            if (line.Trim().Length == 0) continue;
            SetRowText(count, line);
            _lineNumbers.Add(i + 1);
            count++;
        }
        for (int i = count; i < _rows.Count; i++)
            _rows[i].gameObject.SetActive(false);

        // いちばん長い行に幅を合わせ、コード全体が画面の中央に来るようにする
        float width = 0f;
        for (int i = 0; i < count; i++)
            width = Mathf.Max(width, _rows[i].GetPreferredValues().x);
        for (int i = 0; i < count; i++)
            _rows[i].rectTransform.sizeDelta = new Vector2(width, fontSize * 1.4f);
        centerBand.sizeDelta = new Vector2(width + bandPadding * 2f, centerBand.sizeDelta.y);

        // 周回数は帯の左端のすぐ外に、右寄せで置く
        loopCounter.rectTransform.anchoredPosition = new Vector2(-(width / 2f + bandPadding + 16f), 0f);
        loopCounter.gameObject.SetActive(false);
    }

    /// <summary>
    /// ループの周回数を出す（null ならループの外なので消す）。周が変わるたびに小さく弾ませる
    /// </summary>
    public void SetLoopCounter(string text)
    {
        if (text == null)
        {
            loopCounter.gameObject.SetActive(false);
            return;
        }
        if (loopCounter.gameObject.activeSelf && loopCounter.text == text) return;

        loopCounter.gameObject.SetActive(true);
        loopCounter.text = text;
        RectTransform rect = loopCounter.rectTransform;
        rect.DOKill(true);
        rect.localScale = Vector3.one * 1.3f;
        rect.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    private void SetRowText(int index, string text)
    {
        if (index == _rows.Count)
            _rows.Add(CreateRow());
        _rows[index].gameObject.SetActive(true);
        _rows[index].text = text;
    }

    private TextMeshProUGUI CreateRow()
    {
        var row = new GameObject("Row", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        row.transform.SetParent(rowParent, false);
        row.font = inputField.textComponent.font;
        row.fontSize = fontSize;
        row.alignment = TextAlignmentOptions.MidlineLeft;
        row.textWrappingMode = TextWrappingModes.NoWrap;
        row.richText = false;
        row.raycastTarget = false;
        return row;
    }

    // 各行をドラムの表面に置く。中央から離れるほど奥へ傾き、縦につぶれて薄くなる
    void LateUpdate()
    {
        for (int i = 0; i < _lineNumbers.Count; i++)
        {
            float angle = (i - _position) * degreesPerRow * Mathf.Deg2Rad;
            float facing = Mathf.Cos(angle);
            RectTransform rect = _rows[i].rectTransform;
            rect.anchoredPosition = new Vector2(0f, -radius * Mathf.Sin(angle));
            rect.localScale = new Vector3(Mathf.Lerp(0.85f, 1f, facing), Mathf.Max(facing, 0f), 1f);

            // 裏側（90度より奥）に回った行は見せない
            float alpha = facing <= 0f ? 0f : Mathf.Lerp(edgeAlpha, 1f, facing * facing);
            float centerness = Mathf.InverseLerp(Mathf.Cos(degreesPerRow * Mathf.Deg2Rad), 1f, facing);
            Color color = Color.Lerp(sideColor, centerColor, centerness);
            color = Color.Lerp(color, _flashColor, _flash * centerness);
            color.a = alpha;
            _rows[i].color = color;
        }
    }
}
