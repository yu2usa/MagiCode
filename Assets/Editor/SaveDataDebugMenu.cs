using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// エディター専用のセーブデータ操作メニュー。
/// Unity メニューバー「MagiCode > Debug」から実行できる。
/// </summary>
public static class SaveDataDebugMenu
{
    private const string SaveKey = "clearedStages";

    /// <summary>
    /// セーブデータをすべて削除する（クリア済み・キーワード・チュートリアルフラグ）
    /// </summary>
    [MenuItem("MagiCode/Debug/セーブデータを全て削除（クリア・キーワード・チュートリアル）")]
    private static void DeleteAllSaveData()
    {
        if (!EditorUtility.DisplayDialog(
            "セーブデータ全削除",
            "クリア済みステージ・取得済みキーワード・チュートリアル既読フラグをすべて削除します。\n完全な初期状態に戻ります。\n\nよろしいですか？",
            "全て削除する", "キャンセル")) return;

        ClearDataManager.ClearAllData();
        Debug.Log("[SaveDataDebug] セーブデータを全て削除しました（クリア済み・キーワード・チュートリアル）");
    }

    /// <summary>
    /// クリアデータをすべて削除する（ステージ1のみ解放の初期状態に戻る）
    /// </summary>
    [MenuItem("MagiCode/Debug/クリアデータを全削除（ステージロック初期化）")]
    private static void ClearAllSaveData()
    {
        if (!EditorUtility.DisplayDialog(
            "クリアデータ全削除",
            "クリア済みステージのデータをすべて削除します。\nステージ1のみ解放の初期状態に戻ります。\n\nよろしいですか？",
            "削除する", "キャンセル")) return;

        if (ES3.KeyExists(SaveKey))
        {
            ES3.DeleteKey(SaveKey);
            Debug.Log("[SaveDataDebug] クリアデータを全削除しました");
        }
        else
        {
            Debug.Log("[SaveDataDebug] 削除するクリアデータがありません");
        }
    }

    /// <summary>
    /// 現在のクリアデータをConsoleに表示する
    /// </summary>
    [MenuItem("MagiCode/Debug/クリアデータを確認")]
    private static void ShowSaveData()
    {
        if (!ES3.KeyExists(SaveKey))
        {
            Debug.Log("[SaveDataDebug] クリアデータなし（ステージ1のみ解放の初期状態）");
            return;
        }

        var cleared = ES3.Load<System.Collections.Generic.List<string>>(SaveKey);
        Debug.Log($"[SaveDataDebug] クリア済みステージ（{cleared.Count}件）:\n" +
                  string.Join("\n", cleared));
    }

    /// <summary>
    /// まだクリアされていない最初のステージを1つクリア済みにする。
    /// 押すたびに順番に次のステージが解放される。
    /// ステージの順序は StageConfig アセット名のアルファベット順で決まる。
    /// </summary>
    [MenuItem("MagiCode/Debug/次のステージをクリア済みにする（順番に解放）")]
    private static void MarkNextStageCleared()
    {
        var guids = AssetDatabase.FindAssets("t:StageConfig");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[SaveDataDebug] StageConfig アセットが見つかりません");
            return;
        }

