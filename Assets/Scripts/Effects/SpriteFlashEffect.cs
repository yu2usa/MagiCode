using System.Collections;
using UnityEngine;

/// <summary>
/// スプライトの白フラッシュ効果を制御するコンポーネント
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("フラッシュの色")]
    public Color flashColor = Color.white;
    [Tooltip("フラッシュの持続時間")]
    public float flashDuration = 0.1f;

    private SpriteRenderer _spriteRenderer;
    private Material _flashMaterial;
    private Material _originalMaterial;
    private Coroutine _flashCoroutine;

    // シェーダーのプロパティID
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorProp = Shader.PropertyToID("_FlashColor");

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _spriteRenderer.material;

        // フラッシュ用マテリアルを作成
        SetupFlashMaterial();
    }

    /// <summary>
    /// フラッシュ用マテリアルをセットアップ
    /// </summary>
    private void SetupFlashMaterial()
    {
        // SpriteFlashシェーダーを検索
        Shader flashShader = Shader.Find("Custom/SpriteFlash");

        if (flashShader != null)
        {
            _flashMaterial = new Material(flashShader);
            _flashMaterial.mainTexture = _originalMaterial.mainTexture;
            _flashMaterial.SetColor(FlashColorProp, flashColor);
        }
        else
        {
            Debug.LogWarning("[SpriteFlashEffect] Custom/SpriteFlash shader not found. Using fallback.");
            _flashMaterial = null;
        }
    }

    /// <summary>
    /// 白フラッシュを実行
    /// </summary>
    public void Flash()
    {
        Flash(flashColor, flashDuration);
    }

    /// <summary>
    /// 指定色でフラッシュを実行
    /// </summary>
    public void Flash(Color color, float duration)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        if (_flashMaterial != null)
        {
            // シェーダーベースのフラッシュ
            _flashMaterial.SetColor(FlashColorProp, color);
            _spriteRenderer.material = _flashMaterial;

            // フラッシュイン
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _flashMaterial.SetFloat(FlashAmount, t);
                yield return null;
            }

            _flashMaterial.SetFloat(FlashAmount, 1f);

            // フラッシュアウト
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / halfDuration);
                _flashMaterial.SetFloat(FlashAmount, t);
                yield return null;
            }

            _flashMaterial.SetFloat(FlashAmount, 0f);
            _spriteRenderer.material = _originalMaterial;
        }
        else
        {
            // フォールバック: SpriteRendererのcolorを使用
            Color originalColor = _spriteRenderer.color;

            // フラッシュイン
            _spriteRenderer.color = color;
            yield return new WaitForSeconds(duration);

            // 元に戻す
            _spriteRenderer.color = originalColor;
        }

        _flashCoroutine = null;
    }

    void OnDestroy()
    {
        // 動的に作成したマテリアルを破棄
        if (_flashMaterial != null)
        {
            Destroy(_flashMaterial);
        }
    }
}
