using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// ステージのエリア数・配置に合わせてカメラを自動ズーム・センタリングする
/// StageConfigのエリア座標と全キャラクターの実座標を合わせて計算することで、
/// エリア数が増えても・Y軸オフセットがあっても必ず全体が収まる
/// </summary>
public class SmoothCameraZoom : MonoBehaviour
{
    [Header("自動ズーム設定")]
    [Tooltip("水平方向の余白（ワールド単位）")]
    [SerializeField] private float horizontalPadding = 3f;
    [Tooltip("垂直方向の余白（ワールド単位）")]
    [SerializeField] private float verticalPadding = 2f;
    [Tooltip("最小 orthographicSize（小さすぎる場合の下限）")]
    [SerializeField] private float minOrthographicSize = 3f;
    [Tooltip("ズーム・移動アニメーションの時間（秒）")]
    [SerializeField] private float adjustDuration = 0.6f;
    [Tooltip("アニメーションのイージング")]
    [SerializeField] private Ease adjustEase = Ease.OutQuad;

    [Header("背景追従")]
    [Tooltip("カメラのズームに合わせて拡大縮小する地形背景（Stage/Field）")]
    [SerializeField] private Transform terrainRoot;
    [Tooltip("地形で画面の上まで覆うステージで、地形の上端を画面の上端からどれだけ上に出すか（ワールド単位。絵の上端の透明な行を隠す）")]
    [SerializeField] private float terrainTopOverscan = 0.5f;

    // 画面設計の基準となる縦横比
    private const float DESIGN_ASPECT = 16f / 9f;

    private Camera _cam;

    // シーンに配置した時点のカメラと背景の関係。ズーム後も画面上の背景の見え方をこれに揃える
    private float _baseSize;
    private Vector3 _baseCamPos;
    private Vector3 _terrainBasePos;
    private Vector3 _terrainBaseScale;

