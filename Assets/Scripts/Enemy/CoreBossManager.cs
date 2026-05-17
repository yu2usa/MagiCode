using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// CoreBossステージの管理クラス（シングルトン）
/// EnemyController.Start()から自己登録される方式で、タイミング依存を排除
/// </summary>
public class CoreBossManager : MonoBehaviour
{
    public static CoreBossManager Instance { get; private set; }

    [Header("手動設定（StageConfig未使用時のフォールバック）")]
    public List<EnemyController> coreTotems = new List<EnemyController>();
    public EnemyController coreBoss;

    [Header("スワップアニメーション")]
    [SerializeField] private float swapDuration = 0.5f;
    [SerializeField] private Ease swapEase = Ease.OutQuad;

    [Header("数字ラベル")]
    [Tooltip("トーテム頭上の数字ラベルのY軸オフセット")]
    [SerializeField] private float numberLabelOffsetY = 1.2f;
    [Tooltip("数字ラベルのフォントサイズ（ワールドスペース単位）")]
    [SerializeField] private float numberLabelFontSize = 0.5f;
    [Tooltip("数字ラベルの色")]
    [SerializeField] private Color numberLabelColor = Color.yellow;
    [Tooltip("数字ラベルのフォント（未設定時はTMPデフォルト）")]
    [SerializeField] private TMP_FontAsset numberLabelFont;

    // ソート完了時にCoreへ与えるダメージ
    private const int SORTED_DAMAGE = 30;

    // 各トーテムの登録時エリア（リセット用）
    private readonly Dictionary<EnemyController, int> _initialAreas = new Dictionary<EnemyController, int>();

    /// <summary>現在のトーテム配置がソート済みかどうか（TurnControllerから参照）</summary>
    public bool IsCurrentlySorted => IsSorted();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==============================================================
    // EnemyController.Start()から呼ばれる登録API
    // ==============================================================

    /// <summary>
    /// CoreTotemとして登録し、頭上ラベルを生成する。
    /// EnemyController.Start()の末尾から呼ばれる。
    /// </summary>
    public void RegisterTotem(EnemyController totem)
    {
        if (coreTotems.Contains(totem)) return;
        coreTotems.Add(totem);
        // 登録時のエリアを初期位置として記録（リセット用）
        _initialAreas[totem] = totem.enemyCurrentPos;
        SetupNumberLabel(totem);
        RefreshCoreDefense();
        Debug.Log($"[CoreBossManager] CoreTotem登録: {totem.name}, 番号={totem.assignedNumber}, 初期エリア={totem.enemyCurrentPos}, 合計{coreTotems.Count}体");
    }

    /// <summary>
    /// CoreBossとして登録する。EnemyController.Start()の末尾から呼ばれる。
    /// </summary>
    public void RegisterBoss(EnemyController boss)
    {
        coreBoss = boss;
        RefreshCoreDefense();
        Debug.Log($"[CoreBossManager] CoreBoss登録: {boss.name}");
    }

    // ==============================================================
    // 数字ラベル
    // ==============================================================

