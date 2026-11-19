using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// メニュー画面に入ったときの演出。ステージボタンとそれをつなぐ線をフェードインさせ、
/// そのあと（初回の会話があるときは会話が終わってから）、設定・タイトルへ戻る・戦術書のボタンを上からスライドインさせる
/// </summary>
public class MenuEntrance : MonoBehaviour
{
    [Tooltip("ステージボタンと線をまとめたもの（スクロールするマップ）")]
    [SerializeField] private CanvasGroup map;
    [Tooltip("画面の上から入ってくるボタン（設定・タイトルへ戻る・戦術書）")]
    [SerializeField] private RectTransform[] topButtons;
    [Tooltip("初回の会話")]
    [SerializeField] private MenuTutorialManager tutorial;
    [SerializeField] private float mapFadeDuration = 0.6f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private Ease slideEase = Ease.OutBack;
    [Tooltip("上のボタンを1つずつずらす間隔（秒）")]
    [SerializeField] private float buttonStagger = 0.08f;

    IEnumerator Start()
    {
        // 最初の描画より前に、マップを透明に・上のボタンを画面の上の外へ置いておく
        float screenHeight = ((RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform).rect.height;
        var shownPositions = new Vector2[topButtons.Length];
        for (int i = 0; i < topButtons.Length; i++)
        {
            shownPositions[i] = topButtons[i].anchoredPosition;
            topButtons[i].anchoredPosition += new Vector2(0f, screenHeight);
        }
        map.alpha = 0f;
        map.blocksRaycasts = false;

        yield return map.DOFade(1f, mapFadeDuration).WaitForCompletion();
        map.blocksRaycasts = true;

        if (tutorial.ShouldAutoPlay)
            yield return tutorial.PlayTutorial();

        for (int i = 0; i < topButtons.Length; i++)
            topButtons[i].DOAnchorPos(shownPositions[i], slideDuration).SetEase(slideEase).SetDelay(i * buttonStagger);
    }
}
