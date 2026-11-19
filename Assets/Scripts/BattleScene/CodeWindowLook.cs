using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// コードウィンドウの見た目を切り替える。
/// 入力中（カーソルがある）かマウスが乗っているときは通常表示、それ以外（敵ターンやコード実行中など）は
/// 枠を消し、うっすら暗い背景に文字と行番号だけを表示する。
/// ただし、まだ一度も入力欄をクリックしていない間は、入力欄の場所が分かるよう枠を出しておく
/// </summary>
public class CodeWindowLook : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [Tooltip("簡易表示で消す部品（枠・行番号の背景・編集中の行ハイライト・折りたたみボタンなど）")]
    [SerializeField] private CanvasGroup[] frameParts;
    [Tooltip("エディタの背景。簡易表示ではうっすら暗い半透明にする")]
    [SerializeField] private Image editorBackground;
    [Tooltip("簡易表示のときの背景の濃さ（0〜1）")]
    [SerializeField] private float minimalBackgroundAlpha = 0.45f;
    [SerializeField] private float fadeDuration = 0.2f;

    private RectTransform _rect;
    private Camera _uiCamera;
    private float _normalBackgroundAlpha;
    private bool _isFull = true;
    // 行動中（レターボックス表示中）は、マウスを乗せても枠を出さない
    private bool _isActionView;
    // 一度でも入力欄をクリックしたか（それまでは最初のターンでも枠を出しておく）
    private bool _wasFocused;

    public void SetActionView(bool isActionView)
    {
        _isActionView = isActionView;
    }

    void Awake()
    {
        _rect = (RectTransform)transform;
        _uiCamera = GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        _normalBackgroundAlpha = editorBackground.color.a;
    }

    void Update()
    {
        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, _uiCamera);
        _wasFocused |= inputField.isFocused;
        bool isFull = !_isActionView && (!_wasFocused || hovered || inputField.isFocused);
        if (isFull == _isFull) return;

        _isFull = isFull;
        float partsAlpha = isFull ? 1f : 0f;
        foreach (var part in frameParts)
        {
            part.DOKill();
            part.DOFade(partsAlpha, fadeDuration);
            // 見えていない折りたたみボタンなどは押せないようにする
            part.blocksRaycasts = isFull;
        }
        editorBackground.DOKill();
        editorBackground.DOFade(isFull ? _normalBackgroundAlpha : minimalBackgroundAlpha, fadeDuration);
    }
}
