using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ストーリー会話を次のセリフへ進める入力の判定。
/// ボタンなどクリックで動くUIの上でのクリックは、そのUIの操作なので会話送りには使わない
/// </summary>
public static class DialogueInput
{
    private static readonly List<RaycastResult> _hits = new List<RaycastResult>();

    public static bool AdvancePressed()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            return true;
        return Input.GetMouseButtonDown(0) && !IsPointerOnClickableUI();
    }

    // クリック位置の一番手前のUIが、自身または親でクリックを受け取るものか
    private static bool IsPointerOnClickableUI()
    {
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _hits.Clear();
        EventSystem.current.RaycastAll(pointer, _hits);
        if (_hits.Count == 0) return false;

        GameObject top = _hits[0].gameObject;
        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(top) != null
            || ExecuteEvents.GetEventHandler<IPointerDownHandler>(top) != null;
    }
}
