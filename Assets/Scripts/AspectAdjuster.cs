using UnityEngine;

/// <summary>
/// 画面の縦横比が変わっても、設計上の範囲（aspectVec の比率）が必ず画面内に収まるようにカメラの表示範囲を広げる。
/// 黒帯は出さず、設計より横長の画面では左右、縦長の画面では上下が広く見える
/// </summary>
public class AspectKeeper : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera; // 対象とするカメラ

    [SerializeField]
    private Vector2 aspectVec; // 設計時の解像度（縦横比として使う）

    private float _baseSize;

    void Awake()
    {
        _baseSize = targetCamera.orthographicSize;
        targetCamera.rect = new Rect(0, 0, 1, 1);
    }

    void Update()
    {
        targetCamera.orthographicSize = _baseSize * WidthFitScale(targetCamera.aspect, aspectVec.x / aspectVec.y);
    }

    /// <summary>
    /// 設計時の横幅を画面に収めるために、表示範囲を縦方向へ何倍に広げるか（設計より横長の画面なら 1）
    /// </summary>
    private static float WidthFitScale(float screenAspect, float designAspect)
    {
        return Mathf.Max(1f, designAspect / screenAspect);
    }
}
