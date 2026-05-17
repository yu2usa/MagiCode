using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameProcess : MonoBehaviour
{
    public GameObject codeEditor;
    public GameObject openButton;
    void Start()
    {
        codeEditor.SetActive(false);
    }

   public void OpenCodeEditor()
    {
        codeEditor.SetActive(true);
        openButton.SetActive(false);
    }
    public void CloseCodeEditor()
    {
        codeEditor.SetActive(false);
        openButton.SetActive(true);
    }
}
