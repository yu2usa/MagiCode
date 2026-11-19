using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 敗北画面の演出。勝利画面と同じく DEFEAT のロゴを画面中央に出し、上へ移動させながら背景をグレーアウトして、
/// ステージのアドバイスと「もう一度」「メニューへ」ボタンを表示する。
/// 何度も負けているときは「ヒントを見る」ボタンも出し、押すとアドバイスの代わりに答えのひな形を見せる
/// </summary>
public class DefeatScreen : MonoBehaviour
{
    [SerializeField] private RectTransform logo;
    [Tooltip("背景を暗くする全画面の画像（敗北画面の土台）")]
    [SerializeField] private Image dim;
    [SerializeField] private TMP_Text adviceText;
    [SerializeField] private CanvasGroup advice;
    [Tooltip("「もう一度」「メニューへ」「ヒントを見る」をまとめたもの（ロゴの演出が終わるまで押せない）")]
    [SerializeField] private CanvasGroup buttons;
    [SerializeField] private Button retryButton;

    [Header("救済ヒント")]
    [SerializeField] private Button hintButton;
    [Tooltip("答えのひな形を出す文字（コードの字下げが崩れないよう左寄せ）")]
    [SerializeField] private TMP_Text hintText;

    [Header("演出")]
    [Tooltip("グレーアウト時の背景の濃さ（0〜1）")]
    [SerializeField] private float dimAlpha = 0.6f;
    [Tooltip("ロゴが上へ移動した後のY座標（画面中央からの距離）")]
    [SerializeField] private float logoRaisedY = 300f;
    [Tooltip("ロゴを画面中央に見せておく時間（秒）")]
    [SerializeField] private float logoHoldTime = 0.8f;

    /// <summary>
    /// アドバイスが空のステージでは本文を出さない。hint が空なら「ヒントを見る」は出さない
    /// </summary>
    public IEnumerator Play(string adviceMessage, string hint, UnityAction onRetry)
    {
        adviceText.text = adviceMessage;
        adviceText.gameObject.SetActive(!string.IsNullOrEmpty(adviceMessage));
        hintText.gameObject.SetActive(false);
        hintText.text = "<b>テラのヒント</b>\n" + hint;
        hintButton.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        hintButton.onClick.AddListener(ShowHint);
        retryButton.onClick.AddListener(onRetry);

        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
        logo.anchoredPosition = Vector2.zero;
        logo.localScale = Vector3.zero;
        advice.alpha = 0f;
        buttons.alpha = 0f;
        buttons.interactable = false;
        gameObject.SetActive(true);

        // ロゴを画面中央にポップ表示
        yield return logo.DOScale(1f, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(logoHoldTime);

        // ロゴを上へ移動させながら、その後ろの画面をグレーアウト
        logo.DOAnchorPosY(logoRaisedY, 0.5f).SetEase(Ease.InOutCubic);
        yield return dim.DOFade(dimAlpha, 0.5f).WaitForCompletion();

        yield return advice.DOFade(1f, 0.4f).WaitForCompletion();
        buttons.interactable = true;
        buttons.DOFade(1f, 0.3f);
    }

    // アドバイスの場所に答えのひな形を出す（「もう一度」で戻ったとき、それを見ながら書き直せる）
    private void ShowHint()
    {
        adviceText.gameObject.SetActive(false);
        hintText.gameObject.SetActive(true);
        hintButton.gameObject.SetActive(false);
    }
}
