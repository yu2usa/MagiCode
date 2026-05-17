using System.Collections;
using UnityEngine;

/// <summary>
/// BGM・SE の再生と音量管理を担うシングルトン。
/// DontDestroyOnLoad でシーン間を通して常駐する。
/// Inspector で AudioSource を未割り当てでも自動生成するため設定漏れで例外が起きない。
/// 音量は PlayerPrefs に保存され次回起動時も引き継がれる。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // どのシーンから開始しても AudioManager が必ず存在するよう、ゲーム起動時に自動生成する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        new UnityEngine.GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private const string BGMVolumeKey = "BGMVolume";
    private const string SEVolumeKey  = "SEVolume";

    // AudioSource は常に AudioManager 自身に AddComponent して生成する。
    // Inspector からの参照設定は不可（別シーンのオブジェクトを誤参照してシーン遷移で破棄されるのを防ぐ）
    private AudioSource bgmSource;
    private AudioSource seSource;
    private Coroutine _bgmABCoroutine;

    public float BGMVolume { get; private set; } = 0.5f;
    public float SEVolume  { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inspector 未割り当て時は AudioSource を自動生成する（NullReferenceException 防止）
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
        }

        BGMVolume = PlayerPrefs.GetFloat(BGMVolumeKey, 0.5f);
        SEVolume  = PlayerPrefs.GetFloat(SEVolumeKey,  1f);

        bgmSource.volume = BGMVolume;
        seSource.volume  = 1f; // SE は PlayOneShot の volumeScale で制御
    }

    // ===================== BGM =====================

    /// <summary>BGM を単曲ループで再生する。同じクリップが再生中なら何もしない。</summary>
    public void PlayBGM(AudioClip clip)
    {
        StopABLoop();
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>clipA → clipB → clipA ... と交互にループ再生する。</summary>
    public void PlayBGMAB(AudioClip clipA, AudioClip clipB)
    {
        StopABLoop();
        bgmSource.loop = false;
        _bgmABCoroutine = StartCoroutine(ABLoop(clipA, clipB));
    }

    private IEnumerator ABLoop(AudioClip clipA, AudioClip clipB)
    {
        AudioClip[] clips = { clipA, clipB };
        int index = 0;
        while (true)
        {
            bgmSource.clip = clips[index];
            bgmSource.Play();
            yield return new WaitWhile(() => bgmSource.isPlaying);
            index = 1 - index;
        }
    }

    private void StopABLoop()
    {
        if (_bgmABCoroutine == null) return;
        StopCoroutine(_bgmABCoroutine);
        _bgmABCoroutine = null;
    }

    /// <summary>BGM を停止する。</summary>
    public void StopBGM()
    {
        StopABLoop();
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // ===================== SE =====================

    /// <summary>効果音を1回だけ再生する。</summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip, SEVolume);
    }

    // ===================== 音量設定 =====================

    /// <summary>BGM 音量を設定し PlayerPrefs に保存する（0〜1）。</summary>
    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        bgmSource.volume = BGMVolume;
        PlayerPrefs.SetFloat(BGMVolumeKey, BGMVolume);
        PlayerPrefs.Save();
    }

    /// <summary>SE 音量を設定し PlayerPrefs に保存する（0〜1）。</summary>
    public void SetSEVolume(float volume)
    {
        SEVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SEVolumeKey, SEVolume);
        PlayerPrefs.Save();
    }
}
