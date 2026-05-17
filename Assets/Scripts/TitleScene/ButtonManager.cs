using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] GameObject creditWindow;
    
    public void StartGame()
    {
        SceneManager.LoadScene("Menu");
    }

    public void OpenCredit()
    {
        creditWindow.SetActive(true);
    }
    public void CloseCredit()
    {
        creditWindow.SetActive(false);
    }

    

    public void ExitGame()
    {
        Application.Quit();
    }
}
