using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 移動アクション
/// </summary>
[CreateAssetMenu(fileName = "MoveAction", menuName = "MagiCode/Enemy Actions/Move")]
public class MoveAction : EnemyAction
{
    public enum MoveType
    {
        Forward,
        Backward,
        ToPosition,
        Random,
        TowardPlayer,
        AwayFromPlayer,
        TowardCenter
    }

    [Header("移動設定")]
    public MoveType moveType;
    [Tooltip("ToPosition時の目標位置")]
    public int targetPosition;
    public float moveDuration = 0.8f;
    public float preMoveDelay = 1.0f;

    public override IEnumerator Execute(EnemyActionContext context)
    {
        var areas = context.Enemy.EnemyAreas;
        int newPosition = CalculateTargetPosition(context);

        // 移動不可な場合は何もしない
        if (areas == null || areas.Count == 0 || newPosition < 0 || newPosition >= areas.Count)
        {
            Debug.LogWarning($"[MoveAction] 移動不可: areas={areas?.Count ?? 0}, newPos={newPosition}");
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        if (newPosition == context.EnemyPosition)
        {
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        if (preMoveDelay > 0)
            yield return new WaitForSeconds(preMoveDelay);

        // 進行方向を向く
        context.Enemy.FaceTowardPosition(newPosition);

        // 残像を生成
        context.Enemy.CreateAfterImage();

        // 移動アニメーション開始
        context.Enemy.TriggerMoveAnimation();

        Debug.Log($"[MoveAction] 移動開始: {context.EnemyPosition} → {newPosition}");

        // EnemyDataの移動時間を使用
        float duration = context.Enemy.GetMoveDuration();

        // 移動先でのオフセットを計算（同じエリアに他の敵がいる場合）
        float offsetX = context.Enemy.GetMoveTargetOffset(newPosition);
        float offsetY = context.Enemy.GetPositionOffsetY();
        Vector2 targetPos = areas[newPosition];
        Vector3 finalPos = new Vector3(targetPos.x + offsetX, targetPos.y + offsetY, context.Enemy.transform.position.z);

        yield return context.Enemy.transform
            .DOMove(finalPos, duration)
            .SetId(EnemyController.ENEMY_TWEEN_ID)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        context.Enemy.enemyCurrentPos = newPosition;

        // 移動アニメーション終了
        context.Enemy.TriggerMoveEndAnimation();

        // 移動後、左向き（アイドル状態）に戻す
        context.Enemy.FaceLeft();

        Debug.Log($"[MoveAction] 移動完了: 現在位置 = {newPosition}");
    }

    public int CalculateTargetPosition(EnemyActionContext context)
    {
        var areas = context.Enemy.EnemyAreas;
        if (areas == null || areas.Count == 0) return -1;

        int current = context.EnemyPosition;
        int player = context.PlayerPosition;
        int maxPos = areas.Count - 1;

        return moveType switch
        {
            MoveType.Forward => Mathf.Min(current + 1, maxPos),
            MoveType.Backward => Mathf.Max(current - 1, 0),
            MoveType.ToPosition => Mathf.Clamp(targetPosition, 0, maxPos),
            MoveType.Random => Random.Range(0, 2) == 0
                ? Mathf.Max(current - 1, 0)
                : Mathf.Min(current + 1, maxPos),
            MoveType.TowardPlayer => current < player
                ? Mathf.Min(current + 1, maxPos)
                : current > player
                    ? Mathf.Max(current - 1, 0)
                    : current,
            MoveType.AwayFromPlayer => current < player
                ? Mathf.Max(current - 1, 0)
                : current > player
                    ? Mathf.Min(current + 1, maxPos)
                    : (Random.Range(0, 2) == 0 ? Mathf.Max(current - 1, 0) : Mathf.Min(current + 1, maxPos)),
            MoveType.TowardCenter => CalculateMoveTowardCenter(current, maxPos),
            _ => current
        };
    }

    /// <summary>
    /// 中央エリアに向かって移動する位置を計算
    /// </summary>
    private int CalculateMoveTowardCenter(int current, int maxPos)
    {
        int center = maxPos / 2;
        if (current < center)
            return current + 1;
        if (current > center)
            return current - 1;
        return current; // 既に中央
    }

    public override bool CanExecute(EnemyActionContext context)
    {
        int target = CalculateTargetPosition(context);
        return target != context.EnemyPosition;
    }

    public override string GetDescription()
    {
        return moveType switch
        {
            MoveType.Forward => "前進",
            MoveType.Backward => "後退",
            MoveType.ToPosition => $"エリア{targetPosition}へ移動",
            MoveType.Random => "ランダム移動",
            MoveType.TowardPlayer => "プレイヤーに接近",
            MoveType.AwayFromPlayer => "プレイヤーから離れる",
            MoveType.TowardCenter => "中央へ移動",
            _ => "移動"
        };
    }
}
