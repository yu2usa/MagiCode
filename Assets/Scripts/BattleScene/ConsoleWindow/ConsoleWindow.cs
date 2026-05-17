using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ConsoleWindow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(true);
    }

    // Update is called once per frame
  public void OpenWindow()
    {
        {
            this.GetComponent<RectTransform>().DOMove(new Vector2(- 1073, -41), 0.5f).SetEase(Ease.InSine);
        }
    }

    public void CloseWindow()
    {
        {

        }
    }
}
