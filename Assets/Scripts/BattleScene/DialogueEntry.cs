using UnityEngine;

/// <summary>
/// バトル前会話の話者種別
/// </summary>
public enum DialogueSpeaker
{
    Player,
    Assistant,
    Enemy,
}

/// <summary>
/// バトル前会話の1セリフ分のデータ
/// </summary>
[System.Serializable]
public class DialogueEntry
{
    [Tooltip("話者")]
    public DialogueSpeaker speaker;

    [Tooltip("セリフ本文")]
    [TextArea(1, 4)]
    public string text;

    [Tooltip("セリフ開始時に話者スプライトを上下に2回バウンスさせる")]
    public bool bounce;

    [Header("キャラクター表示制御（このセリフで表示するキャラにチェック）")]
    [Tooltip("このセリフでキャラクター表示を変更する")]
    public bool changeVisibility;
    [Tooltip("プレイヤーを表示する")]
    public bool showPlayer;
    [Tooltip("アシスタントを表示する")]
    public bool showAssistant;
    [Tooltip("敵を表示する")]
    public bool showEnemy;

    [Header("敵スプライト選択")]
    [Tooltip("表示する敵のインデックス（0 = 1体目、1 = 2体目...）")]
    public int enemyIndex = 0;
}
