using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AIAAWindow のヒント表示/非表示をトグルするボタン。
/// このスクリプトをボタン GameObject にアタッチし、hintWindow を Inspector で設定する。
/// </summary>
public class HintToggleButton : MonoBehaviour
{
    [Tooltip("トグル対象の AIAAWindow")]
    [SerializeField] private AIAAWindow hintWindow;

    [Tooltip("ボタンのラベル TMP_Text")]
    [SerializeField] private TMP_Text buttonLabel;

    private const string LabelVisible = "チュートリアルを\n非表示";
    private const string LabelHidden  = "チュートリアルを\n表示";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        hintWindow.ToggleUserVisibility();
        // _userHidden は AIAAWindow 内部なので、gameObject.activeSelf で現在状態を判断
        buttonLabel.text = hintWindow.gameObject.activeSelf ? LabelVisible : LabelHidden;
    }
}
