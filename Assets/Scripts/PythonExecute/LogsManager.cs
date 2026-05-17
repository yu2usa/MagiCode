using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;

/// <summary>
/// JSONのlogsを順番にリストに追加して管理（カテゴリ分類版 + 行番号対応 + API認証対応）
/// </summary>
public class LogsListManager : MonoBehaviour
{
    public GameObject turnController;
    public GameObject player;

    public string errorLog;

    public TextMeshProUGUI console;

    [Header("Python Code")]
    public TMP_InputField pythonCodeInputField;

    public GameObject strategy_manual;
    public GameObject ai_assistant;

    [Header("Server Settings")]
    [Tooltip("ローカルサーバーのWebSocket URL（local_server.py 起動後に接続）")]
    public string serverUrl = "ws://localhost:8000/ws";

    [Header("Code Highlight")]
    public CodeLineHighlighter codeLineHighlighter;

    [Header("Execution UI")]
    [Tooltip("コード実行中に表示するUI（実行完了で非表示になる）")]
    public GameObject executingUI;

    [Header("Warning UI")]
    [Tooltip("エラー・セキュリティ警告UIの親オブジェクト（ai_log Image）")]
    [SerializeField] GameObject warningUI;

    // warningUI 内の TextMeshProUGUI（子から自動取得）
    private TextMeshProUGUI _warningText;

    [Header("Connection Status")]
    [Tooltip("接続状態を表示するUI（オプション）")]
    public TextMeshProUGUI connectionStatusText;

    [Header("Execution Logs")]
    public List<ExecutionLogEntry> executionLogs = new List<ExecutionLogEntry>();

    [Header("Security Logs")]
    public List<ExecutionLogEntry> securityLogs = new List<ExecutionLogEntry>();

    [Header("Error Logs")]
    public List<ExecutionLogEntry> errorLogs = new List<ExecutionLogEntry>();

    [Header("Final Variables")]
    public Dictionary<string, object> finalVariables = new Dictionary<string, object>();

    // レート制限の残り回数
    private int rateLimitRemaining = -1;

    // EnemiesHP などの先頭挿入行数（行番号オフセット補正用）
    private int _prependedLineCount = 0;

    // コードエディタ先頭のロック部分（ステージ設定から注入）
    private string _lockedCode = "";
    private string _lastValidText = "";

    private WebSocket webSocket;

    // 接続状態
    public bool IsConnected => webSocket != null && webSocket.State == WebSocketState.Open;

    void Start()
    {
        // warningUI の子から TextMeshProUGUI をキャッシュ
        if (warningUI != null)
            _warningText = warningUI.GetComponentInChildren<TextMeshProUGUI>();

        // 初期状態はオフライン
        UpdateConnectionStatus("Status: Offline");
    }

    /// <summary>
    /// サーバーに接続してコードを実行
    /// </summary>
    async public void ConnectAndExecute()
    {
        // InputFieldが空またはホワイトスペース（スペース・改行・Tab）のみの場合は何もしない
        if (string.IsNullOrWhiteSpace(pythonCodeInputField.text))
        {
            Debug.Log("[LogsManager] コードが空のため実行をスキップ");
            return;
        }

        // ステージのコード制限チェック
        if (!CheckCodeRestrictions(pythonCodeInputField.text))
            return;

        // 実行中UIを表示
        if (executingUI != null)
            executingUI.SetActive(true);

        // チュートリアルヒントをここで即時非表示（INTERPRETING...と重ならないように）
        turnController.GetComponent<TurnController>().NotifyCodeExecutionStarted();

        UpdateConnectionStatus("Status: Connecting...");

        Debug.Log($"🔗 Connecting to: {serverUrl}");
        webSocket = new WebSocket(serverUrl);

        webSocket.OnOpen += OnConnected;
        webSocket.OnMessage += OnMessageReceived;

        webSocket.OnError += (error) => {
            if (executingUI != null)
                executingUI.SetActive(false);

            string errorMessage = error ?? "Connection error occurred";
            Debug.LogError($"❌ WebSocket Error: {errorMessage}");
            UpdateConnectionStatus("Status: Error");
        };

        webSocket.OnClose += (closeCode) => {
            if (executingUI != null)
                executingUI.SetActive(false);

            Debug.Log($"🔌 WebSocket Closed: {closeCode}");
            UpdateConnectionStatus("Status: Disconnected");
        };

        try
        {
            await webSocket.Connect();
        }
        catch (Exception e)
        {
            if (executingUI != null)
                executingUI.SetActive(false);

            Debug.LogError($"❌ Failed to connect to server: {e.Message}");
            UpdateConnectionStatus("Status: Offline");
        }
    }

