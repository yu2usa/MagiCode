using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CodeListUIManager : MonoBehaviour
{
    // プログラムの構造を表すクラス（内部クラスとして定義）
    [System.Serializable]
    public class ProgramLine
    {
        public enum LineType
        {
            Command,        // 単一コマンド（Slash(), Evade()など）
            IfStart,        // if文の開始
            IfEnd,          // if文の終了（}）
            LoopStart,      // loop文の開始
            LoopEnd,        // loop文の終了（}）
            VariableSet,    // 変数定義（Set文）
            ArraySet,       // 配列定義（Set文）
            Comment         // コメント行
        }

        public LineType lineType;
        public string content;      // 表示するテキスト
        public int indentLevel;     // インデントのレベル（0, 1, 2...）
        public string condition;    // if文の条件やloop文の回数など
        public string variableName; // 変数名（Set文用）
        public string variableValue;// 変数の値（Set文用）
        public List<string> arrayValues; // 配列の値（配列Set文用）

        public ProgramLine(LineType type, string text, int indent = 0, string cond = "")
        {
            lineType = type;
            content = text;
            indentLevel = indent;
            condition = cond;
            variableName = "";
            variableValue = "";
            arrayValues = new List<string>();
        }

        // 変数定義用コンストラクタ
        public ProgramLine(string varName, string varValue, int indent = 0)
        {
            lineType = LineType.VariableSet;
            variableName = varName;
            variableValue = varValue;
            content = $"Set({varName}, \"{varValue}\")";
            indentLevel = indent;
            condition = "";
            arrayValues = new List<string>();
        }

        // 配列定義用コンストラクタ
        public ProgramLine(string varName, List<string> arrValues, int indent = 0)
        {
            lineType = LineType.ArraySet;
            variableName = varName;
            arrayValues = new List<string>(arrValues);
            string arrayString = "[\"" + string.Join("\", \"", arrValues) + "\"]";
            content = $"Set({varName}, {arrayString})";
            indentLevel = indent;
            condition = "";
            variableValue = "";
        }

        // パラメータなしのコンストラクタ（Unity Serialization用）
        public ProgramLine()
        {
            lineType = LineType.Command;
            content = "";
            indentLevel = 0;
            condition = "";
            variableName = "";
            variableValue = "";
            arrayValues = new List<string>();
        }
    }

    [Header("UI Settings")]
    [SerializeField] private TMP_InputField codeOutput;

    [Header("Text Synchronization")]
    [SerializeField] private bool enableTextSync = true; // テキスト同期を有効にする
    [SerializeField] private bool realTimeSync = false; // リアルタイム同期（重い処理）
    [SerializeField] private KeyCode syncKey = KeyCode.F5; // 手動同期用キー
    [SerializeField] private float syncInterval = 2.0f; // 自動同期間隔（秒）
    [SerializeField] private bool syncFromText = false; // 手動同期ボタン

    [Header("Card List")]
    [SerializeField] private List<ProgramLine> programLines = new List<ProgramLine>();

    [Header("Display Settings")]
    [SerializeField] private string indentString = "    "; // インデント用の文字列
    [SerializeField] private bool showLineNumbers = true;
    [SerializeField] private bool enableSyntaxHighlight = true; // シンタックスハイライト有効化

    [Header("Syntax Highlight Colors")]
    [SerializeField] private Color commentColor = Color.green;
    [SerializeField] private Color keywordColor = new Color(0.8f, 0.4f, 1f); // 紫色 (if, loop)
    [SerializeField] private Color variableColor = Color.white; // 変数名: 白色
    [SerializeField] private Color stringColor = new Color(1f, 0.6f, 0f); // 文字列: オレンジ色
    [SerializeField] private Color numberColor = new Color(1f, 0.6f, 0f); // 数値: オレンジ
    [SerializeField] private Color braceColor = Color.white;
    [SerializeField] private Color commandColor = new Color(1f, 1f, 0.7f); // コマンド: 薄い黄色

    [Header("Inspector Controls")]
    [SerializeField] private bool autoUpdate = true;
    [Space(10)]

    [Header("Quick Actions")]
    [SerializeField] private bool createBubbleSort = false;
    [SerializeField] private bool createSelectionSort = false;
    [SerializeField] private bool createInsertionSort = false;
    [SerializeField] private bool clearProgram = false;
    [SerializeField] private bool showNestingInfo = false;
    [SerializeField] private bool validateProgram = false;

    [Space(10)]
    [Header("Manual Input")]
    [SerializeField] private string commandToAdd = "Slash()";
    [SerializeField] private bool addCommand = false;
    [SerializeField] private string ifCondition = "HP < 50";
    [SerializeField] private bool addIfStatement = false;
    [SerializeField] private string loopCount = "10";
    [SerializeField] private bool addLoopStatement = false;
    [SerializeField] private bool closeBlock = false;

    [Space(10)]
    [Header("Variable Definition")]
    [SerializeField] private string variableName = "Weapon";
    [SerializeField] private string variableValue = "Sword";
    [SerializeField] private bool addVariable = false;

    [Space(5)]
    [Header("Array Definition")]
    [SerializeField] private string arrayName = "Enemies";
    [SerializeField] private string[] arrayElements = { "Zombie", "Skeleton", "Slime" };
    [SerializeField] private bool addArray = false;

    [Space(5)]
    [Header("Comment")]
    [SerializeField] private string commentText = "// コメント";
    [SerializeField] private bool addComment = false;

    [Space(5)]
    [Header("Debug Tools")]
    [SerializeField] private bool debugIndentState = false;
    [SerializeField] private bool fixAllIndents = false;
    [SerializeField] private bool toggleSyntaxHighlight = false;

    private List<ProgramLine> previousProgramLines = new List<ProgramLine>();
    private List<Transform> trackedChildren = new List<Transform>();

    // テキスト同期用
    private string lastKnownText = "";
    private float lastSyncTime = 0f;
    private bool isUpdatingFromCode = false; // UI更新中フラグ

    void Start()
    {
        // 初期サンプルデータ（コメントアウトされていたので空にする）
        // InitializeSampleData();
        UpdateUI();

        // InputFieldの変更を検知するリスナーを追加
        if (codeOutput != null)
        {
            codeOutput.onValueChanged.AddListener(OnInputFieldChanged);
            lastKnownText = codeOutput.text;
        }
    }

    void Update()
    {
        // インスペクターでのボタン操作を処理
        HandleInspectorControls();

        // テキスト同期処理
        if (enableTextSync && !isUpdatingFromCode)
        {
            HandleTextSynchronization();
        }

        // リストが変更されたかチェック
        if (HasListChanged())
        {
            UpdateUI();
        }

        // 子オブジェクトのチェック - 外れたオブジェクトの処理
        for (int i = trackedChildren.Count - 1; i >= 0; i--)
        {
            Transform child = trackedChildren[i];
            if (child == null || child.parent != this.transform)
            {
                // 子が外れた場合、該当するリスト要素も削除する
                if (i < programLines.Count)
                {
                    programLines.RemoveAt(i);
                }
                trackedChildren.RemoveAt(i);
            }
        }

        // 新たに追加された子オブジェクトの確認
        foreach (Transform child in transform)
        {
            if (!trackedChildren.Contains(child))
            {
                CardDiscrimination card = child.GetComponent<CardDiscrimination>();
                if (card != null)
                {
                    // 単一コマンドとして追加
                    AddCommand(card.cardType);
                    trackedChildren.Add(child);
                }
            }
        }
    }

    // InputFieldの変更を検知
    private void OnInputFieldChanged(string newText)
    {
        if (enableTextSync && realTimeSync && !isUpdatingFromCode)
        {
            lastKnownText = newText;
            SyncFromTextContent();
        }
    }

    // テキスト同期処理
    private void HandleTextSynchronization()
    {
        if (codeOutput == null) return;

        string currentText = codeOutput.text;

        // 手動同期キーが押された場合
        if (Input.GetKeyDown(syncKey))
        {
            SyncFromTextContent();
            return;
        }

        // 定期的な同期
        if (Time.time - lastSyncTime > syncInterval)
        {
            if (currentText != lastKnownText)
            {
                SyncFromTextContent();
            }
            lastSyncTime = Time.time;
        }
    }

    // テキスト内容からプログラム構造に同期
    public void SyncFromTextContent()
    {
        if (codeOutput == null) return;

        string textContent = codeOutput.text;

        // リッチテキストタグを除去してから処理
        textContent = RemoveRichTextTags(textContent);
        lastKnownText = textContent;

        try
        {
            isUpdatingFromCode = true; // 更新フラグを設定

            // テキストを行に分割
            string[] lines = textContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.None);

            List<ProgramLine> newProgramLines = new List<ProgramLine>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                ProgramLine parsedLine = ParseTextLine(line, i + 1);

                if (parsedLine != null)
                {
                    newProgramLines.Add(parsedLine);
                }
            }

            // プログラム構造を更新
            programLines = newProgramLines;

            // 前回のリストも更新して無限ループを防ぐ
            UpdatePreviousLines();

            Debug.Log($"<color=cyan>Synchronized {newProgramLines.Count} lines from text</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"テキスト同期エラー: {ex.Message}");
        }
        finally
        {
            isUpdatingFromCode = false; // 更新フラグをリセット
        }
    }

    // リッチテキストタグを除去
    private string RemoveRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // <color=#RRGGBB>タグと</color>タグを除去
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<color=#[0-9A-Fa-f]{6}>", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</color>", "");

        // 他のリッチテキストタグも除去（必要に応じて追加）
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");

        return text;
    }

    // テキスト行を解析してProgramLineに変換
    private ProgramLine ParseTextLine(string line, int lineNumber)
    {
        if (string.IsNullOrEmpty(line)) return null;

        // 行番号を除去
        string cleanLine = RemoveLineNumber(line);
        if (string.IsNullOrWhiteSpace(cleanLine)) return null;

        // インデントレベルを計算
        int indentLevel = CalculateIndentLevel(cleanLine);

        // インデントを除去
        cleanLine = cleanLine.Trim();

        // 行の種類を判定して適切なProgramLineを作成
        return ClassifyAndCreateLine(cleanLine, indentLevel);
    }

    // 行番号を除去
    private string RemoveLineNumber(string line)
    {
        // "01  " のような行番号パターンを検出して除去
        if (showLineNumbers)
        {
            // 先頭の数字+空白のパターンをマッチ
            Match match = Regex.Match(line, @"^\d+\s+(.*)$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return line;
    }

    // インデントレベルを計算
    private int CalculateIndentLevel(string line)
    {
        int indentCount = 0;
        int indentSize = indentString.Length;

        for (int i = 0; i < line.Length; i += indentSize)
        {
            if (i + indentSize <= line.Length)
            {
                string substring = line.Substring(i, indentSize);
                if (substring == indentString)
                {
                    indentCount++;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return indentCount;
    }

    // 行を分類してProgramLineを作成
    private ProgramLine ClassifyAndCreateLine(string content, int indentLevel)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        // コメント行
        if (content.StartsWith("//"))
        {
            return new ProgramLine(ProgramLine.LineType.Comment, content, indentLevel);
        }

        // 閉じブレース
        if (content == "}")
        {
            return new ProgramLine(ProgramLine.LineType.IfEnd, content, indentLevel);
        }

        // 開きブレース
        if (content == "{")
        {
            return new ProgramLine(ProgramLine.LineType.IfStart, content, indentLevel);
        }

        // if文
        if (content.StartsWith("if(") && content.EndsWith(")"))
        {
            string condition = ExtractCondition(content);
            return new ProgramLine(ProgramLine.LineType.IfStart, content, indentLevel, condition);
        }

        // loop文
        if (content.StartsWith("loop(") && content.EndsWith(")"))
        {
            string loopCount = ExtractCondition(content);
            return new ProgramLine(ProgramLine.LineType.LoopStart, content, indentLevel, loopCount);
        }

        // Set文（変数・配列定義）
        if (content.StartsWith("Set(") && content.EndsWith(")"))
        {
            return ParseSetStatement(content, indentLevel);
        }

        // その他はコマンドとして扱う
        return new ProgramLine(ProgramLine.LineType.Command, content, indentLevel);
    }

    // 条件式やループ回数を抽出
    private string ExtractCondition(string line)
    {
        int start = line.IndexOf('(') + 1;
        int end = line.LastIndexOf(')');

        if (start > 0 && end > start)
        {
            return line.Substring(start, end - start);
        }

        return "";
    }

    // Set文を解析
    private ProgramLine ParseSetStatement(string content, int indentLevel)
    {
        try
        {
            // Set(varName, value) の形式から変数名と値を抽出
            string inner = content.Substring(4, content.Length - 5); // "Set(" と ")" を除去

            int commaIndex = FindOuterComma(inner);
            if (commaIndex < 0) return new ProgramLine(ProgramLine.LineType.Command, content, indentLevel);

            string varName = inner.Substring(0, commaIndex).Trim();
            string value = inner.Substring(commaIndex + 1).Trim();

            // 配列の場合
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                string arrayContent = value.Substring(1, value.Length - 2);
                List<string> elements = ParseArrayElements(arrayContent);

                ProgramLine line = new ProgramLine(varName, elements, indentLevel);
                return line;
            }
            else
            {
                // 単一変数の場合
                string cleanValue = value.Trim('"');
                ProgramLine line = new ProgramLine(varName, cleanValue, indentLevel);
                return line;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Set文の解析に失敗: {content} - {ex.Message}");
            return new ProgramLine(ProgramLine.LineType.Command, content, indentLevel);
        }
    }

    // 配列要素を解析
    private List<string> ParseArrayElements(string arrayContent)
    {
        List<string> elements = new List<string>();

        if (string.IsNullOrWhiteSpace(arrayContent))
        {
            return elements;
        }

        // 簡単なカンマ分割（入れ子は考慮しない）
        string[] parts = arrayContent.Split(',');

        foreach (string part in parts)
        {
            string cleanPart = part.Trim().Trim('"');
            if (!string.IsNullOrEmpty(cleanPart))
            {
                elements.Add(cleanPart);
            }
        }

        return elements;
    }

    // 外側のカンマを見つける
    private int FindOuterComma(string input)
    {
        int depth = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '[' || c == '(')
            {
                depth++;
            }
            else if (c == ']' || c == ')')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    // インスペクター上のボタン操作を処理
    private void HandleInspectorControls()
    {
        // ソートアルゴリズム生成
        if (createBubbleSort)
        {
            createBubbleSort = false;
            CreateBubbleSort();
            Debug.Log("Bubble Sort created!");
        }

        if (createSelectionSort)
        {
            createSelectionSort = false;
            CreateSelectionSort();
            Debug.Log("Selection Sort created!");
        }

        if (createInsertionSort)
        {
            createInsertionSort = false;
            CreateInsertionSort();
            Debug.Log("Insertion Sort created!");
        }

        // プログラム操作
        if (clearProgram)
        {
            clearProgram = false;
            ClearProgram();
            Debug.Log("Program cleared!");
        }

        if (showNestingInfo)
        {
            showNestingInfo = false;
            ShowNestingLevels();
        }

        if (validateProgram)
        {
            validateProgram = false;
            bool isValid = ValidateStructure();
            Debug.Log($"Program validation: {(isValid ? "VALID" : "INVALID")}");
        }

        // 手動入力
        if (addCommand)
        {
            addCommand = false;
            AddCommand(commandToAdd);
            Debug.Log($"Command added: {commandToAdd}");
        }

        if (addIfStatement)
        {
            addIfStatement = false;
            AddIfStatement(ifCondition);
            Debug.Log($"If statement added: {ifCondition}");
        }

        if (addLoopStatement)
        {
            addLoopStatement = false;
            AddLoopStatement(loopCount);
            Debug.Log($"Loop statement added: {loopCount}");
        }

        if (closeBlock)
        {
            closeBlock = false;
            CloseBlock();
            Debug.Log("Block closed");
        }

        // 変数定義
        if (addVariable)
        {
            addVariable = false;
            AddVariable(variableName, variableValue);
            Debug.Log($"Variable added: {variableName} = {variableValue}");
        }

        // 配列定義
        if (addArray)
        {
            addArray = false;
            AddArray(arrayName, new List<string>(arrayElements));
            Debug.Log($"Array added: {arrayName} with {arrayElements.Length} elements");
        }

        // コメント追加
        if (addComment)
        {
            addComment = false;
            AddComment(commentText);
            Debug.Log($"Comment added: {commentText}");
        }

        // テキスト同期
        if (syncFromText)
        {
            syncFromText = false;
            SyncFromTextContent();
            Debug.Log("Synchronized from text content!");
        }

        // デバッグツール
        if (debugIndentState)
        {
            debugIndentState = false;
            DebugIndentState();
        }

        if (fixAllIndents)
        {
            fixAllIndents = false;
            AutoAdjustIndent();
            Debug.Log("All indents fixed!");
        }

        if (toggleSyntaxHighlight)
        {
            toggleSyntaxHighlight = false;
            enableSyntaxHighlight = !enableSyntaxHighlight;
            UpdateUI(); // 表示を更新
            Debug.Log($"Syntax highlight: {(enableSyntaxHighlight ? "ON" : "OFF")}");
        }
    }

    // 初期サンプルデータを設定（変数定義を含むサンプル）
    private void InitializeSampleData()
    {
        programLines.Clear();

        // 変数と配列の定義
        AddVariable("PlayerHP", "100");
        AddVariable("Weapon", "Sword");
        AddArray("Enemies", new List<string> { "Zombie", "Skeleton", "Slime" });
        AddComment("// バブルソートアルゴリズム");

        // バブルソートのサンプルプログラム
        AddLoopStatement("n-1");  // 外側のループ
        AddLoopStatement("n-i-1"); // 内側のループ
        AddIfStatement("arr[j] > arr[j+1]");  // 条件分岐
        AddCommand("Swap(arr[j], arr[j+1])"); // 交換処理
        CloseBlock(); // if文終了
        CloseBlock(); // 内側のループ終了
        CloseBlock(); // 外側のループ終了

        AddCommand("Complete()"); // 完了処理
    }

    // リストが変更されたかチェック
    private bool HasListChanged()
    {
        if (programLines.Count != previousProgramLines.Count)
            return true;

        for (int i = 0; i < programLines.Count; i++)
        {
            if (programLines[i].content != previousProgramLines[i].content ||
                programLines[i].lineType != previousProgramLines[i].lineType ||
                programLines[i].indentLevel != previousProgramLines[i].indentLevel)
                return true;
        }

        return false;
    }

    // 前回のリストを更新
    private void UpdatePreviousLines()
    {
        previousProgramLines = new List<ProgramLine>();
        foreach (ProgramLine line in programLines)
        {
            ProgramLine newLine = new ProgramLine(line.lineType, line.content, line.indentLevel, line.condition);
            newLine.variableName = line.variableName;
            newLine.variableValue = line.variableValue;
            newLine.arrayValues = new List<string>(line.arrayValues);
            previousProgramLines.Add(newLine);
        }
    }

    // UIを更新
    public void UpdateUI()
    {
        if (codeOutput == null)
        {
            Debug.LogError("Code Output InputField is not assigned!");
            return;
        }

        isUpdatingFromCode = true; // 更新フラグを設定

        // テキストを構築
        string displayText = "";
        for (int i = 0; i < programLines.Count; i++)
        {
            ProgramLine line = programLines[i];

            // 行番号の追加（オプション）
            if (showLineNumbers)
            {
                string lineNumber = $"{i + 1:D2}  ";
                if (enableSyntaxHighlight)
                {
                    lineNumber = $"<color=#808080>{lineNumber}</color>"; // グレー
                }
                displayText += lineNumber;
            }

            // インデントの追加
            for (int j = 0; j < line.indentLevel; j++)
            {
                displayText += indentString;
            }

            // 内容の追加（色分け付き）
            if (enableSyntaxHighlight)
            {
                displayText += ApplySyntaxHighlight(line);
            }
            else
            {
                displayText += line.content;
            }

            // 最後の行でない場合は改行
            if (i < programLines.Count - 1)
            {
                displayText += "\n";
            }
        }

        // InputFieldに反映
        codeOutput.text = displayText;
        lastKnownText = displayText;

        // 現在のリストを保存
        UpdatePreviousLines();

        isUpdatingFromCode = false; // 更新フラグをリセット
    }

    // シンタックスハイライトを適用
    private string ApplySyntaxHighlight(ProgramLine line)
    {
        string content = line.content;
        string colorHex;

        switch (line.lineType)
        {
            case ProgramLine.LineType.Comment:
                colorHex = ColorUtility.ToHtmlStringRGB(commentColor);
                return $"<color=#{colorHex}>{content}</color>";

            case ProgramLine.LineType.IfStart:
                if (content == "{" || content == "}")
                {
                    colorHex = ColorUtility.ToHtmlStringRGB(braceColor);
                    return $"<color=#{colorHex}>{content}</color>";
                }
                else
                {
                    return HighlightIfStatement(content);
                }

            case ProgramLine.LineType.IfEnd:
                colorHex = ColorUtility.ToHtmlStringRGB(braceColor);
                return $"<color=#{colorHex}>{content}</color>";

            case ProgramLine.LineType.LoopStart:
                if (content == "{" || content == "}")
                {
                    colorHex = ColorUtility.ToHtmlStringRGB(braceColor);
                    return $"<color=#{colorHex}>{content}</color>";
                }
                else
                {
                    return HighlightLoopStatement(content);
                }

            case ProgramLine.LineType.LoopEnd:
                colorHex = ColorUtility.ToHtmlStringRGB(braceColor);
                return $"<color=#{colorHex}>{content}</color>";

            case ProgramLine.LineType.VariableSet:
            case ProgramLine.LineType.ArraySet:
                return HighlightSetStatement(content);

            case ProgramLine.LineType.Command:
            default:
                return HighlightCommand(content);
        }
    }

    // if文のハイライト
    private string HighlightIfStatement(string content)
    {
        string keywordColorHex = ColorUtility.ToHtmlStringRGB(keywordColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        // if(condition) の形式を解析
        if (content.StartsWith("if(") && content.EndsWith(")"))
        {
            string condition = content.Substring(3, content.Length - 4); // "if(" と ")" を除去
            condition = HighlightExpression(condition);

            return $"<color=#{keywordColorHex}>if</color><color=#{braceColorHex}>(</color>{condition}<color=#{braceColorHex}>)</color>";
        }

        return content;
    }

    // loop文のハイライト
    private string HighlightLoopStatement(string content)
    {
        string keywordColorHex = ColorUtility.ToHtmlStringRGB(keywordColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        // loop(count) の形式を解析
        if (content.StartsWith("loop(") && content.EndsWith(")"))
        {
            string count = content.Substring(5, content.Length - 6); // "loop(" と ")" を除去
            count = HighlightExpression(count);

            return $"<color=#{keywordColorHex}>loop</color><color=#{braceColorHex}>(</color>{count}<color=#{braceColorHex}>)</color>";
        }

        return content;
    }

    // Set文のハイライト
    private string HighlightSetStatement(string content)
    {
        string keywordColorHex = ColorUtility.ToHtmlStringRGB(keywordColor);
        string variableColorHex = ColorUtility.ToHtmlStringRGB(variableColor);
        string stringColorHex = ColorUtility.ToHtmlStringRGB(stringColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        // Set(varName, value) の形式を解析
        if (content.StartsWith("Set(") && content.EndsWith(")"))
        {
            string inner = content.Substring(4, content.Length - 5); // "Set(" と ")" を除去
            int commaIndex = FindOuterComma(inner);

            if (commaIndex > 0)
            {
                string varName = inner.Substring(0, commaIndex).Trim();
                string value = inner.Substring(commaIndex + 1).Trim();

                // 配列の場合
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    string arrayContent = value.Substring(1, value.Length - 2);
                    string[] elements = arrayContent.Split(',');

                    string highlightedArray = $"<color=#{braceColorHex}>[</color>";
                    for (int i = 0; i < elements.Length; i++)
                    {
                        string element = elements[i].Trim().Trim('"');
                        highlightedArray += $"<color=#{stringColorHex}>\"{element}\"</color>";
                        if (i < elements.Length - 1)
                        {
                            highlightedArray += $"<color=#{braceColorHex}>, </color>";
                        }
                    }
                    highlightedArray += $"<color=#{braceColorHex}>]</color>";

                    return $"<color=#{keywordColorHex}>Set</color><color=#{braceColorHex}>(</color><color=#{variableColorHex}>{varName}</color><color=#{braceColorHex}>, </color>{highlightedArray}<color=#{braceColorHex}>)</color>";
                }
                else
                {
                    // 単一変数
                    string cleanValue = value.Trim('"');
                    return $"<color=#{keywordColorHex}>Set</color><color=#{braceColorHex}>(</color><color=#{variableColorHex}>{varName}</color><color=#{braceColorHex}>, </color><color=#{stringColorHex}>\"{cleanValue}\"</color><color=#{braceColorHex}>)</color>";
                }
            }
        }

        return content;
    }

    // コマンドのハイライト
    private string HighlightCommand(string content)
    {
        string commandColorHex = ColorUtility.ToHtmlStringRGB(commandColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        // コマンド(引数) の形式を解析
        int parenIndex = content.IndexOf('(');
        if (parenIndex > 0 && content.EndsWith(")"))
        {
            string commandName = content.Substring(0, parenIndex);
            string args = content.Substring(parenIndex + 1, content.Length - parenIndex - 2);

            string highlightedArgs = "";
            if (!string.IsNullOrEmpty(args))
            {
                highlightedArgs = HighlightExpression(args);
            }

            return $"<color=#{commandColorHex}>{commandName}</color><color=#{braceColorHex}>(</color>{highlightedArgs}<color=#{braceColorHex}>)</color>";
        }

        // 代入文の場合 (arr[j] = value など)
        if (content.Contains("=") && !content.Contains("==") && !content.Contains("!="))
        {
            string[] parts = content.Split('=');
            if (parts.Length == 2)
            {
                string left = HighlightExpression(parts[0].Trim());
                string right = HighlightExpression(parts[1].Trim());
                return $"{left} <color=#{braceColorHex}>=</color> {right}";
            }
        }

        return $"<color=#{commandColorHex}>{content}</color>";
    }

    // 式のハイライト（変数、配列アクセス、数値など）
    private string HighlightExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return expression;

        string variableColorHex = ColorUtility.ToHtmlStringRGB(variableColor);
        string numberColorHex = ColorUtility.ToHtmlStringRGB(numberColor);
        string stringColorHex = ColorUtility.ToHtmlStringRGB(stringColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        expression = expression.Trim();

        // 文字列リテラル
        if (expression.StartsWith("\"") && expression.EndsWith("\""))
        {
            return $"<color=#{stringColorHex}>{expression}</color>";
        }

        // 数値
        if (System.Text.RegularExpressions.Regex.IsMatch(expression, @"^\d+$"))
        {
            return $"<color=#{numberColorHex}>{expression}</color>";
        }

        // 配列アクセス arr[index] の形式
        if (expression.Contains("[") && expression.EndsWith("]"))
        {
            int bracketStart = expression.IndexOf('[');
            string varName = expression.Substring(0, bracketStart);
            string index = expression.Substring(bracketStart + 1, expression.Length - bracketStart - 2);

            string highlightedIndex = HighlightExpression(index);
            return $"<color=#{variableColorHex}>{varName}</color><color=#{braceColorHex}>[</color>{highlightedIndex}<color=#{braceColorHex}>]</color>";
        }

        // 比較演算子を含む式
        if (expression.Contains(">") || expression.Contains("<") || expression.Contains("==") || expression.Contains("!="))
        {
            return HighlightComparison(expression);
        }

        // 算術式 (n-1, j+1 など)
        if (expression.Contains("+") || expression.Contains("-"))
        {
            return HighlightArithmetic(expression);
        }

        // 単純な変数
        return $"<color=#{variableColorHex}>{expression}</color>";
    }

    // 比較式のハイライト
    private string HighlightComparison(string expression)
    {
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };

        foreach (string op in operators)
        {
            if (expression.Contains(op))
            {
                string[] parts = expression.Split(new[] { op }, System.StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string left = HighlightExpression(parts[0].Trim());
                    string right = HighlightExpression(parts[1].Trim());
                    return $"{left} <color=#{braceColorHex}>{op}</color> {right}";
                }
            }
        }

        return expression;
    }

    // 算術式のハイライト
    private string HighlightArithmetic(string expression)
    {
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);

        // 簡単な加減算のみ対応
        if (expression.Contains("+"))
        {
            string[] parts = expression.Split('+');
            if (parts.Length == 2)
            {
                string left = HighlightExpression(parts[0].Trim());
                string right = HighlightExpression(parts[1].Trim());
                return $"{left}<color=#{braceColorHex}>+</color>{right}";
            }
        }
        else if (expression.Contains("-"))
        {
            string[] parts = expression.Split('-');
            if (parts.Length == 2)
            {
                string left = HighlightExpression(parts[0].Trim());
                string right = HighlightExpression(parts[1].Trim());
                return $"{left}<color=#{braceColorHex}>-</color>{right}";
            }
        }

        return expression;
    }

    // === 外部から使用するメソッド ===

    // 単一コマンドを追加（自動インデント計算）
    public void AddCommand(string command)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(ProgramLine.LineType.Command, command, currentIndent));
        UpdateUI();
    }

    // 単一コマンドを追加（手動インデント指定）
    public void AddCommand(string command, int indentLevel)
    {
        programLines.Add(new ProgramLine(ProgramLine.LineType.Command, command, indentLevel));
        UpdateUI();
    }

    // if文を追加（自動インデント計算）
    public void AddIfStatement(string condition)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(ProgramLine.LineType.IfStart, $"if({condition})", currentIndent, condition));
        programLines.Add(new ProgramLine(ProgramLine.LineType.IfStart, "{", currentIndent));
        UpdateUI();
    }

    // if文を追加（手動インデント指定）
    public void AddIfStatement(string condition, int indentLevel)
    {
        programLines.Add(new ProgramLine(ProgramLine.LineType.IfStart, $"if({condition})", indentLevel, condition));
        programLines.Add(new ProgramLine(ProgramLine.LineType.IfStart, "{", indentLevel));
        UpdateUI();
    }

    // loop文を追加（自動インデント計算）
    public void AddLoopStatement(string count)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(ProgramLine.LineType.LoopStart, $"loop({count})", currentIndent, count));
        programLines.Add(new ProgramLine(ProgramLine.LineType.LoopStart, "{", currentIndent));
        UpdateUI();
    }

    // loop文を追加（手動インデント指定）
    public void AddLoopStatement(string count, int indentLevel)
    {
        programLines.Add(new ProgramLine(ProgramLine.LineType.LoopStart, $"loop({count})", indentLevel, count));
        programLines.Add(new ProgramLine(ProgramLine.LineType.LoopStart, "{", indentLevel));
        UpdateUI();
    }

    // 現在のインデントレベルを取得（テキスト同期に対応）
    private int GetCurrentIndentLevel()
    {
        // プログラムが空の場合
        if (programLines.Count == 0)
        {
            return 0;
        }

        // 最後の行のインデントレベルを基準にする
        ProgramLine lastLine = programLines[programLines.Count - 1];

        // 最後の行が閉じブレースの場合、その内部のインデントレベルを計算
        if (lastLine.content == "}")
        {
            return lastLine.indentLevel;
        }
        // 最後の行が開きブレースの場合、次の行はインデント+1
        else if (lastLine.content == "{")
        {
            return lastLine.indentLevel + 1;
        }
        // 通常の行の場合、同じインデントレベル
        else
        {
            return lastLine.indentLevel;
        }
    }

    // ブロック構造を分析してインデントレベルを計算
    private int CalculateIndentFromBlockStructure()
    {
        int openBlocks = 0;

        foreach (ProgramLine line in programLines)
        {
            if (line.content == "{")
            {
                openBlocks++;
            }
            else if (line.content == "}")
            {
                openBlocks--;
            }
        }

        return Mathf.Max(0, openBlocks);
    }

    // ブロックを閉じる（}を追加）- テキスト同期対応版
    public void CloseBlock()
    {
        if (programLines.Count == 0)
        {
            // リストが空の場合は、インデント0で追加
            programLines.Add(new ProgramLine(ProgramLine.LineType.IfEnd, "}", 0));
            UpdateUI();
            return;
        }

        // 対応する開きブレースを見つける方法を改良
        int indentLevel = FindMatchingOpenBraceIndent();

        programLines.Add(new ProgramLine(ProgramLine.LineType.IfEnd, "}", indentLevel));
        UpdateUI();
    }

    // 対応する開きブレースのインデントレベルを見つける
    private int FindMatchingOpenBraceIndent()
    {
        int braceCount = 0;

        // 後ろから検索して、対応する開きブレースを見つける
        for (int i = programLines.Count - 1; i >= 0; i--)
        {
            ProgramLine line = programLines[i];

            if (line.content == "}")
            {
                braceCount++;
            }
            else if (line.content == "{")
            {
                if (braceCount == 0)
                {
                    // 対応する開きブレースが見つかった
                    return line.indentLevel;
                }
                braceCount--;
            }
        }

        // 対応する開きブレースが見つからない場合
        // 最後の行のインデントレベルを参考にする
        if (programLines.Count > 0)
        {
            ProgramLine lastLine = programLines[programLines.Count - 1];
            return Mathf.Max(0, lastLine.indentLevel - 1);
        }

        return 0;
    }

    // 指定したインデックスに新しい行を挿入
    public void InsertLineAt(int index, ProgramLine newLine)
    {
        if (index >= 0 && index <= programLines.Count)
        {
            programLines.Insert(index, newLine);
            UpdateUI();
        }
    }

    // 指定したインデックスの行を削除
    public void RemoveLineAt(int index)
    {
        if (index >= 0 && index < programLines.Count)
        {
            programLines.RemoveAt(index);
            UpdateUI();
        }
    }

    // プログラム全体をクリア
    public void ClearProgram()
    {
        programLines.Clear();
        UpdateUI();
    }

    // 現在のプログラムラインを取得
    public List<ProgramLine> GetProgramLines()
    {
        return new List<ProgramLine>(programLines);
    }

    // プログラムラインを一括設定
    public void SetProgramLines(List<ProgramLine> newLines)
    {
        programLines = new List<ProgramLine>(newLines);
        UpdateUI();
    }

    // インデントレベルを自動調整
    public void AutoAdjustIndent()
    {
        int currentIndent = 0;

        for (int i = 0; i < programLines.Count; i++)
        {
            ProgramLine line = programLines[i];

            // 閉じブラケットの場合、先にインデントを減らす
            if (line.content == "}")
            {
                currentIndent = Mathf.Max(0, currentIndent - 1);
                line.indentLevel = currentIndent;
            }
            else
            {
                line.indentLevel = currentIndent;

                // 開きブラケットの場合、次の行からインデントを増やす
                if (line.content == "{")
                {
                    currentIndent++;
                }
            }
        }

        UpdateUI();
    }

    // プログラムの構造が正しいかチェック
    public bool ValidateStructure()
    {
        int braceCount = 0;

        foreach (ProgramLine line in programLines)
        {
            if (line.content == "{")
            {
                braceCount++;
            }
            else if (line.content == "}")
            {
                braceCount--;
                if (braceCount < 0)
                {
                    Debug.LogError("Too many closing braces!");
                    return false;
                }
            }
        }

        if (braceCount != 0)
        {
            Debug.LogError("Mismatched braces!");
            return false;
        }

        return true;
    }

    // === 変数・配列定義用メソッド ===

    // 変数を定義（自動インデント）
    public void AddVariable(string varName, string varValue)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(varName, varValue, currentIndent));
        UpdateUI();
    }

    // 変数を定義（手動インデント）
    public void AddVariable(string varName, string varValue, int indentLevel)
    {
        programLines.Add(new ProgramLine(varName, varValue, indentLevel));
        UpdateUI();
    }

    // 配列を定義（自動インデント）
    public void AddArray(string arrayName, List<string> arrayValues)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(arrayName, arrayValues, currentIndent));
        UpdateUI();
    }

    // 配列を定義（手動インデント）
    public void AddArray(string arrayName, List<string> arrayValues, int indentLevel)
    {
        programLines.Add(new ProgramLine(arrayName, arrayValues, indentLevel));
        UpdateUI();
    }

    // コメントを追加（自動インデント）
    public void AddComment(string comment)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(ProgramLine.LineType.Comment, comment, currentIndent));
        UpdateUI();
    }

    // コメントを追加（手動インデント）
    public void AddComment(string comment, int indentLevel)
    {
        programLines.Add(new ProgramLine(ProgramLine.LineType.Comment, comment, indentLevel));
        UpdateUI();
    }

    // === ソートアルゴリズム専用メソッド（変数定義込み） ===

    // バブルソートを生成（変数定義込み）
    public void CreateBubbleSort()
    {
        ClearProgram();
        AddComment("// バブルソートアルゴリズム");
        AddArray("arr", new List<string> { "5", "2", "8", "1", "9" });
        AddVariable("n", "arr.length");
        AddLoopStatement("n-1");  // 外側のループ
        AddLoopStatement("n-i-1"); // 内側のループ
        AddIfStatement("arr[j] > arr[j+1]");  // 条件分岐
        AddCommand("Swap(arr[j], arr[j+1])"); // 交換処理
        CloseBlock(); // if文終了
        CloseBlock(); // 内側のループ終了
        CloseBlock(); // 外側のループ終了
        AddComment("// ソート完了");
    }

    // 選択ソートを生成（変数定義込み）
    public void CreateSelectionSort()
    {
        ClearProgram();
        AddComment("// 選択ソートアルゴリズム");
        AddArray("arr", new List<string> { "5", "2", "8", "1", "9" });
        AddVariable("n", "arr.length");
        AddLoopStatement("n-1");  // 外側のループ
        AddVariable("minIndex", "i");
        AddLoopStatement("n"); // 内側のループ（i+1からn-1まで）
        AddIfStatement("arr[j] < arr[minIndex]");
        AddVariable("minIndex", "j");
        CloseBlock(); // if文終了
        CloseBlock(); // 内側のループ終了
        AddCommand("Swap(arr[i], arr[minIndex])");
        CloseBlock(); // 外側のループ終了
        AddComment("// ソート完了");
    }

    // 挿入ソートを生成（変数定義込み）
    public void CreateInsertionSort()
    {
        ClearProgram();
        AddComment("// 挿入ソートアルゴリズム");
        AddArray("arr", new List<string> { "5", "2", "8", "1", "9" });
        AddVariable("n", "arr.length");
        AddLoopStatement("n-1");  // i = 1 to n-1
        AddVariable("key", "arr[i]");
        AddVariable("j", "i - 1");
        AddLoopStatement("while j >= 0");  // while文として表現
        AddIfStatement("arr[j] > key");
        AddCommand("arr[j+1] = arr[j]");
        AddCommand("j = j - 1");
        CloseBlock(); // if文終了
        CloseBlock(); // while文終了
        AddCommand("arr[j+1] = key");
        CloseBlock(); // 外側のループ終了
        AddComment("// ソート完了");
    }

    // ネストレベルを可視化するメソッド
    public void ShowNestingLevels()
    {
        Debug.Log("=== Nesting Levels ===");
        for (int i = 0; i < programLines.Count; i++)
        {
            ProgramLine line = programLines[i];
            string indentDisplay = new string('-', line.indentLevel * 2);
            Debug.Log($"Line {i + 1}: {indentDisplay} [{line.indentLevel}] {line.content}");
        }
    }

    // デバッグ用：プログラムの実行シミュレーション
    public void SimulateExecution()
    {
        Debug.Log("=== Program Execution Simulation ===");
        ExecuteLines(programLines, 0, programLines.Count);
    }

    private void ExecuteLines(List<ProgramLine> lines, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            ProgramLine line = lines[i];
            string indent = "";
            for (int j = 0; j < line.indentLevel; j++) indent += "  ";

            switch (line.lineType)
            {
                case ProgramLine.LineType.Command:
                    Debug.Log($"{indent}Executing: {line.content}");
                    break;

                case ProgramLine.LineType.IfStart:
                    if (line.content.StartsWith("if("))
                    {
                        Debug.Log($"{indent}Checking condition: {line.condition}");
                    }
                    break;

                case ProgramLine.LineType.LoopStart:
                    if (line.content.StartsWith("loop("))
                    {
                        Debug.Log($"{indent}Starting loop: {line.condition} times");
                    }
                    break;

                case ProgramLine.LineType.VariableSet:
                    Debug.Log($"{indent}Setting variable: {line.variableName} = {line.variableValue}");
                    break;

                case ProgramLine.LineType.ArraySet:
                    Debug.Log($"{indent}Setting array: {line.variableName} = [{string.Join(", ", line.arrayValues)}]");
                    break;

                case ProgramLine.LineType.Comment:
                    Debug.Log($"{indent}Comment: {line.content}");
                    break;
            }
        }
    }

    // === 元のコードとの互換性を保つメソッド ===

    // 外部からカードを追加（元のメソッドとの互換性）
    public void AddCard(string cardType)
    {
        AddCommand(cardType);
    }

    // 外部からカードを削除（元のメソッドとの互換性）
    public void RemoveCard(string cardType)
    {
        for (int i = programLines.Count - 1; i >= 0; i--)
        {
            if (programLines[i].content == cardType)
            {
                programLines.RemoveAt(i);
                break;
            }
        }
        UpdateUI();
    }

    // 外部からインデックスでカードを削除（元のメソッドとの互換性）
    public void RemoveCardAt(int index)
    {
        RemoveLineAt(index);
    }

    // カードリストをクリア（元のメソッドとの互換性）
    public void ClearCards()
    {
        ClearProgram();
    }

    // カードの数を取得（元のメソッドとの互換性）
    public int GetCardCount()
    {
        return programLines.Count;
    }

    // カードの順番を入れ替え
    public void SwapCards(int index1, int index2)
    {
        if (index1 >= 0 && index1 < programLines.Count &&
            index2 >= 0 && index2 < programLines.Count)
        {
            ProgramLine temp = programLines[index1];
            programLines[index1] = programLines[index2];
            programLines[index2] = temp;
            UpdateUI();
        }
    }

    // 特定のインデックスのカードを変更
    public void SetCardAt(int index, string newCardType)
    {
        if (index >= 0 && index < programLines.Count)
        {
            programLines[index].content = newCardType;
            UpdateUI();
        }
    }

    // 特定のカードが含まれているかチェック
    public bool ContainsCard(string cardType)
    {
        foreach (ProgramLine line in programLines)
        {
            if (line.content == cardType)
                return true;
        }
        return false;
    }

    // === UI操作メソッド（テキスト同期対応版） ===

    public void InputCommand()
    {
        AddCommand("Attack()");
    }

    public void InputArray(string input)
    {
        AddArray("arrayName", new List<string> { input });
    }

    public void InputVariable()
    {
        AddVariable("variableName", "value");
    }

    public void InputLoop()
    {
        AddLoopStatement("");
    }

    public void InputIf()
    {
        AddIfStatement("");
    }

    public void CloseSection()
    {
        CloseBlock();
    }

    // テキスト同期を実行
    public void TextSync()
    {
        SyncFromTextContent();
    }

    // === デバッグ用メソッド ===

    // 現在のインデント状態をデバッグ表示
    public void DebugIndentState()
    {
        Debug.Log("=== Current State Debug ===");
        Debug.Log($"GetCurrentIndentLevel(): {GetCurrentIndentLevel()}");
        Debug.Log($"CalculateIndentFromBlockStructure(): {CalculateIndentFromBlockStructure()}");
        Debug.Log($"Syntax Highlight: {(enableSyntaxHighlight ? "ON" : "OFF")}");

        if (programLines.Count > 0)
        {
            ProgramLine lastLine = programLines[programLines.Count - 1];
            Debug.Log($"Last line: '{lastLine.content}' (type: {lastLine.lineType}, indent: {lastLine.indentLevel})");
        }

        Debug.Log($"Total lines: {programLines.Count}");
        Debug.Log("=== End Debug ===");
    }
}