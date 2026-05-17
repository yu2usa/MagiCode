using System;
using System.Collections.Generic;
using UnityEngine;

public class OldModeraInterpreter : MonoBehaviour
{
    // 変数を格納する辞書
    // 変数名をキー、値をオブジェクト型で格納
    // object型にすることで、文字列や配列など様々な型を扱えるようにする

    // クラス変数として明示的に初期化
    private Dictionary<string, object> variables = new Dictionary<string, object>();

    // 無限ループ防止用
    private const int MAX_EXECUTIONS = 1000;
    private int executedCount = 0;

    // デバッグ用フラグ
    private bool debugMode = true; // デバッグを有効に

    public void Run(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("コードが空です");
            return;
        }

        try
        {
            // 変数をクリアする
            variables.Clear();
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

    // 行のリストを実行する再帰メソッド（多重ループに対応）
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

            if (string.IsNullOrWhiteSpace(line) || line == "{" || line == "}")
            {
                // 空行と括弧はスキップ
                i++;
                continue;
            }

            // ループ命令を検出
            if (line.StartsWith("loop") && line.Contains("(") && line.Contains(")"))
            {
                if (debugMode)
                {
                    Debug.Log($"ループ命令を検出: {line}");
                }

                int loopCount = ParseLoopCount(line);
                i++; // ブロック開始に移動

                // インデックスが範囲外になっていないか確認
                if (i >= lines.Length)
                {
                    endIndex = i;
                    return;
                }

                // ブロックの開始行
                int blockStartLine = i;

                // ブロック内容を抽出（ブロック終了後のインデックスを取得）
                ExtractBlockLines(lines, ref i, out int blockEndIndex);

                // 処理するためにブロック内容を配列に変換
                string[] blockLines = GetBlockLines(lines, blockStartLine, blockEndIndex);

                if (debugMode)
                {
                    Debug.Log($"Loop {loopCount} times, block from line {blockStartLine} to {blockEndIndex}");
                    if (blockLines != null && blockLines.Length > 0)
                    {
                        Debug.Log($"Block content: {string.Join(", ", blockLines)}");
                    }
                }

                // ループを実行
                for (int j = 0; j < loopCount && executedCount < MAX_EXECUTIONS; j++)
                {
                    if (debugMode)
                    {
                        Debug.Log($"ループ実行 {j + 1}/{loopCount}");
                    }

                    if (blockLines != null && blockLines.Length > 0)
                    {
                        // 再帰的に実行（戻り値は無視）
                        ExecuteLines(blockLines, 0, out int _);
                    }
                }

                // ブロックの終了位置の次に進む（i は既に更新済み）
                continue;
            }
            else if (line.StartsWith("if") && line.Contains("(") && line.Contains(")"))
            {
                if (debugMode)
                {
                    Debug.Log($"条件分岐を検出: {line}");
                }

                bool condition = EvaluateCondition(line);
                i++; // ブロック開始に移動

                // インデックスが範囲外になっていないか確認
                if (i >= lines.Length)
                {
                    endIndex = i;
                    return;
                }

                // ブロックの開始行
                int blockStartLine = i;

                // ブロック内容を抽出（ブロック終了後のインデックスを取得）
                ExtractBlockLines(lines, ref i, out int blockEndIndex);

                // 処理するためにブロック内容を配列に変換
                string[] blockLines = GetBlockLines(lines, blockStartLine, blockEndIndex);

                if (debugMode)
                {
                    Debug.Log($"If condition evaluated to {condition}, block from line {blockStartLine} to {blockEndIndex}");
                }

                // 条件が真なら実行
                if (condition && blockLines != null && blockLines.Length > 0)
                {
                    // 再帰的に実行（戻り値は無視）
                    ExecuteLines(blockLines, 0, out int _);
                }

                // ブロックの終了位置の次に進む（i は既に更新済み）
                continue;
            }
            else
            {
                ExecuteCommand(line);
                i++;
            }
        }

