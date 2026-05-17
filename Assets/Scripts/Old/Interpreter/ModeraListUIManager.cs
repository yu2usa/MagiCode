using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ModeraListUIManager : MonoBehaviour
{
    // プログラムの構造を表すクラス（内部クラスとして定義）
    [System.Serializable]
    public class ProgramLine
    {
        public enum LineType
        {
            VariableDeclaration,    // 変数宣言（int x = 5）
            ArrayDeclaration,       // 配列宣言（string[] arr = ["a", "b"]）
            FunctionDef,            // 関数定義（def function_name():）
            ForLoop,                // forループ（for i in range(n):）
            IfStatement,            // if文（if condition:）
            Assignment,             // 代入文（x = 10）
            FunctionCall,           // 関数呼び出し（function_name()）
            Comment,                // コメント行（# コメント）
            Indent,                 // インデント用の空行
            BlockEnd                // ブロック終了（Pythonでは明示的な終了なし）
        }

        public LineType lineType;
        public string content;          // 表示するテキスト
        public int indentLevel;         // インデントのレベル（0, 1, 2...）
        public string condition;        // if文の条件やfor文の範囲など
        public string variableType;     // 変数の型（int, string など）
        public string variableName;     // 変数名
        public string variableValue;    // 変数の値
        public List<string> arrayValues; // 配列の値
        public string functionName;     // 関数名
        public List<string> parameters; // 関数のパラメータ

        public ProgramLine(LineType type, string text, int indent = 0, string cond = "")
        {
            lineType = type;
            content = text;
            indentLevel = indent;
            condition = cond;
            variableType = "";
            variableName = "";
            variableValue = "";
            arrayValues = new List<string>();
            functionName = "";
            parameters = new List<string>();
        }

        // 変数宣言用コンストラクタ
        public ProgramLine(string varType, string varName, string varValue, int indent = 0)
        {
            lineType = LineType.VariableDeclaration;
            variableType = varType;
            variableName = varName;
            variableValue = varValue;
            content = $"{varType} {varName} = \"{varValue}\"";
            indentLevel = indent;
            condition = "";
            arrayValues = new List<string>();
            functionName = "";
            parameters = new List<string>();
        }

        // 配列宣言用コンストラクタ
        public ProgramLine(string arrayType, string arrayName, List<string> arrValues, int indent = 0)
        {
            lineType = LineType.ArrayDeclaration;
            variableType = arrayType + "[]";
            variableName = arrayName;
            arrayValues = new List<string>(arrValues);
            string arrayString = "[\"" + string.Join("\", \"", arrValues) + "\"]";
            content = $"{arrayType}[] {arrayName} = {arrayString}";
            indentLevel = indent;
            condition = "";
            variableValue = "";
            functionName = "";
            parameters = new List<string>();
        }

        // パラメータなしのコンストラクタ（Unity Serialization用）
        public ProgramLine()
        {
            lineType = LineType.FunctionCall;
            content = "";
            indentLevel = 0;
            condition = "";
            variableType = "";
            variableName = "";
            variableValue = "";
            arrayValues = new List<string>();
            functionName = "";
            parameters = new List<string>();
        }
    }

    [Header("UI Settings")]
    [SerializeField] private TMP_InputField codeOutput;

    [Header("Text Synchronization")]
    [SerializeField] private bool enableTextSync = true;
    [SerializeField] private bool realTimeSync = false;
    [SerializeField] private KeyCode syncKey = KeyCode.F5;
    [SerializeField] private float syncInterval = 2.0f;
    [SerializeField] private bool syncFromText = false;

    [Header("Program List")]
    [SerializeField] private List<ProgramLine> programLines = new List<ProgramLine>();

    [Header("Display Settings")]
    [SerializeField] private string indentString = "    ";
    [SerializeField] private bool showLineNumbers = true;
    [SerializeField] private bool enableSyntaxHighlight = true;

    [Header("Syntax Highlight Colors")]
    [SerializeField] private Color commentColor = Color.green;
    [SerializeField] private Color keywordColor = new Color(0.8f, 0.4f, 1f); // 紫色 (def, if, for)
    [SerializeField] private Color variableColor = Color.white; // 変数名: 白色
    [SerializeField] private Color stringColor = new Color(1f, 0.6f, 0f); // 文字列: オレンジ色
    [SerializeField] private Color numberColor = new Color(1f, 0.6f, 0f); // 数値: オレンジ
    [SerializeField] private Color braceColor = Color.white;
    [SerializeField] private Color commandColor = new Color(1f, 1f, 0.7f); // コマンド: 薄い黄色
    [SerializeField] private Color typeColor = Color.cyan; // 型名: シアン

    [Header("Quick Actions")]
    [SerializeField] private bool createSampleFunction = false;
    [SerializeField] private bool createForLoopSample = false;
    [SerializeField] private bool clearProgram = false;
    [SerializeField] private bool validateProgram = false;

    [Header("Manual Input - Variables")]
    [SerializeField] private string varType = "int";
    [SerializeField] private string varName = "x";
    [SerializeField] private string varValue = "10";
    [SerializeField] private bool addVariable = false;

    [Header("Manual Input - Arrays")]
    [SerializeField] private string arrayType = "string";
    [SerializeField] private string arrayName = "names";
    [SerializeField] private string[] arrayElements = { "item1", "item2", "item3" };
    [SerializeField] private bool addArray = false;

    [Header("Manual Input - Control Flow")]
    [SerializeField] private string forVariable = "i";
    [SerializeField] private string forRange = "10";
    [SerializeField] private bool addForLoop = false;
    [SerializeField] private string ifCondition = "x > 5";
    [SerializeField] private bool addIfStatement = false;

    [Header("Manual Input - Functions")]
    [SerializeField] private string functionName = "my_function";
    [SerializeField] private string[] functionParams = { "int x", "string name" };
    [SerializeField] private bool addFunction = false;
    [SerializeField] private string callFunctionName = "attack";
    [SerializeField] private string[] callArgs = { "enemy", "sword" };
    [SerializeField] private bool addFunctionCall = false;

    [Header("Manual Input - Others")]
    [SerializeField] private string commentText = "# コメント";
    [SerializeField] private bool addComment = false;
    [SerializeField] private string assignmentVar = "x";
    [SerializeField] private string assignmentValue = "20";
    [SerializeField] private bool addAssignment = false;

    private List<ProgramLine> previousProgramLines = new List<ProgramLine>();
    private List<Transform> trackedChildren = new List<Transform>();

    // テキスト同期用
    private string lastKnownText = "";
    private float lastSyncTime = 0f;
    private bool isUpdatingFromCode = false;

    void Start()
    {
        // 初期サンプルデータ
        InitializeSampleData();
        UpdateUI();

        if (codeOutput != null)
        {
            codeOutput.onValueChanged.AddListener(OnInputFieldChanged);
            lastKnownText = codeOutput.text;
        }
    }

    void Update()
    {
        HandleInspectorControls();

        if (enableTextSync && !isUpdatingFromCode)
        {
            HandleTextSynchronization();
        }

        if (HasListChanged())
        {
            UpdateUI();
        }

        HandleChildObjects();
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

        if (Input.GetKeyDown(syncKey))
        {
            SyncFromTextContent();
            return;
        }

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
        textContent = RemoveRichTextTags(textContent);
        lastKnownText = textContent;

        try
        {
            isUpdatingFromCode = true;

            string[] lines = textContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.None);
            List<ProgramLine> newProgramLines = new List<ProgramLine>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                ProgramLine parsedLine = ParsePythonLine(line, i + 1);

                if (parsedLine != null)
                {
                    newProgramLines.Add(parsedLine);
                }
            }

            programLines = newProgramLines;
            UpdatePreviousLines();

            Debug.Log($"<color=cyan>Synchronized {newProgramLines.Count} Python lines from text</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Python同期エラー: {ex.Message}");
        }
        finally
        {
            isUpdatingFromCode = false;
        }
    }

    // Python行を解析してProgramLineに変換
    private ProgramLine ParsePythonLine(string line, int lineNumber)
    {
        if (string.IsNullOrEmpty(line)) return null;

        string cleanLine = RemoveLineNumber(line);
        if (string.IsNullOrWhiteSpace(cleanLine)) return null;

        int indentLevel = CalculateIndentLevel(cleanLine);
        cleanLine = cleanLine.Trim();

        return ClassifyPythonLine(cleanLine, indentLevel);
    }

    // Python行を分類してProgramLineを作成
    private ProgramLine ClassifyPythonLine(string content, int indentLevel)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        // コメント行
        if (content.StartsWith("#"))
        {
            return new ProgramLine(ProgramLine.LineType.Comment, content, indentLevel);
        }

        // 関数定義
        if (content.StartsWith("def ") && content.EndsWith(":"))
        {
            return ParseFunctionDefinition(content, indentLevel);
        }

        // forループ
        if (content.StartsWith("for ") && content.Contains(" in range(") && content.EndsWith(":"))
        {
            return ParseForLoop(content, indentLevel);
        }

        // if文
        if (content.StartsWith("if ") && content.EndsWith(":"))
        {
            return ParseIfStatement(content, indentLevel);
        }

        // 変数宣言
        if (Regex.IsMatch(content, @"^(int|string|float|bool)\s+\w+\s*="))
        {
            return ParseVariableDeclaration(content, indentLevel);
        }

        // 配列宣言
        if (Regex.IsMatch(content, @"^(int|string|float|bool)\[\]\s+\w+\s*="))
        {
            return ParseArrayDeclaration(content, indentLevel);
        }

        // 代入文
        if (content.Contains("=") && !content.Contains("==") && !content.Contains("!="))
        {
            return ParseAssignment(content, indentLevel);
        }

        // 関数呼び出し
        if (content.Contains("(") && content.EndsWith(")"))
        {
            return ParseFunctionCall(content, indentLevel);
        }

        // その他はコメントとして扱う
        return new ProgramLine(ProgramLine.LineType.FunctionCall, content, indentLevel);
    }

    // 関数定義の解析
    private ProgramLine ParseFunctionDefinition(string content, int indentLevel)
    {
        var match = Regex.Match(content, @"def\s+(\w+)\s*\((.*?)\)\s*:");
        if (match.Success)
        {
            string funcName = match.Groups[1].Value;
            string paramString = match.Groups[2].Value.Trim();

            var line = new ProgramLine(ProgramLine.LineType.FunctionDef, content, indentLevel);
            line.functionName = funcName;

            if (!string.IsNullOrEmpty(paramString))
            {
                var paramParts = paramString.Split(',');
                foreach (var param in paramParts)
                {
                    line.parameters.Add(param.Trim());
                }
            }

            return line;
        }

        return new ProgramLine(ProgramLine.LineType.FunctionDef, content, indentLevel);
    }

    // forループの解析
    private ProgramLine ParseForLoop(string content, int indentLevel)
    {
        var match = Regex.Match(content, @"for\s+(\w+)\s+in\s+range\s*\(\s*(.+?)\s*\)\s*:");
        if (match.Success)
        {
            string iterVar = match.Groups[1].Value;
            string rangeExpr = match.Groups[2].Value;

            var line = new ProgramLine(ProgramLine.LineType.ForLoop, content, indentLevel, rangeExpr);
            line.variableName = iterVar;

            return line;
        }

        return new ProgramLine(ProgramLine.LineType.ForLoop, content, indentLevel);
    }

    // if文の解析
    private ProgramLine ParseIfStatement(string content, int indentLevel)
    {
        string condition = content.Substring(3, content.Length - 4).Trim(); // "if " と ":" を除去
        return new ProgramLine(ProgramLine.LineType.IfStatement, content, indentLevel, condition);
    }

    // 変数宣言の解析
    private ProgramLine ParseVariableDeclaration(string content, int indentLevel)
    {
        var match = Regex.Match(content, @"^(\w+)\s+(\w+)\s*=\s*""?([^""]+)""?$");
        if (match.Success)
        {
            string varType = match.Groups[1].Value;
            string varName = match.Groups[2].Value;
            string varValue = match.Groups[3].Value;

            return new ProgramLine(varType, varName, varValue, indentLevel);
        }

        return new ProgramLine(ProgramLine.LineType.VariableDeclaration, content, indentLevel);
    }

    // 配列宣言の解析
    private ProgramLine ParseArrayDeclaration(string content, int indentLevel)
    {
        var match = Regex.Match(content, @"^(\w+)\[\]\s+(\w+)\s*=\s*\[(.*?)\]$");
        if (match.Success)
        {
            string arrayType = match.Groups[1].Value;
            string arrayName = match.Groups[2].Value;
            string elements = match.Groups[3].Value;

            var elementList = new List<string>();
            if (!string.IsNullOrEmpty(elements))
            {
                var elementArray = elements.Split(',');
                foreach (var element in elementArray)
                {
                    string cleanElement = element.Trim().Trim('"');
                    elementList.Add(cleanElement);
                }
            }

            return new ProgramLine(arrayType, arrayName, elementList, indentLevel);
        }

        return new ProgramLine(ProgramLine.LineType.ArrayDeclaration, content, indentLevel);
    }

    // 代入文の解析
    private ProgramLine ParseAssignment(string content, int indentLevel)
    {
        var parts = content.Split('=');
        if (parts.Length == 2)
        {
            string varName = parts[0].Trim();
            string value = parts[1].Trim().Trim('"');

            var line = new ProgramLine(ProgramLine.LineType.Assignment, content, indentLevel);
            line.variableName = varName;
            line.variableValue = value;

            return line;
        }

        return new ProgramLine(ProgramLine.LineType.Assignment, content, indentLevel);
    }

    // 関数呼び出しの解析
    private ProgramLine ParseFunctionCall(string content, int indentLevel)
    {
        var match = Regex.Match(content, @"^(\w+)\s*\((.*?)\)$");
        if (match.Success)
        {
            string funcName = match.Groups[1].Value;
            string argsString = match.Groups[2].Value;

            var line = new ProgramLine(ProgramLine.LineType.FunctionCall, content, indentLevel);
            line.functionName = funcName;

            if (!string.IsNullOrEmpty(argsString))
            {
                var argArray = argsString.Split(',');
                foreach (var arg in argArray)
                {
                    line.parameters.Add(arg.Trim());
                }
            }

            return line;
        }

        return new ProgramLine(ProgramLine.LineType.FunctionCall, content, indentLevel);
    }

    // リッチテキストタグを除去
    private string RemoveRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = Regex.Replace(text, @"<color=#[0-9A-Fa-f]{6}>", "");
        text = Regex.Replace(text, @"</color>", "");
        text = Regex.Replace(text, @"<[^>]+>", "");

        return text;
    }

    // 行番号を除去
    private string RemoveLineNumber(string line)
    {
        if (showLineNumbers)
        {
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

    // インスペクター操作を処理
    private void HandleInspectorControls()
    {
        if (createSampleFunction)
        {
            createSampleFunction = false;
            CreateSampleFunction();
            Debug.Log("Sample function created!");
        }

        if (createForLoopSample)
        {
            createForLoopSample = false;
            CreateForLoopSample();
            Debug.Log("For loop sample created!");
        }

        if (clearProgram)
        {
            clearProgram = false;
            ClearProgram();
            Debug.Log("Program cleared!");
        }

        if (validateProgram)
        {
            validateProgram = false;
            bool isValid = ValidateProgram();
            Debug.Log($"Program validation: {(isValid ? "VALID" : "INVALID")}");
        }

        if (addVariable)
        {
            addVariable = false;
            AddVariable(varType, varName, varValue);
            Debug.Log($"Variable added: {varType} {varName} = {varValue}");
        }

        if (addArray)
        {
            addArray = false;
            AddArray(arrayType, arrayName, new List<string>(arrayElements));
            Debug.Log($"Array added: {arrayType}[] {arrayName}");
        }

        if (addForLoop)
        {
            addForLoop = false;
            AddForLoop(forVariable, forRange);
            Debug.Log($"For loop added: for {forVariable} in range({forRange})");
        }

        if (addIfStatement)
        {
            addIfStatement = false;
            AddIfStatement(ifCondition);
            Debug.Log($"If statement added: if {ifCondition}");
        }

        if (addFunction)
        {
            addFunction = false;
            AddFunction(functionName, new List<string>(functionParams));
            Debug.Log($"Function added: def {functionName}");
        }

        if (addFunctionCall)
        {
            addFunctionCall = false;
            AddFunctionCall(callFunctionName, new List<string>(callArgs));
            Debug.Log($"Function call added: {callFunctionName}");
        }

        if (addComment)
        {
            addComment = false;
            AddComment(commentText);
            Debug.Log($"Comment added: {commentText}");
        }

        if (addAssignment)
        {
            addAssignment = false;
            AddAssignment(assignmentVar, assignmentValue);
            Debug.Log($"Assignment added: {assignmentVar} = {assignmentValue}");
        }

        if (syncFromText)
        {
            syncFromText = false;
            SyncFromTextContent();
            Debug.Log("Synchronized from text content!");
        }
    }

    // 子オブジェクトの処理
    private void HandleChildObjects()
    {
        for (int i = trackedChildren.Count - 1; i >= 0; i--)
        {
            Transform child = trackedChildren[i];
            if (child == null || child.parent != this.transform)
            {
                if (i < programLines.Count)
                {
                    programLines.RemoveAt(i);
                }
                trackedChildren.RemoveAt(i);
            }
        }

        foreach (Transform child in transform)
        {
            if (!trackedChildren.Contains(child))
            {
                CardDiscrimination card = child.GetComponent<CardDiscrimination>();
                if (card != null)
                {
                    AddFunctionCall(card.cardType, new List<string>());
                    trackedChildren.Add(child);
                }
            }
        }
    }

    // 初期サンプルデータ
    private void InitializeSampleData()
    {
        programLines.Clear();

        AddComment("# Python風プログラムサンプル");
        AddVariable("int", "player_hp", "100");
        AddArray("string", "enemies", new List<string> { "Zombie", "Skeleton", "Slime" });
        AddFunction("attack_enemy", new List<string> { "string enemy", "string weapon" });
        AddForLoop("i", "3");
        AddIfStatement("player_hp > 50");
        AddFunctionCall("attack_enemy", new List<string> { "enemies[i]", "sword" });
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
            newLine.variableType = line.variableType;
            newLine.variableName = line.variableName;
            newLine.variableValue = line.variableValue;
            newLine.arrayValues = new List<string>(line.arrayValues);
            newLine.functionName = line.functionName;
            newLine.parameters = new List<string>(line.parameters);
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

        isUpdatingFromCode = true;

        string displayText = "";
        for (int i = 0; i < programLines.Count; i++)
        {
            ProgramLine line = programLines[i];

            if (showLineNumbers)
            {
                string lineNumber = $"{i + 1:D2}  ";
                if (enableSyntaxHighlight)
                {
                    lineNumber = $"<color=#808080>{lineNumber}</color>";
                }
                displayText += lineNumber;
            }

            for (int j = 0; j < line.indentLevel; j++)
            {
                displayText += indentString;
            }

            if (enableSyntaxHighlight)
            {
                displayText += ApplyPythonSyntaxHighlight(line);
            }
            else
            {
                displayText += line.content;
            }

            if (i < programLines.Count - 1)
            {
                displayText += "\n";
            }
        }

        codeOutput.text = displayText;
        lastKnownText = displayText;

        UpdatePreviousLines();

        isUpdatingFromCode = false;
    }

    // Python用シンタックスハイライトを適用
    private string ApplyPythonSyntaxHighlight(ProgramLine line)
    {
        string content = line.content;
        string keywordColorHex = ColorUtility.ToHtmlStringRGB(keywordColor);
        string variableColorHex = ColorUtility.ToHtmlStringRGB(variableColor);
        string stringColorHex = ColorUtility.ToHtmlStringRGB(stringColor);
        string numberColorHex = ColorUtility.ToHtmlStringRGB(numberColor);
        string braceColorHex = ColorUtility.ToHtmlStringRGB(braceColor);
        string commandColorHex = ColorUtility.ToHtmlStringRGB(commandColor);
        string typeColorHex = ColorUtility.ToHtmlStringRGB(typeColor);
        string commentColorHex = ColorUtility.ToHtmlStringRGB(commentColor);

        switch (line.lineType)
        {
            case ProgramLine.LineType.Comment:
                return $"<color=#{commentColorHex}>{content}</color>";

            case ProgramLine.LineType.FunctionDef:
                return HighlightFunctionDef(content, keywordColorHex, variableColorHex, braceColorHex);

            case ProgramLine.LineType.ForLoop:
                return HighlightForLoop(content, keywordColorHex, variableColorHex, braceColorHex);

            case ProgramLine.LineType.IfStatement:
                return HighlightIfStatement(content, keywordColorHex, variableColorHex, braceColorHex);

            case ProgramLine.LineType.VariableDeclaration:
                return HighlightVariableDeclaration(content, typeColorHex, variableColorHex, stringColorHex, braceColorHex);

            case ProgramLine.LineType.ArrayDeclaration:
                return HighlightArrayDeclaration(content, typeColorHex, variableColorHex, stringColorHex, braceColorHex);

            case ProgramLine.LineType.Assignment:
                return HighlightAssignment(content, variableColorHex, stringColorHex, braceColorHex);

            case ProgramLine.LineType.FunctionCall:
            default:
                return HighlightFunctionCall(content, commandColorHex, variableColorHex, stringColorHex, braceColorHex);
        }
    }

    // 関数定義のハイライト
    private string HighlightFunctionDef(string content, string keywordColorHex, string variableColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(def)\s+(\w+)\s*\((.*?)\)\s*(:)$");
        if (match.Success)
        {
            string defKeyword = match.Groups[1].Value;
            string funcName = match.Groups[2].Value;
            string parameters = match.Groups[3].Value;
            string colon = match.Groups[4].Value;

            string highlightedParams = HighlightParameters(parameters, variableColorHex);

            return $"<color=#{keywordColorHex}>{defKeyword}</color> <color=#{variableColorHex}>{funcName}</color><color=#{braceColorHex}>(</color>{highlightedParams}<color=#{braceColorHex}>)</color><color=#{braceColorHex}>{colon}</color>";
        }

        return content;
    }

    // forループのハイライト
    private string HighlightForLoop(string content, string keywordColorHex, string variableColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(for)\s+(\w+)\s+(in)\s+(range)\s*\(\s*(.+?)\s*\)\s*(:)$");
        if (match.Success)
        {
            string forKeyword = match.Groups[1].Value;
            string iterVar = match.Groups[2].Value;
            string inKeyword = match.Groups[3].Value;
            string rangeKeyword = match.Groups[4].Value;
            string rangeExpr = match.Groups[5].Value;
            string colon = match.Groups[6].Value;

            string highlightedRange = HighlightExpression(rangeExpr, variableColorHex);

            return $"<color=#{keywordColorHex}>{forKeyword}</color> <color=#{variableColorHex}>{iterVar}</color> <color=#{keywordColorHex}>{inKeyword}</color> <color=#{keywordColorHex}>{rangeKeyword}</color><color=#{braceColorHex}>(</color>{highlightedRange}<color=#{braceColorHex}>)</color><color=#{braceColorHex}>{colon}</color>";
        }

        return content;
    }

    // if文のハイライト
    private string HighlightIfStatement(string content, string keywordColorHex, string variableColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(if)\s+(.+?)\s*(:)$");
        if (match.Success)
        {
            string ifKeyword = match.Groups[1].Value;
            string condition = match.Groups[2].Value;
            string colon = match.Groups[3].Value;

            string highlightedCondition = HighlightCondition(condition, variableColorHex, braceColorHex);

            return $"<color=#{keywordColorHex}>{ifKeyword}</color> {highlightedCondition}<color=#{braceColorHex}>{colon}</color>";
        }

        return content;
    }

    // 変数宣言のハイライト
    private string HighlightVariableDeclaration(string content, string typeColorHex, string variableColorHex, string stringColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(\w+)\s+(\w+)\s*=\s*""?([^""]+)""?$");
        if (match.Success)
        {
            string varType = match.Groups[1].Value;
            string varName = match.Groups[2].Value;
            string varValue = match.Groups[3].Value;

            return $"<color=#{typeColorHex}>{varType}</color> <color=#{variableColorHex}>{varName}</color> <color=#{braceColorHex}>=</color> <color=#{stringColorHex}>\"{varValue}\"</color>";
        }

        return content;
    }

    // 配列宣言のハイライト
    private string HighlightArrayDeclaration(string content, string typeColorHex, string variableColorHex, string stringColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(\w+)\[\]\s+(\w+)\s*=\s*\[(.*?)\]$");
        if (match.Success)
        {
            string arrayType = match.Groups[1].Value;
            string arrayName = match.Groups[2].Value;
            string elements = match.Groups[3].Value;

            string highlightedElements = HighlightArrayElements(elements, stringColorHex, braceColorHex);

            return $"<color=#{typeColorHex}>{arrayType}</color><color=#{braceColorHex}>[]</color> <color=#{variableColorHex}>{arrayName}</color> <color=#{braceColorHex}>=</color> <color=#{braceColorHex}>[</color>{highlightedElements}<color=#{braceColorHex}>]</color>";
        }

        return content;
    }

    // 代入文のハイライト
    private string HighlightAssignment(string content, string variableColorHex, string stringColorHex, string braceColorHex)
    {
        var parts = content.Split('=');
        if (parts.Length == 2)
        {
            string varName = parts[0].Trim();
            string value = parts[1].Trim();

            string highlightedValue = HighlightValue(value, stringColorHex);

            return $"<color=#{variableColorHex}>{varName}</color> <color=#{braceColorHex}>=</color> {highlightedValue}";
        }

        return content;
    }

    // 関数呼び出しのハイライト
    private string HighlightFunctionCall(string content, string commandColorHex, string variableColorHex, string stringColorHex, string braceColorHex)
    {
        var match = Regex.Match(content, @"^(\w+)\s*\((.*?)\)$");
        if (match.Success)
        {
            string funcName = match.Groups[1].Value;
            string args = match.Groups[2].Value;

            string highlightedArgs = HighlightArguments(args, variableColorHex, stringColorHex, braceColorHex);

            return $"<color=#{commandColorHex}>{funcName}</color><color=#{braceColorHex}>(</color>{highlightedArgs}<color=#{braceColorHex}>)</color>";
        }

        return $"<color=#{commandColorHex}>{content}</color>";
    }

    // パラメータのハイライト
    private string HighlightParameters(string parameters, string variableColorHex)
    {
        if (string.IsNullOrEmpty(parameters)) return parameters;

        var paramArray = parameters.Split(',');
        var highlighted = new List<string>();

        foreach (var param in paramArray)
        {
            string trimmedParam = param.Trim();
            var parts = trimmedParam.Split(' ');
            if (parts.Length == 2)
            {
                highlighted.Add($"<color=#{ColorUtility.ToHtmlStringRGB(typeColor)}>{parts[0]}</color> <color=#{variableColorHex}>{parts[1]}</color>");
            }
            else
            {
                highlighted.Add($"<color=#{variableColorHex}>{trimmedParam}</color>");
            }
        }

        return string.Join($"<color=#{ColorUtility.ToHtmlStringRGB(braceColor)}>, </color>", highlighted);
    }

    // 式のハイライト
    private string HighlightExpression(string expression, string variableColorHex)
    {
        expression = expression.Trim();

        if (Regex.IsMatch(expression, @"^\d+$"))
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(numberColor)}>{expression}</color>";
        }

        return $"<color=#{variableColorHex}>{expression}</color>";
    }

    // 条件のハイライト
    private string HighlightCondition(string condition, string variableColorHex, string braceColorHex)
    {
        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };

        foreach (string op in operators)
        {
            if (condition.Contains(op))
            {
                string[] parts = condition.Split(new[] { op }, System.StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string left = HighlightValue(parts[0].Trim(), variableColorHex);
                    string right = HighlightValue(parts[1].Trim(), variableColorHex);
                    return $"{left} <color=#{braceColorHex}>{op}</color> {right}";
                }
            }
        }

        return $"<color=#{variableColorHex}>{condition}</color>";
    }

    // 配列要素のハイライト
    private string HighlightArrayElements(string elements, string stringColorHex, string braceColorHex)
    {
        if (string.IsNullOrEmpty(elements)) return elements;

        var elementArray = elements.Split(',');
        var highlighted = new List<string>();

        foreach (var element in elementArray)
        {
            string cleanElement = element.Trim().Trim('"');
            highlighted.Add($"<color=#{stringColorHex}>\"{cleanElement}\"</color>");
        }

        return string.Join($"<color=#{braceColorHex}>, </color>", highlighted);
    }

    // 引数のハイライト
    private string HighlightArguments(string args, string variableColorHex, string stringColorHex, string braceColorHex)
    {
        if (string.IsNullOrEmpty(args)) return args;

        var argArray = args.Split(',');
        var highlighted = new List<string>();

        foreach (var arg in argArray)
        {
            string trimmedArg = arg.Trim();
            highlighted.Add(HighlightValue(trimmedArg, variableColorHex));
        }

        return string.Join($"<color=#{braceColorHex}>, </color>", highlighted);
    }

    // 値のハイライト
    private string HighlightValue(string value, string defaultColorHex)
    {
        value = value.Trim();

        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(stringColor)}>{value}</color>";
        }

        if (Regex.IsMatch(value, @"^\d+$"))
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(numberColor)}>{value}</color>";
        }

        return $"<color=#{defaultColorHex}>{value}</color>";
    }

    // === 外部から使用するメソッド ===

    public void AddVariable(string type, string name, string value)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(type, name, value, currentIndent));
        UpdateUI();
    }

    public void AddArray(string type, string name, List<string> values)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(type, name, values, currentIndent));
        UpdateUI();
    }

    public void AddFunction(string name, List<string> parameters)
    {
        int currentIndent = GetCurrentIndentLevel();
        string paramString = string.Join(", ", parameters);
        string content = $"def {name}({paramString}):";

        var line = new ProgramLine(ProgramLine.LineType.FunctionDef, content, currentIndent);
        line.functionName = name;
        line.parameters = new List<string>(parameters);

        programLines.Add(line);
        UpdateUI();
    }

    public void AddForLoop(string variable, string range)
    {
        int currentIndent = GetCurrentIndentLevel();
        string content = $"for {variable} in range({range}):";

        var line = new ProgramLine(ProgramLine.LineType.ForLoop, content, currentIndent, range);
        line.variableName = variable;

        programLines.Add(line);
        UpdateUI();
    }

    public void AddIfStatement(string condition)
    {
        int currentIndent = GetCurrentIndentLevel();
        string content = $"if {condition}:";

        programLines.Add(new ProgramLine(ProgramLine.LineType.IfStatement, content, currentIndent, condition));
        UpdateUI();
    }

    public void AddFunctionCall(string name, List<string> arguments)
    {
        int currentIndent = GetCurrentIndentLevel();
        string argString = string.Join(", ", arguments);
        string content = $"{name}({argString})";

        var line = new ProgramLine(ProgramLine.LineType.FunctionCall, content, currentIndent);
        line.functionName = name;
        line.parameters = new List<string>(arguments);

        programLines.Add(line);
        UpdateUI();
    }

    public void AddComment(string comment)
    {
        int currentIndent = GetCurrentIndentLevel();
        programLines.Add(new ProgramLine(ProgramLine.LineType.Comment, comment, currentIndent));
        UpdateUI();
    }

    public void AddAssignment(string variable, string value)
    {
        int currentIndent = GetCurrentIndentLevel();
        string content = $"{variable} = \"{value}\"";

        var line = new ProgramLine(ProgramLine.LineType.Assignment, content, currentIndent);
        line.variableName = variable;
        line.variableValue = value;

        programLines.Add(line);
        UpdateUI();
    }

    public void ClearProgram()
    {
        programLines.Clear();
        UpdateUI();
    }

    public bool ValidateProgram()
    {
        // Python風プログラムの簡単な検証
        return true; // 簡単な実装
    }

    private int GetCurrentIndentLevel()
    {
        if (programLines.Count == 0) return 0;

        ProgramLine lastLine = programLines[programLines.Count - 1];

        // Python風では、コロンで終わる行の後はインデント+1
        if (lastLine.content.EndsWith(":"))
        {
            return lastLine.indentLevel + 1;
        }

        return lastLine.indentLevel;
    }

    // サンプル作成メソッド
    public void CreateSampleFunction()
    {
        ClearProgram();
        AddComment("# サンプル関数");
        AddFunction("calculate_damage", new List<string> { "int base_damage", "string weapon_type" });
        AddIfStatement("weapon_type == \"sword\"");
        AddAssignment("damage", "base_damage * 2");
        AddFunctionCall("print", new List<string> { "\"Using sword, damage doubled!\"" });
    }

    public void CreateForLoopSample()
    {
        ClearProgram();
        AddComment("# forループサンプル");
        AddArray("string", "enemies", new List<string> { "Goblin", "Orc", "Dragon" });
        AddForLoop("i", "len(enemies)");
        AddFunctionCall("attack", new List<string> { "enemies[i]", "\"sword\"" });
    }
}