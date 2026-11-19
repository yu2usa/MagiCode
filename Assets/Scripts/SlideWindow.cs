using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 背景をその場でグレーアウトしながら、ウィンドウを画面の外からスライドさせて出すポップアップ（設定・クレジットなど）。
/// このコンポーネントは画面全体の暗幕（Image）に付け、動かすウィンドウ本体を子に置く
/// </summary>
[RequireComponent(typeof(Image))]
public class SlideWindow : MonoBehaviour
{
    public enum SlideFrom { Bottom, Top }

    [Tooltip("スライドさせるウィンドウ本体（中身はすべてこの子に置く）")]
    [SerializeField] private RectTransform window;
    [Tooltip("どちらの画面端から出てくるか")]
    [SerializeField] private SlideFrom slideFrom = SlideFrom.Bottom;

    [Header("演出")]
    // 開いた直後のフレームは表示の準備で重くなりやすいので、動き出しがゆっくりなイージングにする
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private Ease slideInEase = Ease.InOutCubic;
    [SerializeField] private float slideOutDuration = 0.25f;
    [SerializeField] private Ease slideOutEase = Ease.InQuad;

    private Image _dim;
    private float _dimAlpha;
    private float _shownY;

    // シーンには非表示で置いておく。Awake は初めて Open() で表示したときに走る
    void Awake()
    {
        _dim = GetComponent<Image>();
        _dimAlpha = _dim.color.a;
        _shownY = window.anchoredPosition.y;

        // ウィンドウ本体は暗幕（閉じるボタン）の子なので、本体のクリックが暗幕まで伝わって閉じないよう止める
        window.gameObject.AddComponent<ClickBlocker>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        KillTweens();

        // 画面の外から定位置へ
        window.anchoredPosition = new Vector2(window.anchoredPosition.x, HiddenY());
        window.DOAnchorPosY(_shownY, slideInDuration).SetEase(slideInEase);
        _dim.color = new Color(_dim.color.r, _dim.color.g, _dim.color.b, 0f);
        _dim.DOFade(_dimAlpha, slideInDuration);
    }

    public void Close()
    {
        KillTweens();
        _dim.DOFade(0f, slideOutDuration);
        window.DOAnchorPosY(HiddenY(), slideOutDuration).SetEase(slideOutEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // 暗幕は画面いっぱいの大きさなので、その高さぶん上げ下げすれば画面外になる
    private float HiddenY()
    {
        float height = ((RectTransform)transform).rect.height;
        return slideFrom == SlideFrom.Top ? _shownY + height : _shownY - height;
    }

    private void KillTweens()
    {
        window.DOKill();
        _dim.DOKill();
    }
}
