using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardClickEvent : MonoBehaviour
{
    public GameObject conditionInput;

    private void Start()
    {
        if (conditionInput != null)
        {
            conditionInput.SetActive(false);
        }
    }
    

    public void SetConditon()
    {
        conditionInput.SetActive(true);
        
    }

    public void HideCondition()
    {
        conditionInput.SetActive(false);
    }

}
