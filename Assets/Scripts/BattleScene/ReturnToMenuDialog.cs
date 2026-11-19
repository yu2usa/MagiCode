using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 戦闘中に「メニューに戻る」ボタンを押したときの確認ウィンドウ。画面下からスライドして出てくる。
/// はい → メニューへ戻る、いいえ・ウィンドウの外 → 下へ引っ込んで戦闘に戻る
/// </summary>
public class ReturnToMenuDialog : MonoBehaviour
{
    [SerializeField] private RectTransform window;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [Tooltip("ウィンドウの外の暗幕（押すと閉じる）")]
    [SerializeField] private Button outsideArea;
    [Tooltip("遷移先のメニューシーン名")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("演出")]
    // 開いた直後のフレームは表示の準備で重くなりやすいので、動き出しがゆっくりなイージングにする
    // （OutQuart だとその1フレームで大半が動き、スライドせずに出たように見える）
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private Ease slideInEase = Ease.InOutCubic;
    [SerializeField] private float slideOutDuration = 0.25f;
    [SerializeField] private Ease slideOutEase = Ease.InQuad;

    private Image _dim;
    private float _dimAlpha;
    private float _shownY;

    void Awake()
    {
        _dim = outsideArea.GetComponent<Image>();
        _dimAlpha = _dim.color.a;
        _shownY = window.anchoredPosition.y;

        yesButton.onClick.AddListener(ReturnToMenu);
        noButton.onClick.AddListener(Close);
        outsideArea.onClick.AddListener(Close);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        KillTweens();

        // 画面の下の外から定位置へ
        window.anchoredPosition = new Vector2(window.anchoredPosition.x, HiddenY());
        window.DOAnchorPosY(_shownY, slideInDuration).SetEase(slideInEase);
        _dim.color = new Color(_dim.color.r, _dim.color.g, _dim.color.b, 0f);
        _dim.DOFade(_dimAlpha, slideInDuration);
    }

    private void Close()
    {
        KillTweens();
        _dim.DOFade(0f, slideOutDuration);
        window.DOAnchorPosY(HiddenY(), slideOutDuration).SetEase(slideOutEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // この確認ウィンドウは画面いっぱいの大きさなので、その高さぶん下げれば画面外になる
    private float HiddenY()
    {
        return _shownY - ((RectTransform)transform).rect.height;
    }

    private void KillTweens()
    {
        window.DOKill();
        _dim.DOKill();
    }

    private void ReturnToMenu()
    {
        // とどめのスロー演出中に戻った場合も、メニューは通常の速さにする
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