    // 最後に合わせた画面サイズ。ウィンドウサイズが変わったら合わせ直す
    private Vector2Int _screenSize;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.rect = new Rect(0, 0, 1, 1);
        _baseSize = _cam.orthographicSize;
        _baseCamPos = transform.position;
        _terrainBasePos = terrainRoot.position;
        _terrainBaseScale = terrainRoot.localScale;
        _screenSize = new Vector2Int(Screen.width, Screen.height);
    }

    void Update()
    {
        var size = new Vector2Int(Screen.width, Screen.height);
        if (size == _screenSize) return;

        _screenSize = size;
        Adjust();
    }

    IEnumerator Start()
    {
        _cam = GetComponent<Camera>();

        // EnemyController.Start()（エリア座標への配置）が完了するまで2フレーム待機
        yield return null;
        yield return null;
        Adjust();
    }

    /// <summary>
    /// 全エリア座標（StageConfig）＋全キャラクターの実座標を包むようにカメラを調整する。
    /// ターン開始時など外部から再調整が必要な場合も呼び出し可能。
    /// </summary>
    public void Adjust()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        float minX, maxX, minY, maxY;
        if (!CollectBounds(out minX, out maxX, out minY, out maxY)) return;

        // 全体を収めるのに必要な orthographicSize を計算（水平・垂直の大きい方を採用）。
        // 16:9 より横長の画面は 16:9 と同じ大きさで映して左右を広く見せ、縦長の画面は上下を広げて横幅を収める
        float sizeForWidth  = ((maxX - minX + horizontalPadding) * 0.5f) / Mathf.Min(_cam.aspect, DESIGN_ASPECT);
        float sizeForHeight = (maxY - minY + verticalPadding) * 0.5f;
        float targetSize    = Mathf.Max(minOrthographicSize, sizeForWidth, sizeForHeight);

        float centerX   = (minX + maxX) * 0.5f;
        float centerY   = (minY + maxY) * 0.5f;
        Vector3 targetPos = new Vector3(centerX, centerY, transform.position.z);

        // 再調整時に前のTweenを止めてから開始
        DOTween.Kill("CameraAdjust");
        transform.DOMove(targetPos, adjustDuration)
            .SetEase(adjustEase)
            .SetId("CameraAdjust");
        DOTween.To(() => _cam.orthographicSize,
                   x  => _cam.orthographicSize = x,
                   targetSize, adjustDuration)
            .SetEase(adjustEase)
            .SetId("CameraAdjust")
            .OnComplete(RefreshAllHealthBars);

        FitTerrain(targetSize, centerX, centerY, minY);
    }

    /// <summary>
    /// ズーム率と同じだけ背景を拡大縮小し、画面上の背景の大きさを一定に保つ。
    /// 足元の高さ（キャラクターとエリアの最下点）を基準に拡大するので、キャラクターが地面から浮いたり沈んだりしない
    /// </summary>
    private void FitTerrain(float targetSize, float centerX, float centerY, float groundY)
    {
        float zoom = targetSize / _baseSize;
        Vector3 targetPos = new Vector3(
            centerX + (_terrainBasePos.x - _baseCamPos.x) * zoom,
            groundY + (_terrainBasePos.y - groundY) * zoom,
            _terrainBasePos.z);
        Vector3 targetScale = _terrainBaseScale * zoom;

        // 屋内のステージは、エリアが多くてカメラが引くと地形の絵の上端が画面に入り、上から空がのぞく。
        // 足元の高さを基準にさらに拡大して、地形の上端が画面の上端を越えるようにする
        float neededTop = centerY + targetSize + terrainTopOverscan;
        float top = targetPos.y + terrainRoot.GetComponent<SpriteRenderer>().sprite.bounds.max.y * targetScale.y;
        if (FindObjectOfType<TurnController>().stageConfig.terrainFillsScreenTop && top < neededTop)
        {
            float extra = (neededTop - groundY) / (top - groundY);
            targetScale *= extra;
            targetPos = new Vector3(
                centerX + (targetPos.x - centerX) * extra,
                groundY + (targetPos.y - groundY) * extra,
                targetPos.z);
        }

        terrainRoot.DOMove(targetPos, adjustDuration).SetEase(adjustEase).SetId("CameraAdjust");
        terrainRoot.DOScale(targetScale, adjustDuration).SetEase(adjustEase).SetId("CameraAdjust");
    }

    /// <summary>
    /// カメラ調整完了後に全敵のHPバー位置を再計算する
    /// </summary>
    private void RefreshAllHealthBars()
    {
        foreach (var enemy in FindObjectsOfType<EnemyController>())
            enemy.RefreshHealthBarPosition();
    }

    /// <summary>
    /// StageConfigの全エリア座標と全キャラクターの実座標を収集し、境界を返す。
    /// 少なくとも1点が取得できた場合に true を返す。
    /// </summary>
    private bool CollectBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = float.MaxValue; maxX = float.MinValue;
        minY = float.MaxValue; maxY = float.MinValue;
        bool found = false;

        // StageConfig のエリア座標（エリア配置の基準点）
        var tc = FindObjectOfType<TurnController>();
        if (tc != null && tc.stageConfig != null)
        {
            foreach (var pos in tc.stageConfig.playerAreaPositions)
                Expand(pos.x, pos.y, ref minX, ref maxX, ref minY, ref maxY, ref found);
            foreach (var pos in tc.stageConfig.enemyAreaPositions)
                Expand(pos.x, pos.y, ref minX, ref maxX, ref minY, ref maxY, ref found);
        }

        // プレイヤーの実座標（Y軸オフセット適用後の実位置）
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            Expand(player.transform.position.x, player.transform.position.y,
                   ref minX, ref maxX, ref minY, ref maxY, ref found);

        // 全敵の実座標
        foreach (var enemy in FindObjectsOfType<EnemyController>())
            Expand(enemy.transform.position.x, enemy.transform.position.y,
                   ref minX, ref maxX, ref minY, ref maxY, ref found);

        return found;
    }

    private static void Expand(float x, float y,
        ref float minX, ref float maxX, ref float minY, ref float maxY, ref bool found)
    {
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
        found = true;
    }
}
