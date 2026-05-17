using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;  // WebSocket ライブラリを導入

public class PythonDebugger : MonoBehaviour
{
    public InputField codeInput;
    public Text outputText;
    private WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8000/run");

        websocket.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Python Msg: " + msg);
            outputText.text += msg + "\n";
        };

        await websocket.Connect();
    }

    public async void RunCode()
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(codeInput.text);
            outputText.text = ""; // 出力をクリア
        }
    }
}