using System;
using System.Collections;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

/// <summary>
/// エラーフリー版：構文エラーを回避した安全な実装
/// </summary>
public class SafePythonExecutor : MonoBehaviour
{
    [Header("Python Code")]
    [TextArea(10, 20)]
    public string pythonCode = "print('Hello from Unity!')\nx = 42\ny = 58\nresult = x + y\nprint('Result:', result)";

    [Header("Settings")]
    public string serverUrl = "ws://localhost:8000/ws";

    private WebSocket webSocket;
    private bool connected = false;

    void Start()
    {
        Debug.Log("Safe Python Executor Starting...");
        ConnectToServer();
    }

    async void ConnectToServer()
    {
        try
        {
            webSocket = new WebSocket(serverUrl);

            webSocket.OnOpen += OnConnected;
            webSocket.OnMessage += OnMessageReceived;
            webSocket.OnError += OnError;
            webSocket.OnClose += OnClosed;

            await webSocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
        }
    }

    void OnConnected()
    {
        connected = true;
        Debug.Log("Connected to server!");
        ExecutePythonCode();
    }

    void ExecutePythonCode()
    {
        if (!connected || string.IsNullOrEmpty(pythonCode))
        {
            Debug.LogWarning("Cannot execute: not connected or no code");
            return;
        }

        Debug.Log("Sending Python code:");
        Debug.Log("--- CODE START ---");
        Debug.Log(pythonCode);
        Debug.Log("--- CODE END ---");

        var request = new { code = pythonCode };
        string jsonRequest = JsonConvert.SerializeObject(request);
        webSocket.SendText(jsonRequest);
    }

    void OnMessageReceived(byte[] data)
    {
        try
        {
            string response = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log("Response received:");
            Debug.Log("==========================================");

            // JSONを整形して表示
            var jsonObj = JsonConvert.DeserializeObject(response);
            string prettyJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            Debug.Log(prettyJson);

            Debug.Log("==========================================");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to process response: " + e.Message);
        }
    }

    void OnError(string error)
    {
        Debug.LogError("WebSocket Error: " + error);
    }

    void OnClosed(WebSocketCloseCode closeCode)
    {
        connected = false;
        Debug.Log("Connection closed: " + closeCode.ToString());
    }

    void Update()
    {
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
    }

    async void OnDestroy()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }

    [ContextMenu("Execute Now")]
    public void ExecuteNow()
    {
        if (connected)
        {
            ExecutePythonCode();
        }
        else
        {
            Debug.Log("Reconnecting...");
            ConnectToServer();
        }
    }
}

/// <summary>
/// 最もシンプルな版
/// </summary>
public class SimplePythonRunner : MonoBehaviour
{
    [TextArea(5, 10)]
    public string code = "print('Hello World')\nprint(2 + 3)";

    void Start()
    {
        StartCoroutine(RunPython());
    }

    IEnumerator RunPython()
    {
        Debug.Log("Simple Python Runner Starting...");

        var ws = new WebSocket("ws://localhost:8000/ws");
        bool connected = false;

        ws.OnOpen += () => {
            connected = true;
            Debug.Log("Connected!");
        };

        ws.OnMessage += (data) => {
            string response = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log("Python Response:");
            Debug.Log(response);
        };

        // 接続
        yield return StartCoroutine(Connect(ws));

        // 接続待ち
        while (!connected)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // コード送信
        var request = new { code = this.code };
        string json = JsonConvert.SerializeObject(request);
        ws.SendText(json);
        Debug.Log("Code sent: " + this.code);

        // メッセージ処理
        for (int i = 0; i < 30; i++)
        {
            ws.DispatchMessageQueue();
            yield return new WaitForSeconds(0.1f);
        }

        // 切断
        yield return StartCoroutine(Disconnect(ws));
    }

    IEnumerator Connect(WebSocket ws)
    {
        var task = ws.Connect();
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }

    IEnumerator Disconnect(WebSocket ws)
    {
        var task = ws.Close();
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }
}

/// <summary>
/// ワンボタン実行版
/// </summary>
public class OneButtonExecutor : MonoBehaviour
{
    [Header("Python Code")]
    public string pythonCode = "for i in range(5):\n    print(f'Count: {i}')";

    private WebSocket ws;

    [ContextMenu("Run Python Code")]
    public void RunCode()
    {
        StartCoroutine(ExecuteCode());
    }

    IEnumerator ExecuteCode()
    {
        Debug.Log("One Button Executor - Running code...");

        ws = new WebSocket("ws://localhost:8000/ws");
        bool done = false;

        ws.OnOpen += () => {
            Debug.Log("Connected - sending code");
            var req = new { code = pythonCode };
            ws.SendText(JsonConvert.SerializeObject(req));
        };

        ws.OnMessage += (data) => {
            string resp = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log("=== PYTHON RESULT ===");
            Debug.Log(resp);
            Debug.Log("=== END RESULT ===");
            done = true;
        };

        ws.OnError += (err) => {
            Debug.LogError("Error: " + err);
            done = true;
        };

        // 接続
        yield return StartCoroutine(ConnectWS());

        // 完了まで待機
        while (!done)
        {
            ws.DispatchMessageQueue();
            yield return new WaitForSeconds(0.1f);
        }

        // 切断
        if (ws.State == WebSocketState.Open)
        {
            yield return StartCoroutine(CloseWS());
        }
    }

    IEnumerator ConnectWS()
    {
        var task = ws.Connect();
        while (!task.IsCompleted)
            yield return null;
    }

    IEnumerator CloseWS()
    {
        var task = ws.Close();
        while (!task.IsCompleted)
            yield return null;
    }
}