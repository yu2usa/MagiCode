using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// タイトル画面からゲームに入ったとき、初回のみイントロ会話演出を表示する。
/// ButtonManager.StartGame() から StartCoroutine(PlayIntroThenLoad("Menu")) で呼ぶ。
///
/// 話者の対応:
///   DialogueSpeaker.Player    → キャラクター1（左）
///   DialogueSpeaker.Assistant → キャラクター2（右）
///   DialogueSpeaker.Enemy     → キャラクター3（中央など・任意）
/// </summary>
public class TitleIntroManager : MonoBehaviour
{
    [Header("背景")]
    [Tooltip("イントロ中に表示する背景画像（Image コンポーネント）")]
    [SerializeField] private Image backgroundImage;

    [Header("UI References")]
    [Tooltip("イントロ演出全体のパネル（背景・ウィンドウをまとめる）")]
    [SerializeField] private GameObject introPanel;
    [Tooltip("話者名の枠ごと（背景画像＋テキストの親オブジェクト）。hideSpeakerName で非表示になる。")]
    [SerializeField] private GameObject speakerNameFrame;
    [Tooltip("話者名テキスト")]
    [SerializeField] private TMP_Text speakerNameText;
    [Tooltip("セリフ本文テキスト")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("次へ促す表示（アイコン等）")]
    [SerializeField] private GameObject continuePrompt;
    [Tooltip("全スキップボタン（未設定時は無効）")]
    [SerializeField] private Button skipButton;

    [Header("Character Sprites")]
    [Tooltip("キャラクター1のスプライト Image（左側 / Player）")]
    [SerializeField] private Image character1Sprite;
    [Tooltip("キャラクター2のスプライト Image（右側 / Assistant）")]
    [SerializeField] private Image character2Sprite;
    [Tooltip("キャラクター3のスプライト Image（任意 / Enemy）")]
    [SerializeField] private Image character3Sprite;

    [Header("Sprite Settings")]
    [Tooltip("キャラクター1を左右反転するか")]
    [SerializeField] private bool flipCharacter1 = false;
    [Tooltip("キャラクター2を左右反転するか（プレイヤーと向き合わせるためデフォルトON）")]
    [SerializeField] private bool flipCharacter2 = true;
    [Tooltip("キャラクター3を左右反転するか")]
    [SerializeField] private bool flipCharacter3 = false;
    [Tooltip("発言中のキャラクターの色")]
    [SerializeField] private Color activeColor = Color.white;
    [Tooltip("発言していないキャラクターのグレーアウト色")]
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("話者名")]
    [Tooltip("DialogueSpeaker.Player に対応する話者名")]
    [SerializeField] private string character1Name = "キャラクター1";
    [Tooltip("DialogueSpeaker.Assistant に対応する話者名")]
    [SerializeField] private string character2Name = "キャラクター2";
    [Tooltip("DialogueSpeaker.Enemy に対応する話者名")]
    [SerializeField] private string character3Name = "キャラクター3";

    [Header("Settings")]
    [Tooltip("OFFのとき、初回メニュー遷移時でもイントロ演出を一切はさまない")]
    [SerializeField] private bool enableIntro = true;
    [Tooltip("タイプライター演出の1文字あたりの表示間隔（秒）")]
    [SerializeField] private float typewriterDelay = 0.04f;
    [Tooltip("バウンスの高さ（ピクセル）")]
    [SerializeField] private float bounceHeight = 18f;
    [Tooltip("バウンス1往復の時間（秒）")]
    [SerializeField] private float bounceDuration = 0.35f;
    [Tooltip("ONのとき初回のみ再生する。OFFのとき毎回再生する。")]
    [SerializeField] private bool playOnlyOnce = true;

    [Header("Dialogue")]
    [Tooltip("イントロで再生するセリフリスト")]
    [SerializeField] private List<DialogueEntry> introDialogue = new List<DialogueEntry>();

    [Header("会話中に非表示にするオブジェクト")]
    [Tooltip("イントロ中に非表示にする GameObject（タイトルUIなど）。終了後に再表示される。")]
    [SerializeField] private List<GameObject> hideWhileTalking = new List<GameObject>();

    private bool _advanceRequested;
    private bool _skipRequested;

    /// <summary>イントロを再生すべき状態か（ダイアログ設定済み かつ 初回条件を満たす）</summary>
    public bool ShouldPlay =>
        enableIntro &&
        introDialogue.Count > 0 &&
        (!playOnlyOnce || !ClearDataManager.IsTitleIntroSeen());

    private void Start()
    {
        introPanel.SetActive(false);
        skipButton?.onClick.AddListener(SkipAll);
        ApplyFlip(character1Sprite, flipCharacter1);
        ApplyFlip(character2Sprite, flipCharacter2);
        ApplyFlip(character3Sprite, flipCharacter3);
    }

    /// <summary>
    /// イントロ会話を再生し、終了後に指定シーンへ遷移する。
    /// ButtonManager から StartCoroutine で呼ぶ。
    /// </summary>
    public IEnumerator PlayIntroThenLoad(string sceneName)
    {
        introPanel.SetActive(true);
        _skipRequested = false;

        foreach (var obj in hideWhileTalking)
            if (obj != null) obj.SetActive(false);

        SetSpriteVisible(character1Sprite, false);
        SetSpriteVisible(character2Sprite, false);
        SetSpriteVisible(character3Sprite, false);

        foreach (var entry in introDialogue)
        {
            if (entry.changeVisibility)
            {
                SetSpriteVisible(character1Sprite, entry.showPlayer);
                SetSpriteVisible(character2Sprite, entry.showAssistant);
                SetSpriteVisible(character3Sprite, entry.showEnemy);
            }

            UpdateSpeakerHighlight(entry.speaker);

            if (entry.bounce)
                StartCoroutine(PlayBounce(GetSpriteForSpeaker(entry.speaker)));

            // 話者名枠の表示/非表示（ナレーションか hideSpeakerName のときは枠ごと隠す）
            if (speakerNameFrame != null)
                speakerNameFrame.SetActive(entry.ShowsSpeakerName);
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
        SetSpriteColor(character1Sprite, true);
        SetSpriteColor(character2Sprite, true);
        SetSpriteColor(character3Sprite, true);

        if (playOnlyOnce)
            ClearDataManager.MarkTitleIntroSeen();

        introPanel.SetActive(false);

        foreach (var obj in hideWhileTalking)
            if (obj != null) obj.SetActive(true);

        SceneManager.LoadScene(sceneName);
    }

    public void SkipAll() => _skipRequested = true;

    private void Update()
    {
        if (!introPanel.activeSelf) return;
        if (DialogueInput.AdvancePressed())
            _advanceRequested = true;
    }

    private void UpdateSpeakerHighlight(DialogueSpeaker speaker)
    {
        SetSpriteColor(character1Sprite, speaker == DialogueSpeaker.Player);
        SetSpriteColor(character2Sprite, speaker == DialogueSpeaker.Assistant);
        SetSpriteColor(character3Sprite, speaker == DialogueSpeaker.Enemy);
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
        DialogueSpeaker.Player    => character1Sprite,
        DialogueSpeaker.Assistant => character2Sprite,
        DialogueSpeaker.Enemy     => character3Sprite,
        _                         => null,
    };

    private string GetSpeakerName(DialogueSpeaker speaker) => speaker switch
    {
        DialogueSpeaker.Player    => character1Name,
        DialogueSpeaker.Assistant => character2Name,
        DialogueSpeaker.Enemy     => character3Name,
        _                         => "",
    };

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
