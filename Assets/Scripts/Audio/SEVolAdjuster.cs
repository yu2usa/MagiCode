using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title シーンの SettingWindow > SEVolAdjuster (Slider) にアタッチする。
/// 起動時に保存済みの音量をスライダーに反映し、操作すると AudioManager と PlayerPrefs に即時保存する。
/// AudioManager が存在しない場合は PlayerPrefs を直接参照するためシーン起動順に依存しない。
/// </summary>
[RequireComponent(typeof(Slider))]
public class SEVolAdjuster : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // AudioManager がいれば優先。いなければ PlayerPrefs から直接読む
        float saved = AudioManager.Instance != null
            ? AudioManager.Instance.SEVolume
            : PlayerPrefs.GetFloat("SEVolume", 1f);

        _slider.SetValueWithoutNotify(saved);
        _slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSEVolume(value);
        else
        {
            // AudioManager がいない場合でも設定だけ保存しておく
            PlayerPrefs.SetFloat("SEVolume", value);
            PlayerPrefs.Save();
        }
    }
}
