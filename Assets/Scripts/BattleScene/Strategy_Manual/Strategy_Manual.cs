using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class Strategy_Manual : MonoBehaviour
{
    public List<string> manual_text;
    public TextMeshProUGUI manual_textbox;

    void Start()
    {
        
    }
    public void ShowReference()
    {
        this.gameObject.SetActive(true);
    }

    public void CloseReference()
    {
        this.gameObject.SetActive(false);
    }

}
