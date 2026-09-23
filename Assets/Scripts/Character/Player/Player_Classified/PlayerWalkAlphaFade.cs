using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the alpha channel fading effect on the player's sprite renderer
/// when pressing the Space key during the walking state (State.Moving).
/// </summary>
[DisallowMultipleComponent]
public class PlayerWalkAlphaFade : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Alpha Settings")]
    [Tooltip("Valor objetivo mínimo al que desciende el canal alfa durante el efecto.")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.3f;

    [Tooltip("Tiempo en segundos que tarda en descender hasta el valor mínimo.")]
    [SerializeField, Min(0.01f)] private float fadeDownDuration = 0.2f;

    [Tooltip("Tiempo en segundos que se mantiene en el valor mínimo antes de recuperarse.")]
    [SerializeField, Min(0f)] private float holdDuration = 0.1f;

    [Tooltip("Tiempo en segundos que tarda en regresar a la normalidad.")]
    [SerializeField, Min(0.01f)] private float fadeUpDuration = 0.3f;

    private PlayerController _player;
    private PlayerInputHandler _input;
    private SpriteRenderer _spriteRenderer;
    private SpriteMaterialColorOverride _colorOverride;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _fadeCoroutine;
    private float _cachedOriginalAlpha = 1f;

    public float MinAlpha
    {
        get => minAlpha;
        set => minAlpha = Mathf.Clamp01(value);
    }
    public float FadeDownDuration
    {
        get => fadeDownDuration;
        set => fadeDownDuration = Mathf.Max(0.01f, value);
    }
    public float HoldDuration
    {
        get => holdDuration;
        set => holdDuration = Mathf.Max(0f, value);
    }
    public float FadeUpDuration
    {
        get => fadeUpDuration;
        set => fadeUpDuration = Mathf.Max(0.01f, value);
    }

    private void Start()
    {
        _player = GetComponent<PlayerController>();
        _input = GetComponent<PlayerInputHandler>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        _colorOverride = GetComponentInChildren<SpriteMaterialColorOverride>(true);
        _propBlock = new MaterialPropertyBlock();

        if (_spriteRenderer != null)
            _cachedOriginalAlpha = _spriteRenderer.color.a;
    }

    private void Update()
    {
        if (CheckSpaceInput() && IsEligibleForFade())
        {
            TriggerFade();
        }
    }

    private void OnDisable()
    {
        ResetAlphaInstantly();
    }

    public bool IsEligibleForFade()
    {
        if (_spriteRenderer == null || !_spriteRenderer.enabled || _spriteRenderer.sharedMaterial == null)
            return false;

        return true;
    }

    public void TriggerFade()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(AlphaFadeRoutine());
    }

    private IEnumerator AlphaFadeRoutine()
    {
        float startAlpha = _spriteRenderer != null ? _spriteRenderer.color.a : _cachedOriginalAlpha;
        float targetMin = minAlpha * _cachedOriginalAlpha;

        // Fase 1: Bajada de alfa
        float elapsed = 0f;
        while (elapsed < fadeDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDownDuration);
            ApplyAlpha(Mathf.Lerp(startAlpha, targetMin, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        ApplyAlpha(targetMin);

        // Fase 2: Mantener en el valor mínimo
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fase 3: Recuperación de alfa
        elapsed = 0f;
        while (elapsed < fadeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeUpDuration);
            ApplyAlpha(Mathf.Lerp(targetMin, _cachedOriginalAlpha, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        ResetAlphaInstantly();
    }

    private void ApplyAlpha(float alpha)
    {
        if (_spriteRenderer == null) return;

        // 1. Color del SpriteRenderer
        Color c = _spriteRenderer.color;
        c.a = alpha;
        _spriteRenderer.color = c;

        // 2. SpriteMaterialColorOverride si existe en Visual
        if (_colorOverride != null)
        {
            Color overrideColor = _colorOverride.TintColor;
            overrideColor.a = alpha;
            _colorOverride.TintColor = overrideColor;
        }

        // 3. MaterialPropertyBlock para compatibilidad con URP/Lit shaders
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        _spriteRenderer.GetPropertyBlock(_propBlock);
        Color baseColor = Color.white;
        baseColor.a = alpha;
        _propBlock.SetColor(BaseColorId, baseColor);
        _propBlock.SetColor(ColorId, baseColor);
        _spriteRenderer.SetPropertyBlock(_propBlock);
    }

    public void ResetAlphaInstantly()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        ApplyAlpha(_cachedOriginalAlpha);
    }

    private bool CheckSpaceInput()
    {
        if (_input != null && _input.IsSpacePressed)
        {
            _input.ConsumeSpaceInput();
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return false;
    }
}
