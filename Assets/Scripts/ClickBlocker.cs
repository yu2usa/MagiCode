using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// クリックを受け止めて、親へ伝えないようにする。
/// 子をクリックしても子に処理がないと、クリックは親のボタンまで伝わる（暗幕の「閉じる」が押されてしまう）ので、
/// ウィンドウ本体に付けて止める
/// </summary>
public class ClickBlocker : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
    }
}
