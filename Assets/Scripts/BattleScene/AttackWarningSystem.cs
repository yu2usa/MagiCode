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

    [Tooltip("ウィンドウを閉じるボタン（Button コンポーネント付きのパネル）")]
    [SerializeField] private Button closeButton;

    [Tooltip("ウィンドウが右からスライドインする距離（Canvas ピクセル単位）")]
    [SerializeField] private float windowSlideInDistance = 420f;

    [Tooltip("ウィンドウのスライドイン時間（秒）")]
    [SerializeField] private float windowSlideInDuration = 0.3f;

    // 通知ウィンドウの RectTransform とデフォルト位置（初期化時に取得）
    private RectTransform _windowRect;
    private float _windowDefaultX;

    // 生成済みの警告ボタン一覧（非表示時に破棄する）
    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    private void Awake()
    {
        if (enemyAttackNotificationWindow != null)
        {
            _windowRect = enemyAttackNotificationWindow.GetComponent<RectTransform>();
            _windowDefaultX = _windowRect.anchoredPosition.x;
        }

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
        }
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
    }

    // ボタン押下で通知ウィンドウをトグル
    private void ToggleNotificationWindow()
    {
        if (enemyAttackNotificationWindow == null) return;

        if (enemyAttackNotificationWindow.activeSelf)
            CloseNotificationWindow();
        else
            OpenNotificationWindow();
    }

    // 右からスライドインしてウィンドウを開く
    private void OpenNotificationWindow()
    {
        if (_windowRect == null) return;

        // 進行中のアニメーションを止めてから開始
        _windowRect.DOKill();

        enemyAttackNotificationWindow.SetActive(true);

        // 右端外側から始めてデフォルト位置へスライドイン
        _windowRect.anchoredPosition = new Vector2(_windowDefaultX + windowSlideInDistance, _windowRect.anchoredPosition.y);
        _windowRect.DOAnchorPosX(_windowDefaultX, windowSlideInDuration).SetEase(Ease.OutQuart);
    }

    // ウィンドウを閉じる（アニメーションなし）
    private void CloseNotificationWindow()
    {
        if (enemyAttackNotificationWindow == null) return;

        _windowRect?.DOKill();
        enemyAttackNotificationWindow.SetActive(false);
    }
}
