using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 習得コード一覧の1行アイテム。
/// ホバーで説明ツールチップを表示し、クリックでコードの例を入力欄へ貼り付ける（戦闘中のみ）。
/// Prefab 構成: GameObject + KeywordItem + TMP_Text (label)
/// </summary>
public class KeywordItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text label;

    private string _description;
    private Action<string> _onHover;
    private Action _onExit;
    private Action _onClick;

    /// <summary>
    /// アイテムを初期化する。StrategyBookWindowManager から呼ぶ。貼り付けられないときは onClick に null を渡す
    /// </summary>
    public void Setup(string keyword, string description, Action<string> onHover, Action onExit, Action onClick)
    {
        label.text = keyword;
        _description = description;
        _onHover = onHover;
        _onExit = onExit;
        _onClick = onClick;
    }

    public void OnPointerEnter(PointerEventData eventData) => _onHover?.Invoke(_description);
    public void OnPointerExit(PointerEventData eventData)  => _onExit?.Invoke();
    public void OnPointerClick(PointerEventData eventData) => _onClick?.Invoke();
}
