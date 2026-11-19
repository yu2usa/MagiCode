using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// コマごとに大きさが違うドット絵のアニメーションを UI の Image で表示するためのもの。
/// Image はコマを枠いっぱいに引き伸ばすので、1ピクセルはみ出すだけのコマでも絵全体が縮んでぶれる。
/// そこで毎フレーム、枠の大きさをコマの大きさ × 1ピクセルの大きさにし、コマの基準点（足元）を同じ場所に置く
/// </summary>
[RequireComponent(typeof(Image))]
public class ImagePixelFit : MonoBehaviour
{
    [Tooltip("絵の1ピクセルを画面上で何単位の大きさにするか")]
    [SerializeField] private float pixelScale = 1f;

    private Image _image;
    private RectTransform _rect;

    void Awake()
    {
        _image = GetComponent<Image>();
        _rect = (RectTransform)transform;
    }

    // Animator が絵を差し替えた後に合わせる
    void LateUpdate()
    {
        Sprite sprite = _image.sprite;
        _rect.pivot = sprite.pivot / sprite.rect.size;
        _rect.sizeDelta = sprite.rect.size * pixelScale;
    }
}
