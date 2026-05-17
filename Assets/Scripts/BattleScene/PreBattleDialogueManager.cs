using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// バトル開始前の会話演出を順次再生するマネージャー。
/// TurnController から Play() を StartCoroutine で呼び出して使用する。
/// </summary>
public class PreBattleDialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("会話ウィンドウ全体")]
    [SerializeField] private GameObject dialoguePanel;
    [Tooltip("話者名テキスト")]
    [SerializeField] private TMP_Text speakerNameText;
    [Tooltip("セリフ本文テキスト")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("次へ促す表示（アイコン等）")]
    [SerializeField] private GameObject continuePrompt;

    [Header("Character Sprites")]
    [Tooltip("プレイヤーのスプライト Image")]
    [SerializeField] private Image playerSprite;
    [Tooltip("アシスタントのスプライト Image")]
    [SerializeField] private Image assistantSprite;
    [Tooltip("敵のスプライト Image（元画像サイズを自動適用）")]
    [SerializeField] private Image enemySprite;

    [Header("Sprite Settings")]
    [Tooltip("発言中のキャラクターの色（通常は白）")]
    [SerializeField] private Color activeColor = Color.white;
    [Tooltip("発言していないキャラクターのグレーアウト色")]
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("話者名")]
    [SerializeField] private string playerName = "プレイヤー";
    [SerializeField] private string assistantName = "アシスタント";
    [SerializeField] private string enemyName = "敵";

    [Header("Settings")]
    [Tooltip("タイプライター演出の1文字あたりの表示間隔（秒）")]
    [SerializeField] private float typewriterDelay = 0.04f;
    [Tooltip("バウンスの高さ（ピクセル）")]
    [SerializeField] private float bounceHeight = 18f;
    [Tooltip("バウンス1往復の時間（秒）")]
    [SerializeField] private float bounceDuration = 0.35f;

    [Header("Keywords Window")]
    [Tooltip("dialogue末尾に表示するコード獲得ウィンドウ（未設定時はスキップ）")]
    [SerializeField] private PreBattleKeywordsWindow keywordsWindow;

    private bool _advanceRequested;
    private bool _skipRequested;

    // TurnController から SetKeywords() で設定したキーワード
    private List<string> _pendingKeywords = new List<string>();

    // 初期化フラグ（非アクティブ状態でStart()が呼ばれない場合のフォールバック用）
    private bool _initialized = false;
    // SetEnemySprite()が呼ばれた場合、Play()内でサイズを再適用しないようにするフラグ
    private bool _enemySizeApplied = false;
    // ステージに登場する敵のスプライトリスト（enemyIndex で切り替え）
    private List<Sprite> _enemySprites = new List<Sprite>();
    // 各敵スプライトの左右反転フラグリスト（_enemySprites と同インデックス）
    private List<bool> _enemyFlips = new List<bool>();
    // 現在のスプライト設定時に使う反転フラグ
    private bool _currentFlip = false;

    // アクティブな状態で起動した場合はStartで初期化
    private void Start() => Initialize();

    /// <summary>
    /// 会話リストを先頭から順番に表示し、全て完了したら終了する。
    /// TurnController から yield return StartCoroutine(Play(...)) で使用する。
    /// </summary>
    public IEnumerator Play(List<DialogueEntry> entries)
    {
        dialoguePanel.SetActive(true);

        // 非アクティブ状態でStart()が呼ばれなかった場合のフォールバック初期化
        Initialize();

        _skipRequested = false;

        // 初期状態: 全スプライトを非表示
        SetSpriteVisible(playerSprite,    false);
        SetSpriteVisible(assistantSprite, false);
        SetSpriteVisible(enemySprite,     false);

        foreach (var entry in entries)
        {
            // 複数の敵がいる場合、このセリフで表示する敵スプライトを切り替える
            ApplyEnemySpriteByIndex(entry.enemyIndex);

            // キャラクター表示変更（changeVisibility が立っているセリフのみ）
            if (entry.changeVisibility)
                ApplyVisibility(entry);

            // 発言キャラを前面に・他をグレーアウト
            UpdateSpeakerHighlight(entry.speaker);

            // バウンス演出とセリフを同時開始（フラグが立っているセリフのみ）
            if (entry.bounce)
                StartCoroutine(PlayBounce(GetSpriteForSpeaker(entry.speaker)));

            speakerNameText.text = GetSpeakerName(entry.speaker);
            _advanceRequested = false;

            // 1文字ずつ表示（クリックでスキップ可）
            yield return StartCoroutine(TypewriterEffect(entry.text));

            // 次のセリフへ進む入力を待機（SkipAll でも即時解除）
            continuePrompt.SetActive(true);
            _advanceRequested = false;
            yield return new WaitUntil(() => _advanceRequested || _skipRequested);
            continuePrompt.SetActive(false);

            if (_skipRequested) break;
        }

        // 全スプライトをアクティブカラーに戻す
        RestoreSprites();

        // スキップされていない場合のみキーワードウィンドウを表示
        if (!_skipRequested && keywordsWindow != null && _pendingKeywords.Count > 0)
            yield return StartCoroutine(keywordsWindow.ShowAndWait(_pendingKeywords, () => _skipRequested));

        dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// dialogue末尾に表示するキーワードリストを設定する（Play() 前に呼ぶ）
    /// </summary>
    public void SetKeywords(List<string> keywords) => _pendingKeywords = keywords ?? new List<string>();

    /// <summary>
    /// 実際の敵名で enemyName を上書きする（Play() 前に呼ぶ）
    /// </summary>
    public void SetEnemyName(string name) => enemyName = name;

    /// <summary>
    /// ステージに登場する敵のスプライトをリストで設定する（Play() 前に呼ぶ）。
    /// 先頭スプライトを即時適用する。
    /// </summary>
    public void SetEnemySprites(List<Sprite> sprites, List<bool> flips = null)
    {
        _enemySprites = sprites ?? new List<Sprite>();
        _enemyFlips   = flips   ?? new List<bool>();
        if (_enemySprites.Count > 0 && _enemySprites[0] != null)
        {
            _currentFlip = _enemyFlips.Count > 0 && _enemyFlips[0];
            SetEnemySprite(_enemySprites[0]);
        }
    }

    /// <summary>
    /// 敵スプライト画像を差し替える（Play() 前に呼ぶ）。
    /// サイズは SetNativeSize × 10.29 で再計算する。
    /// </summary>
    public void SetEnemySprite(Sprite sprite)
    {
        if (enemySprite == null || sprite == null) return;
        enemySprite.sprite = sprite;
        enemySprite.SetNativeSize();
        var rt = enemySprite.GetComponent<RectTransform>();
        rt.sizeDelta *= 10.29f;
        // 右向きスプライトを左向きに反転する（X スケールを負にして水平反転）
        rt.localScale = new Vector3(_currentFlip ? -1f : 1f, 1f, 1f);
        _enemySizeApplied = true;
    }

    /// <summary>
    /// ボタンや外部スクリプトからセリフを進める際に呼び出す
    /// </summary>
    public void AdvanceDialogue() => _advanceRequested = true;

    /// <summary>
    /// 残りの全セリフをスキップして会話演出を終了する
    /// </summary>
    public void SkipAll() => _skipRequested = true;

    private void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        // クリック・スペース・Enterで次へ
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            _advanceRequested = true;
    }

    /// <summary>
    /// 初期化処理。Start() と Play() の両方から呼ばれるが、_initialized で一度だけ実行する。
    /// </summary>
    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // SetEnemySprite()が呼ばれていなければ、デフォルトスプライトのサイズを適用
        if (!_enemySizeApplied && enemySprite != null)
        {
            enemySprite.SetNativeSize();
            enemySprite.GetComponent<RectTransform>().sizeDelta *= 10.29f;
        }

    }

    /// <summary>
    /// 発言中のスプライトを強調し、他をグレーアウトする
    /// </summary>
    private void UpdateSpeakerHighlight(DialogueSpeaker speaker)
    {
        SetSpriteColor(playerSprite,    speaker == DialogueSpeaker.Player);
        SetSpriteColor(assistantSprite, speaker == DialogueSpeaker.Assistant);
        SetSpriteColor(enemySprite,     speaker == DialogueSpeaker.Enemy);
    }

    /// <summary>
    /// 会話終了後にスプライト色を元に戻す
    /// </summary>
    private void RestoreSprites()
    {
        SetSpriteColor(playerSprite,    true);
        SetSpriteColor(assistantSprite, true);
        SetSpriteColor(enemySprite,     true);
    }

    /// <summary>
    /// entry.changeVisibility が true のとき、各キャラの表示/非表示を適用する
    /// </summary>
    private void ApplyVisibility(DialogueEntry entry)
    {
        SetSpriteVisible(playerSprite,    entry.showPlayer);
        SetSpriteVisible(assistantSprite, entry.showAssistant);
        SetSpriteVisible(enemySprite,     entry.showEnemy);
    }

    private void SetSpriteVisible(Image sprite, bool visible)
    {
        if (sprite == null) return;
        sprite.gameObject.SetActive(visible);
    }

    private void SetSpriteColor(Image sprite, bool isActive)
    {
        // 非表示スプライトはスキップ
        if (sprite == null || !sprite.gameObject.activeSelf) return;
        sprite.color = isActive ? activeColor : inactiveColor;
    }

    /// <summary>
    /// _enemySprites リストから index に対応するスプライトに切り替える。
    /// 敵が1体のみの場合はスキップ（既に設定済み）。
    /// </summary>
    private void ApplyEnemySpriteByIndex(int index)
    {
        if (_enemySprites.Count <= 1) return;
        int i = (index >= 0 && index < _enemySprites.Count) ? index : 0;
        if (_enemySprites[i] != null)
        {
            _currentFlip = i < _enemyFlips.Count && _enemyFlips[i];
            SetEnemySprite(_enemySprites[i]);
        }
    }

    private Image GetSpriteForSpeaker(DialogueSpeaker speaker) => speaker switch
    {
        DialogueSpeaker.Player    => playerSprite,
        DialogueSpeaker.Assistant => assistantSprite,
        DialogueSpeaker.Enemy     => enemySprite,
        _                         => null,
    };

    /// <summary>
    /// 指定スプライトを2回上下にバウンスさせる
    /// </summary>
    private IEnumerator PlayBounce(Image sprite)
    {
        if (sprite == null) yield break;

        var rect = sprite.GetComponent<RectTransform>();
        float originalY = rect.anchoredPosition.y;
        float halfDuration = bounceDuration * 0.25f;

        var seq = DOTween.Sequence();
        for (int i = 0; i < 2; i++)
        {
            seq.Append(rect.DOAnchorPosY(originalY + bounceHeight, halfDuration).SetEase(Ease.OutQuad));
            seq.Append(rect.DOAnchorPosY(originalY,                halfDuration).SetEase(Ease.InQuad));
        }
        yield return seq.WaitForCompletion();
    }

    /// <summary>
    /// タイプライター演出。入力があれば即座に全文表示してスキップする
    /// </summary>
    private IEnumerator TypewriterEffect(string text)
    {
        dialogueText.text = "";

        foreach (char c in text)
        {
            // スキップ入力（1セリフ飛ばし or 全スキップ）があれば全文を即表示
            if (_advanceRequested || _skipRequested)
            {
                dialogueText.text = text;
                _advanceRequested = false;
                yield break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterDelay);
        }
    }

    private string GetSpeakerName(DialogueSpeaker speaker) => speaker switch
    {
        DialogueSpeaker.Player    => playerName,
        DialogueSpeaker.Assistant => assistantName,
        DialogueSpeaker.Enemy     => enemyName,
        _                         => "",
    };
}
