using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 勝利画面の演出。VICTORY のロゴを画面中央に出し、上へ移動させながら背景をグレーアウトして、
/// 戦績と「メニューに戻る」ボタンを表示する
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private RectTransform logo;
    [Tooltip("背景を暗くする全画面の画像（勝利画面の土台）")]
    [SerializeField] private Image dim;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private CanvasGroup stats;
    [SerializeField] private CanvasGroup menuButton;

    [Header("演出")]
    [Tooltip("グレーアウト時の背景の濃さ（0〜1）")]
    [SerializeField] private float dimAlpha = 0.6f;
    [Tooltip("ロゴが上へ移動した後のY座標（画面中央からの距離）")]
    [SerializeField] private float logoRaisedY = 300f;
    [Tooltip("ロゴを画面中央に見せておく時間（秒）")]
    [SerializeField] private float logoHoldTime = 0.8f;

    public IEnumerator Play(BattleRecord record)
    {
        statsText.text =
            $"かかったターン数<pos=72%>{record.turns}\n" +
            $"エラーなく実行できた回数<pos=72%>{record.cleanRuns}\n" +
            $"行動回数<pos=72%>{record.actions}\n" +
            $"与えたダメージ<pos=72%>{record.damageDealt}\n" +
            $"受けたダメージ<pos=72%>{record.damageTaken}";

        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
        logo.anchoredPosition = Vector2.zero;
        logo.localScale = Vector3.zero;
        stats.alpha = 0f;
        menuButton.alpha = 0f;
        menuButton.interactable = false;
        gameObject.SetActive(true);

        // ロゴを画面中央にポップ表示
        yield return logo.DOScale(1f, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(logoHoldTime);

        // ロゴを上へ移動させながら、その後ろの画面をグレーアウト
        logo.DOAnchorPosY(logoRaisedY, 0.5f).SetEase(Ease.InOutCubic);
        yield return dim.DOFade(dimAlpha, 0.5f).WaitForCompletion();

        yield return stats.DOFade(1f, 0.4f).WaitForCompletion();
        menuButton.interactable = true;
        menuButton.DOFade(1f, 0.3f);
    }
}
