using UnityEngine;

/// <summary>
/// シーン開始時に指定 BGM を AudioManager に再生させるコンポーネント。
/// Title・Menu シーンのルートオブジェクトにアタッチして bgmClip を設定するだけで動作する。
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Tooltip("このシーンで流す BGM（A トラック）")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("設定した場合 A→B→A... と交互にループ再生。タイトル・メニュー用。")]
    [SerializeField] private AudioClip bgmClipB;

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        if (bgmClipB != null)
            AudioManager.Instance.PlayBGMAB(bgmClip, bgmClipB);
        else
            AudioManager.Instance.PlayBGM(bgmClip);
    }
}
