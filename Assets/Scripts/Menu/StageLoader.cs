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
}
