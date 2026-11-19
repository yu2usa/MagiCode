using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃が当たった瞬間に、ゲーム全体を数フレームだけ止める（ヒットストップ）。
/// 止まっている間は受けた側だけを左右に震わせ、当たった手応えを強くする。
/// 止める長さはダメージに比例させ、倒しきる一撃はさらに長くする
/// </summary>
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    [SerializeField] private BalanceConfig balance;

    // 揺らしているキャラと、揺らす前の位置
    private readonly Dictionary<Transform, Vector3> _origins = new Dictionary<Transform, Vector3>();
    // いつ（実時間）から いつまで止めるか。止めている間に次が当たったら長い方に延ばす
    private float _start;
    private float _until;
    private Coroutine _freeze;
    private float _savedTimeScale;

    /// <summary>
    /// 止まっている間か。受けた側のリアクション（のけぞり・HPの減少）は、止まり終わってから始める
    /// </summary>
    public bool IsStopped => _freeze != null;

    void Awake()
    {
        Instance = this;
    }

    public void Play(int damage, Transform target, bool isFinishing)
    {
        int frames = isFinishing
            ? balance.finishingHitStopFrames
            : Mathf.Min(damage * balance.hitStopFramesPerDamage, balance.maxHitStopFrames);

        if (!_origins.ContainsKey(target))
            _origins[target] = target.localPosition;
        if (_freeze == null)
            _start = Time.unscaledTime;
        _until = Mathf.Max(_until, Time.unscaledTime + frames / 60f);
        if (_freeze == null)
            _freeze = StartCoroutine(Freeze());
    }

    /// <summary>
    /// 止まり終わるまで待つ（受けた側のダメージ処理の最初に呼ぶ）
    /// </summary>
    public IEnumerator WaitUntilResumed()
    {
        while (IsStopped)
            yield return null;
    }

    // とどめのスロー演出中に当たることもあるので、止める前の速さを覚えて戻す。
    // 揺れは大きく始めて収束させ、最後に全員を元の位置へ戻してから時間を動かす
    private IEnumerator Freeze()
    {
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        float side = 1f;
        while (Time.unscaledTime < _until)
        {
            float remaining = (_until - Time.unscaledTime) / (_until - _start);
            foreach (var pair in _origins)
                pair.Key.localPosition = pair.Value + Vector3.right * (side * balance.hitStopShake * remaining);
            side = -side;
            yield return null;
        }
        foreach (var pair in _origins)
            pair.Key.localPosition = pair.Value;
        _origins.Clear();
        Time.timeScale = _savedTimeScale;
        _freeze = null;
    }

    // 止めている途中でシーンを離れても、ゲームが止まったままにならないようにする
    void OnDestroy()
    {
        if (_freeze != null)
            Time.timeScale = _savedTimeScale;
    }
}
