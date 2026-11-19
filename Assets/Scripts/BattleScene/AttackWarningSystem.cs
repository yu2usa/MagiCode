using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// プレイヤー防御フェーズ中、敵の攻撃予定エリアに警告マークボタンを生成するシステム。
/// エリアラベルオブジェクトの X 座標に合わせてプレファブをインスタンス化する。
/// TurnController から ShowWarningsForAreas() / HideAllWarnings() を呼び出して使用する。
/// </summary>
public class AttackWarningSystem : MonoBehaviour
{
    [Header("Warning Buttons")]
    [Tooltip("エリアに表示する警告マークのプレファブ")]
    [SerializeField] private GameObject warningButtonPrefab;

    [Tooltip("警告ボタンを配置する Y 座標（ワールドスペース）")]
    [SerializeField] private float warningButtonY = 2f;

    [Tooltip("スライドイン開始位置の Y オフセット（目標 Y より下から登場させる距離）")]
    [SerializeField] private float slideInOffsetY = 1f;

    [Tooltip("スライドインにかかる時間（秒）")]
    [SerializeField] private float slideInDuration = 0.3f;

    [Tooltip("ボタンの親 Transform（未設定時はシーンルートに生成）")]
    [SerializeField] private Transform warningButtonParent;

    [Header("Notification Window")]
    [Tooltip("ボタン押下で開閉する敵行動通知ウィンドウ")]
    [SerializeField] private GameObject enemyAttackNotificationWindow;

    [Tooltip("ウィンドウの外を押して閉じるボタン（画面全体の暗幕）")]
    [SerializeField] private Button closeButton;

    [Tooltip("右からスライドする通知ウィンドウ本体")]
    [SerializeField] private RectTransform window;

    [Tooltip("ウィンドウの後ろで画面をグレーアウトする暗幕（その場でフェードする）")]
    [SerializeField] private Image dim;

    // 開いた直後のフレームは表示の準備で重くなりやすいので、動き出しがゆっくりなイージングにする
    [Tooltip("ウィンドウのスライドイン時間（秒）")]
    [SerializeField] private float windowSlideInDuration = 0.4f;
    [SerializeField] private Ease windowSlideInEase = Ease.InOutCubic;
    [Tooltip("ウィンドウのスライドアウト時間（秒）")]
    [SerializeField] private float windowSlideOutDuration = 0.25f;
    [SerializeField] private Ease windowSlideOutEase = Ease.InQuad;

    [Header("Danger Zone")]
    [Tooltip("攻撃されるエリアの地面に重ねる帯の色（点滅で一番濃いとき）")]
    [SerializeField] private Color dangerZoneColor = new Color(1f, 0.1f, 0.1f, 0.6f);
    [Tooltip("点滅で一番薄いときの濃さ（0 にすると消えた瞬間に気づきにくい）")]
    [SerializeField] private float dangerZoneMinAlpha = 0.15f;
    [Tooltip("帯の上端のY座標（キャラが立つ草の上端あたり）")]
    [SerializeField] private float dangerZoneTop = 0.72f;
    [Tooltip("帯の下端のY座標（エリア番号の下まで）")]
    [SerializeField] private float dangerZoneBottom = -0.6f;
    [Tooltip("隣のエリアの帯とくっつかないよう、左右を詰める幅")]
    [SerializeField] private float dangerZoneGap = 0.1f;
    [SerializeField] private float dangerZoneBlinkDuration = 0.5f;

    // ウィンドウの表示位置と暗幕の濃さ（初期化時に取得）
    private float _windowShownX;
    private float _dimAlpha;
    private bool _isOpen;

    // 生成済みの警告ボタン一覧（非表示時に破棄する）
    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
    // 生成済みの危険エリアの帯（非表示時に破棄する）
    private readonly List<SpriteRenderer> _dangerZones = new List<SpriteRenderer>();
    // 帯に使う1ワールド単位四方の白い絵
    private Sprite _whiteSprite;

    private void Awake()
    {
        _windowShownX = window.anchoredPosition.x;
        _dimAlpha = dim.color.a;

        // 閉じるボタンにリスナーを登録
        closeButton?.onClick.AddListener(CloseNotificationWindow);
    }

    private void Start()
    {
        HideAllWarnings();
    }

