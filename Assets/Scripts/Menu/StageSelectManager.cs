using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// ボタンとStageConfigのペア
/// </summary>
[System.Serializable]
public class StageButtonEntry
{
    [Tooltip("クリックするボタン")]
    public Button button;
    [Tooltip("このボタンに対応するステージ設定")]
    public StageConfig stageConfig;
    [Tooltip("未解放時にこのステージを覆うロックパネル（LockPanelUnlockEffect をアタッチしておくこと）")]
    public RectTransform lockPanel;
    [Tooltip("このステージから次のステージへの接続線（次ステージ解放時に出現）")]
    public GameObject connectionLine;
}

/// <summary>
/// メニューシーンのステージ選択を管理する。
/// ボタンクリックで詳細パネルをスライドイン表示し、入場ボタンでバトルシーンへ遷移する。
/// </summary>
public class StageSelectManager : MonoBehaviour
{
    [Header("Stage Buttons")]
    [Tooltip("ボタンとステージ設定のペアリスト")]
    public List<StageButtonEntry> stageButtons = new List<StageButtonEntry>();

    [Header("Scene Settings")]
    [Tooltip("遷移先のバトルシーン名")]
    public string battleSceneName = "Battle";

    [Header("Obtained Code Window")]
    [Tooltip("クリア後に習得コードを表示するウィンドウ（未設定時はスキップ）")]
    [SerializeField] private ObtainedCodeWindow obtainedCodeWindow;

    [Header("Defeat Advice Window")]
    [Tooltip("敗北後にアドバイスを表示するウィンドウ（未設定時はスキップ）")]
    [SerializeField] private DefeatAdviceWindow defeatAdviceWindow;

    [Header("Connection Line")]
    [Tooltip("未解放状態の接続線のグレーアウト色（LockPanelUnlockEffect の lineLockedColor と同じ値にすること）")]
    [SerializeField] private Color lockedLineColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Stage Detail Panel")]
    [Tooltip("右からスライドインするステージ詳細パネル")]
    [SerializeField] private RectTransform detailPanel;
    [Tooltip("ステージタイトルを表示するテキスト")]
    [SerializeField] private TMP_Text stageTitleText;
    [Tooltip("ステージ説明を表示するテキスト")]
    [SerializeField] private TMP_Text stageDescriptionText;
    [Tooltip("バトルシーンへ遷移する入場ボタン")]
    [SerializeField] private Button enterButton;
    [Tooltip("パネル外クリック検出用の全画面透明ボタン（Hierarchyでパネルより下に配置）")]
    [SerializeField] private Button overlayButton;
    [Tooltip("スライドインのアニメーション時間（秒）")]
    [SerializeField] private float slideInDuration = 0.35f;
    [Tooltip("スライドインのイージング")]
    [SerializeField] private Ease slideInEase = Ease.OutQuart;

    private StageConfig _selectedConfig;
    // Inspectorで設定したパネルの表示位置（X座標）を記憶する
    private float _panelShownX;
    // canvas座標系でのパネル非表示位置（パネル幅分だけ右外）
    private float _panelHiddenX;

    IEnumerator Start()
    {
        // ステージボタンのリスナー登録
        foreach (var entry in stageButtons)
        {
            StageConfig config = entry.stageConfig;
            entry.button.onClick.AddListener(() => ShowStageDetail(config));
        }

        // 入場ボタンのリスナー登録
        enterButton.onClick.AddListener(LoadSelectedStage);

        // パネル外クリックでパネルを閉じる
        overlayButton.onClick.AddListener(HideStageDetail);
        overlayButton.gameObject.SetActive(false);

        // パネルの表示位置と非表示位置をcanvas座標系で記憶してから退避
        // （Screen.widthはピクセル単位のためcanvas座標と不一致になる場合があるため、パネル幅で計算）
        _panelShownX = detailPanel.anchoredPosition.x;
        _panelHiddenX = _panelShownX + detailPanel.rect.width;
        detailPanel.anchoredPosition = new Vector2(_panelHiddenX, detailPanel.anchoredPosition.y);

        // 敗北後：アドバイスウィンドウをステージ解放演出より先に表示
        if (defeatAdviceWindow != null && !string.IsNullOrEmpty(StageLoader.justDefeatedStageName))
        {
            var defeatedConfig = FindStageByName(StageLoader.justDefeatedStageName);
            if (defeatedConfig != null && !string.IsNullOrEmpty(defeatedConfig.defeatAdvice))
                yield return StartCoroutine(defeatAdviceWindow.ShowAndWait(defeatedConfig.defeatAdvice));
            StageLoader.justDefeatedStageName = null;
        }

        // ステージクリア後：習得コードウィンドウをステージ解放演出より先に表示
        Debug.Log($"[StageSelect] window={obtainedCodeWindow != null}, clearedStage=\"{StageLoader.justClearedStageName ?? "null"}\"");
        if (obtainedCodeWindow != null && !string.IsNullOrEmpty(StageLoader.justClearedStageName))
        {
            var clearedConfig = FindStageByName(StageLoader.justClearedStageName);
            Debug.Log($"[StageSelect] clearedConfig={clearedConfig?.stageName ?? "null"}, keywords={clearedConfig?.obtainedKeywords?.Count ?? -1}");
            if (clearedConfig != null && clearedConfig.obtainedKeywords.Count > 0)
                yield return StartCoroutine(obtainedCodeWindow.ShowAndWait(clearedConfig.obtainedKeywords));
        }

        // ロックパネルと接続線の初期化（クリア直後は解放アニメーション付き）
        InitializeStageLocks();
    }

