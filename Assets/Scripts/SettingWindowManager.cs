using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingWindowManager : MonoBehaviour
{
    [SerializeField] GameObject settingWindow;
    public void OpenSettingWindow()
    {
        settingWindow.SetActive(true);
    }

    public void CloseSettingWindow()
    {
        settingWindow.SetActive(false);
    }

}
