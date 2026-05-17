using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// バトル開始前のdialogue末尾に表示するコード獲得ウィンドウ。
/// キーワードを一括表示（アニメーションなし）し、閉じるまたはSkipAllで非表示になる。
/// PreBattleDialogueManager に SerializeField で登録して使う。
/// </summary>
public class PreBattleKeywordsWindow : MonoBehaviour
{
    [Tooltip("キーワードを縦に並べる TMP_Text")]
    [SerializeField] private TMP_Text contentText;

    [Tooltip("ウィンドウを閉じるボタン")]
    [SerializeField] private Button closeButton;

    [Tooltip("フェード制御用 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    private bool _closeRequested;

    /// <summary>
    /// キーワードを表示し、閉じるかスキップされるまで待機する。
    /// PreBattleDialogueManager.Play() 内から yield return で呼ぶ。
    /// checkSkip: SkipAll が呼ばれたか確認するデリゲート。
    /// </summary>
    public IEnumerator ShowAndWait(List<string> keywords, Func<bool> checkSkip)
    {
        _closeRequested = false;

        // キーワードを改行で一括表示（アニメーションなし）
        contentText.text = string.Join("\n", keywords);

        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        yield return canvasGroup.DOFade(1f, fadeInDuration).WaitForCompletion();

        closeButton.onClick.AddListener(OnClose);
        yield return new WaitUntil(() => _closeRequested || checkSkip());
        closeButton.onClick.RemoveListener(OnClose);

        // 閉じるボタンの場合はフェードアウト、スキップの場合は即非表示
        if (_closeRequested)
            yield return canvasGroup.DOFade(0f, fadeOutDuration).WaitForCompletion();

        gameObject.SetActive(false);
    }

    private void OnClose() => _closeRequested = true;
}
