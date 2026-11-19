using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// タイトル画面から抜けるときの演出。ボタンは左へ、タイトルロゴは上へ、立ち絵は下へ、
/// それぞれ画面の外へ加速しながら出ていく
/// </summary>
public class TitleExitTransition : MonoBehaviour
{
    [Tooltip("左へ出ていくボタン（上から順に、少しずつずらして動かす）")]
    [SerializeField] private RectTransform[] buttons;
    [Tooltip("上へ出ていくタイトルロゴ")]
    [SerializeField] private RectTransform[] titles;
    [Tooltip("下へ出ていく立ち絵")]
    [SerializeField] private RectTransform[] illustrations;
    [SerializeField] private float duration = 0.5f;
    // 出ていく動きなので、ゆっくり動き出して画面の外で一番速くなるようにする
    [SerializeField] private Ease ease = Ease.InCubic;
    [Tooltip("ボタンを1つずつずらす間隔（秒）")]
    [SerializeField] private float buttonStagger = 0.05f;

    /// <summary>
    /// すべてが画面の外へ出きるまで待つ。演出中はボタンを押せないようにする
    /// </summary>
    public IEnumerator Play()
    {
        Vector2 screen = ((RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform).rect.size;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].GetComponent<Button>().interactable = false;
            Move(buttons[i], new Vector2(-screen.x, 0f), i * buttonStagger);
        }
        foreach (var title in titles)
            Move(title, new Vector2(0f, screen.y), 0f);
        foreach (var illustration in illustrations)
            Move(illustration, new Vector2(0f, -screen.y), 0f);

        yield return new WaitForSeconds(duration + (buttons.Length - 1) * buttonStagger);
    }

    private void Move(RectTransform target, Vector2 offset, float delay)
    {
        target.DOKill();
        target.DOAnchorPos(target.anchoredPosition + offset, duration).SetEase(ease).SetDelay(delay);
    }
}
