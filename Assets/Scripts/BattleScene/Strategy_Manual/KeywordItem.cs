using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 習得コード一覧の1行アイテム。
/// ホバーで説明ツールチップを表示する。
/// Prefab 構成: GameObject + KeywordItem + TMP_Text (label)
/// </summary>
public class KeywordItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;

    private string _description;
    private Action<string> _onHover;
    private Action _onExit;

    /// <summary>
    /// アイテムを初期化する。StrategyBookWindowManager から呼ぶ。
    /// </summary>
    public void Setup(string keyword, string description, Action<string> onHover, Action onExit)
    {
        label.text = keyword;
        _description = description;
        _onHover = onHover;
        _onExit = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData) => _onHover?.Invoke(_description);
    public void OnPointerExit(PointerEventData eventData)  => _onExit?.Invoke();
}
