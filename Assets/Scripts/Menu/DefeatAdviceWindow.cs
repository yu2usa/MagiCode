using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 敗北後にメニュー画面で表示するアドバイスウィンドウ。
/// ObtainedCodeWindow と同様に StageSelectManager.Start() から yield return で呼ぶ。
/// </summary>
public class DefeatAdviceWindow : MonoBehaviour
{
    [Tooltip("アドバイス本文を表示する TMP_Text")]
    [SerializeField] private TMP_Text adviceText;

    [Tooltip("ウィンドウを閉じるボタン")]
    [SerializeField] private Button closeButton;

    [Header("アニメーション")]
    [Tooltip("フェード制御用 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("スケールアニメーション対象の RectTransform（ウィンドウ本体）")]
    [SerializeField] private RectTransform windowRect;

    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;

    private bool _closeRequested;

    /// <summary>
    /// ウィンドウを表示し、閉じられるまで待機するコルーチン。
    /// StageSelectManager.Start() から yield return で呼ぶ。
    /// </summary>
    public IEnumerator ShowAndWait(string advice)
    {
        _closeRequested = false;

        adviceText.text = advice;

        gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (windowRect != null)  windowRect.localScale = Vector3.one * 0.85f;

        if (canvasGroup != null)
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.Linear);
        if (windowRect != null)
            windowRect.DOScale(Vector3.one, fadeInDuration).SetEase(showEase);

        closeButton.onClick.AddListener(OnClose);

        yield return new WaitForSeconds(fadeInDuration);

        if (!_closeRequested)
            yield return new WaitUntil(() => _closeRequested);

        closeButton.onClick.RemoveListener(OnClose);

        if (canvasGroup != null)
            yield return canvasGroup.DOFade(0f, fadeOutDuration).WaitForCompletion();

        gameObject.SetActive(false);
    }

    private void OnClose() => _closeRequested = true;
}
