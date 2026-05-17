using UnityEngine;
using TMPro;

public class CodeRunner : MonoBehaviour
{
    public TMP_InputField inputField; // ユーザーがコードを書く場所

    public void RunCode()
    {
        string userCode = inputField.text;
        var interpreter = new ModeraInterpreter();
        interpreter.Run(userCode);
    }
}
