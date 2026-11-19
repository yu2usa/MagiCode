using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// メニュー画面の背景に、画面の中央に一番近いステージの戦闘背景を出す。
/// ステージマップを横にスクロールして近いステージが変わったとき、またはステージのボタンを押したときに、
/// そのステージの壁紙へフェードで切り替える
/// </summary>
public class StageWallpaper : MonoBehaviour
{
    [SerializeField] private StageSelectManager stageSelect;
    [SerializeField] private ScrollRect scroll;
    [Tooltip("今の壁紙（切り替えのときは新しい壁紙がここでフェードインする）")]
    [SerializeField] private Image front;
    [Tooltip("切り替えの間、前の壁紙を後ろに残しておく画像")]
    [SerializeField] private Image back;
    [Tooltip("壁紙の明るさ。ステージボタンや線が見えるよう少し暗くする")]
    [SerializeField] private Color tint = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private float fadeDuration = 0.5f;

    private StageConfig _current;
    private float _lastScrollX;

    void Start()
    {
        _current = NearestStage();
        front.sprite = _current.menuWallpaper;
        front.color = tint;
        back.color = Color.clear;
        // ボタンを押したときのカメラの寄りでもスクロールの通知が来るので、
        // スクロールの位置が実際に動いたときだけ、中央に近いステージに合わせる
        _lastScrollX = scroll.horizontalNormalizedPosition;
        scroll.onValueChanged.AddListener(position =>
        {
            if (Mathf.Abs(position.x - _lastScrollX) < 0.001f) return;
            _lastScrollX = position.x;
            SwitchTo(NearestStage());
        });

        // ステージのボタンを押したら、そのステージの壁紙にする
        foreach (var entry in stageSelect.stageButtons)
        {
            StageConfig config = entry.stageConfig;
            entry.button.onClick.AddListener(() => SwitchTo(config));
        }
    }

    private void SwitchTo(StageConfig stage)
    {
        if (stage == _current) return;
        _current = stage;

        // 今見えている壁紙を後ろへ回し、その上で新しい壁紙をフェードインさせる
        back.sprite = front.sprite;
        back.color = front.color;
        front.DOKill();
        front.sprite = stage.menuWallpaper;
        front.color = new Color(tint.r, tint.g, tint.b, 0f);
        front.DOFade(tint.a, fadeDuration);
    }

    // カメラの中心に一番近いステージボタン。
    // スクロールは両端で止まり、端のステージは画面の中央まで来ないので、スクロールの位置（0〜1）を
    // 左端のボタンから右端のボタンまでの間に当てはめた点を「カメラの中心」とみなす
    private StageConfig NearestStage()
    {
        var buttons = stageSelect.stageButtons;
        float leftX = ButtonX(buttons[0]);
        float rightX = ButtonX(buttons[0]);
        foreach (var entry in buttons)
        {
            leftX = Mathf.Min(leftX, ButtonX(entry));
            rightX = Mathf.Max(rightX, ButtonX(entry));
        }
        float centerX = Mathf.Lerp(leftX, rightX, scroll.horizontalNormalizedPosition);

        StageConfig nearest = null;
        float best = float.MaxValue;
        foreach (var entry in buttons)
        {
            float distance = Mathf.Abs(ButtonX(entry) - centerX);
            if (distance >= best) continue;
            best = distance;
            nearest = entry.stageConfig;
        }
        return nearest;
    }

    // マップ（スクロールする中身）の上での、ボタンの中心の左右位置
    private float ButtonX(StageButtonEntry entry)
    {
        var rect = (RectTransform)entry.button.transform;
        return scroll.content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).x;
    }
}
