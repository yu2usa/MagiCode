using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// コード実行中と敵の行動中の画面。必要なUI（HP/APゲージ・ダメージ表示・コード表示・設定などのボタン）以外を
/// 画面外へ退かせ、画面の上下にレターボックスを出す。プレイヤーの入力に戻るときに元の位置へ戻す
/// </summary>
public class BattleActionView : MonoBehaviour
{
    [Tooltip("実行中・敵の行動中も画面に残すUI（キャンバス直下）。HP/APゲージとダメージ表示は自動で残る")]
    [SerializeField] private RectTransform[] keepOnScreen;
    [Tooltip("画面上端のレターボックス（上端基準・高さぶん上に隠しておく）")]
    [SerializeField] private RectTransform letterboxTop;
    [Tooltip("画面下端のレターボックス（下端基準・高さぶん下に隠しておく）")]
    [SerializeField] private RectTransform letterboxBottom;
    [Tooltip("キャンバス直下以外で退かせるUI（実行ボタンなど）。近い画面端の外へ移動する")]
    [SerializeField] private RectTransform[] alsoHide;
    [Tooltip("戦闘表示の間は枠を消したままにするコードウィンドウ")]
    [SerializeField] private CodeWindowLook codeWindowLook;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [Tooltip("レターボックスが出きってから行動を始めるまでの間（秒）")]
    [SerializeField] private float actionStartDelay = 0.3f;

    // 退かせたUIと元の位置
    private readonly Dictionary<RectTransform, Vector2> _moved = new Dictionary<RectTransform, Vector2>();
    private RectTransform _canvasRect;
    private bool _isActionView;

    void Awake()
    {
        _canvasRect = (RectTransform)letterboxTop.parent;
    }

    /// <summary>
    /// 戦闘表示に切り替え、レターボックスが出きってから少し間を置くまで待つ。
    /// すでに戦闘表示なら待たない（行動はそのまま続ける）
    /// </summary>
    public IEnumerator Enter()
    {
        if (_isActionView) yield break;
        _isActionView = true;

        Vector2 canvasSize = _canvasRect.rect.size;
        foreach (RectTransform ui in _canvasRect)
        {
            if (!ui.gameObject.activeSelf || StaysOnScreen(ui)) continue;
            MoveOffscreen(ui, canvasSize);
        }
        foreach (var ui in alsoHide)
            MoveOffscreen(ui, canvasSize);
        codeWindowLook.SetActionView(true);
        SlideLetterbox(0f);

        yield return new WaitForSeconds(slideDuration + actionStartDelay);
    }

    private void MoveOffscreen(RectTransform ui, Vector2 canvasSize)
    {
        // 戦闘開始時のスライドインなど途中の動きは終わらせてから、戻す位置を覚える
        ui.DOKill(true);
        _moved[ui] = ui.anchoredPosition;
        ui.DOAnchorPos(ui.anchoredPosition + OffscreenOffset(ui, _canvasRect, canvasSize), slideDuration)
            .SetEase(slideEase);
    }

    public void Exit()
    {
        if (!_isActionView) return;
        _isActionView = false;

        foreach (var pair in _moved)
        {
            pair.Key.DOKill();
            pair.Key.DOAnchorPos(pair.Value, slideDuration).SetEase(slideEase);
        }
        _moved.Clear();
        codeWindowLook.SetActionView(false);
        SlideLetterbox(1f);
    }

    private bool StaysOnScreen(RectTransform ui)
    {
        return ui == letterboxTop || ui == letterboxBottom
            || System.Array.IndexOf(keepOnScreen, ui) >= 0
            || ui.GetComponent<GaugeTrail>() != null
            || ui.GetComponent<DamageIndicator>() != null;
    }

    // hidden=0 で画面内、1 で帯の高さぶん画面外
    private void SlideLetterbox(float hidden)
    {
        letterboxTop.DOKill();
        letterboxTop.DOAnchorPosY(letterboxTop.rect.height * hidden, slideDuration).SetEase(slideEase);
        letterboxBottom.DOKill();
        letterboxBottom.DOAnchorPosY(-letterboxBottom.rect.height * hidden, slideDuration).SetEase(slideEase);
    }

    /// <summary>
    /// 画面中心から見て、縦横のうち離れている方向の画面外へ出すための移動量
    /// </summary>
    public static Vector2 OffscreenOffset(RectTransform ui, RectTransform canvasRect, Vector2 canvasSize)
    {
        Vector2 center = canvasRect.InverseTransformPoint(ui.TransformPoint(ui.rect.center));
        Vector2 rel = new Vector2(center.x / canvasSize.x, center.y / canvasSize.y);
        if (Mathf.Abs(rel.x) >= Mathf.Abs(rel.y))
            return new Vector2(Mathf.Sign(rel.x) * canvasSize.x, 0f);
        return new Vector2(0f, Mathf.Sign(rel.y) * canvasSize.y);
    }
}
