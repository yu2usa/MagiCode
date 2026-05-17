using TMPro;
using UnityEngine;

public class CodeInputInterpolation : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;


    public void AddIfStatement()
    {
        
    }

    void Update()
    {
        int currentLine = GetCaretLineNumber();
        Debug.Log("現在の行: " + currentLine);
    }

    public int GetCaretLineNumber()
    {
        if (inputField == null || inputField.textComponent == null)
            return 0;

        // TextMeshProのテキスト情報を取得
        TMP_TextInfo textInfo = inputField.textComponent.textInfo;
        int caretPosition = inputField.caretPosition;

        // テキストが空の場合は1行目
        if (textInfo.characterCount == 0)
            return 1;

        // カーソル位置が範囲外の場合の処理
        if (caretPosition >= textInfo.characterCount)
            caretPosition = textInfo.characterCount - 1;

        // カーソル位置の文字情報から行番号を取得
        if (caretPosition >= 0 && caretPosition < textInfo.characterCount)
        {
            return textInfo.characterInfo[caretPosition].lineNumber + 1; // 1から始まる行番号
        }

        return 1;
    }

    // より詳細な情報を取得
    public void GetDetailedCaretInfo()
    {
        if (inputField == null || inputField.textComponent == null)
            return;

        TMP_TextInfo textInfo = inputField.textComponent.textInfo;
        int caretPosition = inputField.caretPosition;

        if (caretPosition >= 0 && caretPosition < textInfo.characterCount)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[caretPosition];

            Debug.Log($"行番号: {charInfo.lineNumber + 1}");
            Debug.Log($"文字インデックス: {charInfo.index}");
            Debug.Log($"行内の位置: {caretPosition - textInfo.lineInfo[charInfo.lineNumber].firstCharacterIndex}");
        }
    }

    // 総行数を取得
    public int GetTotalLineCount()
    {
        if (inputField == null || inputField.textComponent == null)
            return 0;

        return inputField.textComponent.textInfo.lineCount;
    }

    // 指定した行の開始位置を取得
    public int GetLineStartPosition(int lineIndex)
    {
        if (inputField == null || inputField.textComponent == null)
            return 0;

        TMP_TextInfo textInfo = inputField.textComponent.textInfo;

        if (lineIndex >= 0 && lineIndex < textInfo.lineCount)
        {
            return textInfo.lineInfo[lineIndex].firstCharacterIndex;
        }

        return 0;
    }

    // 指定した行の終了位置を取得
    public int GetLineEndPosition(int lineIndex)
    {
        if (inputField == null || inputField.textComponent == null)
            return 0;

        TMP_TextInfo textInfo = inputField.textComponent.textInfo;

        if (lineIndex >= 0 && lineIndex < textInfo.lineCount)
        {
            return textInfo.lineInfo[lineIndex].lastCharacterIndex;
        }

        return 0;
    }
}