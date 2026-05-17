using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// ダメージ表示のポップアップ
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("上昇する距離（ワールドスペース単位 / UIの場合はピクセル換算）")]
    public float floatDistance = 1.0f;
    [Tooltip("アニメーション時間")]
    public float duration = 1.5f;
    [Tooltip("フェードアウト開始タイミング（0-1）")]
    public float fadeStartRatio = 0.5f;

    [Header("Text Settings")]
    [Tooltip("フォントサイズ")]
    public float fontSize = 5f;
    [Tooltip("テキストの色")]
    public Color textColor = Color.white;
    [Tooltip("クリティカル時の色")]
    public Color criticalColor = Color.yellow;

    // TMP_Textは TextMeshPro と TextMeshProUGUI の共通基底クラス
    private TMP_Text _textMesh;

    void Awake()
    {
        _textMesh = GetComponent<TMP_Text>();
        if (_textMesh == null)
        {
            // TMP_Text がなければ新規生成してfontSizeを適用
            _textMesh = gameObject.AddComponent<TextMeshPro>();
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontSize = fontSize;
        }
        // 既存のTMP_TextがあればPrefab側のfontSizeを維持する

        if (_textMesh is TextMeshPro tmp3D)
            tmp3D.sortingOrder = 100;
    }

    /// <summary>
    /// ダメージを表示してアニメーション
    /// </summary>
    public void Show(int damage, bool isCritical = false)
    {
        _textMesh.text = damage.ToString();
        _textMesh.color = isCritical ? criticalColor : textColor;
        PlayFloatAnimation();
    }

    /// <summary>
    /// 任意テキストを表示してアニメーション（MP軽減通知などに使用）
    /// </summary>
    public void ShowText(string text, Color color)
    {
        _textMesh.text = text;
        _textMesh.color = color;
        PlayFloatAnimation();
    }

    /// <summary>
    /// 浮上→フェードアウト→スケールポップのアニメーションを再生
    /// </summary>
    private void PlayFloatAnimation()
    {
        Sequence seq = DOTween.Sequence();
        RectTransform rt = GetComponent<RectTransform>();

        if (rt != null)
        {
            // UIプレハブの場合: anchoredPosition で浮上
            // ワールド単位 → スクリーンピクセル → Canvasローカル単位 に変換
            float pixelFloat = CalcUIFloatPixels();
            Vector2 startAP = rt.anchoredPosition;
            seq.Append(rt.DOAnchorPos(startAP + Vector2.up * pixelFloat, duration).SetEase(Ease.OutQuad));
        }
        else
        {
            // ワールドスペース TextMeshPro の場合
            Vector3 startPos = transform.position;
            seq.Append(transform.DOMove(startPos + Vector3.up * floatDistance, duration).SetEase(Ease.OutQuad));
        }

        // フェードアウト
        float fadeDelay = duration * fadeStartRatio;
        float fadeDuration = duration * (1f - fadeStartRatio);
        seq.Insert(fadeDelay, _textMesh.DOFade(0f, fadeDuration));

        // スケールアニメーション（ポップ効果）
        transform.localScale = Vector3.zero;
        seq.Insert(0f, transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));

        seq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// floatDistance（ワールド単位）をCanvasローカルピクセルに変換
    /// </summary>
    private float CalcUIFloatPixels()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || Camera.main == null) return 80f;

        // 1ワールド単位分のスクリーンピクセル数を計算
        float y0 = Camera.main.WorldToScreenPoint(Vector3.zero).y;
        float y1 = Camera.main.WorldToScreenPoint(Vector3.up).y;
        float pixelsPerUnit = Mathf.Abs(y1 - y0);

        // CanvasのscaleFactorでスクリーンピクセル → Canvasローカル単位に変換
        return floatDistance * pixelsPerUnit / canvas.scaleFactor;
    }

    /// <summary>
    /// ダメージインジケーターを生成して表示（フォールバック用）
    /// </summary>
    public static void Spawn(Vector3 position, int damage, bool isCritical = false)
    {
        GameObject obj = new GameObject("DamageIndicator");
        obj.transform.position = position + Vector3.up * 0.5f;
        var indicator = obj.AddComponent<DamageIndicator>();
        indicator.Show(damage, isCritical);
    }

    /// <summary>
    /// Prefabからテキストインジケーターを生成（MP軽減通知などに使用）
    /// </summary>
    public static void SpawnTextFromPrefab(GameObject prefab, Vector3 worldPosition, string text, Color color)
    {
        if (prefab == null) return;

        if (prefab.GetComponent<RectTransform>() != null)
        {
            Canvas canvas = null;
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
                canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            GameObject obj = Instantiate(prefab, canvas.transform);
            obj.transform.SetAsLastSibling();

            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPoint);
            rt.anchoredPosition = localPoint;

            var indicator = obj.GetComponent<DamageIndicator>();
            if (indicator == null)
                indicator = obj.AddComponent<DamageIndicator>();
            indicator.ShowText(text, color);
        }
        else
        {
            GameObject obj = Instantiate(prefab, worldPosition + Vector3.up * 0.5f, Quaternion.identity);
            var indicator = obj.GetComponent<DamageIndicator>();
            if (indicator == null)
                indicator = obj.AddComponent<DamageIndicator>();
            indicator.ShowText(text, color);
        }
    }

    /// <summary>
    /// Prefabからダメージインジケーターを生成
    /// UIプレハブ（RectTransform）はCanvas内にインスタンス化し、ワールド座標を変換して配置
    /// </summary>
    public static void SpawnFromPrefab(GameObject prefab, Vector3 worldPosition, int damage, bool isCritical = false)
    {
        if (prefab == null)
        {
            Spawn(worldPosition, damage, isCritical);
            return;
        }

        if (prefab.GetComponent<RectTransform>() != null)
        {
            // "Canvas" という名前のCanvasを優先して取得、なければ最初のCanvasを使用
            Canvas canvas = null;
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
                canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                Spawn(worldPosition, damage, isCritical);
                return;
            }

            GameObject obj = Instantiate(prefab, canvas.transform);

            // 最前面に描画されるよう一番最後の子にする
            obj.transform.SetAsLastSibling();

            RectTransform rt = obj.GetComponent<RectTransform>();

            // ワールド座標 → スクリーン座標 → Canvasローカル座標 に変換
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPoint);
            rt.anchoredPosition = localPoint;

            var indicator = obj.GetComponent<DamageIndicator>();
            if (indicator == null)
                indicator = obj.AddComponent<DamageIndicator>();
            indicator.Show(damage, isCritical);
        }
        else
        {
            // ワールドスペースプレハブ
            GameObject obj = Instantiate(prefab, worldPosition + Vector3.up * 0.5f, Quaternion.identity);
            var indicator = obj.GetComponent<DamageIndicator>();
            if (indicator == null)
                indicator = obj.AddComponent<DamageIndicator>();
            indicator.Show(damage, isCritical);
        }
    }
}
