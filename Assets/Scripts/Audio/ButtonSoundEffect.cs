using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンにアタッチしてホバー音・クリック音を付与するコンポーネント。
/// Button.onClick に登録するため IPointerClickHandler より確実に動作する。
/// </summary>
public class ButtonSoundEffect : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("マウスを合わせたときの効果音")]
    [SerializeField] private AudioClip hoverClip;
    [Tooltip("ボタンを押したときの効果音")]
    [SerializeField] private AudioClip clickClip;

    private void Start()
    {
        // 自身または親の Button を取得（子オブジェクトにアタッチした場合も対応）
        var button = GetComponentInParent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"[ButtonSoundEffect] Button コンポーネントが見つかりません: {gameObject.name}", gameObject);
            return;
        }

        button.onClick.AddListener(PlayClickSound);
        Debug.Log($"[ButtonSoundEffect] '{button.gameObject.name}' の onClick に登録しました (clickClip={clickClip?.name ?? "未設定"})", gameObject);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (hoverClip == null) return;
        AudioManager.Instance?.PlaySE(hoverClip);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[ButtonSoundEffect] AudioManager が見つかりません。AudioManager を含むシーンから開始してください。", gameObject);
            return;
        }
        if (clickClip == null)
        {
            Debug.LogWarning($"[ButtonSoundEffect] clickClip が未設定です: {gameObject.name}", gameObject);
            return;
        }
        AudioManager.Instance.PlaySE(clickClip);
        Debug.Log($"[ButtonSoundEffect] SE再生: {clickClip.name}", gameObject);
    }
}
