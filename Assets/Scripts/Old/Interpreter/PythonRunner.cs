using UnityEngine;
using TMPro;

public class PythonRunner : MonoBehaviour
{
    public TMP_InputField inputField; // ユーザーがコードを書く場所

    public void RunCode()
    {
        string userCode = inputField.text;
        var interpreter = new ModeraInterpreter();
        interpreter.Run(userCode);
    }
}
