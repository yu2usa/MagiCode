using TMPro;
using UnityEngine;

/// <summary>
/// チュートリアルアドバイス表示ウィンドウ（Canvas > AIAAWindow）。
/// テキスト量に応じてウィンドウの高さを自動調整し、AIIcon を縦中央に固定する。
/// TurnController からフェーズ開始時に ShowHint() / HideHint() を呼び出す。
/// </summary>
public class AIAAWindow : MonoBehaviour
{
    [Tooltip("アドバイスを表示する Text(TMP)（AdviceText）")]
    [SerializeField] private TextMeshProUGUI adviceText;

    [Tooltip("AIアイコン（RectTransform）。y座標をウィンドウ縦中央に自動合わせする")]
    [SerializeField] private RectTransform aiIconRect;

    [Tooltip("テキストの上下に加える余白（px）")]
    [SerializeField] private float verticalPadding = 30f;

    [Tooltip("ヒント表示中のみ表示する閉じる/開くボタン")]
    [SerializeField] private GameObject closeButton;

    private RectTransform _rect;

    // ユーザーが意図的に非表示にしているか（true の間は ShowHint() を無視）
    private bool _userHidden = false;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// ウィンドウを表示してアドバイステキストをセットし、高さを再計算する。
    /// ユーザーが非表示にしている場合はテキストのみ更新し、表示はしない。
    /// </summary>
    public void ShowHint(string message)
    {
        adviceText.text = message;
        if (closeButton) closeButton.SetActive(true);
        if (_userHidden) return;

        gameObject.SetActive(true);

        // テキストのレイアウトを確定させてから高さを取得
        Canvas.ForceUpdateCanvases();
        ResizeWindow();
    }

    /// <summary>
    /// システム側からウィンドウを非表示にする（_userHidden は変えない）
    /// </summary>
    public void HideHint()
    {
        gameObject.SetActive(false);
        if (closeButton) closeButton.SetActive(false);
    }

    /// <summary>
    /// ユーザーボタン用トグル。
    /// 非表示にした場合は次の ShowHint() 呼び出しでも表示されない。
    /// 再表示した場合はテキストが残っていれば即座に表示する。
    /// </summary>
    public void ToggleUserVisibility()
    {
        _userHidden = !_userHidden;

        if (_userHidden)
        {
            gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(adviceText.text))
        {
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            ResizeWindow();
        }
    }

    /// <summary>
    /// AdviceText の preferredHeight に基づいてウィンドウ高さを更新し、
    /// AIIcon を AIAAWindow の縦中央（anchoredPosition.y = 0）に配置する
    /// </summary>
    private void ResizeWindow()
    {
        // ウィンドウ高さ = テキスト高さ + 上下パディング
        float newHeight = (adviceText.preferredHeight + verticalPadding * 2f);
        _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, newHeight);

        // AIIcon の x は固定のまま、y をウィンドウ縦中央（= 0）に合わせる
        // AIIcon のアンカーが AIAAWindow の中心(0.5, 0.5)であることが前提
        if (aiIconRect != null)
            aiIconRect.anchoredPosition = new Vector2(aiIconRect.anchoredPosition.x, 0f);
    }
}
