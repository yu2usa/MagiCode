using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 上手なコードが決まった瞬間に、画面上部へ大きく褒め言葉を出す（例: ループの中の行動で敵を倒した）。
/// とどめのスロー演出やヒットストップで止まらないよう、実時間で動かす
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PraiseStamp : MonoBehaviour
{
    public static PraiseStamp Instance { get; private set; }

    [SerializeField] private TMP_Text label;
    [Tooltip("褒め言葉の下に出す、テラのひと言")]
    [SerializeField] private TMP_Text speech;
    // テラのひと言を読み切れて、とどめの後に勝利ロゴが上へ動き出す前には消える長さ
    [Tooltip("出しておく時間（秒）")]
    [SerializeField] private float holdTime = 1.8f;
    [Tooltip("空の明るい背景でも読めるよう、文字に付ける縁取りの太さ")]
    [SerializeField] private float outlineWidth = 0.25f;

    private CanvasGroup _group;

    // シーンでは表示したまま透明にしておく（非表示だと Awake が走らず Instance が入らない）
    void Awake()
    {
        Instance = this;
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        // 縁取りは実行時に付ける（エディタで付けるとシーンに保存されない複製マテリアルができる）
        label.outlineWidth = outlineWidth;
        label.outlineColor = Color.black;
    }

    public void Show(string text, string teraLine)
    {
        label.text = text;
        speech.text = teraLine;
        transform.DOKill();
        _group.DOKill();

        _group.alpha = 1f;
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        _group.DOFade(0f, 0.4f).SetDelay(holdTime).SetUpdate(true);
    }
}
