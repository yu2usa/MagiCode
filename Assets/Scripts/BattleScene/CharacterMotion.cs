using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー・敵の座標移動。DOTween を使わずコルーチンで動かす。
/// 同じキャラクターに新しい移動を始めると、実行中の古い移動はその場で終了する
/// </summary>
public static class CharacterMotion
{
    // 2次の加減速（DOTween の OutQuad / InQuad と同じ曲線）
    public static readonly AnimationCurve EaseOut = new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
    public static readonly AnimationCurve EaseIn = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));

    // キャラクターごとの移動番号。古い番号の移動は新しい移動に譲って終了する
    private static readonly Dictionary<Transform, int> _motionIds = new Dictionary<Transform, int>();

    /// <summary>
    /// 一定速度で目的地まで歩く（加減速すると歩行アニメーションと足運びがずれるため等速）
    /// </summary>
    public static IEnumerator WalkTo(Transform target, Vector3 destination, float speed)
    {
        int id = Begin(target);
        while (IsCurrent(target, id) && target.position != destination)
        {
            target.position = Vector3.MoveTowards(target.position, destination, speed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// 指定時間をかけて目的地まで動かす。curve は経過割合(0→1)に対する進み具合(0→1)
    /// </summary>
    public static IEnumerator MoveOver(Transform target, Vector3 destination, float duration, AnimationCurve curve)
    {
        int id = Begin(target);
        Vector3 start = target.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!IsCurrent(target, id)) yield break;
            target.position = Vector3.LerpUnclamped(start, destination, curve.Evaluate(t / duration));
            yield return null;
        }
        if (IsCurrent(target, id)) target.position = destination;
    }

    private static int Begin(Transform target)
    {
        _motionIds.TryGetValue(target, out int id);
        _motionIds[target] = ++id;
        return id;
    }

    // 移動中にキャラクターが破棄された場合も終了させる
    private static bool IsCurrent(Transform target, int id) => target != null && _motionIds[target] == id;
}