    /// <summary>
    /// 各ステージのロックパネルと接続線を初期化する。
    /// ・解放済み（既クリア）  ：パネル即時非表示、接続線即時表示
    /// ・解放済み（直前クリア）：LockPanelUnlockEffect で点滅→破片→線フィル演出
    /// ・未解放              ：ボタン無効化、パネル表示のまま
    /// </summary>
    private void InitializeStageLocks()
    {
        string justCleared = StageLoader.justClearedStageName;
        Debug.Log($"[StageSelect] InitializeStageLocks開始: justCleared={justCleared ?? "null"}, ステージ数={stageButtons.Count}");

        // エフェクトが処理する接続線（Pass2でスキップするため記録）
        var linesHandledByEffect = new HashSet<GameObject>();

        // Pass 1: ロックパネルを処理。新解放はエフェクト起動 + 対象の線を記録
        for (int i = 0; i < stageButtons.Count; i++)
        {
            var entry = stageButtons[i];
            bool unlocked = IsStageUnlocked(i);
            // 直前クリアによって解放されたステージ（i-1がクリアされた）
            bool isNewUnlock = i > 0 && stageButtons[i - 1].stageConfig?.stageName == justCleared;

            Debug.Log($"[StageSelect] ステージ{i + 1}: stageName={entry.stageConfig?.stageName ?? "null"}, unlocked={unlocked}, isNewUnlock={isNewUnlock}, lockPanel={entry.lockPanel?.name ?? "null"}");

            if (!unlocked)
            {
                entry.button.interactable = false;
                continue;
            }

            if (entry.lockPanel == null)
            {
                Debug.LogWarning($"[StageSelect] ステージ{i + 1}: lockPanel が未設定です（インスペクターで RectTransform を割り当ててください）");
                continue;
            }

            if (isNewUnlock)
            {
                // 新解放：LockPanelUnlockEffect が無ければ自動追加（デフォルト値で動作）
                var effect = entry.lockPanel.GetComponent<LockPanelUnlockEffect>()
                             ?? entry.lockPanel.gameObject.AddComponent<LockPanelUnlockEffect>();

                // この線は LockPanelUnlockEffect が担当するのでPass2でスキップ
                var prevLine = stageButtons[i - 1].connectionLine;
                if (prevLine != null) linesHandledByEffect.Add(prevLine);
                effect.Play(prevLine);
            }
            else
            {
                // 既解放：即時非表示
                entry.lockPanel.gameObject.SetActive(false);
            }
        }

        // Pass 2: 接続線の表示色を設定（エフェクト担当の線はスキップ）
        // 解放済みは元の色のまま、未解放はグレーアウト。常時 SetActive(true) で表示する。
        for (int i = 0; i < stageButtons.Count; i++)
        {
            var entry = stageButtons[i];
            if (entry.connectionLine == null) continue;
            if (linesHandledByEffect.Contains(entry.connectionLine)) continue;

            entry.connectionLine.SetActive(true);

            bool nextUnlocked = (i + 1 < stageButtons.Count) && IsStageUnlocked(i + 1);
            if (!nextUnlocked)
            {
                var lineImage = entry.connectionLine.GetComponent<Image>();
                if (lineImage != null) lineImage.color = lockedLineColor;
            }
        }

        // 消費済みフラグをクリア
        StageLoader.justClearedStageName = null;
    }

    /// <summary>
    /// stageName が一致する StageConfig をボタンリストから検索する
    /// </summary>
    private StageConfig FindStageByName(string stageName)
    {
        foreach (var entry in stageButtons)
        {
            if (entry.stageConfig?.stageName == stageName)
                return entry.stageConfig;
        }
        return null;
    }

    /// <summary>
    /// 指定インデックスのステージが解放済みかどうかを返す。
    /// インデックス0（ステージ1）は常に解放済み。
    /// それ以外は前のステージのクリアデータを参照する。
    /// </summary>
    private bool IsStageUnlocked(int index)
    {
        if (index == 0) return true;
        var prevConfig = stageButtons[index - 1].stageConfig;
        return prevConfig != null && ClearDataManager.IsStageClear(prevConfig.stageName);
    }

    /// <summary>
    /// ステージボタンクリック時に詳細パネルの内容を更新し、右からスライドインで表示する
    /// </summary>
    private void ShowStageDetail(StageConfig config)
    {
        _selectedConfig = config;
        stageTitleText.text = config.stageName;
        stageDescriptionText.text = config.stageDescription;

        // オーバーレイを有効化してパネル外クリックを受け取る
        overlayButton.gameObject.SetActive(true);

        // 右からスライドイン（すでに開いていれば上書きアニメーションで更新）
        detailPanel.DOAnchorPosX(_panelShownX, slideInDuration).SetEase(slideInEase);
    }

    /// <summary>
    /// パネルを右へスライドアウトして閉じる
    /// </summary>
    private void HideStageDetail()
    {
        overlayButton.gameObject.SetActive(false);
        detailPanel.DOAnchorPosX(_panelHiddenX, slideInDuration).SetEase(slideInEase);
    }

    /// <summary>
    /// 選択中のステージをStageLoaderに設定してバトルシーンへ遷移する
    /// </summary>
    private void LoadSelectedStage()
    {
        StageLoader.pendingStage = _selectedConfig;
        SceneManager.LoadScene(battleSceneName);
    }
}
