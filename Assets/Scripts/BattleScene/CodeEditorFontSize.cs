using UnityEngine;
using TMPro;
using InGameCodeEditor;

/// <summary>
/// InGameCodeEditor の全テキストコンポーネントのフォントサイズを一括設定する
/// CodeEditor または その親 GameObject にアタッチして使用する
/// </summary>
[ExecuteAlways]
public class CodeEditorFontSize : MonoBehaviour
{
    [SerializeField] private float fontSize = 14f;

    private void Start() => Apply();

    // インスペクターで値を変えたとき即時反映
    private void OnValidate() => Apply();

    [ContextMenu("フォントサイズを適用")]
    public void Apply()
    {
        // CodeEditor コンポーネントを自身または子から取得
        var codeEditor = GetComponent<CodeEditor>() ?? GetComponentInChildren<CodeEditor>();
        if (codeEditor == null) return;

        // TMP_InputField 本体のフォントサイズ
        codeEditor.InputField.pointSize = fontSize;

        // CodeEditor の GameObject 内の TMP のみ対象（兄弟・親の UI は除外）
        foreach (var tmp in codeEditor.GetComponentsInChildren<TextMeshProUGUI>())
            tmp.fontSize = fontSize;
    }

    /// <summary>
    /// スクリプトからフォントサイズを変更する
    /// </summary>
    public void SetFontSize(float size)
    {
        fontSize = size;
        Apply();
    }
}
