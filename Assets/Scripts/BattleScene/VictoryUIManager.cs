using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Victory UI に付けるスクリプト。
/// 「メニューに戻る」ボタンでメニューシーンへ遷移する。
/// </summary>
public class VictoryUIManager : MonoBehaviour
{
    [Tooltip("メニューに戻るボタン")]
    [SerializeField] private Button returnToMenuButton;

    [Tooltip("遷移先のメニューシーン名")]
    [SerializeField] private string menuSceneName = "Menu";

    void Start()
    {
        returnToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(menuSceneName));
    }
}
