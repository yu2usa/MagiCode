using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// メニュー画面初回入場時にチュートリアル会話を表示するマネージャー。
/// 初回入場時に自動再生し、以降はリプレイボタンの onClick から PlayTutorial() を呼ぶ。
///
/// 想定 Hierarchy:
///   MenuTutorialManager [このスクリプトをアタッチ]
///     └── TutorialPanel
///           ├── PlayerSprite  (Image — 左側)
///           ├── AssistantSprite (Image — 右側)
///           ├── SpeakerNameText (TMP_Text)
///           ├── DialogueText    (TMP_Text)
///           ├── ContinuePrompt  (GameObject)
///           └── SkipButton      (Button)
/// </summary>
public class MenuTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("会話ウィンドウ全体")]
    [SerializeField] private GameObject tutorialPanel;
    [Tooltip("話者名テキスト")]
    [SerializeField] private TMP_Text speakerNameText;
    [Tooltip("セリフ本文テキスト")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("次へ促す表示（アイコン等）")]
    [SerializeField] private GameObject continuePrompt;

    [Header("Character Sprites")]
    [Tooltip("プレイヤーのスプライト Image（左側）")]
    [SerializeField] private Image playerSprite;
    [Tooltip("AIアシスタントのスプライト Image（右側）")]
    [SerializeField] private Image assistantSprite;

    [Header("Sprite Settings")]
    [Tooltip("プレイヤースプライトを左右反転するか（左向き画像を右向きにしたい場合にON）")]
    [SerializeField] private bool flipPlayer = false;
    [Tooltip("アシスタントスプライトを左右反転するか（右向き画像を左向きにしてプレイヤーと向き合わせる場合にON）")]
    [SerializeField] private bool flipAssistant = true;
    [Tooltip("発言中のキャラクターの色")]
    [SerializeField] private Color activeColor = Color.white;
    [Tooltip("発言していないキャラクターのグレーアウト色")]
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("話者名")]
    [SerializeField] private string playerName = "プレイヤー";
    [SerializeField] private string assistantName = "アシスタント";

    [Header("Settings")]
    [Tooltip("タイプライター演出の1文字あたりの表示間隔（秒）")]
    [SerializeField] private float typewriterDelay = 0.04f;
    [Tooltip("バウンスの高さ（ピクセル）")]
    [SerializeField] private float bounceHeight = 18f;
    [Tooltip("バウンス1往復の時間（秒）")]
    [SerializeField] private float bounceDuration = 0.35f;

    [Header("Dialogue")]
    [Tooltip("ONのとき初回入場時にチュートリアルを自動再生する")]
    [SerializeField] private bool enableTutorial = true;
    [Tooltip("チュートリアルで再生するセリフリスト")]
    [SerializeField] private List<DialogueEntry> tutorialDialogue = new List<DialogueEntry>();

    [Header("Skip Button")]
    [Tooltip("全スキップボタン（未設定時は無効）")]
    [SerializeField] private Button skipButton;

    [Header("会話中に非表示にするオブジェクト")]
    [Tooltip("会話中に非表示にする GameObject（戦術書ボタン等）。会話終了後に再表示される。")]
    [SerializeField] private List<GameObject> hideWhileTalking = new List<GameObject>();

    private bool _advanceRequested;
    private bool _skipRequested;

    private void Start()
    {
        // プレイヤーとアシスタントが向き合うようスプライトの向きを設定
        ApplyFlip(playerSprite,   flipPlayer);
        ApplyFlip(assistantSprite, flipAssistant);

        skipButton?.onClick.AddListener(SkipAll);

        tutorialPanel.SetActive(false);

        // 初回入場時のみ自動再生（enableTutorial が ON の場合のみ）
        if (enableTutorial && !ClearDataManager.IsMenuTutorialSeen())
            StartCoroutine(PlayTutorial());
    }

    /// <summary>
    /// チュートリアル会話を最初から再生する。
    /// リプレイボタンの onClick に登録して使用する。
    /// </summary>
    public void PlayTutorialFromButton() => StartCoroutine(PlayTutorial());

    private IEnumerator PlayTutorial()
    {
        if (tutorialDialogue == null || tutorialDialogue.Count == 0) yield break;

        tutorialPanel.SetActive(true);
        _skipRequested = false;

        // 会話中は指定オブジェクトを非表示
        foreach (var obj in hideWhileTalking)
            if (obj != null) obj.SetActive(false);

        // 初期状態: 両スプライトを非表示
        SetSpriteVisible(playerSprite,   false);
        SetSpriteVisible(assistantSprite, false);

        foreach (var entry in tutorialDialogue)
        {
            if (entry.changeVisibility)
            {
                SetSpriteVisible(playerSprite,   entry.showPlayer);
                SetSpriteVisible(assistantSprite, entry.showAssistant);
            }

            UpdateSpeakerHighlight(entry.speaker);

            if (entry.bounce)
                StartCoroutine(PlayBounce(GetSpriteForSpeaker(entry.speaker)));

            speakerNameText.text = GetSpeakerName(entry.speaker);
            _advanceRequested = false;

            yield return StartCoroutine(TypewriterEffect(entry.text));

            continuePrompt.SetActive(true);
            _advanceRequested = false;
            yield return new WaitUntil(() => _advanceRequested || _skipRequested);
            continuePrompt.SetActive(false);

            if (_skipRequested) break;
        }

        // 全スプライトをアクティブカラーに戻す
        SetSpriteColor(playerSprite,   true);
        SetSpriteColor(assistantSprite, true);

        tutorialPanel.SetActive(false);

        // 非表示にしていたオブジェクトを再表示
        foreach (var obj in hideWhileTalking)
            if (obj != null) obj.SetActive(true);

        // 既読フラグを保存（初回のみ。リプレイ時も上書きするが実害なし）
        ClearDataManager.MarkMenuTutorialSeen();
    }

    /// <summary>全セリフをスキップしてチュートリアルを終了する</summary>
    public void SkipAll() => _skipRequested = true;

    private void Update()
    {
        if (!tutorialPanel.activeSelf) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            _advanceRequested = true;
    }

    // --- 内部メソッド ---

    private void UpdateSpeakerHighlight(DialogueSpeaker speaker)
    {
        SetSpriteColor(playerSprite,   speaker == DialogueSpeaker.Player);
        SetSpriteColor(assistantSprite, speaker == DialogueSpeaker.Assistant);
    }

    private void SetSpriteVisible(Image sprite, bool visible)
    {
        if (sprite != null) sprite.gameObject.SetActive(visible);
    }

    private void SetSpriteColor(Image sprite, bool isActive)
    {
        if (sprite == null || !sprite.gameObject.activeSelf) return;
        sprite.color = isActive ? activeColor : inactiveColor;
    }

    private Image GetSpriteForSpeaker(DialogueSpeaker speaker) => speaker switch
    {
        DialogueSpeaker.Player    => playerSprite,
        DialogueSpeaker.Assistant => assistantSprite,
        _                         => null,
    };

    private string GetSpeakerName(DialogueSpeaker speaker) => speaker switch
    {
        DialogueSpeaker.Player    => playerName,
        DialogueSpeaker.Assistant => assistantName,
        _                         => "",
    };

    /// <summary>スプライトを水平反転する</summary>
    private void ApplyFlip(Image sprite, bool flip)
    {
        if (sprite == null) return;
        var rt = sprite.GetComponent<RectTransform>();
        var s = rt.localScale;
        rt.localScale = new Vector3(flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
    }

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

    private IEnumerator TypewriterEffect(string text)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
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
}
