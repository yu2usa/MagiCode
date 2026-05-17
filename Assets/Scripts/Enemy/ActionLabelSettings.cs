using UnityEngine;
using TMPro;

/// <summary>
/// 敵の頭上行動ラベルのスタイル設定をまとめた ScriptableObject。
/// 1つのアセットを全 EnemyController に割り当てることで、
/// ここを変更するだけで全敵の行動ラベルを一括調整できる。
/// Create > MagiCode > Action Label Settings でアセット作成。
/// </summary>
[CreateAssetMenu(fileName = "ActionLabelSettings", menuName = "MagiCode/Action Label Settings")]
public class ActionLabelSettings : ScriptableObject
{
    [Tooltip("頭上の高さ（Y軸オフセット、ワールドスペース単位）")]
    public float offsetY = 1.2f;

    [Tooltip("フォントサイズ（ワールドスペース単位）")]
    public float fontSize = 0.4f;

    [Tooltip("テキストの色")]
    public Color color = Color.white;

    [Tooltip("フォント（未設定時は TMP デフォルト）")]
    public TMP_FontAsset font;
}
