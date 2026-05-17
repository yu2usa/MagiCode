using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GeminiClient : MonoBehaviour
{
    // デプロイしたGAS Web AppのURL
    private const string GAS_URL = "https://script.google.com/macros/s/AKfycbwyNWI0AOGoocia5lxYHq7KEvxj5Plvpk61vCkcUsMiM4Nf2uzVvhziX2XvxM4yHbCw/exec";


    private void Start()
    {
        // ゲーム開始時にGeminiに質問
        AskGemini();
    }

    public void AskGemini(string message = "地球上で最も大きい動物は？")
    {
        StartCoroutine(SendRequest(message));
    }

    private IEnumerator SendRequest(string message)
    {
        // リクエストボディ
        string jsonBody = JsonUtility.ToJson(new RequestBody { message = message });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using var request = new UnityWebRequest(GAS_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
            Debug.Log("Gemini返答: " + response.reply);
            OnReceiveReply(response.reply);
        }
        else
        {
            Debug.LogError("エラー: " + request.error);
            Debug.LogError("レスポンス: " + request.downloadHandler.text);
        }
    }

    private void OnReceiveReply(string reply)
    {
        // ここで返答を使う（UIに表示など）
        Debug.Log(reply);
    }

    // JSON用データクラス
    [System.Serializable]
    private class RequestBody
    {
        public string message;
    }

    [System.Serializable]
    private class GeminiResponse
    {
        public string reply;
        public string error;
    }
}