using System.Collections.Generic;

/// <summary>
/// EasySave3を使ってクリア済みステージ名・取得済みキーワードステージを永続化する静的マネージャー
/// </summary>
public static class ClearDataManager
{
    private const string SaveKey         = "clearedStages";
    private const string KeywordsKey     = "obtainedKeywordStages";
    private const string TutorialKey     = "menuTutorialSeen";

    public static List<string> GetClearedStages()
    {
        return ES3.Load<List<string>>(SaveKey, defaultValue: new List<string>());
    }

    public static bool IsStageClear(string stageName)
    {
        return GetClearedStages().Contains(stageName);
    }

    public static void MarkStageClear(string stageName)
    {
        var cleared = GetClearedStages();
        if (cleared.Contains(stageName)) return;

        cleared.Add(stageName);
        ES3.Save(SaveKey, cleared);
    }

    // --- キーワード取得済み（ステージ入場時に保存） ---

    public static bool IsKeywordsObtained(string stageName)
    {
        return ES3.Load<List<string>>(KeywordsKey, defaultValue: new List<string>()).Contains(stageName);
    }

    public static void MarkKeywordsObtained(string stageName)
    {
        var obtained = ES3.Load<List<string>>(KeywordsKey, defaultValue: new List<string>());
        if (obtained.Contains(stageName)) return;

        obtained.Add(stageName);
        ES3.Save(KeywordsKey, obtained);
    }

    // --- メニューチュートリアル既読フラグ ---

    public static bool IsMenuTutorialSeen()
    {
        return ES3.Load<bool>(TutorialKey, defaultValue: false);
    }

    public static void MarkMenuTutorialSeen()
    {
        ES3.Save(TutorialKey, true);
    }

    /// <summary>
    /// チュートリアル既読フラグをリセットする（データセーブセクションの「再生」ボタン用）
    /// </summary>
    public static void ResetMenuTutorial()
    {
        ES3.Save(TutorialKey, false);
    }

    // --- 全データ一括クリア ---

    /// <summary>
    /// クリア済みステージ・取得済みキーワード・チュートリアル既読フラグをすべて削除する。
    /// データセーブセクションの「全データクリア」ボタンから呼ぶ。
    /// </summary>
    public static void ClearAllData()
    {
        ES3.DeleteKey(SaveKey);
        ES3.DeleteKey(KeywordsKey);
        ES3.DeleteKey(TutorialKey);
    }
}