        // アセット名のアルファベット順に並べる（S_Config_1, S_Config_2… の命名規則に対応）
        var configs = new System.Collections.Generic.List<StageConfig>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (config != null && !string.IsNullOrEmpty(config.stageName))
                configs.Add(config);
        }
        configs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        // まだクリアされていない最初のステージを探してクリア済みにする
        var cleared = ES3.KeyExists(SaveKey)
            ? ES3.Load<System.Collections.Generic.List<string>>(SaveKey)
            : new System.Collections.Generic.List<string>();

        StageConfig target = null;
        foreach (var config in configs)
        {
            if (!cleared.Contains(config.stageName))
            {
                target = config;
                break;
            }
        }

        if (target == null)
        {
            Debug.Log("[SaveDataDebug] 全ステージがクリア済みです");
            return;
        }

        cleared.Add(target.stageName);
        ES3.Save(SaveKey, cleared);
        Debug.Log($"[SaveDataDebug] 「{target.stageName}」をクリア済みにしました（クリア済み: {cleared.Count}/{configs.Count}件）");
    }

    /// <summary>
    /// 最後にクリア済みにしたステージを1つ取り消す（順番に1つ戻す）
    /// </summary>
    [MenuItem("MagiCode/Debug/直前のクリアを取り消す（1つ戻す）")]
    private static void UnmarkLastCleared()
    {
        if (!ES3.KeyExists(SaveKey))
        {
            Debug.Log("[SaveDataDebug] クリアデータがありません");
            return;
        }

        var cleared = ES3.Load<System.Collections.Generic.List<string>>(SaveKey);
        if (cleared.Count == 0)
        {
            Debug.Log("[SaveDataDebug] クリアデータが空です");
            return;
        }

        string removed = cleared[cleared.Count - 1];
        cleared.RemoveAt(cleared.Count - 1);

        if (cleared.Count == 0)
            ES3.DeleteKey(SaveKey);
        else
            ES3.Save(SaveKey, cleared);

        Debug.Log($"[SaveDataDebug] 「{removed}」のクリアを取り消しました（残り: {cleared.Count}件）");
    }

    /// <summary>
    /// ウィンドウを開いて「どのステージまで」クリア済みにするかを選ぶ
    /// </summary>
    [MenuItem("MagiCode/Debug/特定ステージまでクリア済みにする...")]
    private static void OpenMarkUpToStageWindow()
    {
        MarkUpToStageWindow.Open();
    }

    /// <summary>
    /// 指定ステージをクリア済みとしてマークする（解放テスト用）
    /// </summary>
    [MenuItem("MagiCode/Debug/全ステージをクリア済みにする（全解放テスト）")]
    private static void MarkAllCleared()
    {
        // StageConfig アセットをすべて検索してクリア済みに登録
        var guids = AssetDatabase.FindAssets("t:StageConfig");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[SaveDataDebug] StageConfig アセットが見つかりません");
            return;
        }

        var names = new System.Collections.Generic.List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (config != null && !string.IsNullOrEmpty(config.stageName))
                names.Add(config.stageName);
        }

        ES3.Save(SaveKey, names);
        Debug.Log($"[SaveDataDebug] {names.Count}件のステージをクリア済みに設定しました:\n" +
                  string.Join("\n", names));
    }
}

/// <summary>
/// 「特定ステージまでクリア済みにする」操作ウィンドウ。
/// ドロップダウンでステージを選択し、ステージ1からそこまでを一括でクリア済みに設定する。
/// </summary>
public class MarkUpToStageWindow : EditorWindow
{
    private const string SaveKey = "clearedStages";

    private List<StageConfig> _configs;
    private int _selectedIndex;

    public static void Open()
    {
        var window = GetWindow<MarkUpToStageWindow>("ステージ指定クリア");
        window.minSize = new Vector2(320, 110);
        window.LoadConfigs();
    }

    private void LoadConfigs()
    {
        _configs = new List<StageConfig>();
        var guids = AssetDatabase.FindAssets("t:StageConfig");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (config != null && !string.IsNullOrEmpty(config.stageName))
                _configs.Add(config);
        }
        _configs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        // デフォルトは最後のステージ
        _selectedIndex = _configs.Count > 0 ? _configs.Count - 1 : 0;
    }

    private void OnGUI()
    {
        if (_configs == null || _configs.Count == 0)
        {
            EditorGUILayout.HelpBox("StageConfig アセットが見つかりません", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("ステージ1 〜 選択ステージ までクリア済みに設定します。",
                                   EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(6);

        var names = _configs.ConvertAll(c => c.stageName).ToArray();
        _selectedIndex = EditorGUILayout.Popup("クリア済みにするステージ", _selectedIndex, names);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("設定する", GUILayout.Height(28)))
        {
            var toMark = _configs.GetRange(0, _selectedIndex + 1);
            var stageNames = toMark.ConvertAll(c => c.stageName);
            ES3.Save(SaveKey, stageNames);
            Debug.Log($"[SaveDataDebug] ステージ1〜「{_configs[_selectedIndex].stageName}」まで" +
                      $"クリア済みに設定しました（{stageNames.Count}件）");
            Close();
        }
    }
}
