using UnityEngine;
using DG.Tweening;

/// <summary>
/// Camera.main を DOTween で揺らす静的ユーティリティ。
/// 呼び出し元は CameraShake.Shake() 一行で使える。
/// </summary>
public static class CameraShake
{
    /// <summary>
    /// カメラシェイクを実行する
    /// </summary>
    /// <param name="duration">揺れの時間（秒）</param>
    /// <param name="strength">揺れの強さ（ワールド単位）</param>
    public static void Shake(float duration = 0.18f, float strength = 0.12f)
    {
        Camera.main?.transform.DOShakePosition(duration, strength, 12, 90, false, true);
    }
}
