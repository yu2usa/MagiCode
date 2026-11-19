/// <summary>
/// メニューシーンからバトルシーンへ、選択されたステージ設定を渡す静的ブリッジ
/// </summary>
public static class StageLoader
{
    /// <summary>
    /// メニューで選択されたステージ。バトルシーン側で読み取り後にnullへリセットする
    /// </summary>
    public static StageConfig pendingStage;

    /// <summary>
    /// 直前のバトルでクリアしたステージ名。メニューに戻った際の解放アニメーション再生に使う
    /// </summary>
    public static string justClearedStageName;

    /// <summary>
    /// 直前のバトルで敗北したステージ名。メニューに戻った際のアドバイス表示に使う
    /// </summary>
    public static string justDefeatedStageName;

    /// <summary>
    /// 敗北画面の「もう一度」で持ち越すコード。null なら通常の入場（戦闘前の会話あり）
    /// </summary>
    public static string retryCode;

    /// <summary>
    /// ステージ名ごとの負けた回数（ゲームを起動している間だけ数える）。何度も負けたら救済のヒントを出す
    /// </summary>
    public static readonly System.Collections.Generic.Dictionary<string, int> defeatCounts =
        new System.Collections.Generic.Dictionary<string, int>();
}
