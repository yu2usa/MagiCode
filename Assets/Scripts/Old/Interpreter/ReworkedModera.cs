using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Diagnostics;

public class ModeraInterpreter : MonoBehaviour
{
    // === ログレベル設定 ===
    public enum LogLevel
    {
        None = 0,
        Errors = 1,
        Basic = 2,
        Verbose = 3,
        Debug = 4
    }

    [Header("ログ設定")]
    [SerializeField] private LogLevel logLevel = LogLevel.Basic;
    [SerializeField] private bool showExecutionTime = true;
    [SerializeField] private bool showVariableChanges = true;
    [SerializeField] private bool showFunctionCalls = true;
    [SerializeField] private bool showLoopIterations = false;
    [SerializeField] private bool colorCodeLogs = true;

    [Header("パフォーマンス監視")]
    [SerializeField] private bool trackPerformance = true;
    [SerializeField] private int maxLogEntries = 1000;

    // === ログ用カラーコード ===
    private string COLOR_INFO { get { return colorCodeLogs ? "<color=cyan>" : ""; } }
    private string COLOR_SUCCESS { get { return colorCodeLogs ? "<color=green>" : ""; } }
    private string COLOR_WARNING { get { return colorCodeLogs ? "<color=yellow>" : ""; } }
    private string COLOR_ERROR { get { return colorCodeLogs ? "<color=red>" : ""; } }
    private string COLOR_DEBUG { get { return colorCodeLogs ? "<color=gray>" : ""; } }
    private string COLOR_END { get { return colorCodeLogs ? "</color>" : ""; } }

    // === カスタム例外クラス ===
    public class ModeraException : Exception
    {
        public int LineNumber { get; }
        public ModeraException(string message, int lineNumber = -1) : base(message)
        {
            LineNumber = lineNumber;
        }
    }

    public class ModeraReturnException : Exception
    {
        public object ReturnValue { get; }
        public ModeraReturnException(object value)
        {
            ReturnValue = value;
        }
    }

    public class ModeraBreakException : Exception { }
    public class ModeraContinueException : Exception { }

    // === 型システム ===
    public enum ModeraType
    {
        Int, Float, Bool, String, IntArray, FloatArray, BoolArray, StringArray, Void
    }

    public class ModeraValue
    {
        public ModeraType Type { get; }
        public object Value { get; }

        public ModeraValue(ModeraType type, object value)
        {
            Type = type;
            Value = value;
        }

        public T GetValue<T>()
        {
            if (Value is T result)
                return result;
            throw new ModeraException($"Type mismatch: expected {typeof(T)}, got {Value?.GetType()}");
        }

        public override string ToString()
        {
            if (Value is Array array)
            {
                var elements = new List<string>();
                foreach (var item in array)
                {
                    elements.Add(item?.ToString() ?? "null");
                }
                return $"[{string.Join(", ", elements)}]";
            }
            return Value?.ToString() ?? "null";
        }
    }

    public class ModeraFunction
    {
        public string Name { get; }
        public List<(ModeraType type, string name)> Parameters { get; }
        public ModeraType ReturnType { get; }
        public List<string> Body { get; }
        public int BaseIndent { get; }

