using UnityEngine;

/// <summary>
/// チュートリアル用ヒント1件分のデータ。
/// StageConfig に登録してフェーズ開始時にアシスタントが発言する。
/// </summary>
[System.Serializable]
public class TutorialHint
{
    [Tooltip("対象のプレイヤーターン（1始まり）。0を指定すると全ターンで表示される")]
    public int playerTurn;

    [Tooltip("対象フェーズ（Attack=攻撃, Defense=防御）")]
    public Phase phase;

    [Tooltip("アシスタントのセリフ")]
    [TextArea(1, 3)]
    public string message;
}