        endIndex = i;
    }

    // ブロック内容を配列として取得
    private string[] GetBlockLines(string[] lines, int start, int end)
    {
        if (start >= end || start < 0 || end > lines.Length)
        {
            return new string[0];
        }

        // 波括弧を除いた実際のコード行だけを抽出
        List<string> result = new List<string>();
        for (int i = start; i < end; i++)
        {
            string line = lines[i].Trim();
            if (line != "{" && line != "}" && !string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
            }
        }

        return result.ToArray();
    }

    // ブロック内容を抽出し、ブロック終了後のインデックスを返す
    private void ExtractBlockLines(string[] lines, ref int index, out int endIndex)
    {
        if (lines == null || index < 0 || index >= lines.Length)
        {
            endIndex = index;
            return;
        }

        int depth = 0;
        int startIndex = index;

        // 最初の行が { かどうかチェック
        if (index < lines.Length && lines[index].Trim() == "{")
        {
            depth++;
            index++;
        }
        else
        {
            // 開始ブレースがない場合は次の1行だけを含む
            if (index < lines.Length)
            {
                index++;
            }
            endIndex = index;
            return;
        }

        int maxLines = 200; // 安全装置
        int lineCount = 0;

        while (index < lines.Length && lineCount < maxLines)
        {
            lineCount++;
            string line = lines[index].Trim();

            if (line == "{")
            {
                depth++;
                index++;
            }
            else if (line == "}")
            {
                depth--;
                index++;

                if (depth <= 0)
                {
                    // ブロック終了
                    break;
                }
            }
            else
            {
                index++;
            }
        }

        if (depth > 0)
        {
            Debug.LogWarning($"括弧の不一致: 行 {startIndex} から始まるブロックが正しく閉じられていません");
            // 念のため進める
            index = startIndex + 1;
        }

        if (lineCount >= maxLines)
        {
            Debug.LogWarning("ブロック抽出中に最大行数に達しました");
            // 念のため進める
            index = startIndex + 1;
        }

        endIndex = index;
    }

    int ParseLoopCount(string line)
    {
        if (string.IsNullOrEmpty(line)) return 0;

        try
        {
            int start = line.IndexOf('(');
            if (start < 0) return 0;
            start += 1;

            int end = line.IndexOf(')', start);
            if (end < start) return 0;

            string numStr = line.Substring(start, end - start).Trim();
            if (string.IsNullOrEmpty(numStr)) return 0;

            // 変数参照の場合
            if (variables.ContainsKey(numStr) && variables[numStr] is string strVal)
            {
                if (int.TryParse(strVal, out int val))
                    return Math.Max(0, Math.Min(val, 100)); // 安全のため100回に制限
            }

            // 直接数値の場合
            if (int.TryParse(numStr, out int count))
            {
                // 負の値や極端に大きい値を防止
                return Math.Max(0, Math.Min(count, 100)); // 安全のため100回に制限
            }

            Debug.LogWarning($"無効なループ回数: {numStr}、デフォルトで0になります");
            return 0;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ループ回数の解析エラー: {ex.Message}");
            return 0;
        }
    }

    bool EvaluateCondition(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;

        try
        {
            int start = line.IndexOf('(');
            if (start < 0) return false;
            start += 1;

            int end = line.LastIndexOf(')');
            if (end < start) return false;

            string condition = line.Substring(start, end - start).Trim();
            if (string.IsNullOrEmpty(condition)) return false;

            // ==, != 演算子をサポート
            if (condition.Contains("=="))
            {
                string[] parts = condition.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length != 2) return false;

                string left = parts[0].Trim();
                string right = parts[1].Trim().Trim('"');

                object leftValue = GetVariableValue(left);
                object rightValue = GetVariableValue(right);

                if (leftValue == null || rightValue == null) return false;

                return leftValue.ToString() == rightValue.ToString();
            }
            else if (condition.Contains("!="))
            {
                string[] parts = condition.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length != 2) return false;

                string left = parts[0].Trim();
                string right = parts[1].Trim().Trim('"');

                object leftValue = GetVariableValue(left);
                object rightValue = GetVariableValue(right);

                if (leftValue == null || rightValue == null) return false;

                return leftValue.ToString() != rightValue.ToString();
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"条件評価エラー: {ex.Message}");
            return false;
        }
    }

    object GetVariableValue(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // 変数参照
        if (variables.ContainsKey(name))
        {
            return variables[name];
        }

        // リテラル値
        return name;
    }

    void ExecuteCommand(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        // 括弧だけの行はスキップ
        if (line == "{" || line == "}")
        {
            return;
        }

        executedCount++;

        try
        {
            if (line.StartsWith("Attack") && line.Contains("(") && line.Contains(")"))
            {
                // 複数の引数を処理するよう修正
                List<string> args = ExtractArgs(line);

                if (args.Count >= 2)
                {
                    string enemyType = args[0];
                    string weaponType = args[1];

                    Debug.Log($"Attacking {enemyType} with {weaponType}!");
                }
                else if (args.Count == 1)
                {
                    // 後方互換性のため、単一引数も処理
                    string arg = args[0];
                    Debug.Log($"Attacking with {arg}!");
                }
                else
                {
                    Debug.LogWarning("Attack command requires at least one argument");
                }
            }
            else if (line.StartsWith("Set("))
            {
                int start = line.IndexOf('(');
                if (start < 0) return;
                start += 1;

                int end = line.LastIndexOf(')');
                if (end < start) return;

                string args = line.Substring(start, end - start);
                if (string.IsNullOrEmpty(args)) return;

                // カンマの位置を特定
                int commaPos = FindOuterComma(args);
                if (commaPos < 0) return;

                string key = args.Substring(0, commaPos).Trim();
                string value = args.Substring(commaPos + 1).Trim();

                if (string.IsNullOrEmpty(key)) return;

                // 配列の場合
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    string content = value.Substring(1, value.Length - 2);
                    // 入れ子配列に対応するため分割方法を改善
                    var elements = SplitByOuterCommas(content);

                    List<string> list = new List<string>();
                    foreach (var elem in elements)
                    {
                        if (elem != null)
                        {
                            list.Add(elem.Trim().Trim('"'));
                        }
                    }

                    variables[key] = list;

                    if (debugMode)
                    {
                        Debug.Log($"配列を設定: '{key}' = [{string.Join(", ", list)}]");
                    }
                }
                else
                {
                    // 単一値
                    variables[key] = value.Trim('"');

                    if (debugMode)
                    {
                        Debug.Log($"変数を設定: '{key}' = \"{value.Trim('"')}\"");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"未知のコマンド: {line}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"コマンド実行エラー '{line}': {ex.Message}");
        }
    }

    // 複数の引数を抽出するメソッド
    List<string> ExtractArgs(string line)
    {
        List<string> result = new List<string>();

        try
        {
            int start = line.IndexOf('(');
            if (start < 0) return result;
            start += 1;

            int end = line.LastIndexOf(')');
            if (end < start) return result;

            string argsStr = line.Substring(start, end - start).Trim();
            if (string.IsNullOrEmpty(argsStr)) return result;

            // カンマで分割（入れ子構造に対応）
            var args = SplitByOuterCommas(argsStr);

            foreach (var arg in args)
            {
                string processedArg = ExtractValue(arg.Trim());
                result.Add(processedArg);
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"引数抽出エラー: {ex.Message}");
            return result;
        }
    }

    // 入れ子構造に対応したカンマ区切り
    List<string> SplitByOuterCommas(string input)
    {
        List<string> result = new List<string>();
        int depth = 0;
        int startIndex = 0;

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
                // 外側のカンマを見つけた
                result.Add(input.Substring(startIndex, i - startIndex));
                startIndex = i + 1;
            }
        }

        // 最後の部分を追加
        if (startIndex < input.Length)
        {
            result.Add(input.Substring(startIndex));
        }

        return result;
    }

    // 外側のカンマを見つける
    int FindOuterComma(string input)
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

    string ExtractArg(string line)
    {
        if (string.IsNullOrEmpty(line)) return "";

        try
        {
            int start = line.IndexOf('(');
            if (start < 0) return "";
            start += 1;

            int end = line.LastIndexOf(')');
            if (end < start) return "";

            string arg = line.Substring(start, end - start).Trim();
            return ExtractValue(arg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"引数抽出エラー: {ex.Message}");
            return "";
        }
    }

    string ExtractValue(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return "";

        expression = expression.Trim();

        // 配列アクセス対応: 例) Enemies[0]
        if (expression.Contains("[") && expression.EndsWith("]"))
        {
            int bracketStart = expression.IndexOf('[');
            string varName = expression.Substring(0, bracketStart).Trim();
            string indexStr = expression.Substring(bracketStart + 1, expression.Length - bracketStart - 2).Trim();

            int index;
            if (!int.TryParse(indexStr, out index))
            {
                Debug.LogWarning($"無効な配列インデックス: {indexStr}");
                return "";
            }

            if (variables.ContainsKey(varName) && variables[varName] is List<string> list)
            {
                if (index >= 0 && index < list.Count)
                {
                    return list[index];
                }
                else
                {
                    Debug.LogWarning($"配列インデックスが範囲外です: {varName}[{index}], 配列の長さ: {list.Count}");
                    return "";
                }
            }

            Debug.LogWarning($"配列が見つかりません: {varName}");
            return "";
        }

        // 単一変数参照
        if (variables.ContainsKey(expression))
        {
            if (variables[expression] is string strVal)
                return strVal;
            else if (variables[expression] is List<string> list)
                return $"[長さ{list.Count}の配列]";
        }

        // そのまま返す
        return expression;
    }
}