        public ModeraFunction(string name, List<(ModeraType, string)> parameters,
                             ModeraType returnType, List<string> body, int baseIndent)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            Body = body;
            BaseIndent = baseIndent;
        }
    }

    // === 新しい算術式処理システム ===
    public enum TokenType
    {
        Number,      // 123, 3.14
        Variable,    // i, n, arr
        Operator,    // +, -, *, /, %
        LeftParen,   // (
        RightParen,  // )
        ArrayAccess, // arr[index]
        ArrayLength, // arr.length
        EOF
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public object NumericValue { get; }

        public Token(TokenType type, string value, object numericValue = null)
        {
            Type = type;
            Value = value;
            NumericValue = numericValue;
        }

        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }

    private class ArithmeticLexer
    {
        private string expression;
        private int position;
        private ModeraInterpreter interpreter;

        public ArithmeticLexer(string expr, ModeraInterpreter interp)
        {
            expression = expr.Trim();
            position = 0;
            interpreter = interp;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (position < expression.Length)
            {
                SkipWhitespace();
                if (position >= expression.Length) break;

                char current = expression[position];

                // 数値リテラル
                if (char.IsDigit(current) || current == '.')
                {
                    tokens.Add(ReadNumber());
                }
                // 変数名または配列アクセス
                else if (char.IsLetter(current) || current == '_')
                {
                    tokens.Add(ReadIdentifier());
                }
                // 演算子
                else if ("+-*/%".Contains(current))
                {
                    tokens.Add(new Token(TokenType.Operator, current.ToString()));
                    position++;
                }
                // 括弧
                else if (current == '(')
                {
                    tokens.Add(new Token(TokenType.LeftParen, "("));
                    position++;
                }
                else if (current == ')')
                {
                    tokens.Add(new Token(TokenType.RightParen, ")"));
                    position++;
                }
                else
                {
                    throw new ModeraException($"Unexpected character in arithmetic expression: {current}", interpreter.currentLineNumber);
                }
            }

            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }

        private Token ReadNumber()
        {
            int start = position;
            bool hasDecimal = false;

            while (position < expression.Length && (char.IsDigit(expression[position]) || expression[position] == '.'))
            {
                if (expression[position] == '.')
                {
                    if (hasDecimal) break;
                    hasDecimal = true;
                }
                position++;
            }

            string numberStr = expression.Substring(start, position - start);

            if (hasDecimal)
            {
                if (float.TryParse(numberStr, out float floatVal))
                    return new Token(TokenType.Number, numberStr, floatVal);
            }
            else
            {
                if (int.TryParse(numberStr, out int intVal))
                    return new Token(TokenType.Number, numberStr, intVal);
            }

            throw new ModeraException($"Invalid number format: {numberStr}", interpreter.currentLineNumber);
        }

        private Token ReadIdentifier()
        {
            int start = position;

            while (position < expression.Length && (char.IsLetterOrDigit(expression[position]) || expression[position] == '_'))
            {
                position++;
            }

            string identifier = expression.Substring(start, position - start);

            // 配列長さアクセスをチェック
            if (position < expression.Length - 6 && expression.Substring(position, 7) == ".length")
            {
                position += 7;
                return new Token(TokenType.ArrayLength, identifier);
            }

            // 配列アクセスをチェック
            if (position < expression.Length && expression[position] == '[')
            {
                position++; // '['をスキップ
                int bracketStart = position;
                int bracketCount = 1;

                while (position < expression.Length && bracketCount > 0)
                {
                    if (expression[position] == '[') bracketCount++;
                    else if (expression[position] == ']') bracketCount--;
                    position++;
                }

                if (bracketCount != 0)
                    throw new ModeraException("Unmatched array access brackets", interpreter.currentLineNumber);

                string indexExpr = expression.Substring(bracketStart, position - bracketStart - 1);
                return new Token(TokenType.ArrayAccess, $"{identifier}[{indexExpr}]");
            }

            return new Token(TokenType.Variable, identifier);
        }

        private void SkipWhitespace()
        {
            while (position < expression.Length && char.IsWhiteSpace(expression[position]))
            {
                position++;
            }
        }
    }

    private class ArithmeticParser
    {
        private List<Token> tokens;
        private int position;
        private ModeraInterpreter interpreter;

        public ArithmeticParser(List<Token> tokenList, ModeraInterpreter interp)
        {
            tokens = tokenList;
            position = 0;
            interpreter = interp;
        }

        public object Parse()
        {
            return ParseExpression();
        }

        // 式 → 項 (('+' | '-') 項)*
        private object ParseExpression()
        {
            object left = ParseTerm();

            while (CurrentToken().Type == TokenType.Operator &&
                   (CurrentToken().Value == "+" || CurrentToken().Value == "-"))
            {
                string op = CurrentToken().Value;
                Advance();
                object right = ParseTerm();
                left = interpreter.PerformOperation(left, op, right);
            }

            return left;
        }

        // 項 → 因子 (('*' | '/' | '%') 因子)*
        private object ParseTerm()
        {
            object left = ParseFactor();

            while (CurrentToken().Type == TokenType.Operator &&
                   (CurrentToken().Value == "*" || CurrentToken().Value == "/" || CurrentToken().Value == "%"))
            {
                string op = CurrentToken().Value;
                Advance();
                object right = ParseFactor();
                left = interpreter.PerformOperation(left, op, right);
            }

            return left;
        }

        // 因子 → 数値 | 変数 | 配列アクセス | 配列長さ | '(' 式 ')'
        private object ParseFactor()
        {
            Token token = CurrentToken();

            switch (token.Type)
            {
                case TokenType.Number:
                    Advance();
                    return token.NumericValue;

                case TokenType.Variable:
                    Advance();
                    return ResolveVariable(token.Value);

                case TokenType.ArrayAccess:
                    Advance();
                    return ResolveArrayAccess(token.Value);

                case TokenType.ArrayLength:
                    Advance();
                    return ResolveArrayLength(token.Value);

                case TokenType.LeftParen:
                    Advance(); // '('をスキップ
                    object result = ParseExpression();
                    if (CurrentToken().Type != TokenType.RightParen)
                        throw new ModeraException("Missing closing parenthesis", interpreter.currentLineNumber);
                    Advance(); // ')'をスキップ
                    return result;

                default:
                    throw new ModeraException($"Unexpected token in expression: {token}", interpreter.currentLineNumber);
            }
        }

        private Token CurrentToken()
        {
            return position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];
        }

        private void Advance()
        {
            if (position < tokens.Count - 1) position++;
        }

        private object ResolveVariable(string varName)
        {
            var variable = interpreter.GetVariable(varName);
            if (variable == null)
                throw new ModeraException($"Variable '{varName}' not found", interpreter.currentLineNumber);
            return variable.Value;
        }

        private object ResolveArrayAccess(string arrayAccess)
        {
            // "arr[index]" の形式から配列名とインデックスを抽出
            int bracketPos = arrayAccess.IndexOf('[');
            string arrayName = arrayAccess.Substring(0, bracketPos);
            string indexExpr = arrayAccess.Substring(bracketPos + 1, arrayAccess.Length - bracketPos - 2);

            var arrayVar = interpreter.GetVariable(arrayName);
            if (arrayVar == null)
                throw new ModeraException($"Array '{arrayName}' not found", interpreter.currentLineNumber);

            // インデックス式を再帰的に評価
            object indexValue = interpreter.EvaluateArithmeticExpression(indexExpr);
            int index = (int)indexValue;

            return interpreter.GetArrayElementDirect(arrayVar, index);
        }

        private object ResolveArrayLength(string arrayName)
        {
            var arrayVar = interpreter.GetVariable(arrayName);
            if (arrayVar == null)
                throw new ModeraException($"Array '{arrayName}' not found", interpreter.currentLineNumber);

            return interpreter.GetArrayLengthDirect(arrayVar);
        }
    }

    // === フィールド ===
    private Stack<Dictionary<string, ModeraValue>> variableStack = new Stack<Dictionary<string, ModeraValue>>();
    private Dictionary<string, ModeraFunction> functions = new Dictionary<string, ModeraFunction>();
    private StringBuilder debugLog = new StringBuilder();
    public int currentLineNumber = 0;
    private List<string> sourceLines = new List<string>();
    private Stopwatch executionTimer = new Stopwatch();
    private Queue<string> logBuffer = new Queue<string>();

    // === パフォーマンス追跡 ===
    private int totalLinesExecuted = 0;
    private int totalFunctionCalls = 0;
    private int totalLoopIterations = 0;
    private Dictionary<string, int> functionCallCounts = new Dictionary<string, int>();

    // === 事前コンパイル済み正規表現 ===
    private static readonly Regex VariableDeclarationRegex = new Regex(@"^(int|float|bool|string|int\[\]|float\[\]|bool\[\]|string\[\])\s+(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex ArrayAssignmentRegex = new Regex(@"^(\w+)\s*\[\s*(.+?)\s*\]\s*=\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex LengthAccessRegex = new Regex(@"(\w+)\.length", RegexOptions.Compiled);
    private static readonly Regex FunctionDeclarationRegex = new Regex(@"^def\s+(\w+)\s*\((.*?)\)\s*(?:->\s*(\w+|\w+\[\]))?\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex ForLoopRegex = new Regex(@"^for\s+(\w+)\s+in\s+range\s*\(\s*(.+?)\s*\)\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex WhileLoopRegex = new Regex(@"^while\s+(.+)\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex SwitchStatementRegex = new Regex(@"^switch\s+(.+)\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex CaseStatementRegex = new Regex(@"^case\s+(.+)\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex DefaultStatementRegex = new Regex(@"^default\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex IfStatementRegex = new Regex(@"^if\s+(.+)\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex ElseStatementRegex = new Regex(@"^else\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex PrintStatementRegex = new Regex(@"^print\s*\(\s*(.*?)\s*\)$", RegexOptions.Compiled);
    private static readonly Regex ReturnStatementRegex = new Regex(@"^return\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex BreakStatementRegex = new Regex(@"^break\s*$", RegexOptions.Compiled);
    private static readonly Regex ContinueStatementRegex = new Regex(@"^continue\s*$", RegexOptions.Compiled);
    private static readonly Regex FunctionCallRegex = new Regex(@"^(\w+)\s*\(\s*(.*?)\s*\)$", RegexOptions.Compiled);
    private static readonly Regex ArrayAccessRegex = new Regex(@"(\w+)\s*\[\s*(.+?)\s*\]", RegexOptions.Compiled);
    private static readonly Regex ComparisonRegex = new Regex(@"(.+?)\s*(>=|<=|==|!=|>|<)\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex LogicalRegex = new Regex(@"(.+?)\s+(&&|\|\|)\s+(.+)", RegexOptions.Compiled);
    private static readonly Regex SingleLineCommentRegex = new Regex(@"//.*$", RegexOptions.Compiled);
    private static readonly Regex MultiLineCommentRegex = new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);

    void Start()
    {
        LogInfo("Modera Interpreter initialized with log level: " + logLevel);
    }

    // === ログ出力メソッド群 ===
    private void LogInfo(string message)
    {
        if (logLevel >= LogLevel.Basic)
            AddToLogBuffer(COLOR_INFO + "[INFO]" + COLOR_END + " " + message);
    }

    private void LogSuccess(string message)
    {
        if (logLevel >= LogLevel.Basic)
            AddToLogBuffer(COLOR_SUCCESS + "[SUCCESS]" + COLOR_END + " " + message);
    }

    private void LogWarning(string message)
    {
        if (logLevel >= LogLevel.Basic)
            AddToLogBuffer(COLOR_WARNING + "[WARNING]" + COLOR_END + " " + message);
    }

    private void LogError(string message)
    {
        if (logLevel >= LogLevel.Errors)
            AddToLogBuffer(COLOR_ERROR + "[ERROR]" + COLOR_END + " " + message);
    }

    private void LogDebug(string message)
    {
        if (logLevel >= LogLevel.Debug)
            AddToLogBuffer(COLOR_DEBUG + "[DEBUG]" + COLOR_END + " " + message);
    }

    private void LogVerbose(string message)
    {
        if (logLevel >= LogLevel.Verbose)
            AddToLogBuffer(COLOR_INFO + "[VERBOSE]" + COLOR_END + " " + message);
    }

    private void LogExecution(string message)
    {
        if (logLevel >= LogLevel.Verbose)
            AddToLogBuffer(COLOR_INFO + "[EXEC]" + COLOR_END + " Line " + currentLineNumber + ": " + message);
    }

    private void LogVariableChange(string varName, object oldValue, object newValue)
    {
        if (showVariableChanges && logLevel >= LogLevel.Verbose)
            AddToLogBuffer(COLOR_DEBUG + "[VAR]" + COLOR_END + " " + varName + ": " + oldValue + " → " + newValue);
    }

    private void LogFunctionCall(string funcName, List<object> args, object returnValue = null)
    {
        if (showFunctionCalls && logLevel >= LogLevel.Verbose)
        {
            string argsStr = args != null ? string.Join(", ", args) : "";
            string returnStr = returnValue != null ? " → " + returnValue : "";
            AddToLogBuffer(COLOR_SUCCESS + "[FUNC]" + COLOR_END + " " + funcName + "(" + argsStr + ")" + returnStr);
        }
    }

    private void LogLoopIteration(string loopType, int iteration, string condition = "")
    {
        if (showLoopIterations && logLevel >= LogLevel.Debug)
        {
            string condStr = !string.IsNullOrEmpty(condition) ? " | " + condition : "";
            AddToLogBuffer(COLOR_DEBUG + "[LOOP]" + COLOR_END + " " + loopType + " iteration " + iteration + condStr);
        }
    }

    private void AddToLogBuffer(string message)
    {
        logBuffer.Enqueue(message);
        while (logBuffer.Count > maxLogEntries)
            logBuffer.Dequeue();
        UnityEngine.Debug.Log(message);
    }

    private void LogPerformanceStats()
    {
        if (trackPerformance && logLevel >= LogLevel.Basic)
        {
            LogInfo("=== Performance Statistics ===");
            LogInfo("Total execution time: " + executionTimer.ElapsedMilliseconds + "ms");
            LogInfo("Lines executed: " + totalLinesExecuted);
            LogInfo("Function calls: " + totalFunctionCalls);
            LogInfo("Loop iterations: " + totalLoopIterations);

            if (functionCallCounts.Count > 0)
            {
                LogInfo("Function call breakdown:");
                foreach (var kvp in functionCallCounts.OrderByDescending(x => x.Value))
                    LogInfo("  " + kvp.Key + ": " + kvp.Value + " calls");
            }
        }
    }

    // === メインエントリーポイント ===
    public void Run(string code)
    {
        executionTimer.Restart();
        LogInfo("Starting Modera execution...");

        try
        {
            InitializeRuntime();
            code = RemoveComments(code);
            sourceLines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            LogInfo("Source code parsed: " + sourceLines.Count + " lines");

            var processedLines = new List<(string line, int originalLineNumber)>();
            for (int i = 0; i < sourceLines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(sourceLines[i]))
                    processedLines.Add((sourceLines[i], i + 1));
            }

            LogInfo("Starting execution of " + processedLines.Count + " executable lines");
            RunBlock(processedLines, 0, 0);

            executionTimer.Stop();
            LogSuccess("Modera execution completed successfully");

            if (showExecutionTime)
                LogInfo("Total execution time: " + executionTimer.ElapsedMilliseconds + "ms");

            LogPerformanceStats();
        }
        catch (ModeraException ex)
        {
            executionTimer.Stop();
            string errorMsg = ex.LineNumber > 0
                ? "Modera Error (Line " + ex.LineNumber + "): " + ex.Message
                : "Modera Error: " + ex.Message;
            LogError(errorMsg);

            if (logLevel >= LogLevel.Verbose)
                LogDebug("Stack trace: " + ex.StackTrace);
        }
        catch (Exception ex)
        {
            executionTimer.Stop();
            LogError("Unexpected Error: " + ex.Message);
            LogDebug("Stack trace: " + ex.StackTrace);
        }
        finally
        {
            CleanupRuntime();
        }
    }

    private string RemoveComments(string code)
    {
        LogDebug("Removing comments from source code");
        code = MultiLineCommentRegex.Replace(code, "");
        var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
            lines[i] = SingleLineCommentRegex.Replace(lines[i], "");
        return string.Join("\n", lines);
    }

    private void InitializeRuntime()
    {
        LogDebug("Initializing runtime environment");
        variableStack.Clear();
        variableStack.Push(new Dictionary<string, ModeraValue>());
        functions.Clear();
        debugLog.Clear();
        currentLineNumber = 0;
        totalLinesExecuted = 0;
        totalFunctionCalls = 0;
        totalLoopIterations = 0;
        functionCallCounts.Clear();

        // 修正: グローバルスコープが確実に存在することを保証
        var globalScope = new Dictionary<string, ModeraValue>();
        variableStack.Push(globalScope);

        functions.Clear();
        debugLog.Clear();
        currentLineNumber = 0;
        totalLinesExecuted = 0;
        totalFunctionCalls = 0;
        totalLoopIterations = 0;
        functionCallCounts.Clear();

        LogDebug($"Runtime initialized with {variableStack.Count} scopes");
    }

    private void CleanupRuntime()
    {
        LogDebug("Cleaning up runtime environment");
        variableStack.Clear();
        functions.Clear();
        debugLog.Clear();
        sourceLines.Clear();
        System.GC.Collect();
    }

    // === スコープ管理 ===
    private void EnterNewScope()
    {
        var newScope = new Dictionary<string, ModeraValue>();
        variableStack.Push(newScope);
        LogDebug($"Entered new scope (depth: {variableStack.Count})");
    }

    private void ExitScope()
    {
        if (variableStack.Count > 1)
        {
            var poppedScope = variableStack.Pop();
            LogDebug($"Exited scope (depth: {variableStack.Count}) - Removed {poppedScope.Count} variables");
        }
        else
        {
            LogWarning("Attempted to exit global scope - operation ignored");
        }
    }

    // === メイン実行ループ ===
    void RunBlock(List<(string line, int lineNumber)> lines, int startIndex, int baseIndent)
    {
        for (int lineIndex = startIndex; lineIndex < lines.Count; lineIndex++)
        {
            var lineData = lines[lineIndex];
            string rawLine = lineData.line;
            int lineNumber = lineData.lineNumber;

            currentLineNumber = lineNumber;
            totalLinesExecuted++;

            if (string.IsNullOrWhiteSpace(rawLine)) continue;

            int indent = GetIndentLevel(rawLine);
            if (indent < baseIndent) return;

            string line = rawLine.Trim();
            LogExecution(line);

            try
            {
                if (VariableDeclarationRegex.IsMatch(line))
                {
                    ParseVariable(line);
                }
                else if (ArrayAssignmentRegex.IsMatch(line))
                {
                    ParseArrayAssignment(line);
                }
                else if (FunctionDeclarationRegex.IsMatch(line))
                {
                    lineIndex = ParseFunction(lines, lineIndex);
                }
                else if (ForLoopRegex.IsMatch(line))
                {
                    lineIndex = ParseFor(lines, lineIndex, indent);
                }
                else if (WhileLoopRegex.IsMatch(line))
                {
                    lineIndex = ParseWhile(lines, lineIndex, indent);
                }
                else if (SwitchStatementRegex.IsMatch(line))
                {
                    lineIndex = ParseSwitch(lines, lineIndex, indent);
                }
                else if (IfStatementRegex.IsMatch(line))
                {
                    lineIndex = ParseIf(lines, lineIndex, indent);
                }
                else if (ElseStatementRegex.IsMatch(line))
                {
                    throw new ModeraException("else statement without matching if", currentLineNumber);
                }
                else if (PrintStatementRegex.IsMatch(line))
                {
                    ParsePrint(line);
                }
                else if (ReturnStatementRegex.IsMatch(line))
                {
                    ParseReturn(line);
                }
                else if (BreakStatementRegex.IsMatch(line))
                {
                    LogVerbose("Break statement encountered");
                    throw new ModeraBreakException();
                }
                else if (ContinueStatementRegex.IsMatch(line))
                {
                    LogVerbose("Continue statement encountered");
                    throw new ModeraContinueException();
                }
                else if (FunctionCallRegex.IsMatch(line))
                {
                    ParseExpression(line);
                }
                else if (line.Contains("=") && !line.Contains("=="))
                {
                    ParseAssignment(line);
                }
                else
                {
                    LogWarning("Unknown statement: " + line);
                    throw new ModeraException("Unknown statement: " + line, currentLineNumber);
                }
            }
            catch (ModeraReturnException)
            {
                LogVerbose("Function return encountered");
                throw;
            }
            catch (ModeraBreakException)
            {
                LogVerbose("Loop break encountered");
                throw;
            }
            catch (ModeraContinueException)
            {
                LogVerbose("Loop continue encountered");
                throw;
            }
            catch (ModeraException ex)
            {
                LogError("Execution error at line " + currentLineNumber + ": " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                LogError("Unexpected error executing line " + currentLineNumber + ": " + ex.Message);
                throw new ModeraException("Error executing line: " + ex.Message, currentLineNumber);
            }
        }
    }

    // === 新しい算術式評価エンジン ===
    public object EvaluateArithmeticExpression(string expression)
    {
        try
        {
            LogDebug($"Evaluating arithmetic expression: {expression}");

            // 空の式をチェック
            if (string.IsNullOrWhiteSpace(expression))
                throw new ModeraException("Empty arithmetic expression", currentLineNumber);

            // レクサーでトークン化
            var lexer = new ArithmeticLexer(expression, this);
            var tokens = lexer.Tokenize();

            LogDebug($"Tokens: {string.Join(", ", tokens)}");

            // パーサーで評価
            var parser = new ArithmeticParser(tokens, this);
            object result = parser.Parse();

            LogDebug($"Expression result: {result}");
            return result;
        }
        catch (ModeraException)
        {
            throw; // Moderaエラーはそのまま再スロー
        }
        catch (Exception ex)
        {
            throw new ModeraException($"Error in arithmetic expression '{expression}': {ex.Message}", currentLineNumber);
        }
    }

    // === ヘルパーメソッド ===
    public int GetArrayLengthDirect(ModeraValue arrayVar)
    {
        switch (arrayVar.Type)
        {
            case ModeraType.IntArray: return arrayVar.GetValue<int[]>().Length;
            case ModeraType.FloatArray: return arrayVar.GetValue<float[]>().Length;
            case ModeraType.BoolArray: return arrayVar.GetValue<bool[]>().Length;
            case ModeraType.StringArray: return arrayVar.GetValue<string[]>().Length;
            default: throw new ModeraException($"'{arrayVar}' is not an array", currentLineNumber);
        }
    }

    public object GetArrayElementDirect(ModeraValue arrayVar, int index)
    {
        switch (arrayVar.Type)
        {
            case ModeraType.IntArray:
                {
                    int[] arr = arrayVar.GetValue<int[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException($"Array index {index} out of bounds (length: {arr.Length})", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.FloatArray:
                {
                    float[] arr = arrayVar.GetValue<float[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException($"Array index {index} out of bounds (length: {arr.Length})", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.BoolArray:
                {
                    bool[] arr = arrayVar.GetValue<bool[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException($"Array index {index} out of bounds (length: {arr.Length})", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.StringArray:
                {
                    string[] arr = arrayVar.GetValue<string[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException($"Array index {index} out of bounds (length: {arr.Length})", currentLineNumber);
                    return arr[index];
                }
            default:
                throw new ModeraException($"Cannot access element of non-array type", currentLineNumber);
        }
    }

    // === 新しいGetValueSafe（完全に分離された設計） ===
    private object GetValueSafe(string token)
    {
        token = token.Trim();

        if (string.IsNullOrEmpty(token))
            throw new ModeraException("Empty token", currentLineNumber);

        // 算術式かどうかを判定
        if (ContainsArithmetic(token))
        {
            return EvaluateArithmeticExpression(token);
        }

        // 単純な値の直接解決
        return ResolveSimpleValue(token);
    }

    private bool ContainsArithmetic(string token)
    {
        return token.Contains("+") || token.Contains("-") || token.Contains("*") ||
               token.Contains("/") || token.Contains("%") || token.Contains("(") || token.Contains(")");
    }

    private object ResolveSimpleValue(string token)
    {
        // bool リテラル
        if (token == "true") return true;
        if (token == "false") return false;

        // 数値リテラル
        if (token.Contains("."))
        {
            if (float.TryParse(token, out float floatVal))
                return floatVal;
        }
        else
        {
            if (int.TryParse(token, out int intVal))
                return intVal;
        }

        // 文字列リテラル
        if (token.StartsWith("\"") && token.EndsWith("\""))
            return token.Trim('"');

        // 配列長さアクセス
        if (LengthAccessRegex.IsMatch(token))
            return GetArrayLength(token);

        // 配列アクセス
        if (ArrayAccessRegex.IsMatch(token))
            return GetArrayElement(token);

        // 変数参照
        var variable = GetVariable(token);
        if (variable != null)
            return variable.Value;

        // より詳細なエラー情報を提供
        LogError($"Cannot resolve value: '{token}' - Variable not found in any scope");
        if (variableStack.Count > 0 && variableStack.First() != null)
        {
            LogDebug($"Available variables in global scope: {string.Join(", ", variableStack.First().Keys)}");
        }

        throw new ModeraException($"Cannot resolve value: {token}", currentLineNumber);
    }

    public object PerformOperation(object left, string op, object right)
    {
        if (left == null || right == null)
        {
            throw new ModeraException($"Cannot perform operation on null values: {left} {op} {right}", currentLineNumber);
        }

        if (left is int leftInt && right is int rightInt)
        {
            switch (op)
            {
                case "+": return leftInt + rightInt;
                case "-": return leftInt - rightInt;
                case "*": return leftInt * rightInt;
                case "/":
                    if (rightInt == 0) throw new ModeraException("Division by zero", currentLineNumber);
                    return leftInt / rightInt;
                case "%":
                    if (rightInt == 0) throw new ModeraException("Modulo by zero", currentLineNumber);
                    return leftInt % rightInt;
                default: throw new ModeraException("Unknown operator: " + op, currentLineNumber);
            }
        }

        if (left is float leftFloat && right is float rightFloat)
        {
            switch (op)
            {
                case "+": return leftFloat + rightFloat;
                case "-": return leftFloat - rightFloat;
                case "*": return leftFloat * rightFloat;
                case "/":
                    if (Math.Abs(rightFloat) <= float.Epsilon) throw new ModeraException("Division by zero", currentLineNumber);
                    return leftFloat / rightFloat;
                case "%":
                    if (Math.Abs(rightFloat) <= float.Epsilon) throw new ModeraException("Modulo by zero", currentLineNumber);
                    return leftFloat % rightFloat;
                default: throw new ModeraException("Unknown operator: " + op, currentLineNumber);
            }
        }

        if ((left is int && right is float) || (left is float && right is int))
        {
            float leftF = left is int li ? li : (float)left;
            float rightF = right is int ri ? ri : (float)right;

            switch (op)
            {
                case "+": return leftF + rightF;
                case "-": return leftF - rightF;
                case "*": return leftF * rightF;
                case "/":
                    if (Math.Abs(rightF) <= float.Epsilon) throw new ModeraException("Division by zero", currentLineNumber);
                    return leftF / rightF;
                case "%":
                    if (Math.Abs(rightF) <= float.Epsilon) throw new ModeraException("Modulo by zero", currentLineNumber);
                    return leftF % rightF;
                default: throw new ModeraException("Unknown operator: " + op, currentLineNumber);
            }
        }

        throw new ModeraException("Cannot perform operation " + left?.GetType() + " " + op + " " + right?.GetType(), currentLineNumber);
    }

    // === 型システム（Unity互換性対応） ===
    ModeraType ParseType(string typeStr)
    {
        switch (typeStr)
        {
            case "int": return ModeraType.Int;
            case "float": return ModeraType.Float;
            case "bool": return ModeraType.Bool;
            case "string": return ModeraType.String;
            case "int[]": return ModeraType.IntArray;
            case "float[]": return ModeraType.FloatArray;
            case "bool[]": return ModeraType.BoolArray;
            case "string[]": return ModeraType.StringArray;
            case "void": return ModeraType.Void;
            default: throw new ModeraException("Unknown type: " + typeStr, currentLineNumber);
        }
    }

    bool IsCompatibleType(object value, ModeraType expectedType)
    {
        switch (expectedType)
        {
            case ModeraType.Int: return value is int;
            case ModeraType.Float: return value is float;
            case ModeraType.Bool: return value is bool;
            case ModeraType.String: return value is string;
            case ModeraType.IntArray: return value is int[];
            case ModeraType.FloatArray: return value is float[];
            case ModeraType.BoolArray: return value is bool[];
            case ModeraType.StringArray: return value is string[];
            case ModeraType.Void: return true;
            default: return false;
        }
    }

    object GetDefaultValue(ModeraType type)
    {
        switch (type)
        {
            case ModeraType.Int: return 0;
            case ModeraType.Float: return 0.0f;
            case ModeraType.Bool: return false;
            case ModeraType.String: return "";
            case ModeraType.IntArray: return new int[0];
            case ModeraType.FloatArray: return new float[0];
            case ModeraType.BoolArray: return new bool[0];
            case ModeraType.StringArray: return new string[0];
            case ModeraType.Void: return null;
            default: return null;
        }
    }

    // === 変数管理（修正版） ===
    void ParseVariable(string line)
    {
        var match = VariableDeclarationRegex.Match(line);
        if (!match.Success)
            throw new ModeraException("Invalid variable declaration: " + line, currentLineNumber);

        string typeStr = match.Groups[1].Value;
        string name = match.Groups[2].Value;
        string valueStr = match.Groups[3].Value.Trim();

        ModeraType type = ParseType(typeStr);
        ModeraValue value = ParseValue(valueStr, type);

        var currentScope = variableStack.Peek();

        // 修正: 同名変数の場合は値を更新（エラーにしない）
        if (currentScope.ContainsKey(name))
        {
            LogWarning("Variable '" + name + "' redeclared in current scope - treating as assignment");
            object oldValue = currentScope[name].Value;
            currentScope[name] = value;
            LogVariableChange(name, oldValue, value.Value);
        }
        else
        {
            currentScope[name] = value;
            LogVariableChange(name, "undefined", value.ToString());
        }

        LogVerbose("Declared " + type + " variable '" + name + "' = " + value);
    }

    void ParseAssignment(string line)
    {
        string[] parts = line.Split('=', 2);
        if (parts.Length != 2)
            throw new ModeraException("Invalid assignment: " + line, currentLineNumber);

        string varName = parts[0].Trim();
        string valueStr = parts[1].Trim();

        var variable = GetVariable(varName);
        if (variable == null)
            throw new ModeraException("Variable '" + varName + "' not declared", currentLineNumber);

        object oldValue = variable.Value;
        ModeraValue newValue = ParseValue(valueStr, variable.Type);
        SetVariable(varName, newValue);

        LogVariableChange(varName, oldValue, newValue.Value);
    }

    void ParseArrayAssignment(string line)
    {
        var match = ArrayAssignmentRegex.Match(line);
        if (!match.Success)
            throw new ModeraException("Invalid array assignment: " + line, currentLineNumber);

        string arrayName = match.Groups[1].Value;
        string indexStr = match.Groups[2].Value;
        string valueStr = match.Groups[3].Value.Trim();

        var arrayVar = GetVariable(arrayName);
        if (arrayVar == null)
            throw new ModeraException("Array '" + arrayName + "' not found", currentLineNumber);

        int index = (int)EvaluateArithmeticExpression(indexStr);
        object newValue = EvaluateArithmeticExpression(valueStr);

        switch (arrayVar.Type)
        {
            case ModeraType.IntArray:
                {
                    int[] intArr = arrayVar.GetValue<int[]>();
                    if (index < 0 || index >= intArr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + intArr.Length + ")", currentLineNumber);
                    object oldIntValue = intArr[index];
                    intArr[index] = (int)newValue;
                    LogVariableChange(arrayName + "[" + index + "]", oldIntValue, newValue);
                    break;
                }
            case ModeraType.FloatArray:
                {
                    float[] floatArr = arrayVar.GetValue<float[]>();
                    if (index < 0 || index >= floatArr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + floatArr.Length + ")", currentLineNumber);
                    object oldFloatValue = floatArr[index];
                    floatArr[index] = (float)newValue;
                    LogVariableChange(arrayName + "[" + index + "]", oldFloatValue, newValue);
                    break;
                }
            case ModeraType.BoolArray:
                {
                    bool[] boolArr = arrayVar.GetValue<bool[]>();
                    if (index < 0 || index >= boolArr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + boolArr.Length + ")", currentLineNumber);
                    object oldBoolValue = boolArr[index];
                    boolArr[index] = (bool)newValue;
                    LogVariableChange(arrayName + "[" + index + "]", oldBoolValue, newValue);
                    break;
                }
            case ModeraType.StringArray:
                {
                    string[] strArr = arrayVar.GetValue<string[]>();
                    if (index < 0 || index >= strArr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + strArr.Length + ")", currentLineNumber);
                    object oldStrValue = strArr[index];
                    strArr[index] = (string)newValue;
                    LogVariableChange(arrayName + "[" + index + "]", oldStrValue, newValue);
                    break;
                }
            default:
                throw new ModeraException("'" + arrayName + "' is not an array", currentLineNumber);
        }
    }

    ModeraValue ParseValue(string valueStr, ModeraType expectedType)
    {
        valueStr = valueStr.Trim();

        // 修正: リテラル値を最初に判定
        // 数値リテラルの判定を最優先
        if (expectedType == ModeraType.Int && int.TryParse(valueStr, out int intVal))
        {
            return new ModeraValue(ModeraType.Int, intVal);
        }
        if (expectedType == ModeraType.Float && float.TryParse(valueStr, out float floatVal))
        {
            return new ModeraValue(ModeraType.Float, floatVal);
        }
        if (expectedType == ModeraType.Bool && (valueStr == "true" || valueStr == "false"))
        {
            return new ModeraValue(ModeraType.Bool, valueStr == "true");
        }
        if (expectedType == ModeraType.String && valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
        {
            return new ModeraValue(ModeraType.String, valueStr.Trim('"'));
        }

        // 関数呼び出しの場合
        var funcMatch = FunctionCallRegex.Match(valueStr);
        if (funcMatch.Success)
        {
            object result = CallFunction(funcMatch.Groups[1].Value, funcMatch.Groups[2].Value);
            return new ModeraValue(expectedType, result);
        }

        // 配列アクセス・長さアクセスの場合
        if (LengthAccessRegex.IsMatch(valueStr))
        {
            return new ModeraValue(expectedType, GetArrayLength(valueStr));
        }
        else if (ArrayAccessRegex.IsMatch(valueStr))
        {
            object arrayElement = GetArrayElement(valueStr);
            return new ModeraValue(expectedType, arrayElement);
        }

        // 算術式の場合
        if (valueStr.Contains("+") || valueStr.Contains("-") || valueStr.Contains("*") || valueStr.Contains("/") || valueStr.Contains("("))
        {
            object result = EvaluateArithmeticExpression(valueStr);
            return new ModeraValue(expectedType, result);
        }

        // 単純な変数参照
        if (Regex.IsMatch(valueStr, @"^\w+$"))
        {
            var variable = GetVariable(valueStr);
            if (variable != null)
            {
                if (variable.Type != expectedType && !IsTypeCompatible(variable.Type, expectedType))
                    throw new ModeraException("Type mismatch: cannot assign " + variable.Type + " to " + expectedType, currentLineNumber);
                return variable;
            }
        }

        // 配列リテラルの場合
        switch (expectedType)
        {
            case ModeraType.IntArray: return ParseIntArrayLiteral(valueStr);
            case ModeraType.FloatArray: return ParseFloatArrayLiteral(valueStr);
            case ModeraType.BoolArray: return ParseBoolArrayLiteral(valueStr);
            case ModeraType.StringArray: return ParseStringArrayLiteral(valueStr);
            default: throw new ModeraException("Cannot parse value for type " + expectedType, currentLineNumber);
        }
    }

    private bool IsTypeCompatible(ModeraType from, ModeraType to)
    {
        if (from == ModeraType.Int && to == ModeraType.Float)
            return true;
        return from == to;
    }

    // === 全てのリテラル解析メソッド（完全実装） ===
    ModeraValue ParseIntLiteral(string valueStr)
    {
        int intVal;
        if (!int.TryParse(valueStr, out intVal))
            throw new ModeraException("Invalid integer: " + valueStr, currentLineNumber);
        return new ModeraValue(ModeraType.Int, intVal);
    }

    ModeraValue ParseFloatLiteral(string valueStr)
    {
        float floatVal;
        if (!float.TryParse(valueStr, out floatVal))
            throw new ModeraException("Invalid float: " + valueStr, currentLineNumber);
        return new ModeraValue(ModeraType.Float, floatVal);
    }

    ModeraValue ParseBoolLiteral(string valueStr)
    {
        if (valueStr == "true")
            return new ModeraValue(ModeraType.Bool, true);
        else if (valueStr == "false")
            return new ModeraValue(ModeraType.Bool, false);
        else
            throw new ModeraException("Invalid boolean: " + valueStr, currentLineNumber);
    }

    ModeraValue ParseStringLiteral(string valueStr)
    {
        if (!valueStr.StartsWith("\"") || !valueStr.EndsWith("\""))
            throw new ModeraException("String must be quoted: " + valueStr, currentLineNumber);
        return new ModeraValue(ModeraType.String, valueStr.Trim('"'));
    }

    ModeraValue ParseIntArrayLiteral(string valueStr)
    {
        if (!valueStr.StartsWith("[") || !valueStr.EndsWith("]"))
            throw new ModeraException("Array must be enclosed in brackets: " + valueStr, currentLineNumber);

        string content = valueStr.Trim('[', ']').Trim();
        if (string.IsNullOrEmpty(content))
            return new ModeraValue(ModeraType.IntArray, new int[0]);

        string[] parts = content.Split(',');
        int[] result = new int[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            string item = parts[i].Trim();
            int intVal;
            if (!int.TryParse(item, out intVal))
                throw new ModeraException("Invalid integer in array: " + item, currentLineNumber);
            result[i] = intVal;
        }

        return new ModeraValue(ModeraType.IntArray, result);
    }

    ModeraValue ParseFloatArrayLiteral(string valueStr)
    {
        if (!valueStr.StartsWith("[") || !valueStr.EndsWith("]"))
            throw new ModeraException("Array must be enclosed in brackets: " + valueStr, currentLineNumber);

        string content = valueStr.Trim('[', ']').Trim();
        if (string.IsNullOrEmpty(content))
            return new ModeraValue(ModeraType.FloatArray, new float[0]);

        string[] parts = content.Split(',');
        float[] result = new float[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            string item = parts[i].Trim();
            float floatVal;
            if (!float.TryParse(item, out floatVal))
                throw new ModeraException("Invalid float in array: " + item, currentLineNumber);
            result[i] = floatVal;
        }

        return new ModeraValue(ModeraType.FloatArray, result);
    }

    ModeraValue ParseBoolArrayLiteral(string valueStr)
    {
        if (!valueStr.StartsWith("[") || !valueStr.EndsWith("]"))
            throw new ModeraException("Array must be enclosed in brackets: " + valueStr, currentLineNumber);

        string content = valueStr.Trim('[', ']').Trim();
        if (string.IsNullOrEmpty(content))
            return new ModeraValue(ModeraType.BoolArray, new bool[0]);

        string[] parts = content.Split(',');
        bool[] result = new bool[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            string item = parts[i].Trim();
            if (item == "true")
                result[i] = true;
            else if (item == "false")
                result[i] = false;
            else
                throw new ModeraException("Invalid boolean in array: " + item, currentLineNumber);
        }

        return new ModeraValue(ModeraType.BoolArray, result);
    }

    ModeraValue ParseStringArrayLiteral(string valueStr)
    {
        if (!valueStr.StartsWith("[") || !valueStr.EndsWith("]"))
            throw new ModeraException("Array must be enclosed in brackets: " + valueStr, currentLineNumber);

        string content = valueStr.Trim('[', ']').Trim();
        if (string.IsNullOrEmpty(content))
            return new ModeraValue(ModeraType.StringArray, new string[0]);

        string[] parts = content.Split(',');
        string[] result = new string[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            string item = parts[i].Trim();
            if (!item.StartsWith("\"") || !item.EndsWith("\""))
                throw new ModeraException("Array element must be quoted: " + item, currentLineNumber);
            result[i] = item.Trim('"');
        }

        return new ModeraValue(ModeraType.StringArray, result);
    }

    // === 配列操作（完全実装） ===
    int GetArrayLength(string expression)
    {
        var match = LengthAccessRegex.Match(expression);
        if (!match.Success)
            throw new ModeraException("Invalid length access: " + expression, currentLineNumber);

        string arrayName = match.Groups[1].Value;
        var arrayVar = GetVariable(arrayName);
        if (arrayVar == null)
            throw new ModeraException("Array '" + arrayName + "' not found", currentLineNumber);

        switch (arrayVar.Type)
        {
            case ModeraType.IntArray: return arrayVar.GetValue<int[]>().Length;
            case ModeraType.FloatArray: return arrayVar.GetValue<float[]>().Length;
            case ModeraType.BoolArray: return arrayVar.GetValue<bool[]>().Length;
            case ModeraType.StringArray: return arrayVar.GetValue<string[]>().Length;
            default: throw new ModeraException("'" + arrayName + "' is not an array", currentLineNumber);
        }
    }

    object GetArrayElement(string expression)
    {
        var match = ArrayAccessRegex.Match(expression);
        if (!match.Success)
            throw new ModeraException("Invalid array access: " + expression, currentLineNumber);

        string arrName = match.Groups[1].Value;
        string indexStr = match.Groups[2].Value;

        var arrayVar = GetVariable(arrName);
        if (arrayVar == null)
            throw new ModeraException("Array '" + arrName + "' not found", currentLineNumber);

        int index = (int)EvaluateArithmeticExpression(indexStr);

        switch (arrayVar.Type)
        {
            case ModeraType.IntArray:
                {
                    int[] arr = arrayVar.GetValue<int[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + arr.Length + ")", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.FloatArray:
                {
                    float[] arr = arrayVar.GetValue<float[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + arr.Length + ")", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.BoolArray:
                {
                    bool[] arr = arrayVar.GetValue<bool[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + arr.Length + ")", currentLineNumber);
                    return arr[index];
                }
            case ModeraType.StringArray:
                {
                    string[] arr = arrayVar.GetValue<string[]>();
                    if (index < 0 || index >= arr.Length)
                        throw new ModeraException("Array index " + index + " out of bounds (length: " + arr.Length + ")", currentLineNumber);
                    return arr[index];
                }
            default:
                throw new ModeraException("'" + arrName + "' is not an array", currentLineNumber);
        }
    }

    public ModeraValue GetVariable(string name)
    {
        LogDebug($"GetVariable called for: '{name}'");
        LogDebug($"Stack depth: {variableStack.Count}");

        try
        {
            if (variableStack == null || variableStack.Count == 0)
            {
                LogError($"Variable stack is empty when looking for '{name}'");
                return null;
            }

            int scopeIndex = 0;
            foreach (var scope in variableStack)
            {
                LogDebug($"Checking scope {scopeIndex}: {string.Join(", ", scope.Keys)}");

                if (scope != null && scope.ContainsKey(name))
                {
                    LogDebug($"Found variable '{name}' in scope {scopeIndex}");
                    return scope[name];
                }
                scopeIndex++;
            }

            LogError($"Variable '{name}' not found in any of {variableStack.Count} scopes");
            return null;
        }
        catch (Exception ex)
        {
            LogError($"Exception in GetVariable for '{name}': {ex.Message}");
            return null;
        }
    }
    void SetVariable(string name, ModeraValue value)
    {
        if (variableStack.Count == 0)
        {
            throw new ModeraException($"Cannot set variable '{name}' - no scopes available", currentLineNumber);
        }

        foreach (var scope in variableStack)
        {
            if (scope != null && scope.ContainsKey(name))
            {
                var oldValue = scope[name];
                scope[name] = value;
                LogVariableChange(name, oldValue.Value, value.Value);
                return;
            }
        }

        LogError($"Variable '{name}' not found in any scope during assignment");
        LogDebug($"Available scopes: {variableStack.Count}");
        throw new ModeraException($"Variable '{name}' not found", currentLineNumber);
    }

    // === 関数管理（完全実装） ===
    int ParseFunction(List<(string line, int lineNumber)> lines, int startIndex)
    {
        var lineData = lines[startIndex];
        string header = lineData.line;
        var match = FunctionDeclarationRegex.Match(header.Trim());
        if (!match.Success)
            throw new ModeraException("Invalid function declaration: " + header, currentLineNumber);

        string funcName = match.Groups[1].Value;
        string argsStr = match.Groups[2].Value;
        string returnTypeStr = match.Groups[3].Value;

        var parameters = new List<(ModeraType type, string name)>();
        ModeraType returnType = string.IsNullOrEmpty(returnTypeStr) ? ModeraType.Void : ParseType(returnTypeStr);

        if (!string.IsNullOrWhiteSpace(argsStr))
        {
            string[] paramStrs = argsStr.Split(',');
            foreach (string paramStr in paramStrs)
            {
                string[] paramParts = paramStr.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (paramParts.Length != 2)
                    throw new ModeraException("Invalid parameter: " + paramStr, currentLineNumber);

                parameters.Add((ParseType(paramParts[0]), paramParts[1]));
            }
        }

        List<string> body = new List<string>();
        int i = startIndex + 1;
        int baseIndent = GetIndentLevel(lines[startIndex].line);

        for (; i < lines.Count; i++)
        {
            int indent = GetIndentLevel(lines[i].line);
            if (indent <= baseIndent) break;
            body.Add(lines[i].line);
        }

        functions[funcName] = new ModeraFunction(funcName, parameters, returnType, body, baseIndent + 4);
        LogInfo("Function '" + funcName + "' defined with " + parameters.Count + " parameters → " + returnType);

        return i - 1;
    }

    object CallFunction(string funcName, string argsStr)
    {
        if (!functions.ContainsKey(funcName))
            throw new ModeraException("Function '" + funcName + "' not found", currentLineNumber);

        var function = functions[funcName];
        var args = ParseFunctionArguments(argsStr);

        if (args.Count != function.Parameters.Count)
            throw new ModeraException("Function '" + funcName + "' expects " + function.Parameters.Count + " arguments, got " + args.Count, currentLineNumber);

        // 修正: 関数呼び出し前の変数状態をログ出力
        LogDebug($"Before function call - Global variables: {string.Join(", ", variableStack.First().Keys)}");

        var newScope = new Dictionary<string, ModeraValue>();

        for (int i = 0; i < function.Parameters.Count; i++)
        {
            var paramData = function.Parameters[i];
            ModeraType paramType = paramData.type;
            string paramName = paramData.name;
            object argValue = args[i];

            if (!IsCompatibleType(argValue, paramType))
                throw new ModeraException("Argument " + (i + 1) + " type mismatch for function '" + funcName + "'", currentLineNumber);

            // 修正: パラメータ設定をデバッグログで確認
            LogDebug($"Setting parameter '{paramName}' = {argValue} (Type: {paramType})");
            newScope[paramName] = new ModeraValue(paramType, argValue);

            // 修正: 設定後に確認
            if (newScope.ContainsKey(paramName))
                LogDebug($"Parameter '{paramName}' successfully set in scope");
            else
                LogError($"Failed to set parameter '{paramName}' in scope");
        }

        // 修正: スコープの内容をログ出力
        LogDebug($"Function scope contains: {string.Join(", ", newScope.Keys)}");
        try
        {
            var bodyWithLineNumbers = function.Body.Select((line, index) => (line, currentLineNumber + index + 1)).ToList();
            RunBlock(bodyWithLineNumbers, 0, function.BaseIndent);

            object returnValue = function.ReturnType == ModeraType.Void ? null : GetDefaultValue(function.ReturnType);
            LogFunctionCall(funcName, args, returnValue);
            return returnValue;
        }
        catch (ModeraReturnException returnEx)
        {
            LogFunctionCall(funcName, args, returnEx.ReturnValue);
            return returnEx.ReturnValue;
        }
        finally
        {
            if (variableStack.Count > 1)
            {
                variableStack.Pop();
                LogDebug($"Popped function scope. Remaining scopes: {variableStack.Count}");
            }
            else
            {
                LogError("Attempted to pop from variable stack with only global scope remaining");
            }
        }
    }

    List<object> ParseFunctionArguments(string argsStr)
    {
        var args = new List<object>();
        if (string.IsNullOrWhiteSpace(argsStr))
            return args;

        string[] parts = argsStr.Split(',');
        foreach (string part in parts)
        {
            string arg = part.Trim();
            args.Add(GetValueSafe(arg));
        }

        return args;
    }

    void ParseReturn(string line)
    {
        var match = ReturnStatementRegex.Match(line);
        if (!match.Success)
            throw new ModeraException("Invalid return statement: " + line, currentLineNumber);

        string valueStr = match.Groups[1].Value.Trim();

        object returnValue = null;
        if (!string.IsNullOrEmpty(valueStr))
        {
            // 修正: return文の処理前にスコープ状態をログ出力
            LogDebug($"Processing return statement with value: '{valueStr}'");
            LogDebug($"Current scope depth: {variableStack.Count}");

            if (variableStack.Count > 0)
            {
                var currentScope = variableStack.Peek();
                LogDebug($"Current scope variables: {string.Join(", ", currentScope.Keys)}");
            }

            try
            {
                returnValue = GetValueSafe(valueStr);
                LogDebug($"Successfully resolved return value: {returnValue}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to resolve return value '{valueStr}': {ex.Message}");
                throw;
            }
        }

        LogVerbose("Returning value: " + returnValue);
        throw new ModeraReturnException(returnValue);
    }

    void ParseExpression(string line)
    {
        var match = FunctionCallRegex.Match(line);
        if (match.Success)
        {
            CallFunction(match.Groups[1].Value, match.Groups[2].Value);
        }
        else
        {
            throw new ModeraException("Unknown expression: " + line, currentLineNumber);
        }
    }

    // === ループ処理（修正版） ===
    int ParseFor(List<(string line, int lineNumber)> lines, int startIndex, int baseIndent)
    {
        var lineData = lines[startIndex];
        string header = lineData.line;
        var match = ForLoopRegex.Match(header.Trim());
        if (!match.Success)
            throw new ModeraException("Invalid for statement: " + header, currentLineNumber);

        string varName = match.Groups[1].Value;
        string rangeExpr = match.Groups[2].Value;

        int start = 0;
        int end = 0;

        if (rangeExpr.Contains(","))
        {
            string[] parts = rangeExpr.Split(',');
            start = (int)EvaluateArithmeticExpression(parts[0].Trim());
            end = (int)EvaluateArithmeticExpression(parts[1].Trim());
        }
        else
        {
            end = (int)EvaluateArithmeticExpression(rangeExpr);
        }

        LogInfo("Starting for loop: " + varName + " from " + start + " to " + (end - 1));

        var forBody = new List<(string, int)>();
        int i = startIndex + 1;
        for (; i < lines.Count; i++)
        {
            int indent = GetIndentLevel(lines[i].line);
            if (indent <= baseIndent) break;
            forBody.Add(lines[i]);
        }

        for (int k = start; k < end; k++)
        {
            totalLoopIterations++;
            LogLoopIteration("for", k);

            var currentScope = variableStack.Peek();
            currentScope[varName] = new ModeraValue(ModeraType.Int, k);

            // ループ本体用の新しいスコープを作成
            EnterNewScope();
            try
            {
                RunBlock(forBody, 0, baseIndent + 4);
            }
            catch (ModeraBreakException)
            {
                LogVerbose("For loop terminated by break");
                ExitScope();
                break;
            }
            catch (ModeraContinueException)
            {
                LogVerbose("For loop iteration skipped by continue");
                ExitScope();
                continue;
            }
            finally
            {
                ExitScope(); // ループ本体のスコープを確実に終了
            }
        }

        LogInfo("For loop completed: " + (end - start) + " iterations");
        return i - 1;
    }

    int ParseWhile(List<(string line, int lineNumber)> lines, int startIndex, int baseIndent)
    {
        var lineData = lines[startIndex];
        string header = lineData.line;
        var match = WhileLoopRegex.Match(header.Trim());
        if (!match.Success)
            throw new ModeraException("Invalid while statement: " + header, currentLineNumber);

        string condition = match.Groups[1].Value;
        LogInfo("Starting while loop: condition '" + condition + "'");

        var whileBody = new List<(string, int)>();
        int i = startIndex + 1;

        for (; i < lines.Count; i++)
        {
            int indent = GetIndentLevel(lines[i].line);
            if (indent <= baseIndent) break;
            whileBody.Add(lines[i]);
        }

        int iterations = 0;
        while (EvalCondition(condition))
        {
            totalLoopIterations++;
            iterations++;
            LogLoopIteration("while", iterations, condition);

            // ループ本体用の新しいスコープを作成
            EnterNewScope();
            try
            {
                RunBlock(whileBody, 0, baseIndent + 4);
            }
            catch (ModeraBreakException)
            {
                LogVerbose("While loop terminated by break");
                ExitScope();
                break;
            }
            catch (ModeraContinueException)
            {
                LogVerbose("While loop iteration skipped by continue");
                ExitScope();
                continue;
            }
            finally
            {
                ExitScope(); // ループ本体のスコープを確実に終了
            }
        }

        LogInfo("While loop completed: " + iterations + " iterations");
        return i - 1;
    }

    // === 条件分岐（修正版） ===
    int ParseIf(List<(string line, int lineNumber)> lines, int startIndex, int baseIndent)
    {
        var lineData = lines[startIndex];
        string header = lineData.line;
        var match = IfStatementRegex.Match(header.Trim());
        if (!match.Success)
            throw new ModeraException("Invalid if statement: " + header, currentLineNumber);

        string condition = match.Groups[1].Value;

        var ifBody = new List<(string, int)>();
        var elseBody = new List<(string, int)>();
        int i = startIndex + 1;
        bool inElseBlock = false;

        for (; i < lines.Count; i++)
        {
            int indent = GetIndentLevel(lines[i].line);
            if (indent <= baseIndent) break;

            if (indent == baseIndent + 4 && ElseStatementRegex.IsMatch(lines[i].line.Trim()))
            {
                inElseBlock = true;
                continue;
            }

            if (inElseBlock)
                elseBody.Add(lines[i]);
            else
                ifBody.Add(lines[i]);
        }

        bool conditionResult = EvalCondition(condition);
        LogVerbose("If condition '" + condition + "' evaluated to: " + conditionResult);

        if (conditionResult)
        {
            EnterNewScope();
            try
            {
                RunBlock(ifBody, 0, baseIndent + 4);
            }
            finally
            {
                ExitScope();
            }
        }
        else if (elseBody.Count > 0)
        {
            EnterNewScope();
            try
            {
                RunBlock(elseBody, 0, baseIndent + 4);
            }
            finally
            {
                ExitScope();
            }
        }

        return i - 1;
    }

    int ParseSwitch(List<(string line, int lineNumber)> lines, int startIndex, int baseIndent)
    {
        var lineData = lines[startIndex];
        string header = lineData.line;
        var match = SwitchStatementRegex.Match(header.Trim());
        if (!match.Success)
            throw new ModeraException("Invalid switch statement: " + header, currentLineNumber);

        string switchValue = match.Groups[1].Value;
        object switchObj = GetValueSafe(switchValue);
        LogVerbose("Switch statement with value: " + switchObj);

        var cases = new List<(object caseValue, List<(string, int)> caseBody)>();
        List<(string, int)> defaultBody = null;

        int i = startIndex + 1;
        List<(string, int)> currentCaseBody = null;

        for (; i < lines.Count; i++)
        {
            var currentLineData = lines[i];
            string currentLine = currentLineData.line;
            int currentLineNum = currentLineData.lineNumber;

            int indent = GetIndentLevel(currentLine);
            if (indent <= baseIndent) break;

            string trimmedLine = currentLine.Trim();

            if (CaseStatementRegex.IsMatch(trimmedLine))
            {
                var caseMatch = CaseStatementRegex.Match(trimmedLine);
                object caseValue = GetValueSafe(caseMatch.Groups[1].Value);
                currentCaseBody = new List<(string, int)>();
                cases.Add((caseValue, currentCaseBody));
            }
            else if (DefaultStatementRegex.IsMatch(trimmedLine))
            {
                currentCaseBody = new List<(string, int)>();
                defaultBody = currentCaseBody;
            }
            else if (currentCaseBody != null)
            {
                currentCaseBody.Add((currentLine, currentLineNum));
            }
        }

        bool executed = false;
        bool fallThrough = false;

        EnterNewScope();
        try
        {
            foreach (var caseData in cases)
            {
                object caseValue = caseData.caseValue;
                List<(string, int)> caseBody = caseData.caseBody;

                if (fallThrough || AreEqual(switchObj, caseValue))
                {
                    LogVerbose("Executing case: " + caseValue);
                    try
                    {
                        RunBlock(caseBody, 0, baseIndent + 8);
                        fallThrough = true;
                        executed = true;
                    }
                    catch (ModeraBreakException)
                    {
                        executed = true;
                        break;
                    }
                }
            }

            if (!executed && defaultBody != null)
            {
                LogVerbose("Executing default case");
                try
                {
                    RunBlock(defaultBody, 0, baseIndent + 8);
                }
                catch (ModeraBreakException)
                {
                    // break処理
                }
            }
        }
        finally
        {
            ExitScope();
        }

        return i - 1;
    }

    // === 条件評価（完全実装） ===
    bool EvalCondition(string cond)
    {
        cond = cond.Trim();

        if (cond.StartsWith("!"))
        {
            bool negatedResult = !EvalCondition(cond.Substring(1).Trim());
            LogDebug("Negated condition: !(" + cond.Substring(1).Trim() + ") = " + negatedResult);
            return negatedResult;
        }

        var logicalMatch = LogicalRegex.Match(cond);
        if (logicalMatch.Success)
        {
            string left = logicalMatch.Groups[1].Value.Trim();
            string op = logicalMatch.Groups[2].Value;
            string right = logicalMatch.Groups[3].Value.Trim();

            bool leftResult = EvalCondition(left);
            bool rightResult = EvalCondition(right);

            bool result;
            switch (op)
            {
                case "&&": result = leftResult && rightResult; break;
                case "||": result = leftResult || rightResult; break;
                default: throw new ModeraException("Unknown logical operator: " + op, currentLineNumber);
            }

            LogDebug("Logical condition: " + left + " " + op + " " + right + " = " + leftResult + " " + op + " " + rightResult + " = " + result);
            return result;
        }

        var compMatch = ComparisonRegex.Match(cond);
        if (compMatch.Success)
        {
            string left = compMatch.Groups[1].Value.Trim();
            string op = compMatch.Groups[2].Value;
            string right = compMatch.Groups[3].Value.Trim();

            return EvaluateComparison(left, op, right);
        }

        object condValue = GetValueSafe(cond);
        if (condValue is bool boolVal)
        {
            LogDebug("Boolean condition: " + cond + " = " + boolVal);
            return boolVal;
        }

        throw new ModeraException("Invalid condition: " + cond, currentLineNumber);
    }

    bool EvaluateComparison(string left, string op, string right)
    {
        object leftVal = GetValueSafe(left);
        object rightVal = GetValueSafe(right);

        if (leftVal is int leftInt && rightVal is int rightInt)
        {
            bool result;
            switch (op)
            {
                case ">": result = leftInt > rightInt; break;
                case "<": result = leftInt < rightInt; break;
                case ">=": result = leftInt >= rightInt; break;
                case "<=": result = leftInt <= rightInt; break;
                case "==": result = leftInt == rightInt; break;
                case "!=": result = leftInt != rightInt; break;
                default: throw new ModeraException("Unsupported operator for int: " + op, currentLineNumber);
            }
            LogDebug("Int comparison: " + leftInt + " " + op + " " + rightInt + " = " + result);
            return result;
        }

        if (leftVal is float leftFloat && rightVal is float rightFloat)
        {
            bool result;
            switch (op)
            {
                case ">": result = leftFloat > rightFloat; break;
                case "<": result = leftFloat < rightFloat; break;
                case ">=": result = leftFloat >= rightFloat; break;
                case "<=": result = leftFloat <= rightFloat; break;
                case "==": result = Math.Abs(leftFloat - rightFloat) < float.Epsilon; break;
                case "!=": result = Math.Abs(leftFloat - rightFloat) >= float.Epsilon; break;
                default: throw new ModeraException("Unsupported operator for float: " + op, currentLineNumber);
            }
            LogDebug("Float comparison: " + leftFloat + " " + op + " " + rightFloat + " = " + result);
            return result;
        }

        if ((leftVal is int && rightVal is float) || (leftVal is float && rightVal is int))
        {
            float leftF = leftVal is int li ? li : (float)leftVal;
            float rightF = rightVal is int ri ? ri : (float)rightVal;

            bool result;
            switch (op)
            {
                case ">": result = leftF > rightF; break;
                case "<": result = leftF < rightF; break;
                case ">=": result = leftF >= rightF; break;
                case "<=": result = leftF <= rightF; break;
                case "==": result = Math.Abs(leftF - rightF) < float.Epsilon; break;
                case "!=": result = Math.Abs(leftF - rightF) >= float.Epsilon; break;
                default: throw new ModeraException("Unsupported operator for mixed numeric types: " + op, currentLineNumber);
            }
            LogDebug("Mixed comparison: " + leftF + " " + op + " " + rightF + " = " + result);
            return result;
        }

        if (leftVal is bool leftBool && rightVal is bool rightBool)
        {
            bool result;
            switch (op)
            {
                case "==": result = leftBool == rightBool; break;
                case "!=": result = leftBool != rightBool; break;
                default: throw new ModeraException("Unsupported operator for bool: " + op, currentLineNumber);
            }
            LogDebug("Bool comparison: " + leftBool + " " + op + " " + rightBool + " = " + result);
            return result;
        }

        if (leftVal is string leftStr && rightVal is string rightStr)
        {
            int comparison = string.Compare(leftStr, rightStr, StringComparison.Ordinal);
            bool result;
            switch (op)
            {
                case ">": result = comparison > 0; break;
                case "<": result = comparison < 0; break;
                case ">=": result = comparison >= 0; break;
                case "<=": result = comparison <= 0; break;
                case "==": result = comparison == 0; break;
                case "!=": result = comparison != 0; break;
                default: throw new ModeraException("Unsupported operator for string: " + op, currentLineNumber);
            }
            LogDebug("String comparison: '" + leftStr + "' " + op + " '" + rightStr + "' = " + result);
            return result;
        }

        throw new ModeraException("Type mismatch in comparison: " + leftVal?.GetType() + " " + op + " " + rightVal?.GetType(), currentLineNumber);
    }

    bool AreEqual(object left, object right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        if (left is int leftInt && right is float rightFloat)
            return Math.Abs(leftInt - rightFloat) < float.Epsilon;
        if (left is float leftFloat && right is int rightInt)
            return Math.Abs(leftFloat - rightInt) < float.Epsilon;

        return left.Equals(right);
    }

    // === 出力メソッド ===
    void ParsePrint(string line)
    {
        var match = PrintStatementRegex.Match(line);
        if (!match.Success)
            throw new ModeraException("Invalid print statement: " + line, currentLineNumber);

        string valueStr = match.Groups[1].Value.Trim();
        object value = GetValueSafe(valueStr);

        string output = value?.ToString() ?? "null";
        UnityEngine.Debug.Log(COLOR_SUCCESS + "[OUTPUT]" + COLOR_END + " " + output);
        debugLog.AppendLine(output);
    }

    int GetIndentLevel(string line)
    {
        int spaces = 0;
        foreach (char c in line)
        {
            if (c == ' ')
                spaces++;
            else if (c == '\t')
                spaces += 4;
            else
                break;
        }
        return spaces;
    }

    // === ログ機能用パブリックメソッド ===
    public string GetDebugLog()
    {
        return debugLog.ToString();
    }

    public void ClearDebugLog()
    {
        debugLog.Clear();
    }

    public string GetLogBuffer()
    {
        return string.Join("\n", logBuffer);
    }

    public void ClearLogBuffer()
    {
        logBuffer.Clear();
    }

    [ContextMenu("Show Performance Stats")]
    public void ShowPerformanceStats()
    {
        LogPerformanceStats();
    }

    [ContextMenu("Clear All Logs")]
    public void ClearAllLogs()
    {
        ClearDebugLog();
        ClearLogBuffer();
        LogInfo("All logs cleared");
    }

    void OnDestroy()
    {
        LogInfo("Modera Interpreter destroyed");
        CleanupRuntime();
    }
}