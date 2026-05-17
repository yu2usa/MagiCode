using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// ステージクリア後に習得コードを1つずつ順番に表示するウィンドウ。
/// ステージ解放アニメーションの前にメニューシーンで中央表示される。
/// このスクリプトをウィンドウのルート GameObject にアタッチして使う。
/// </summary>
public class ObtainedCodeWindow : MonoBehaviour
{
    [Header("コンテンツ")]
    [Tooltip("各キーワードの行として生成するプレハブ（TMP_Text コンポーネント必須）")]
    [SerializeField] private TMP_Text keywordItemPrefab;

    [Tooltip("キーワードを縦に並べるコンテナ（VerticalLayoutGroup 推奨）")]
    [SerializeField] private Transform keywordContainer;

    [Tooltip("ウィンドウを閉じるボタン")]
    [SerializeField] private Button closeButton;

    [Header("ウィンドウアニメーション")]
    [Tooltip("フェード制御用 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("スケールアニメーション対象の RectTransform（ウィンドウ本体）")]
    [SerializeField] private RectTransform windowRect;

    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;

    [Header("アイテム逐次表示")]
    [Tooltip("次のキーワードを出すまでの待機時間（秒）")]
    [SerializeField] private float revealInterval = 0.4f;

    [Tooltip("1アイテムのフェードイン時間（秒）")]
    [SerializeField] private float itemFadeInDuration = 0.25f;

    private bool _closeRequested;
    private readonly List<GameObject> _spawnedItems = new List<GameObject>();

    /// <summary>
    /// ウィンドウを表示し、閉じられるまで待機するコルーチン。
    /// StageSelectManager.Start() から yield return で呼ぶ。
    /// </summary>
    public IEnumerator ShowAndWait(List<string> keywords)
    {
        Debug.Log($"[ObtainedCodeWindow] ShowAndWait開始: keywords={keywords.Count}, keywordContainer={keywordContainer != null}, keywordItemPrefab={keywordItemPrefab != null}");
        _closeRequested = false;
        ClearItems();

        // ウィンドウをフェードイン＋スケールアップで表示
        gameObject.SetActive(true);
        Debug.Log("[ObtainedCodeWindow] SetActive(true) 完了");
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (windowRect != null) windowRect.localScale = Vector3.one * 0.85f;

        if (canvasGroup != null)
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.Linear);
        if (windowRect != null)
            windowRect.DOScale(Vector3.one, fadeInDuration).SetEase(showEase);

        // 閉じるボタンはウィンドウ表示直後から有効（いつでも閉じられる）
        closeButton.onClick.AddListener(OnClose);

        // ウィンドウ表示完了を待ってからアイテムを逐次表示
        yield return new WaitForSeconds(fadeInDuration);
        yield return StartCoroutine(RevealKeywords(keywords));

        // 全アイテム表示後、まだ閉じていなければ待機
        if (!_closeRequested)
            yield return new WaitUntil(() => _closeRequested);

        closeButton.onClick.RemoveListener(OnClose);

        // フェードアウトして非表示
        if (canvasGroup != null)
            yield return canvasGroup.DOFade(0f, fadeOutDuration).WaitForCompletion();

        ClearItems();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// keywords を上から1つずつフェードイン＋スケールポップで表示する
    /// </summary>
    private IEnumerator RevealKeywords(List<string> keywords)
    {
        Debug.Log($"[ObtainedCodeWindow] RevealKeywords: prefab={keywordItemPrefab != null}, container={keywordContainer != null}");
        if (keywordItemPrefab == null || keywordContainer == null) yield break;

        foreach (var keyword in keywords)
        {
            // 閉じるボタンが押されたら途中でも終了
            if (_closeRequested) yield break;

            var item = Instantiate(keywordItemPrefab, keywordContainer);
            item.text = keyword;
            _spawnedItems.Add(item.gameObject);

            // CanvasGroup フェードイン（Unity fake-null 対策で == null で判定）
            var cg = item.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // スケールポップイン（VerticalLayoutGroup と干渉しない）
            item.transform.localScale = Vector3.one * 0.7f;

            cg.DOFade(1f, itemFadeInDuration);
            item.transform.DOScale(Vector3.one, itemFadeInDuration).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(revealInterval);
        }
    }

    private void ClearItems()
    {
        // _spawnedItems に加え、エディタ上に残留している子オブジェクト（"New Text"など）も全て破棄
        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);
        _spawnedItems.Clear();
    }

    private void OnClose() => _closeRequested = true;
}
