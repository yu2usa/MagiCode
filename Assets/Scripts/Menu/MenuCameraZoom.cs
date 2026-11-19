using UnityEngine;
using DG.Tweening;

/// <summary>
/// メニューのステージマップを、カメラが寄ったように拡大して指定のボタンへクローズアップする。
/// UIはカメラに貼り付いて描画されるのでカメラ自体を動かしても拡大されない。そのため、
/// マップをまとめたこのオブジェクトを拡大・移動する（背景はこの外に置き、拡大しない）
/// </summary>
public class MenuCameraZoom : MonoBehaviour
{
    [Tooltip("クローズアップ時の拡大率")]
    [SerializeField] private float zoom = 1.5f;
    [Tooltip("クローズアップしたボタンを置く画面上の位置（0〜1）。右から出る詳細パネルを避けて左寄りにする")]
    [SerializeField] private Vector2 focusViewport = new Vector2(0.33f, 0.5f);
    [Tooltip("クローズアップにかける時間（秒）")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutQuart;
    [Tooltip("元に戻るのにかける時間（秒）")]
    [SerializeField] private float resetDuration = 0.5f;
    // 戻りはパネルを閉じた直後の重いフレームと重なるので、動き出しがゆっくりなイージングにする
    // （OutQuart だとその1フレームで大半が動いてしまい、一瞬で戻ったように見える）
    [SerializeField] private Ease resetEase = Ease.InOutCubic;

    private RectTransform _view;

    void Awake()
    {
        _view = (RectTransform)transform;
    }

    public void FocusOn(RectTransform target)
    {
        Vector2 screenSize = ((RectTransform)_view.parent).rect.size;

        // 拡大前のマップ上でのボタン中心（今どれだけ拡大していても同じ値になる）
        Vector2 targetInView = _view.InverseTransformPoint(target.TransformPoint(target.rect.center));
        Vector2 focus = Vector2.Scale(focusViewport - new Vector2(0.5f, 0.5f), screenSize);

        // 背景は一緒に動かないので、端のボタンでも必ず focus の位置まで寄せる
        MoveTo(focus - targetInView * zoom, zoom, duration, ease);
    }

    public void ResetZoom()
    {
        MoveTo(Vector2.zero, 1f, resetDuration, resetEase);
    }

    private void MoveTo(Vector2 localPos, float scale, float time, Ease curve)
    {
        _view.DOKill();
        _view.DOLocalMove(localPos, time).SetEase(curve);
        _view.DOScale(scale, time).SetEase(curve);
    }
}
