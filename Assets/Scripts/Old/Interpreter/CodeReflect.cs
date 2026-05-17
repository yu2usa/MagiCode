// === 元のコードとの互換性を保つメソッド ===using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardListUIManager : MonoBehaviour
{
    // プログラムの構造を表すクラス（内部クラスとして定義）
    [System.Serializable]
    public class ProgramLine
    {
        public enum LineType
        {
            Command,    // 単一コマンド（Slash(), Evade()など）
            IfStart,    // if文の開始
            IfEnd,      // if文の終了（}）
            LoopStart,  // loop文の開始
            LoopEnd     // loop文の終了（}）
        }

        public LineType lineType;
        public string content;      // 表示するテキスト
        public int indentLevel;     // インデントのレベル（0, 1, 2...）
        public string condition;    // if文の条件やloop文の回数など

        public ProgramLine(LineType type, string text, int indent = 0, string cond = "")
        {
            lineType = type;
            content = text;
            indentLevel = indent;
            condition = cond;
        }

        // パラメータなしのコンストラクタ（Unity Serialization用）
        public ProgramLine()
        {
            lineType = LineType.Command;
            content = "";
            indentLevel = 0;
            condition = "";
        }
    }

    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Card List")]
    [SerializeField] private List<ProgramLine> programLines = new List<ProgramLine>();

    [Header("Display Settings")]
    [SerializeField] private string indentString = "    "; // インデント用の文字列
    [SerializeField] private bool showLineNumbers = true;

    private List<ProgramLine> previousProgramLines = new List<ProgramLine>();
    private List<Transform> trackedChildren = new List<Transform>();

    void Start()
    {
        // 初期サンプルデータ
        InitializeSampleData();
        UpdateUI();
    }

    void Update()
    {
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

    // 初期サンプルデータを設定（ソートアルゴリズムのサンプル）
    private void InitializeSampleData()
    {
        programLines.Clear();

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

    // UIを更新
    public void UpdateUI()
    {
        if (targetText == null)
        {
            Debug.LogError("Target TextMeshPro is not assigned!");
            return;
        }

        // テキストを構築
        string displayText = "";
        for (int i = 0; i < programLines.Count; i++)
        {
            ProgramLine line = programLines[i];

            // 行番号の追加（オプション）
            if (showLineNumbers)
            {
                displayText += $"{i + 1:D2}  ";
            }

            // インデントの追加
            for (int j = 0; j < line.indentLevel; j++)
            {
                displayText += indentString;
            }

            // 内容の追加
            displayText += line.content;

            // 最後の行でない場合は改行
            if (i < programLines.Count - 1)
            {
                displayText += "\n";
            }
        }

        // TextMeshProに反映
        targetText.text = displayText;

        // 現在のリストを保存
        previousProgramLines = new List<ProgramLine>();
        foreach (ProgramLine line in programLines)
        {
            ProgramLine newLine = new ProgramLine(line.lineType, line.content, line.indentLevel, line.condition);
            previousProgramLines.Add(newLine);
        }
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

    // 現在のインデントレベルを取得
    private int GetCurrentIndentLevel()
    {
        int indentLevel = 0;
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

        return openBlocks;
    }

    // ブロックを閉じる（}を追加）
    public void CloseBlock()
    {
        // 最後の開きブロックを見つけてインデントレベルを取得
        int indentLevel = 0;
        int braceCount = 0;

        // 後ろから検索して、対応する開きブレースを見つける
        for (int i = programLines.Count - 1; i >= 0; i--)
        {
            if (programLines[i].content == "}")
            {
                braceCount++;
            }
            else if (programLines[i].content == "{")
            {
                if (braceCount == 0)
                {
                    indentLevel = programLines[i].indentLevel;
                    break;
                }
                braceCount--;
            }
        }

        programLines.Add(new ProgramLine(ProgramLine.LineType.IfEnd, "}", indentLevel));
        UpdateUI();
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
            }
        }
    }

    // === ソートアルゴリズム専用メソッド ===

    // バブルソートを生成
    public void CreateBubbleSort()
    {
        ClearProgram();
        AddLoopStatement("n-1");  // 外側のループ
        AddLoopStatement("n-i-1"); // 内側のループ
        AddIfStatement("arr[j] > arr[j+1]");  // 条件分岐
        AddCommand("Swap(arr[j], arr[j+1])"); // 交換処理
        CloseBlock(); // if文終了
        CloseBlock(); // 内側のループ終了
        CloseBlock(); // 外側のループ終了
    }

    // 選択ソートを生成
    public void CreateSelectionSort()
    {
        ClearProgram();
        AddLoopStatement("n-1");  // 外側のループ
        AddCommand("minIndex = i");
        AddLoopStatement("n"); // 内側のループ（i+1からn-1まで）
        AddIfStatement("arr[j] < arr[minIndex]");
        AddCommand("minIndex = j");
        CloseBlock(); // if文終了
        CloseBlock(); // 内側のループ終了
        AddCommand("Swap(arr[i], arr[minIndex])");
        CloseBlock(); // 外側のループ終了
    }

    // 挿入ソートを生成
    public void CreateInsertionSort()
    {
        ClearProgram();
        AddLoopStatement("n-1");  // i = 1 to n-1
        AddCommand("key = arr[i]");
        AddCommand("j = i - 1");
        AddLoopStatement("while j >= 0");  // while文として表現
        AddIfStatement("arr[j] > key");
        AddCommand("arr[j+1] = arr[j]");
        AddCommand("j = j - 1");
        CloseBlock(); // if文終了
        CloseBlock(); // while文終了
        AddCommand("arr[j+1] = key");
        CloseBlock(); // 外側のループ終了
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
}