    /// <summary>
    /// 攻撃対象エリアの警告ボタンをインスタンス化する（防御フェーズ開始時に呼ぶ）。
    /// 同一エリアへの重複生成は除外する。areaLabelObjects のインデックスがエリア番号に対応する。
    /// </summary>
    public void ShowWarningsForAreas(List<int> attackedAreas, List<GameObject> areaLabelObjects)
    {
        HideAllWarnings();

        Debug.Log($"[AttackWarningSystem] ShowWarningsForAreas 呼び出し: attackedAreas={attackedAreas.Count}個, areaLabels={areaLabelObjects.Count}個, prefab={warningButtonPrefab != null}");

        if (warningButtonPrefab == null)
        {
            Debug.LogError("[AttackWarningSystem] warningButtonPrefab が未設定です。Inspector で設定してください。");
            return;
        }

        // 重複エリアを除去
        var uniqueAreas = new HashSet<int>(attackedAreas);
        Debug.Log($"[AttackWarningSystem] 対象エリア（重複除去後）: [{string.Join(", ", uniqueAreas)}]");

        foreach (int area in uniqueAreas)
        {
            if (area < 0 || area >= areaLabelObjects.Count || areaLabelObjects[area] == null)
            {
                Debug.LogWarning($"[AttackWarningSystem] エリア {area} のラベルが無効（スキップ）");
                continue;
            }

            float xPos = areaLabelObjects[area].transform.position.x;

            // スライドイン開始位置（目標 Y より下）から生成してアニメーション
            var startPos = new Vector3(xPos, warningButtonY - slideInOffsetY, 0f);
            var targetPos = new Vector3(xPos, warningButtonY, 0f);

            Debug.Log($"[AttackWarningSystem] エリア {area} にボタン生成: startPos={startPos}, targetPos={targetPos}");

            var btn = Instantiate(warningButtonPrefab, startPos, Quaternion.identity, warningButtonParent);
            btn.transform.DOMoveY(targetPos.y, slideInDuration).SetEase(Ease.OutBack);

            // EventTrigger に PointerClick イベントを登録（なければ自動追加）
            var trigger = btn.GetComponent<EventTrigger>() ?? btn.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => ToggleNotificationWindow());
            trigger.triggers.Add(entry);

            _spawnedButtons.Add(btn);
            SpawnDangerZone(area, areaLabelObjects);
        }
    }

    // 攻撃されるエリアの地面を赤く点滅させ、「！」を開かなくても逃げ先が分かるようにする。
    // 幅は隣のエリアとの中間まで。地面（Field）と同じ描画順で手前に置き、キャラやエリア番号より奥に描く
    private void SpawnDangerZone(int area, List<GameObject> areaLabelObjects)
    {
        float center = areaLabelObjects[area].transform.position.x;
        float left = area > 0
            ? (areaLabelObjects[area - 1].transform.position.x + center) / 2f
            : center - HalfAreaSpacing(areaLabelObjects);
        float right = area < areaLabelObjects.Count - 1
            ? (areaLabelObjects[area + 1].transform.position.x + center) / 2f
            : center + HalfAreaSpacing(areaLabelObjects);
        left += dangerZoneGap;
        right -= dangerZoneGap;

        if (_whiteSprite == null)
            _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

        var zone = new GameObject($"DangerZone_{area}").AddComponent<SpriteRenderer>();
        zone.sprite = _whiteSprite;
        zone.sortingOrder = -1;
        zone.transform.position = new Vector3((left + right) / 2f, (dangerZoneTop + dangerZoneBottom) / 2f, 0f);
        zone.transform.localScale = new Vector3(right - left, dangerZoneTop - dangerZoneBottom, 1f);
        zone.color = new Color(dangerZoneColor.r, dangerZoneColor.g, dangerZoneColor.b, dangerZoneMinAlpha);
        zone.DOFade(dangerZoneColor.a, dangerZoneBlinkDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        _dangerZones.Add(zone);
    }

    // 端のエリアは、隣のエリアとの間隔の半分だけ外側まで塗る
    private static float HalfAreaSpacing(List<GameObject> areaLabelObjects)
    {
        return Mathf.Abs(areaLabelObjects[1].transform.position.x - areaLabelObjects[0].transform.position.x) / 2f;
    }

    /// <summary>
    /// 生成済み警告ボタンをすべて破棄し、通知ウィンドウも閉じる（防御フェーズ終了時に呼ぶ）
    /// </summary>
    public void HideAllWarnings()
    {
        CloseNotificationWindow();

        foreach (var btn in _spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        _spawnedButtons.Clear();

        foreach (var zone in _dangerZones)
        {
            zone.DOKill();
            Destroy(zone.gameObject);
        }
        _dangerZones.Clear();
    }

    // ボタン押下で通知ウィンドウをトグル（閉じるアニメーション中に押したら開き直す）
    private void ToggleNotificationWindow()
    {
        if (_isOpen)
            CloseNotificationWindow();
        else
            OpenNotificationWindow();
    }

    // 背景はその場でグレーアウトし、ウィンドウは右の画面外からスライドインする
    private void OpenNotificationWindow()
    {
        _isOpen = true;
        KillWindowTweens();
        enemyAttackNotificationWindow.SetActive(true);

        window.anchoredPosition = new Vector2(WindowHiddenX(), window.anchoredPosition.y);
        window.DOAnchorPosX(_windowShownX, windowSlideInDuration).SetEase(windowSlideInEase);
        dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
        dim.DOFade(_dimAlpha, windowSlideInDuration);
    }

    // ウィンドウを右へ引っ込め、背景を元に戻してから非表示にする
    private void CloseNotificationWindow()
    {
        if (!_isOpen) return;
        _isOpen = false;
        KillWindowTweens();

        dim.DOFade(0f, windowSlideOutDuration);
        window.DOAnchorPosX(WindowHiddenX(), windowSlideOutDuration)
            .SetEase(windowSlideOutEase)
            .OnComplete(() => enemyAttackNotificationWindow.SetActive(false));
    }

    // ウィンドウ幅ぶん右へずらせば画面外になる
    private float WindowHiddenX()
    {
        return _windowShownX + window.rect.width;
    }

    private void KillWindowTweens()
    {
        window.DOKill();
        dim.DOKill();
    }
}
