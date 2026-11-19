using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// コードキーワードと説明文のマスターデータ。
/// Create > MagiCode > Keyword Database でアセット作成。
/// StrategyBookWindowManager にアサインして使う。
/// </summary>
[CreateAssetMenu(fileName = "KeywordDatabase", menuName = "MagiCode/Keyword Database")]
public class KeywordDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("キーワード（StageConfig.obtainedKeywords の文字列と一致させる）")]
        public string keyword;
        [Tooltip("ホバー時に表示する説明文")]
        [TextArea(2, 5)]
        public string description;
        [Tooltip("戦闘中にクリックしたとき、コード入力欄へ貼り付ける例（空なら貼り付けない）")]
        [TextArea(2, 6)]
        public string snippet;
    }

    public List<Entry> entries = new List<Entry>();

    /// <summary>
    /// キーワードに対応する説明文を返す。未登録の場合は空文字。
    /// </summary>
    public string GetDescription(string keyword)
    {
        var entry = entries.Find(e => e.keyword == keyword);
        return entry != null ? entry.description : "";
    }

    /// <summary>
    /// キーワードの貼り付け用の例を返す。未登録・未設定なら空文字
    /// </summary>
    public string GetSnippet(string keyword)
    {
        var entry = entries.Find(e => e.keyword == keyword);
        return entry != null ? entry.snippet : "";
    }
}
