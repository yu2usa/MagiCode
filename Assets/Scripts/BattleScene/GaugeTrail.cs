using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ゲージ（Slider）の値が減ったとき、減る前の位置から灰色の跡が少し遅れて追いかける演出。
/// 回復したときは跡もすぐに合わせる
/// </summary>
[RequireComponent(typeof(Slider))]
public class GaugeTrail : MonoBehaviour
{
    [Tooltip("Fill の後ろに置いた跡の画像（Fill と同じ形）")]
    [SerializeField] private RectTransform trail;
    [Tooltip("減ってから跡が動き出すまでの時間（秒）")]
    [SerializeField] private float holdTime = 0.4f;
    [Tooltip("跡が追いつくまでの時間（秒）")]
    [SerializeField] private float followDuration = 0.5f;

    private Slider _slider;
    private float _shown;       // 跡が今示している割合（0〜1）
    private float _chaseTarget; // 跡が向かっている割合
    private Tween _tween;

    void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    // 表示されていない間の変化は追いかけず、表示した時点の値にそろえる
    void OnEnable()
    {
        _tween?.Kill();
        _shown = _chaseTarget = _slider.normalizedValue;
        Apply();
    }

    void LateUpdate()
    {
        float target = _slider.normalizedValue;
        if (target == _chaseTarget) return;

        _chaseTarget = target;
        _tween?.Kill();

        if (target > _shown)
        {
            _shown = target;
            Apply();
            return;
        }

        _tween = DOTween.To(() => _shown, v => { _shown = v; Apply(); }, target, followDuration)
            .SetDelay(holdTime)
            .SetEase(Ease.InOutQuad)
            .SetLink(gameObject);
    }

    // Slider が Fill を伸び縮みさせるのと同じく、右端のアンカーだけを動かす
    private void Apply()
    {
        trail.anchorMax = new Vector2(_shown, trail.anchorMax.y);
    }
}
