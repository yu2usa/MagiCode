using TMPro;
using UnityEngine;

public class AI_Assistant : MonoBehaviour
{
    public GameObject fukidashi;
    public TextMeshProUGUI fukidashi_text;

    public void ShowErrorMessage(string content)
    {
        this.gameObject.SetActive(false);
        fukidashi.SetActive(true);
        fukidashi_text.text = content;
    }

    public void CloseErrorMessage()
    {
        fukidashi.SetActive(false);
    }

}