    /// <summary>
    /// 接続状態を更新（UIがあれば表示）
    /// </summary>
    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
    }

    /// <summary>
    /// エラーログを文字列として取得
    /// </summary>
    public string GetErrorLogsAsString()
    {
        if (errorLogs.Count == 0)
        {
            return "No errors";
        }

        List<string> errorStrings = new List<string>();

        foreach (var error in errorLogs)
        {
            string errorLine = $"[Line {error.line:D2}] {error.message}";
            errorStrings.Add(errorLine);
        }

        return string.Join("\n", errorStrings);
    }

    void OnConnected()
    {
        Debug.Log("✅ Connected to server!");
        UpdateConnectionStatus("Status: Connected");
        SendPythonCode();
    }

    /// <summary>
    /// コードエディタ先頭に固定コードを設定し、プレイヤーが削除できないようにする。
    /// TurnController.InitializeStage() から呼ばれる。
    /// </summary>
    public void SetLockedCode(string code)
    {
        _lockedCode = code;
        if (string.IsNullOrEmpty(_lockedCode)) return;

        // 末尾改行を保証
        if (!_lockedCode.EndsWith("\n"))
            _lockedCode += "\n";

        _lastValidText = _lockedCode;
        pythonCodeInputField.SetTextWithoutNotify(_lockedCode);
        pythonCodeInputField.onValueChanged.AddListener(EnforceLockedCode);
    }

    /// <summary>
    /// 入力変更のたびにロック部分を保護する。壊された場合は直前の有効テキストに戻す。
    /// </summary>
    private void EnforceLockedCode(string newText)
    {
        if (newText.StartsWith(_lockedCode))
        {
            _lastValidText = newText;
            return;
        }
        // ロック部分が削除・変更された → 直前の有効テキストに復元
        pythonCodeInputField.SetTextWithoutNotify(_lastValidText);
        pythonCodeInputField.caretPosition = _lockedCode.Length;
    }

    void SendPythonCode()
    {
        if (string.IsNullOrEmpty(pythonCodeInputField.text))
        {
            Debug.LogWarning("⚠️ No code to execute");
            return;
        }

        string code = pythonCodeInputField.text;
        _prependedLineCount = 0;

        // SortBossManager が存在する場合、EnemiesHP リストをコード先頭に挿入
        // プレイヤーは EnemiesHP[j] や len(EnemiesHP) でガードのHPを参照できる
        var sortBoss = SortBossManager.Instance;
        if (sortBoss != null)
        {
            var hpValues = sortBoss.GetGuardsHpInAreaOrder();
            string guardsInit = $"EnemiesHP = [{string.Join(", ", hpValues)}]\n";
            code = guardsInit + code;
            _prependedLineCount = 1;
            Debug.Log($"[LogsManager] EnemiesHP 挿入: {guardsInit.Trim()}");
        }

        // CoreBossManager が存在する場合、numbers リストをコード先頭に挿入
        // プレイヤーは numbers[0], numbers[1], numbers[2] でCoreTotemの数字を参照できる
        var coreBossMgr = CoreBossManager.Instance;
        if (coreBossMgr != null)
        {
            var numbers = coreBossMgr.GetNumbersInAreaOrder();
            string numbersInit = $"numbers = [{string.Join(", ", numbers)}]\n";
            code = numbersInit + code;
            _prependedLineCount += 1;
            Debug.Log($"[LogsManager] numbers 挿入: {numbersInit.Trim()}");
        }

        // KeyGolemメカニクスが有効な場合、count をコード先頭に注入
        // プレイヤーは for i in range(count): のように使用できる
        var keyGolem = FindObjectOfType<KeyGolemMechanic>();
        Debug.Log($"[LogsManager] KeyGolemMechanic: {(keyGolem != null ? $"IsActive={keyGolem.IsActive}, count={keyGolem.RequiredCount}" : "null（コンポーネント未検出）")}");
        if (keyGolem != null && keyGolem.IsActive)
        {
            code = $"count = {keyGolem.RequiredCount}\n" + code;
            _prependedLineCount += 1;
            Debug.Log($"[LogsManager] count注入: count={keyGolem.RequiredCount}");
        }

        // 敵の状態（属性・HP）をコード先頭に注入
        // Python dict 形式: enemy['element'], enemy['hp'], enemies[0]['element'] でアクセス
        // print(enemy) / print(enemies) でも内容を確認できる
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            var aliveEnemies = enemyManager.AliveEnemies;
            if (aliveEnemies.Count > 0)
            {
                // dict を使用（JSON シリアライズ可能なためサーバーエラーを回避）
                // アクセス方法: enemy['element'], enemy['hp']
                var entries = aliveEnemies.ConvertAll(e =>
                    $"{{'element': '{DamageTypeToJapanese(e.element)}', 'hp': {e.enemyHealth}}}");

                string enemyBlock =
                    $"enemies = [{string.Join(", ", entries)}]\n" +
                    "enemy = enemies[0]\n";

                code = enemyBlock + code;
                _prependedLineCount += 2;
                Debug.Log($"[LogsManager] 敵情報注入: enemies=[{string.Join(", ", entries)}]");
            }
        }

        var request = new { code = code };
        string jsonRequest = JsonConvert.SerializeObject(request);
        webSocket.SendText(jsonRequest);
        Debug.Log("📤 Python code sent!");
    }

    void OnMessageReceived(byte[] data)
    {
        // 実行中UIを非表示
        if (executingUI != null)
            executingUI.SetActive(false);

        string jsonResponse = System.Text.Encoding.UTF8.GetString(data);

        // リストをクリア（新しい実行結果用）
        executionLogs.Clear();
        securityLogs.Clear();
        errorLogs.Clear();
        finalVariables.Clear();

        // JSONをパースしてリストに追加
        ParseAndAddToLists(jsonResponse);

        // リストの内容を表示
        DisplayLogsFromList();
    }

    void ParseAndAddToLists(string jsonResponse)
    {
        try
        {
            JObject response = JObject.Parse(jsonResponse);

            // === レート制限の残り回数を取得 ===
            if (response["rateLimitRemaining"] != null)
            {
                rateLimitRemaining = response["rateLimitRemaining"].ToObject<int>();
                Debug.Log($"📊 Rate Limit Remaining: {rateLimitRemaining}");

                // レート制限が少なくなったら警告
                if (rateLimitRemaining <= 5 && rateLimitRemaining > 0)
                {
                    Debug.LogWarning($"⚠️ Rate limit warning: Only {rateLimitRemaining} requests remaining!");
                }
                else if (rateLimitRemaining <= 0)
                {
                    Debug.LogError("❌ Rate limit exceeded! Please wait before sending more requests.");
                }
            }

            // === 1. executionLogsをリストに追加 ===
            if (response["executionLogs"] != null)
            {
                JArray execLogsArray = (JArray)response["executionLogs"];

                foreach (JObject logObj in execLogsArray)
                {
                    int rawLine = logObj["line"]?.ToObject<int>() ?? 0;

                    // 先頭挿入行（EnemiesHP など）由来のログはスキップ
                    if (rawLine <= _prependedLineCount) continue;

                    int adjustedLine = rawLine - _prependedLineCount;

                    ExecutionLogEntry logEntry = new ExecutionLogEntry
                    {
                        type = "execution",
                        step = logObj["s"]?.ToObject<int>() ?? 0,
                        line = adjustedLine,
                        message = logObj["msg"]?.ToString() ?? ""
                    };

                    executionLogs.Add(logEntry);
                }
            }

            // === 2. securityLogsをリストに追加 ===
            if (response["securityLogs"] != null)
            {
                JArray secLogsArray = (JArray)response["securityLogs"];

                foreach (JObject logObj in secLogsArray)
                {
                    int rawLine = logObj["line"]?.ToObject<int>() ?? 0;

                    // 先頭挿入行（EnemiesHP など）由来のログはスキップ
                    if (rawLine <= _prependedLineCount) continue;

                    int adjustedLine = rawLine - _prependedLineCount;

                    ExecutionLogEntry logEntry = new ExecutionLogEntry
                    {
                        type = "security",
                        step = logObj["s"]?.ToObject<int>() ?? 0,
                        line = adjustedLine,
                        message = logObj["msg"]?.ToString() ?? ""
                    };

                    securityLogs.Add(logEntry);
                }
            }

            Debug.Log($"✅ Execution Logs: {executionLogs.Count}, Security Logs: {securityLogs.Count}");

            // === 3. errorsをリストに追加 ===
            if (response["errors"] != null)
            {
                JArray errorsArray = (JArray)response["errors"];

                foreach (JObject errorObj in errorsArray)
                {
                    int rawErrorLine = errorObj["line"]?.ToObject<int>() ?? 0;
                    // 先頭挿入行分だけオフセットを引き、最小1行目に補正
                    int adjustedErrorLine = Mathf.Max(1, rawErrorLine - _prependedLineCount);

                    ExecutionLogEntry errorEntry = new ExecutionLogEntry
                    {
                        type = "error",
                        step = errorObj["s"]?.ToObject<int>() ?? 0,
                        line = adjustedErrorLine,
                        message = errorObj["msg"]?.ToString() ?? ""
                    };

                    errorLogs.Add(errorEntry);
                }

                if (errorLogs.Count > 0)
                {
                    Debug.LogWarning($"⚠️ Added {errorLogs.Count} errors to list");

                    // console・ai_assistant は未設定の場合もあるため null チェックする
                    errorLog = GetErrorLogsAsString();
                    if (console != null)
                    {
                        console.text = errorLog;
                        console.GetComponent<ConsoleWindow>()?.OpenWindow();
                    }
                    if (ai_assistant != null)
                    {
                        ai_assistant.SetActive(true);
                        ai_assistant.GetComponent<AI_Assistant>()?.ShowErrorMessage(errorLog);
                    }
                }
            }

            // === 4. finalResultを辞書に追加 ===
            if (response["finalResult"] != null)
            {
                JObject finalResultObj = (JObject)response["finalResult"];

                foreach (var kvp in finalResultObj)
                {
                    finalVariables[kvp.Key] = kvp.Value.ToObject<object>();
                }

                Debug.Log($"📊 Added {finalVariables.Count} variables to dictionary");
            }

            // === 5. サニタイズされたかチェック ===
            if (response["sanitized"] != null && response["sanitized"].ToObject<bool>())
            {
                Debug.LogWarning("🔒 Code was sanitized for security reasons");
            }

            // JSON解析が成功した場合のみプレイヤーアクションへ
            StartPlayerAction();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to parse JSON: {e.Message}");
        }
    }

    void DisplayLogsFromList()
    {
        Debug.Log("========================================");
        Debug.Log("📋 CATEGORIZED LOGS:");
        Debug.Log("========================================");

        // === セキュリティログを表示 ===
        if (securityLogs.Count > 0)
        {
            Debug.Log("🛡️ SECURITY LOGS:");
            foreach (var log in securityLogs)
            {
                Debug.Log($"  🔒 [Line {log.line:D2}] Security Step {log.step:D2}: {log.message}");
            }
            Debug.Log("");
        }

        // === 実行ログを表示 ===
        if (executionLogs.Count > 0)
        {
            Debug.Log("▶️ EXECUTION LOGS:");
            foreach (var log in executionLogs)
            {
                Debug.Log($"  ▶️ [Line {log.line:D2}] Step {log.step:D2}: {log.message}");
            }
            Debug.Log("");
        }

        // === エラーログを表示 ===
        if (errorLogs.Count > 0)
        {
            Debug.LogWarning("❌ ERROR LOGS:");
            foreach (var error in errorLogs)
            {
                Debug.LogError($"  ❌ [Line {error.line:D2}] Error at Step {error.step:D2}: {error.message}");
            }
            Debug.Log("");
        }

        // === 最終変数を表示 ===
        if (finalVariables.Count > 0)
        {
            Debug.Log("📊 FINAL VARIABLES:");
            foreach (var kvp in finalVariables)
            {
                Debug.Log($"   {kvp.Key} = {kvp.Value}");
            }
        }

        Debug.Log("========================================");
        Debug.Log($"📊 Summary: {executionLogs.Count} execution steps, {securityLogs.Count} security steps, {errorLogs.Count} errors");
        Debug.Log("========================================");
    }

    public void StartPlayerAction()
    {
        // エラーまたはセキュリティ警告がある場合はコードを実行せず待機
        // → プレイヤーはコードを修正して再実行できる（ターンは進まない）
        if (errorLogs.Count > 0 || securityLogs.Count > 0)
        {
            ShowWarningMessage();
            return;
        }

        // 警告UIを非表示にしてから実行
        if (warningUI != null)
            warningUI.SetActive(false);

        player.GetComponent<PlayerController>().Execute();
        turnController.GetComponent<TurnController>().isCodeInputReady = true;
    }

    /// <summary>
    /// ステージの制限キーワードが含まれていないか確認する。
    /// 違反があれば warningUI にエラーを表示して false を返す。
    /// </summary>
    // 許可リスト・ブロックリストモードでチェック対象となるキーワード（制御構文＋ゲーム関数）
    private static readonly List<string> _checkableKeywords = new List<string>
    {
        // Python制御構文
        "for", "while", "if", "elif", "else", "def", "class",
        "import", "from", "return", "break", "continue", "pass",
        "try", "except", "finally", "with", "yield", "lambda",
        "assert", "raise", "global", "nonlocal",
        // ゲーム関数
        "Attack", "Guard", "Cast", "MoveTo", "MoveForward", "MoveBackward", "Swap", "print"
    };

    private bool CheckCodeRestrictions(string code)
    {
        // 防御フェーズ中はAttack, Cast, Swapを禁止
        var tc = turnController.GetComponent<TurnController>();
        if (tc.currentPhase == Phase.Defense)
        {
            var phaseViolations = new List<string>();
            foreach (var keyword in new[] { "Attack", "Cast", "Swap" })
            {
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(keyword)}\b"))
                    phaseViolations.Add(keyword);
            }
            if (phaseViolations.Count > 0)
            {
                if (warningUI != null && _warningText != null)
                {
                    _warningText.text = "防御フェーズ中は攻撃コマンドは使用できません";
                    warningUI.SetActive(true);
                }
                return false;
            }
        }

        // Swapは1フェーズに1回のみ使用可能
        int swapCount = Regex.Matches(code, @"\bSwap\s*\(").Count;
        if (swapCount > 1)
        {
            if (warningUI != null && _warningText != null)
            {
                _warningText.text = "Swapは1つしか書けません";
                warningUI.SetActive(true);
            }
            return false;
        }

        var stageConfig = tc.stageConfig;
        if (stageConfig == null) return true;

        // 許可リストモード：allowedKeywords が設定されている場合
        // _checkableKeywords のうち allowedKeywords に含まれないものを禁止
        if (stageConfig.allowedKeywords != null && stageConfig.allowedKeywords.Count > 0)
        {
            var violations = new List<string>();
            foreach (var keyword in _checkableKeywords)
            {
                if (stageConfig.allowedKeywords.Contains(keyword)) continue;
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(keyword)}\b"))
                    violations.Add(keyword);
            }
            if (violations.Count > 0)
            {
                ShowRestrictionWarning(violations);
                return false;
            }
            return true;
        }

        // ブロックリストモード：restrictedKeywords が設定されている場合
        if (stageConfig.restrictedKeywords == null || stageConfig.restrictedKeywords.Count == 0)
            return true;

        var blocked = new List<string>();
        foreach (var keyword in stageConfig.restrictedKeywords)
        {
            if (string.IsNullOrEmpty(keyword)) continue;
            // 単語境界でマッチ（"format" 内の "for" など部分一致を防ぐ）
            if (Regex.IsMatch(code, $@"\b{Regex.Escape(keyword)}\b"))
                blocked.Add(keyword);
        }

        if (blocked.Count == 0) return true;

        ShowRestrictionWarning(blocked);
        return false;
    }

    /// <summary>
    /// DamageType を Python コード内で使う日本語文字列に変換
    /// </summary>
    private static string DamageTypeToJapanese(DamageType type)
    {
        return type switch
        {
            DamageType.Flame      => "フレイム",
            DamageType.Ice        => "アイス",
            DamageType.Lightning  => "ライトニング",
            DamageType.Plant      => "プラント",
            DamageType.Water      => "ウォーター",
            DamageType.Physical   => "フィジカル",
            _                     => "なし"
        };
    }

    private void ShowRestrictionWarning(List<string> violations)
    {
        if (warningUI == null || _warningText == null) return;
        string joined = string.Join("、", violations);
        _warningText.text = $"このステージでは「{joined}」は使用できません";
        warningUI.SetActive(true);
    }

    /// <summary>
    /// エラーまたはセキュリティ警告の内容を warningUI > Text(TMP) に表示
    /// </summary>
    private void ShowWarningMessage()
    {
        if (warningUI == null || _warningText == null) return;

        var lines = new List<string>();

        // エラーの行番号をまとめる
        if (errorLogs.Count > 0)
        {
            string lineNums = string.Join("行、", errorLogs.ConvertAll(e => e.line.ToString()));
            lines.Add($"{lineNums}行にエラーがあります");
        }

        // セキュリティ警告の行番号をまとめる
        if (securityLogs.Count > 0)
        {
            string lineNums = string.Join("行、", securityLogs.ConvertAll(e => e.line.ToString()));
            lines.Add($"{lineNums}行にセキュリティ警告があります");
        }

        _warningText.text = string.Join("\n", lines);
        warningUI.SetActive(true);
    }

    void Update()
    {
        webSocket?.DispatchMessageQueue();
    }

    async void OnDestroy()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }

    // === パブリックメソッド ===

    /// <summary>
    /// レート制限の残り回数を取得
    /// </summary>
    public int GetRateLimitRemaining()
    {
        return rateLimitRemaining;
    }

    /// <summary>
    /// サーバーURLを動的に設定
    /// </summary>
    public void SetServerUrl(string newUrl)
    {
        serverUrl = newUrl;
    }

    public List<ExecutionLogEntry> GetAllLogsInOrder()
    {
        List<ExecutionLogEntry> allLogs = new List<ExecutionLogEntry>();
        allLogs.AddRange(executionLogs);
        allLogs.AddRange(securityLogs);
        return allLogs;
    }

    public ExecutionLogEntry GetLogByStepAndCategory(int step, string category)
    {
        if (category == "execution")
        {
            return executionLogs.Find(l => l.step == step);
        }
        else if (category == "security")
        {
            return securityLogs.Find(l => l.step == step);
        }
        else if (category == "error")
        {
            return errorLogs.Find(l => l.step == step);
        }
        return null;
    }

    public List<ExecutionLogEntry> GetAllLogsByStep(int step)
    {
        List<ExecutionLogEntry> results = new List<ExecutionLogEntry>();

        var execLog = executionLogs.Find(l => l.step == step);
        if (execLog != null) results.Add(execLog);

        var secLog = securityLogs.Find(l => l.step == step);
        if (secLog != null) results.Add(secLog);

        var errLog = errorLogs.Find(l => l.step == step);
        if (errLog != null) results.Add(errLog);

        return results;
    }

    public List<ExecutionLogEntry> GetSecurityLogs()
    {
        return new List<ExecutionLogEntry>(securityLogs);
    }

    public List<ExecutionLogEntry> GetExecutionLogs()
    {
        return new List<ExecutionLogEntry>(executionLogs);
    }

    public int GetTotalLogCount()
    {
        return executionLogs.Count + securityLogs.Count;
    }

    public void ClearAllLogs()
    {
        executionLogs.Clear();
        securityLogs.Clear();
        errorLogs.Clear();
        finalVariables.Clear();
        Debug.Log("All logs cleared!");
    }

    [ContextMenu("Display Logs")]
    public void DisplayLogsManually()
    {
        DisplayLogsFromList();
    }

    [ContextMenu("Show Log Statistics")]
    public void ShowStatistics()
    {
        Debug.Log("=== LOG STATISTICS ===");
        Debug.Log($"Execution Logs: {executionLogs.Count}");
        Debug.Log($"Security Logs: {securityLogs.Count}");
        Debug.Log($"Error Logs: {errorLogs.Count}");
        Debug.Log($"Total Steps: {GetTotalLogCount()}");
        Debug.Log($"Final Variables: {finalVariables.Count}");
        Debug.Log($"Rate Limit Remaining: {rateLimitRemaining}");
    }

    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        Debug.Log($"🔗 Server URL: {serverUrl}");
        Debug.Log($"📡 接続先: {serverUrl}");
    }
}

/// <summary>
/// ログエントリのデータクラス（行番号対応版）
/// </summary>
[System.Serializable]
public class ExecutionLogEntry
{
    public string type;      // "execution", "security", "error"
    public int step;         // カテゴリ内でのステップ番号（1から始まる）
    public int line;         // 元のコードの行番号
    public string message;   // ログメッセージ

    public override string ToString()
    {
        string categoryName = type == "execution" ? "Execution" :
                             type == "security" ? "Security" :
                             type == "error" ? "Error" : "Unknown";
        return $"[{categoryName} Line {line} Step {step}] {message}";
    }

    public string GetCategoryName()
    {
        switch (type)
        {
            case "execution": return "Execution";
            case "security": return "Security";
            case "error": return "Error";
            default: return "Unknown";
        }
    }

    public string GetIcon()
    {
        switch (type)
        {
            case "execution": return "▶️";
            case "security": return "🛡️";
            case "error": return "❌";
            default: return "•";
        }
    }
}