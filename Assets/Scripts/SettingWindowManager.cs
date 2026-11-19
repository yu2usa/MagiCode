using UnityEngine;

/// <summary>
/// 設定ウィンドウの開閉。ほかのウィンドウと同じく、背景をグレーアウトしながら下からスライドして出す
/// </summary>
public class SettingWindowManager : MonoBehaviour
{
    [SerializeField] SlideWindow settingWindow;

    public void OpenSettingWindow()
    {
        settingWindow.Open();
    }

    public void CloseSettingWindow()
    {
        settingWindow.Close();
    }
}
