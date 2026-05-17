using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// バトル前会話演出をまとめてスキップするボタン。
/// スキップボタンの GameObject にアタッチして使用する。
/// </summary>
public class DialogueSkipButton : MonoBehaviour
{
    [Tooltip("PreBattleDialogueManager がアタッチされた GameObject")]
    [SerializeField] private PreBattleDialogueManager dialogueManager;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => dialogueManager.SkipAll());
    }
}
