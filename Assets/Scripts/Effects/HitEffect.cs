using UnityEngine;

/// <summary>
/// 攻撃が当たったキャラクターに一度だけ再生するヒットエフェクト。再生し終えたら消える
/// </summary>
public class HitEffect : MonoBehaviour
{
    /// <summary>
    /// 当たったキャラクターの中心に、キャラクターと同じドットの大きさで表示する
    /// </summary>
    public static void Spawn(GameObject prefab, SpriteRenderer target)
    {
        var effect = Instantiate(prefab, target.bounds.center, Quaternion.identity);
        Vector3 scale = target.transform.lossyScale;
        effect.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f);
    }

    void Start()
    {
        var clip = GetComponent<Animator>().runtimeAnimatorController.animationClips[0];
        Destroy(gameObject, clip.length);
    }
}
