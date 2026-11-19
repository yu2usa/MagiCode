using UnityEngine;

/// <summary>
/// マウスの位置と画面中央をはさんで反対側（対角線上）へ、背景を少しずらすタイトル画面の奥行き演出。
/// 動かしたい背景の Image に付ける
/// </summary>
public class MouseParallax : MonoBehaviour
{
    [Tooltip("マウスが画面の端にあるときにずらす最大量（ピクセル）")]
    [SerializeField] private Vector2 maxOffset = new Vector2(16f, 9f);
    [Tooltip("マウスに追いつく速さ（大きいほどすぐ追いつく）")]
    [SerializeField] private float followSpeed = 4f;

    private RectTransform _rect;
    private Vector2 _basePos;

    void Awake()
    {
        _rect = (RectTransform)transform;
        _basePos = _rect.anchoredPosition;
    }

    void Update()
    {
        // 画面中央を (0,0)、端を ±1 としたマウスの位置。画面の外に出たときは端で止める
        Vector2 mouse = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height) * 2f - Vector2.one;
        mouse = Vector2.Max(Vector2.Min(mouse, Vector2.one), -Vector2.one);

        Vector2 target = _basePos - Vector2.Scale(mouse, maxOffset);
        // フレームレートに左右されずに、なめらかに追いかける
        _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, target, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
    }
}