    /// <summary>
    /// CoreTotem頭上に割り当て番号を常時表示するTextMeshProラベルを生成する
    /// </summary>
    private void SetupNumberLabel(EnemyController totem)
    {
        var labelObj = new GameObject("NumberLabel");
        // worldPositionStays=false でローカル空間に配置
        labelObj.transform.SetParent(totem.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, numberLabelOffsetY, -0.1f);
        // 親(CoreTotem)のY=180°回転による文字反転を打ち消す
        labelObj.transform.localScale = new Vector3(-1f, 1f, 1f);

        var tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = totem.assignedNumber.ToString();
        tmp.fontSize = numberLabelFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = numberLabelColor;
        if (numberLabelFont != null)
            tmp.font = numberLabelFont;

        // スプライトと同じソートレイヤーで、1つ手前に描画
        var sr = totem.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            tmp.sortingLayerID = sr.sortingLayerID;
            tmp.sortingOrder = sr.sortingOrder + 1;
        }
    }

    /// <summary>
    /// 全CoreTotemを登録時の初期エリアへアニメーションしながら戻す。
    /// プレイヤー攻撃フェーズ終了時に未ソートだった場合に呼ばれる。
    /// </summary>
    public IEnumerator ResetToInitialState()
    {
        var lastTween = default(Tween);
        foreach (var totem in coreTotems)
        {
            if (totem == null || !_initialAreas.ContainsKey(totem)) continue;
            int initArea = _initialAreas[totem];
            if (totem.enemyCurrentPos == initArea) continue;

            var areas = totem.EnemyAreas;
            Vector3 target = new Vector3(areas[initArea].x, areas[initArea].y, totem.transform.position.z);
            DOTween.Kill(totem.transform);
            lastTween = totem.transform.DOMove(target, swapDuration).SetEase(swapEase);
            totem.enemyCurrentPos = initArea;
        }

        if (lastTween != null)
            yield return lastTween.WaitForCompletion();

        RefreshCoreDefense();
        Debug.Log("[CoreBossManager] 未ソート: トーテムを初期位置にリセット");
    }

    // ==============================================================
    // ソート判定 / 防御設定
    // ==============================================================

    /// <summary>
    /// エリア順にCoreTotemのassignedNumberを並べたリストを返す
    /// LogsManagerが "numbers = [...]" としてPythonコードに挿入する
    /// </summary>
    public List<int> GetNumbersInAreaOrder()
    {
        return coreTotems
            .Where(t => t != null)
            .OrderBy(t => t.enemyCurrentPos)
            .Select(t => t.assignedNumber)
            .ToList();
    }

    /// <summary>
    /// numbers配列のインデックスj番目とk番目のCoreTotemをDOTweenでスワップする。
    /// j, k はPython側の0始まりリストインデックス（エリア番号ではない）
    /// </summary>
    public IEnumerator SwapTotems(int j, int k)
    {
        // エリア順に並べたリストからインデックスでトーテムを取得
        var ordered = coreTotems
            .Where(t => t != null)
            .OrderBy(t => t.enemyCurrentPos)
            .ToList();

        if (j < 0 || k < 0 || j >= ordered.Count || k >= ordered.Count)
        {
            Debug.LogWarning($"[CoreBossManager] Swap({j}, {k}): インデックスが範囲外です (トーテム数={ordered.Count})");
            yield break;
        }

        var totemJ = ordered[j];
        var totemK = ordered[k];

        var areas = totemJ.EnemyAreas;
        int areaJ = totemJ.enemyCurrentPos;
        int areaK = totemK.enemyCurrentPos;
        Vector3 targetForJ = new Vector3(areas[areaK].x, areas[areaK].y, totemJ.transform.position.z);
        Vector3 targetForK = new Vector3(areas[areaJ].x, areas[areaJ].y, totemK.transform.position.z);

        DOTween.Kill(totemJ.transform);
        DOTween.Kill(totemK.transform);
        totemJ.transform.DOMove(targetForJ, swapDuration).SetEase(swapEase);
        yield return totemK.transform.DOMove(targetForK, swapDuration).SetEase(swapEase).WaitForCompletion();

        // エリア番号を更新
        totemJ.enemyCurrentPos = areaK;
        totemK.enemyCurrentPos = areaJ;

        // ソート状態を再判定してCoreの防御を更新
        RefreshCoreDefense();
    }

    /// <summary>
    /// CoreTotemがエリア順に昇順で並んでいるか確認する
    /// </summary>
    private bool IsSorted()
    {
        var ordered = coreTotems
            .Where(t => t != null)
            .OrderBy(t => t.enemyCurrentPos)
            .ToList();

        // トーテムが未登録（初期化前）はソート未完了として扱う
        if (ordered.Count == 0) return false;
        if (ordered.Count < 2) return true;

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].assignedNumber > ordered[i + 1].assignedNumber) return false;
        }
        return true;
    }

    /// <summary>
    /// ソート完了時にCoreに30ダメージを自動適用する。
    /// TurnController.PlayerAttack() のフェーズ終了時に呼ばれる。
    /// </summary>
    public IEnumerator ApplySortedDamage()
    {
        if (coreBoss == null) yield break;
        Debug.Log("[CoreBossManager] ソート完了: Coreに30ダメージを自動適用");
        coreBoss.GetDamaged(SORTED_DAMAGE);
        // ノックバック＋死亡アニメーション開始を待つ
        yield return new WaitForSeconds(0.6f);
    }

    /// <summary>
    /// ソート状態をCoreボスの被ダメージ設定に反映する。
    /// 未ソート時はプレイヤーの通常攻撃を無効化する（自動ダメージはApplySortedDamage()が担う）
    /// </summary>
    public void RefreshCoreDefense()
    {
        if (coreBoss == null) return;

        if (IsSorted())
        {
            // 手動攻撃でも当たるよう倍率を戻す（ApplySortedDamageが主経路）
            coreBoss.incomingDamageMultiplier = 1f;
            coreBoss.forceDamage = 0;
        }
        else
        {
            coreBoss.incomingDamageMultiplier = 0f;
            coreBoss.forceDamage = 0;
            Debug.Log("[CoreBossManager] 未ソート: Coreへのダメージ無効");
        }
    }
}
