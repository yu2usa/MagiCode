using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ButtonManager : MonoBehaviour
{
    // ほかのウィンドウと同じく、背景をグレーアウトしながら下からスライドして出す
    [SerializeField] SlideWindow creditWindow;
    
    [Tooltip("ゲームを開始したとき、ボタン・ロゴ・立ち絵を画面の外へ出す演出")]
    [SerializeField] TitleExitTransition exitTransition;

    public void StartGame()
    {
        StartCoroutine(StartGameAfterExit());
    }

    // タイトルの要素が画面の外へ出きってから、メニューへ移る
    private IEnumerator StartGameAfterExit()
    {
        yield return exitTransition.Play();

        var intro = FindObjectOfType<TitleIntroManager>();
        if (intro != null && intro.ShouldPlay)
            yield return intro.PlayIntroThenLoad("Menu");
        else
            SceneManager.LoadScene("Menu");
    }

    public void OpenCredit()
    {
        creditWindow.Open();
    }
    public void CloseCredit()
    {
        creditWindow.Close();
    }

    

    public void ExitGame()
    {
        Application.Quit();
    }
}
