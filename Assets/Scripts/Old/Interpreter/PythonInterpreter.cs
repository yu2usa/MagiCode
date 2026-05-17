using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class PythonInterpreter : MonoBehaviour
{
    // 変数を格納する辞書（型情報も保持）
    private Dictionary<string, object> variables = new Dictionary<string, object>();
    private Dictionary<string, string> variableTypes = new Dictionary<string, string>();

    // 関数定義を格納
    private Dictionary<string, FunctionDefinition> functions = new Dictionary<string, FunctionDefinition>();

    // 無限ループ防止用
    private const int MAX_EXECUTIONS = 1000;
    private int executedCount = 0;

    // デバッグ用フラグ
    private bool debugMode = false;

    // 関数定義用のクラス
    [System.Serializable]
    public class FunctionDefinition
    {
        public string name;
        public List<Parameter> parameters;
        public List<string> body;

        [System.Serializable]
        public class Parameter
        {
            public string type;
            public string name;
        }
    }

    public void Run(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("コードが空です");
            return;
        }

        try
        {
            // 変数と関数をクリア
            variables.Clear();
            variableTypes.Clear();
            functions.Clear();
            executedCount = 0;

            var lines = code.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines == null || lines.Length == 0)
            {
                Debug.LogWarning("実行するコードがありません");
                return;
            }

            ExecuteLines(lines, 0, out int _);
        }
        catch (Exception ex)
        {
            Debug.LogError($"実行エラー: {ex.Message}\n{ex.StackTrace}");
        }

        if (executedCount >= MAX_EXECUTIONS)
        {
            Debug.LogWarning("実行が停止しました: コマンド上限に達しました (無限ループの可能性)");
        }
    }

    private void ExecuteLines(string[] lines, int startIndex, out int endIndex)
    {
        int i = startIndex;

        while (i < lines.Length && executedCount < MAX_EXECUTIONS)
        {
            if (i < 0 || i >= lines.Length)
            {
                endIndex = i;
                return;
            }

            string line = lines[i].Trim();

            if (debugMode)
            {
                Debug.Log($"Processing line {i}: {line}");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // 関数定義の検出: def function_name(parameters): または def function_name(parameters)
            if (line.StartsWith("def ") && (line.EndsWith(":") || line.Contains("(")))
            {
                if (debugMode)
                {
                    Debug.Log($"関数定義を検出: {line}");
                }

                ParseFunctionDefinition(line, lines, ref i);
                continue;
            }

            // for文の検出: for i in range(n):
            if (line.StartsWith("for ") && line.Contains(" in range(") && line.EndsWith(":"))
            {
                if (debugMode)
                {
                    Debug.Log($"forループを検出: {line}");
                }

                ExecuteForLoop(line, lines, ref i);
                continue;
            }

            // if文の検出: if condition:
            if (line.StartsWith("if ") && line.EndsWith(":"))
            {
                if (debugMode)
                {
                    Debug.Log($"条件分岐を検出: {line}");
                }

                ExecuteIfStatement(line, lines, ref i);
                continue;
            }

            // 通常のコマンド実行
            ExecuteCommand(line);
            i++;
        }

        endIndex = i;
    }

    private void ParseFunctionDefinition(string line, string[] lines, ref int index)
    {
        try
        {
            // def function_name(type param1, type param2): または def function_name(type param1, type param2) の解析
            var match = Regex.Match(line, @"def\s+(\w+)\s*\((.*?)\)\s*:?");
            if (!match.Success)
            {
                Debug.LogError($"関数定義の構文エラー: {line}");
                index++;
                return;
            }

            string functionName = match.Groups[1].Value;
            string parameterString = match.Groups[2].Value.Trim();

            var function = new FunctionDefinition
            {
                name = functionName,
                parameters = new List<FunctionDefinition.Parameter>(),
                body = new List<string>()
            };

            // パラメータの解析
            if (!string.IsNullOrEmpty(parameterString))
            {
                var paramParts = parameterString.Split(',');
                foreach (var param in paramParts)
                {
                    var paramTrim = param.Trim();
                    var paramMatch = Regex.Match(paramTrim, @"(\w+)\s+(\w+)");
                    if (paramMatch.Success)
                    {
                        function.parameters.Add(new FunctionDefinition.Parameter
                        {
                            type = paramMatch.Groups[1].Value,
                            name = paramMatch.Groups[2].Value
                        });
                    }
                }
            }

            // 関数本体の取得
            index++;
            while (index < lines.Length)
            {
                string bodyLine = lines[index].Trim();
                if (string.IsNullOrEmpty(bodyLine))
                {
                    index++;
                    continue;
                }

                // インデントされた行なら関数本体
                if (lines[index].StartsWith("    ") || lines[index].StartsWith("\t"))
                {
                    function.body.Add(bodyLine);
                    index++;
                }
                else
                {
                    // インデントがない行が来たら関数定義終了
                    break;
                }
            }

            functions[functionName] = function;

            if (debugMode)
            {
                Debug.Log($"関数を定義: {functionName} ({function.parameters.Count}個のパラメータ)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"関数定義エラー: {ex.Message}");
            index++;
        }
    }

    private void ExecuteForLoop(string line, string[] lines, ref int index)
    {
        try
        {
            // for i in range(n): の解析
            var match = Regex.Match(line, @"for\s+(\w+)\s+in\s+range\s*\(\s*(.+?)\s*\)\s*:");
            if (!match.Success)
            {
                Debug.LogError($"forループの構文エラー: {line}");
                index++;
                return;
            }

            string iteratorVar = match.Groups[1].Value;
            string rangeExpression = match.Groups[2].Value;

            int loopCount = EvaluateExpression(rangeExpression);

            if (debugMode)
            {
                Debug.Log($"forループ設定: 変数={iteratorVar}, 回数={loopCount}");
            }

            // ループ本体の取得
            var loopBody = GetIndentedBlock(lines, ref index);

            if (debugMode)
            {
                Debug.Log($"ループ本体: {loopBody.Count}行");
                for (int j = 0; j < loopBody.Count; j++)
                {
                    Debug.Log($"  本体[{j}]: {loopBody[j]}");
                }
            }

            // ループ実行
            for (int i = 0; i < loopCount && executedCount < MAX_EXECUTIONS; i++)
            {
                // イテレータ変数を設定
                variables[iteratorVar] = i.ToString();
                variableTypes[iteratorVar] = "int";

                if (debugMode)
                {
                    Debug.Log($"ループ実行 {i + 1}/{loopCount}, {iteratorVar} = {i}");
                }

                if (loopBody.Count > 0)
                {
                    ExecuteLines(loopBody.ToArray(), 0, out int _);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"forループエラー: {ex.Message}");
            index++;
        }
    }

    private void ExecuteIfStatement(string line, string[] lines, ref int index)
    {
        try
        {
            // if condition: の解析
            string condition = line.Substring(3, line.Length - 4).Trim(); // "if " と ":" を除去

            bool conditionResult = EvaluateCondition(condition);

            // if本体の取得
            var ifBody = GetIndentedBlock(lines, ref index);

            if (debugMode)
            {
                Debug.Log($"If条件評価: {condition} = {conditionResult}");
            }

            // 条件が真なら実行
            if (conditionResult && ifBody.Count > 0)
            {
                ExecuteLines(ifBody.ToArray(), 0, out int _);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"if文エラー: {ex.Message}");
            index++;
        }
    }

    private List<string> GetIndentedBlock(string[] lines, ref int index)
    {
        var block = new List<string>();
        index++; // 次の行に移動

        while (index < lines.Length)
        {
            string line = lines[index];
            if (string.IsNullOrEmpty(line.Trim()))
            {
                index++;
                continue;
            }

            // インデントされた行なら本体
            if (line.StartsWith("    ") || line.StartsWith("\t"))
            {
                block.Add(line.Trim());
                index++;
            }
            else
            {
                // インデントがない行が来たらブロック終了
                break;
            }
        }

        return block;
    }

    private int EvaluateExpression(string expression)
    {
        expression = expression.Trim();

        // 変数参照
        if (variables.ContainsKey(expression))
        {
            if (int.TryParse(variables[expression].ToString(), out int value))
            {
                return Math.Max(0, Math.Min(value, 100)); // 安全制限
            }
        }

        // 直接数値
        if (int.TryParse(expression, out int directValue))
        {
            return Math.Max(0, Math.Min(directValue, 100)); // 安全制限
        }

        Debug.LogWarning($"式の評価に失敗: {expression}");
        return 0;
    }

    private bool EvaluateCondition(string condition)
    {
        try
        {
            condition = condition.Trim();

            // == 演算子
            if (condition.Contains("=="))
            {
                var parts = condition.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string left = GetVariableValue(parts[0].Trim());
                    string right = GetVariableValue(parts[1].Trim().Trim('"'));
                    return left == right;
                }
            }

            // != 演算子
            if (condition.Contains("!="))
            {
                var parts = condition.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string left = GetVariableValue(parts[0].Trim());
                    string right = GetVariableValue(parts[1].Trim().Trim('"'));
                    return left != right;
                }
            }

            // > 演算子
            if (condition.Contains(">") && !condition.Contains(">="))
            {
                var parts = condition.Split('>');
                if (parts.Length == 2)
                {
                    if (int.TryParse(GetVariableValue(parts[0].Trim()), out int left) &&
                        int.TryParse(GetVariableValue(parts[1].Trim()), out int right))
                    {
                        return left > right;
                    }
                }
            }

            // < 演算子
            if (condition.Contains("<") && !condition.Contains("<="))
            {
                var parts = condition.Split('<');
                if (parts.Length == 2)
                {
                    if (int.TryParse(GetVariableValue(parts[0].Trim()), out int left) &&
                        int.TryParse(GetVariableValue(parts[1].Trim()), out int right))
                    {
                        return left < right;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"条件評価エラー: {ex.Message}");
            return false;
        }
    }

    private string GetVariableValue(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        name = name.Trim();

        // 配列アクセス: var[index]
        if (name.Contains("[") && name.EndsWith("]"))
        {
            int bracketStart = name.IndexOf('[');
            string varName = name.Substring(0, bracketStart).Trim();
            string indexStr = name.Substring(bracketStart + 1, name.Length - bracketStart - 2).Trim();

            if (debugMode)
            {
                Debug.Log($"配列アクセス解析: 変数名={varName}, インデックス={indexStr}");
            }

            // インデックスが変数の場合は値を取得
            string resolvedIndex = indexStr;
            if (variables.ContainsKey(indexStr))
            {
                resolvedIndex = variables[indexStr].ToString();
                if (debugMode)
                {
                    Debug.Log($"インデックス変数 {indexStr} の値: {resolvedIndex}");
                }
            }

            if (int.TryParse(resolvedIndex, out int index) &&
                variables.ContainsKey(varName) &&
                variables[varName] is List<string> list)
            {
                if (index >= 0 && index < list.Count)
                {
                    string result = list[index];
                    if (debugMode)
                    {
                        Debug.Log($"配列アクセス結果: {varName}[{index}] = {result}");
                    }
                    return result;
                }
                else
                {
                    Debug.LogWarning($"配列インデックスが範囲外: {varName}[{index}], 配列サイズ: {list.Count}");
                }
            }
            else
            {
                Debug.LogWarning($"配列アクセスエラー: 変数={varName}, インデックス={resolvedIndex}");
            }
            return "";
        }

        // 変数参照
        if (variables.ContainsKey(name))
        {
            object value = variables[name];
            if (debugMode)
            {
                Debug.Log($"変数参照: {name} = {value}");
            }
            return value.ToString();
        }

        // リテラル値（クォートを除去）
        if (name.StartsWith("\"") && name.EndsWith("\""))
        {
            return name.Substring(1, name.Length - 2);
        }

        return name;
    }

    private void ExecuteCommand(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        executedCount++;

        try
        {
            // 変数宣言: int x = 5, string name = "test"
            if (Regex.IsMatch(line, @"^\s*(int|string|float|bool)\s+\w+\s*="))
            {
                ParseVariableDeclaration(line);
                return;
            }

            // 配列宣言: string[] names = ["a", "b"]
            if (Regex.IsMatch(line, @"^\s*(int|string|float|bool)\[\]\s+\w+\s*="))
            {
                ParseArrayDeclaration(line);
                return;
            }

            // 変数代入: x = 10
            if (Regex.IsMatch(line, @"^\s*\w+\s*="))
            {
                ParseAssignment(line);
                return;
            }

            // 関数呼び出し
            if (line.Contains("(") && line.Contains(")"))
            {
                ExecuteFunctionCall(line);
                return;
            }

            Debug.LogWarning($"未知のコマンド: {line}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"コマンド実行エラー '{line}': {ex.Message}");
        }
    }

    private void ParseVariableDeclaration(string line)
    {
        var match = Regex.Match(line, @"^\s*(\w+)\s+(\w+)\s*=\s*(.+)$");
        if (match.Success)
        {
            string type = match.Groups[1].Value;
            string varName = match.Groups[2].Value;
            string value = match.Groups[3].Value.Trim().Trim('"');

            variables[varName] = value;
            variableTypes[varName] = type;

            if (debugMode)
            {
                Debug.Log($"変数宣言: {type} {varName} = {value}");
            }
        }
    }

    private void ParseArrayDeclaration(string line)
    {
        var match = Regex.Match(line, @"^\s*(\w+)\[\]\s+(\w+)\s*=\s*\[(.*?)\]$");
        if (match.Success)
        {
            string type = match.Groups[1].Value;
            string varName = match.Groups[2].Value;
            string elements = match.Groups[3].Value;

            var list = new List<string>();
            if (!string.IsNullOrEmpty(elements))
            {
                var elementArray = elements.Split(',');
                foreach (var element in elementArray)
                {
                    string cleanElement = element.Trim().Trim('"');
                    list.Add(cleanElement);
                    if (debugMode)
                    {
                        Debug.Log($"配列要素追加: '{cleanElement}'");
                    }
                }
            }

            variables[varName] = list;
            variableTypes[varName] = type + "[]";

            if (debugMode)
            {
                Debug.Log($"配列宣言: {type}[] {varName} = [{string.Join(", ", list)}], 要素数: {list.Count}");
                for (int i = 0; i < list.Count; i++)
                {
                    Debug.Log($"  {varName}[{i}] = '{list[i]}'");
                }
            }
        }
    }

    private void ParseAssignment(string line)
    {
        var parts = line.Split('=');
        if (parts.Length == 2)
        {
            string varName = parts[0].Trim();
            string value = parts[1].Trim().Trim('"');

            if (variables.ContainsKey(varName))
            {
                variables[varName] = value;

                if (debugMode)
                {
                    Debug.Log($"変数代入: {varName} = {value}");
                }
            }
            else
            {
                Debug.LogWarning($"未定義の変数: {varName}");
            }
        }
    }

    private void ExecuteFunctionCall(string line)
    {
        var match = Regex.Match(line, @"^(\w+)\s*\((.*?)\)$");
        if (match.Success)
        {
            string functionName = match.Groups[1].Value;
            string argsString = match.Groups[2].Value;

            // 組み込み関数
            if (functionName == "attack")
            {
                var args = ParseArguments(argsString);
                if (args.Count >= 2)
                {
                    Debug.Log($"Attacking {args[0]} with {args[1]}!");
                }
                else if (args.Count == 1)
                {
                    Debug.Log($"Attacking with {args[0]}!");
                }
                return;
            }

            if (functionName == "print")
            {
                var args = ParseArguments(argsString);
                if (args.Count > 0)
                {
                    Debug.Log($"Print: {args[0]}");
                }
                return;
            }

            // ユーザー定義関数
            if (functions.ContainsKey(functionName))
            {
                ExecuteUserFunction(functionName, argsString);
                return;
            }

            Debug.LogWarning($"未知の関数: {functionName}");
        }
    }

    private void ExecuteUserFunction(string functionName, string argsString)
    {
        var function = functions[functionName];
        var args = ParseArguments(argsString);

        if (debugMode)
        {
            Debug.Log($"ユーザー関数呼び出し: {functionName}, 引数数: {args.Count}");
        }

        // パラメータ設定
        for (int i = 0; i < function.parameters.Count && i < args.Count; i++)
        {
            string paramName = function.parameters[i].name;
            string paramType = function.parameters[i].type;
            string argValue = args[i];

            variables[paramName] = argValue;
            variableTypes[paramName] = paramType;

            if (debugMode)
            {
                Debug.Log($"パラメータ設定: {paramType} {paramName} = '{argValue}'");
            }
        }

        // 関数本体実行
        if (function.body.Count > 0)
        {
            if (debugMode)
            {
                Debug.Log($"関数本体実行開始: {functionName} ({function.body.Count}行)");
            }
            ExecuteLines(function.body.ToArray(), 0, out int _);
        }

        if (debugMode)
        {
            Debug.Log($"ユーザー関数実行完了: {functionName}");
        }
    }

    private List<string> ParseArguments(string argsString)
    {
        var args = new List<string>();
        if (string.IsNullOrEmpty(argsString)) return args;

        if (debugMode)
        {
            Debug.Log($"引数解析開始: '{argsString}'");
        }

        var argArray = argsString.Split(',');
        foreach (var arg in argArray)
        {
            string trimmedArg = arg.Trim();
            string processedArg = GetVariableValue(trimmedArg);
            args.Add(processedArg);

            if (debugMode)
            {
                Debug.Log($"引数処理: '{trimmedArg}' -> '{processedArg}'");
            }
        }

        return args;
    }
}