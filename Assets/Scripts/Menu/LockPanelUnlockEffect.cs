using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ロックパネルの解除演出。
/// 点滅 → 破片飛散 → 接続線が左から右へ露わになる。
/// ロックパネルの GameObject にアタッチして使用する。
/// </summary>
public class LockPanelUnlockEffect : MonoBehaviour
{
    [Header("点滅")]
    [Tooltip("点滅する時間（秒）")]
    [SerializeField] private float blinkDuration = 0.5f;
    [Tooltip("点滅回数（偶数推奨）")]
    [SerializeField] private int blinkCount = 6;

    [Header("破片")]
    [Tooltip("生成する破片の数")]
    [SerializeField] private int fragmentCount = 12;
    [Tooltip("破片が飛ぶ距離（Canvasピクセル単位）")]
    [SerializeField] private float fragmentFlyDistance = 120f;
    [Tooltip("破片アニメーションの時間（秒）")]
    [SerializeField] private float fragmentDuration = 0.6f;
    [Tooltip("破片のベースサイズ")]
    [SerializeField] private Vector2 fragmentSize = new Vector2(15f, 15f);

    [Header("接続線")]
    [Tooltip("グレーアウト解除アニメーションの時間（秒）")]
    [SerializeField] private float lineRevealDuration = 1.0f;
    [Tooltip("ロック中の接続線のグレーアウト色（StageSelectManager の lockedLineColor と合わせること）")]
    [SerializeField] private Color lineLockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    /// <summary>
    /// 解除アニメーションを再生する。
    /// connectionLine は点滅終了直後から左→右へフィルで出現する。
    /// </summary>
    public void Play(GameObject connectionLine)
    {
        StartCoroutine(PlaySequence(connectionLine));
    }

    private IEnumerator PlaySequence(GameObject connectionLine)
    {
        // CanvasGroup が無ければ自動追加（Unity の fake-null に対応するため == null で判定）
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        // 接続線をグレーアウト状態に設定（最初の yield 前に確定させることで、
        // アニメーション開始時から確実にグレー状態になる）
        if (connectionLine != null)
        {
            connectionLine.SetActive(true);
            var img = connectionLine.GetComponent<Image>();
            if (img != null)
            {
                img.color = lineLockedColor;
                img.type = Image.Type.Simple;
            }
        }

        // --- 1. 点滅（alpha を 0/1 で交互切り替え） ---
        float interval = blinkDuration / blinkCount;
        for (int i = 0; i < blinkCount; i++)
        {
            cg.alpha = (i % 2 == 0) ? 0f : 1f;
            yield return new WaitForSeconds(interval);
        }

        // --- 2. 破片を飛ばす ---
        SpawnFragments();

        // --- 3. パネルを透明化して非活性に（SetActive だとコルーチンが止まるため CanvasGroup で隠す） ---
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 破片アニメーション完了まで待機してからパネルを完全に非表示
        yield return new WaitForSeconds(fragmentDuration);
        gameObject.SetActive(false);

        // --- 4. ステージアイコン解除後に接続線のグレーアウトを解除 ---
        // パネルが完全に消えてから線のアニメーションを開始する
        if (connectionLine != null)
            RevealLine(connectionLine);

        yield return new WaitForSeconds(lineRevealDuration);
    }

    /// <summary>
    /// パネルの外観（色・サイズ・位置）を元に破片を Canvas 上に生成して飛ばす
    /// </summary>
    private void SpawnFragments()
    {
        // ルートの Canvas を取得（破片はここの直下に生成して panel の非表示後も表示を維持する）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        if (canvas == null) return;

        var panelRect = GetComponent<RectTransform>();
        var canvasRect = canvas.GetComponent<RectTransform>();
        Color baseColor = GetComponent<Image>()?.color ?? Color.white;
        Vector2 panelSize = panelRect.rect.size;

        // パネルの Canvas 座標系での中心を算出
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, panelRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 center);

        for (int i = 0; i < fragmentCount; i++)
        {
            var fragObj = new GameObject("LockFragment");
            fragObj.transform.SetParent(canvas.transform, false);
            fragObj.transform.SetAsLastSibling();

            var img = fragObj.AddComponent<Image>();
            img.color = baseColor;

            var rt = fragObj.GetComponent<RectTransform>();
            rt.sizeDelta = fragmentSize * Random.Range(0.5f, 1.5f);

            // パネル内のランダム位置に配置
            rt.anchoredPosition = center + new Vector2(
                Random.Range(-panelSize.x * 0.4f, panelSize.x * 0.4f),
                Random.Range(-panelSize.y * 0.4f, panelSize.y * 0.4f)
            );

            // ランダム方向（上方向に偏らせて自然な崩壊感に）
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1.5f)).normalized;
            float dist = fragmentFlyDistance * Random.Range(0.6f, 1.4f);
            float rot = Random.Range(-540f, 540f);
            Vector2 startPos = rt.anchoredPosition;

            var seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPos(startPos + dir * dist, fragmentDuration).SetEase(Ease.OutQuad));
            seq.Join(rt.DOLocalRotate(new Vector3(0, 0, rot), fragmentDuration));
            seq.Join(img.DOFade(0f, fragmentDuration * 0.7f).SetDelay(fragmentDuration * 0.2f));
            seq.OnComplete(() => { if (fragObj != null) Destroy(fragObj); });
        }
    }

    /// <summary>
    /// 接続線のグレーアウトを解除する。グレー色から元の色へ lineRevealDuration 秒かけてアニメーションする。
    /// </summary>
    private void RevealLine(GameObject lineObj)
    {
        lineObj.SetActive(true);

        var lineImage = lineObj.GetComponent<Image>();
        if (lineImage == null) return;

        // グレーアウト色から白へ lineRevealDuration 秒かけてアニメーション
        lineImage.DOColor(Color.white, lineRevealDuration).SetEase(Ease.Linear);
    }
}
