using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using MystifyFX;
using UnityEngine;
using UnityEngine.Serialization;
using VolumetricFogAndMist2;

/// <summary>
/// Manages intensity transitions between the 3 lighting states:
/// Tapat (base) → Radiografia (transition) → Lluna (full moon).
///
/// Inspector setup:
/// - Each state defines a Sky to activate and which lights change with their range (from → to).
/// - In Tapat, startIntensity = targetIntensity (fixed value).
/// - In Radiografia and Lluna, startIntensity and targetIntensity define the lerp range.
/// - transitionDuration controls the default duration of each transition.
/// </summary>
public class LightingStateManager : MonoBehaviour
{
    public enum LightingState { Tapat = 0, Radiografia = 1, Lluna = 2, Blue = 3 }

    [System.Serializable]
    public struct LightEntry
    {
        public Light light;
        public float startIntensity;
        public float targetIntensity;
    }

    [System.Serializable]
    public struct LightingStateData
    {
        public string stateName;
        [Tooltip("Trigger del Animator del cielo. Vacío = no dispara nada (ej: estado 1, cielo en reposo).")]
        public string skyAnimatorTrigger;
        public LightEntry[] entries;
    }

    [Header("Sky")]
    [Tooltip("El único Animator del cielo. Comparte el mismo objeto para todos los estados.")]
    [SerializeField] private Animator skyAnimator;
    [Tooltip("HighlightEffect del cielo (en Sky01). Se activa al entrar a la iglesia.")]
    [SerializeField] private HighlightEffect skyHighlightEffect;
    [Tooltip("Si está marcado, asegura que el HighlightEffect del cielo comience desactivado.")]
    [SerializeField] private bool deactivateSkyHighlightOnStart = true;

    [Header("Lights Objects")]
    [Tooltip("Activo en estados 1-3 (Tapat, Radiografia, Lluna).")]
    [SerializeField] private GameObject redLightsObject;
    [Tooltip("Activo solo en estado 4 (Azul).")]
    [SerializeField] private GameObject blueLightsObject;
    [SerializeField] private GameObject redSource;
    [SerializeField] private GameObject blueSource;

    [Header("Baked Lighting")]
    [SerializeField] private LightmapStateManager lightmapStateManager;

    [Header("Lighting States")]
    [SerializeField] private LightingStateData[] states = new LightingStateData[4];

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 3f;

    [Header("Blue State — Delay")]
    [Tooltip("Segundos entre pulsar tecla 4 y el snap a luces azules. El cielo y los objetos cambian al instante.")]
    [SerializeField] private float blueLightsDelay = 2f;

    [Header("Godrays")]
    [Tooltip("Primer GameObject de Godrays (en LightingScene).")]
    [SerializeField] private GameObject godRay1;
    [Tooltip("Segundo GameObject de Godrays (en LightingScene).")]
    [SerializeField] private GameObject godRay2;
    [Tooltip("Si está marcado, desactiva los GameObjects en Start además de poner su escala a 0.")]
    [SerializeField] private bool deactivateGodRaysOnStart = true;
    [Tooltip("Tiempo de espera antes de comenzar el aumento de escala tras cruzar la puerta (en segundos).")]
    [SerializeField, Min(0f)] private float godRaysDelay = 0f;
    [Tooltip("Tiempo que tarda la escala en aumentar de 0 a 1 (en segundos).")]
    [SerializeField, Min(0.01f)] private float godRaysScaleDuration = 1.5f;
    [Tooltip("Escala objetivo final del primer Godray (por defecto 1, 1, 1).")]
    [FormerlySerializedAs("godRaysTargetScale")]
    [SerializeField] private Vector3 godRay1TargetScale = Vector3.one;
    [Tooltip("Escala objetivo final del segundo Godray (por defecto 1, 1, 1).")]
    [SerializeField] private Vector3 godRay2TargetScale = Vector3.one;

    [Header("Mystify Effect")]
    [Tooltip("MystifyEffect al que se le reducirá la propiedad Global Opacity al entrar a la iglesia.")]
    [SerializeField] private MystifyEffect churchMystifyEffect;
    [Tooltip("Tiempo de espera antes de comenzar a bajar la opacidad tras cruzar la puerta (en segundos).")]
    [SerializeField, Min(0f)] private float mystifyFadeDelay = 0f;
    [Tooltip("Tiempo que tarda en reducirse la opacidad (en segundos).")]
    [SerializeField, Min(0.01f)] private float mystifyFadeDuration = 1.5f;
    [Tooltip("Opacidad inicial al empezar o resetear la iglesia (por defecto 1).")]
    [SerializeField, Range(0f, 1f)] private float mystifyStartOpacity = 1f;
    [Tooltip("Opacidad objetivo final al entrar a la iglesia (por defecto 0).")]
    [SerializeField, Range(0f, 1f)] private float mystifyTargetOpacity = 0f;
    [Tooltip("Si está marcado, desactiva el GameObject de MystifyEffect si la opacidad objetivo llega a 0.")]
    [SerializeField] private bool deactivateMystifyWhenZero = false;

    [Header("Volumetric Fog")]
    [Tooltip("Volumetric Fog al que se le aumentará la distancia de inicio (Distant Fog).")]
    [SerializeField] private VolumetricFog distantFog;
    [Tooltip("Tiempo que tarda en transicionar desde la distancia actual hasta la distancia inicial fogStartDistance (en segundos).")]
    [SerializeField, Min(0.001f)] private float fogToStartDuration = 1.0f;
    [Tooltip("Tiempo de espera antes de comenzar a incrementar la distancia hacia fogEndDistance (en segundos).")]
    [SerializeField, Min(0f)] private float fogIncreaseDelay = 0f;
    [Tooltip("Tiempo que tarda en aumentar la distancia hasta fogEndDistance (en segundos).")]
    [SerializeField, Min(0.01f)] private float fogTransitionDuration = 1.5f;
    [Tooltip("Distancia inicial al empezar o resetear la iglesia (por defecto 6).")]
    [SerializeField, Min(0f)] private float fogStartDistance = 6f;
    [Tooltip("Distancia objetivo final al entrar a la iglesia (por defecto 38).")]
    [SerializeField, Min(0f)] private float fogEndDistance = 38f;

    [Header("Timed Lights")]
    [Tooltip("Primera luz a activar por intervalos.")]
    [SerializeField] private Light timedLight1;
    [Tooltip("Segunda luz a activar por intervalos.")]
    [SerializeField] private Light timedLight2;
    [Tooltip("Si está marcado, desactiva las luces en Start además de poner su intensidad a la inicial.")]
    [SerializeField] private bool deactivateTimedLightsOnStart = true;
    [Tooltip("Intensidad inicial antes de comenzar los aumentos.")]
    [SerializeField, Min(0f)] private float timedLightsStartIntensity = 0f;
    [Tooltip("Intensidad objetivo para la primera luz.")]
    [SerializeField, Min(0f)] private float timedLight1TargetIntensity = 1f;
    [Tooltip("Intensidad objetivo para la segunda luz.")]
    [SerializeField, Min(0f)] private float timedLight2TargetIntensity = 1f;
    [Tooltip("Tiempo de espera antes de comenzar la activación de la primera luz (en segundos).")]
    [SerializeField, Min(0f)] private float timedLight1Delay = 0f;
    [Tooltip("Tiempo de espera antes de comenzar la activación de la segunda luz (en segundos).")]
    [SerializeField, Min(0f)] private float timedLight2Delay = 0f;
    [Tooltip("Tiempo que tarda la transición de intensidad con SmoothStep (en segundos).")]
    [SerializeField, Min(0.01f)] private float timedLightsTransitionDuration = 1.5f;

    [Header("Play Mode Test")]
    [SerializeField] private LightingState _previewState;
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [Tooltip("Si está activo, las transiciones al pulsar los números 1-4 solo funcionarán tras interactuar con un botón de test (inicial o diálogo).")]
    [SerializeField] private bool requireTestModeToEnableShortcuts = true;

    public static bool KeyboardTransitionsUnlocked { get; private set; } = false;

    public static void UnlockKeyboardTransitions()
    {
        KeyboardTransitionsUnlocked = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetKeyboardTransitions()
    {
        KeyboardTransitionsUnlocked = false;
    }

    private int _currentStateIndex = -1;
    private int _currentSkyIndex = -1;
    private Coroutine _activeTransition;
    private Coroutine _blueTransitionCoroutine;
    private Coroutine _godRaysCoroutine;
    private Coroutine _mystifyCoroutine;
    private Coroutine _distantFogCoroutine;
    private Coroutine _timedLight1Coroutine;
    private Coroutine _timedLight2Coroutine;
    private bool _godRaysTriggered;
    private readonly List<GameObject> _activatedByManager = new List<GameObject>();
    private readonly Dictionary<Light, FireVisualScript> _fireVisualsByLight =
        new Dictionary<Light, FireVisualScript>();

    private void Awake()
    {
        if (lightmapStateManager == null)
            lightmapStateManager = GetComponent<LightmapStateManager>();

        EnsureSkyHighlightReference();
        EnsureMystifyEffectReference();
        EnsureDistantFogReference();
        CacheFireVisuals();
    }

    private void Start()
    {
        InitGodRays();
        InitMystifyEffect();
        //InitDistantFog();
        InitTimedLights();
    }

    private void EnsureSkyHighlightReference()
    {
        if (skyHighlightEffect == null && skyAnimator != null)
            skyHighlightEffect = skyAnimator.GetComponent<HighlightEffect>();
    }

    private void EnsureMystifyEffectReference()
    {
        if (churchMystifyEffect == null)
        {
            GameObject doorBlur = GameObject.Find("DoorBlur");
            if (doorBlur != null)
                churchMystifyEffect = doorBlur.GetComponent<MystifyEffect>();

            if (churchMystifyEffect == null)
                churchMystifyEffect = Object.FindAnyObjectByType<MystifyEffect>();
        }
    }

    private void InitGodRays()
    {
        EnsureSkyHighlightReference();
        if (deactivateSkyHighlightOnStart && skyHighlightEffect != null)
            skyHighlightEffect.enabled = false;

        if (godRay1 != null)
        {
            godRay1.transform.localScale = Vector3.zero;
            if (deactivateGodRaysOnStart)
                godRay1.SetActive(false);
        }

        if (godRay2 != null)
        {
            godRay2.transform.localScale = Vector3.zero;
            if (deactivateGodRaysOnStart)
                godRay2.SetActive(false);
        }
    }

    private void InitTimedLights()
    {
        if (timedLight1 != null)
        {
            ApplyLightIntensity(timedLight1, timedLightsStartIntensity);
            if (deactivateTimedLightsOnStart)
                timedLight1.gameObject.SetActive(false);
        }

        if (timedLight2 != null)
        {
            ApplyLightIntensity(timedLight2, timedLightsStartIntensity);
            if (deactivateTimedLightsOnStart)
                timedLight2.gameObject.SetActive(false);
        }
    }

    private void InitMystifyEffect()
    {
        EnsureMystifyEffectReference();
        if (churchMystifyEffect != null)
        {
            if (deactivateMystifyWhenZero && !churchMystifyEffect.gameObject.activeSelf)
                churchMystifyEffect.gameObject.SetActive(true);

            SetMystifyOpacity(mystifyStartOpacity);
        }
    }

    private void SetMystifyOpacity(float opacity)
    {
        EnsureMystifyEffectReference();
        if (churchMystifyEffect == null) return;

        MystifyEffectProfile profile = churchMystifyEffect.profile;
        if (profile == null) return;

        profile.globalOpacity = opacity;
        churchMystifyEffect.UpdateMaterialProperties();
        churchMystifyEffect.UpdateMaterialPropertiesNow();
    }

    private void EnsureDistantFogReference()
    {
        if (distantFog == null)
        {
            distantFog = Object.FindAnyObjectByType<VolumetricFog>();
        }
    }

    private void InitDistantFog()
    {
        EnsureDistantFogReference();
        if (distantFog != null)
        {
            SetDistantFogDistance(fogStartDistance);
        }
    }

    private void SetDistantFogDistance(float distance)
    {
        EnsureDistantFogReference();
        if (distantFog == null) return;

        VolumetricFogProfile profile = distantFog.settings;
        if (profile == null) return;

        profile.distantFog = true;
        profile.distantFogStartDistance = distance;
        distantFog.UpdateMaterialProperties();
        distantFog.UpdateMaterialPropertiesNow();
    }

    /// <summary>
    /// Activa la iluminación de la iglesia (cielo y Godrays) en todos los gestores activos con una única búsqueda.
    /// </summary>
    public static void TriggerChurchLighting()
    {
        LightingStateManager[] managers = Object.FindObjectsByType<LightingStateManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var manager in managers)
        {
            manager.EnterChurch(skipDelay: false);
        }
    }

    /// <summary>
    /// Aplica todos los cambios de iluminación al entrar a la iglesia: activa el cielo, anima los Godrays, reduce la opacidad de MystifyEffect y aumenta la distancia de Distant Fog.
    /// </summary>
    public void EnterChurch(bool skipDelay = false)
    {
        ActivateSkyHighlight();
        EnsureMystifyEffectReference();
        EnsureDistantFogReference();

        if (_godRaysTriggered) return;
        _godRaysTriggered = true;

        if (_godRaysCoroutine != null)
            StopCoroutine(_godRaysCoroutine);

        _godRaysCoroutine = StartCoroutine(AnimateGodRaysRoutine(skipDelay));

        if (_mystifyCoroutine != null)
            StopCoroutine(_mystifyCoroutine);

        _mystifyCoroutine = StartCoroutine(FadeMystifyRoutine(skipDelay));

        if (_distantFogCoroutine != null)
            StopCoroutine(_distantFogCoroutine);

        _distantFogCoroutine = StartCoroutine(DistantFogRoutine(skipDelay));

        TriggerTimedLights(skipDelay);
    }

    /// <summary>
    /// Activa la iluminación de la iglesia y asegura que los godrays se animen a su tamaño objetivo
    /// (incluso si habían sido desactivados previamente por otra tecla o si la iglesia ya se había visitado).
    /// </summary>
    public void ActivateChurchLightingAndGodRays(bool skipDelay = true)
    {
        ActivateSkyHighlight();
        EnsureMystifyEffectReference();
        EnsureDistantFogReference();

        // Animar Godrays si no están ya completamente activos a su escala final
        if (!AreGodRaysActive() || _godRaysCoroutine != null)
        {
            if (_godRaysCoroutine != null)
                StopCoroutine(_godRaysCoroutine);

            _godRaysTriggered = true;
            _godRaysCoroutine = StartCoroutine(AnimateGodRaysRoutine(skipDelay));
        }

        // Mystify effect fade
        if (_mystifyCoroutine != null)
            StopCoroutine(_mystifyCoroutine);
        _mystifyCoroutine = StartCoroutine(FadeMystifyRoutine(skipDelay));

        // Distant fog
        if (_distantFogCoroutine != null)
            StopCoroutine(_distantFogCoroutine);
        _distantFogCoroutine = StartCoroutine(DistantFogRoutine(skipDelay));

        // Timed lights
        TriggerTimedLights(skipDelay);
    }

    /// <summary>
    /// Activa la iluminación de la iglesia y los godrays en todos los LightingStateManager activos.
    /// </summary>
    public static void TriggerChurchLightingAndGodRays(bool skipDelay = true)
    {
        LightingStateManager[] managers = Object.FindObjectsByType<LightingStateManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var manager in managers)
        {
            manager.ActivateChurchLightingAndGodRays(skipDelay);
        }
    }

    /// <summary>
    /// Activa el brillo del cielo mediante el componente HighlightEffect (Sky01).
    /// </summary>
    public void ActivateSkyHighlight()
    {
        EnsureSkyHighlightReference();
        if (skyHighlightEffect != null)
            skyHighlightEffect.enabled = true;
    }

    /// <summary>
    /// Activa el brillo del cielo en todos los LightingStateManager activos.
    /// </summary>
    public static void ActivateAllSkyHighlights()
    {
        LightingStateManager[] managers = Object.FindObjectsByType<LightingStateManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var manager in managers)
        {
            manager.ActivateSkyHighlight();
        }
    }

    /// <summary>
    /// Indica si alguno de los godrays está actualmente activo o en proceso de animación.
    /// </summary>
    public bool AreGodRaysActive()
    {
        return (_godRaysCoroutine != null)
            || (godRay1 != null && godRay1.activeSelf)
            || (godRay2 != null && godRay2.activeSelf);
    }

    /// <summary>
    /// Desactiva de inmediato los Godrays y restablece su escala a cero.
    /// </summary>
    public void DeactivateGodRays()
    {
        if (_godRaysCoroutine != null)
        {
            StopCoroutine(_godRaysCoroutine);
            _godRaysCoroutine = null;
        }

        if (godRay1 != null)
        {
            godRay1.transform.localScale = Vector3.zero;
            godRay1.SetActive(false);
        }

        if (godRay2 != null)
        {
            godRay2.transform.localScale = Vector3.zero;
            godRay2.SetActive(false);
        }

        _godRaysTriggered = false;
    }

    /// <summary>
    /// Desactiva los Godrays en todos los LightingStateManager activos si están activados.
    /// </summary>
    public static void DeactivateAllGodRaysIfActive()
    {
        LightingStateManager[] managers = Object.FindObjectsByType<LightingStateManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var manager in managers)
        {
            if (manager.AreGodRaysActive())
                manager.DeactivateGodRays();
        }
    }

    /// <summary>
    /// Inicia la secuencia de activación y aumento de intensidad en intervalos para las dos luces temporizadas.
    /// </summary>
    public void TriggerTimedLights(bool skipDelay = false)
    {
        if (_timedLight1Coroutine != null)
            StopCoroutine(_timedLight1Coroutine);

        float delay1 = skipDelay ? 0f : timedLight1Delay;
        _timedLight1Coroutine = StartCoroutine(AnimateTimedLightRoutine(timedLight1, delay1, timedLight1TargetIntensity));

        if (_timedLight2Coroutine != null)
            StopCoroutine(_timedLight2Coroutine);

        float delay2 = skipDelay ? 0f : timedLight2Delay;
        _timedLight2Coroutine = StartCoroutine(AnimateTimedLightRoutine(timedLight2, delay2, timedLight2TargetIntensity));
    }

    /// <summary>
    /// Restablece la iluminación de la iglesia al estado inicial (Godrays a escala cero, cielo desactivado, MystifyEffect restaurado y Distant Fog reseteado).
    /// </summary>
    public void ResetChurchLighting()
    {
        if (_godRaysCoroutine != null)
        {
            StopCoroutine(_godRaysCoroutine);
            _godRaysCoroutine = null;
        }

        if (_mystifyCoroutine != null)
        {
            StopCoroutine(_mystifyCoroutine);
            _mystifyCoroutine = null;
        }

        if (_distantFogCoroutine != null)
        {
            StopCoroutine(_distantFogCoroutine);
            _distantFogCoroutine = null;
        }

        if (_timedLight1Coroutine != null)
        {
            StopCoroutine(_timedLight1Coroutine);
            _timedLight1Coroutine = null;
        }

        if (_timedLight2Coroutine != null)
        {
            StopCoroutine(_timedLight2Coroutine);
            _timedLight2Coroutine = null;
        }

        _godRaysTriggered = false;
        InitGodRays();
        InitMystifyEffect();
        InitDistantFog();
        InitTimedLights();
    }

    private IEnumerator AnimateTimedLightRoutine(Light targetLight, float delay, float targetIntensity)
    {
        if (targetLight == null)
            yield break;

        if (!targetLight.gameObject.activeSelf || delay > 0f)
            ApplyLightIntensity(targetLight, timedLightsStartIntensity);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!targetLight.gameObject.activeSelf)
            targetLight.gameObject.SetActive(true);

        float duration = Mathf.Max(0.001f, timedLightsTransitionDuration);
        float elapsed = 0f;
        float startIntensity = targetLight.intensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, smoothT);
            ApplyLightIntensity(targetLight, currentIntensity);
            yield return null;
        }

        ApplyLightIntensity(targetLight, targetIntensity);
    }

    private IEnumerator DistantFogRoutine(bool skipDelay = false)
    {
        EnsureDistantFogReference();
        if (distantFog == null)
            yield break;

        VolumetricFogProfile profile = distantFog.settings;
        if (profile == null)
            yield break;

        profile.distantFog = true;

        // Fase 1: Transicionar desde el valor actual hasta fogStartDistance
        float currentDistance = profile.distantFogStartDistance;
        float toStartDuration = Mathf.Max(0.001f, fogToStartDuration);
        float elapsed = 0f;

        while (elapsed < toStartDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / toStartDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float dist = Mathf.Lerp(currentDistance, fogStartDistance, smoothT);

            profile.distantFogStartDistance = dist;
            distantFog.UpdateMaterialProperties();
            distantFog.UpdateMaterialPropertiesNow();

            yield return null;
        }

        profile.distantFogStartDistance = fogStartDistance;
        distantFog.UpdateMaterialProperties();
        distantFog.UpdateMaterialPropertiesNow();

        // Delay opcional antes de aumentar hacia fogEndDistance
        if (!skipDelay && fogIncreaseDelay > 0f)
            yield return new WaitForSeconds(fogIncreaseDelay);

        // Fase 2: Transicionar desde fogStartDistance hasta fogEndDistance
        float duration = Mathf.Max(0.001f, fogTransitionDuration);
        elapsed = 0f;
        float startDist = profile.distantFogStartDistance;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float dist = Mathf.Lerp(startDist, fogEndDistance, smoothT);

            profile.distantFogStartDistance = dist;
            distantFog.UpdateMaterialProperties();
            distantFog.UpdateMaterialPropertiesNow();

            yield return null;
        }

        profile.distantFogStartDistance = fogEndDistance;
        distantFog.UpdateMaterialProperties();
        distantFog.UpdateMaterialPropertiesNow();

        _distantFogCoroutine = null;
    }

    private IEnumerator FadeMystifyRoutine(bool skipDelay = false)
    {
        EnsureMystifyEffectReference();
        if (churchMystifyEffect == null)
            yield break;

        MystifyEffectProfile profile = churchMystifyEffect.profile;
        if (profile == null)
            yield break;

        if (!skipDelay && mystifyFadeDelay > 0f)
            yield return new WaitForSeconds(mystifyFadeDelay);

        float duration = Mathf.Max(0.001f, mystifyFadeDuration);
        float elapsed = 0f;
        float startOpacity = profile.globalOpacity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentOpacity = Mathf.Lerp(startOpacity, mystifyTargetOpacity, smoothT);

            profile.globalOpacity = currentOpacity;
            churchMystifyEffect.UpdateMaterialProperties();
            churchMystifyEffect.UpdateMaterialPropertiesNow();

            yield return null;
        }

        profile.globalOpacity = mystifyTargetOpacity;
        churchMystifyEffect.UpdateMaterialProperties();
        churchMystifyEffect.UpdateMaterialPropertiesNow();

        if (deactivateMystifyWhenZero && mystifyTargetOpacity <= 0f)
        {
            churchMystifyEffect.gameObject.SetActive(false);
        }

        _mystifyCoroutine = null;
    }

    private IEnumerator AnimateGodRaysRoutine(bool skipDelay = false)
    {
        if (godRay1 != null)
        {
            godRay1.transform.localScale = Vector3.zero;
            godRay1.SetActive(true);
        }

        if (godRay2 != null)
        {
            godRay2.transform.localScale = Vector3.zero;
            godRay2.SetActive(true);
        }

        if (!skipDelay && godRaysDelay > 0f)
            yield return new WaitForSeconds(godRaysDelay);

        if (godRay1 != null && !godRay1.activeSelf)
            godRay1.SetActive(true);

        if (godRay2 != null && !godRay2.activeSelf)
            godRay2.SetActive(true);

        float duration = Mathf.Max(0.001f, godRaysScaleDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (godRay1 != null)
                godRay1.transform.localScale = Vector3.Lerp(Vector3.zero, godRay1TargetScale, smoothT);

            if (godRay2 != null)
                godRay2.transform.localScale = Vector3.Lerp(Vector3.zero, godRay2TargetScale, smoothT);

            yield return null;
        }

        if (godRay1 != null)
            godRay1.transform.localScale = godRay1TargetScale;

        if (godRay2 != null)
            godRay2.transform.localScale = godRay2TargetScale;

        _godRaysCoroutine = null;
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts) return;
        if (requireTestModeToEnableShortcuts && !KeyboardTransitionsUnlocked) return;

        if      (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ApplyNumberTransition(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ApplyNumberTransition(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ApplyNumberTransition(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) ApplyNumberTransition(4);
    }

    /// <summary>
    /// Ejecuta la transición correspondiente al pulsar los números del teclado (1-4):
    /// - Todos los números: activan el brillo del cielo (HighlightEffect) y sitúan la luz móvil de golpe en su punto final.
    /// - Tecla 3: vincula todo lo que ocurre al entrar en la iglesia (iluminación y godrays).
    /// - Teclas 1, 2 y 4: desactivan los godrays si están activados.
    /// </summary>
    public void ApplyNumberTransition(int phase)
    {
        // 1. Activar el brillo del cielo del componente highlight (todos los números)
        ActivateAllSkyHighlights();

        // 2. Reposicionar la luz que se mueve en X de golpe en su punto final (todos los números)
        IntroSequenceManager.SnapAllMovingLightsToFinalPosition();

        // 3. Vincular iluminación/godrays de la iglesia o desactivar godrays:
        if (phase == 3)
        {
            // El número 3 vincula todo lo que ocurre al entrar en la iglesia (iluminación y godrays)
            TriggerChurchLightingAndGodRays(skipDelay: true);
        }
        else
        {
            // El resto de números desactivan los godrays si están activados
            DeactivateAllGodRaysIfActive();
        }

        // 4. Transición de iluminación de la fase correspondiente
        ApplyPhase(phase);
    }

    public void ApplyPhase(int phase)
    {
        switch (phase)
        {
            case 1:
                TransitionTo(LightingState.Tapat);
                break;
            case 2:
                TransitionTo(LightingState.Radiografia);
                break;
            case 3:
                TransitionTo(LightingState.Lluna);
                break;
            case 4:
                StartBlueTransition();
                break;
            default:
                Debug.LogWarning($"[{nameof(LightingStateManager)}] La fase {phase} no existe.", this);
                break;
        }
    }

    /// <summary>Dispara el cielo azul al instante y aplica el snap de luces tras blueLightsDelay segundos.</summary>
    public void StartBlueTransition()
    {
        if (_currentStateIndex == (int)LightingState.Blue && _blueTransitionCoroutine == null)
            return;

        CancelBlueTransition();
        StopActiveTransition();
        ApplyBakeForState(LightingState.Blue);

        _blueTransitionCoroutine = StartCoroutine(BlueTransitionCoroutine());
    }

    private IEnumerator BlueTransitionCoroutine()
    {
        TriggerSkyOnly(LightingState.Blue);
        yield return new WaitForSeconds(blueLightsDelay);

        _blueTransitionCoroutine = null;
        SnapToState(LightingState.Blue);
    }

    /// <summary>Transitions to the given state, interpolating from startIntensity to targetIntensity. Does nothing if already in that state.</summary>
    public void TransitionTo(LightingState state, float duration = -1f)
    {
        int idx = (int)state;
        if (idx < 0 || idx >= states.Length) return;

        CancelBlueTransition();

        if (idx == _currentStateIndex && _activeTransition == null) return;

        StopActiveTransition();

        _currentStateIndex = idx;
        ApplyBakeForState(state);
        SwitchSky(idx);

        float dur = duration < 0f ? transitionDuration : duration;
        _activeTransition = StartCoroutine(TransitionCoroutine(states[idx], dur));
    }

    /// <summary>Advances to the next state (Tapat → Radiografia → Lluna).</summary>
    public void AdvanceState()
    {
        if (_currentStateIndex < states.Length - 1)
            TransitionTo((LightingState)(_currentStateIndex + 1));
    }

    /// <summary>Forces the given state instantly (no interpolation). Useful for initializing the scene at startup.</summary>
    public void SnapToState(LightingState state)
    {
        int idx = (int)state;
        if (idx < 0 || idx >= states.Length) return;

        CancelBlueTransition();
        StopActiveTransition();

        ApplyBakeForState(state);
        SwitchSky(idx);
        DeactivatePreviousActivations();
        ActivateStateObjects(states[idx]);

        foreach (var entry in states[idx].entries)
        {
            if (entry.light != null)
                ApplyLightIntensity(entry.light, entry.targetIntensity);
        }
        _currentStateIndex = idx;
    }

    /// <summary>Dispara únicamente el trigger del Animator del cielo, sin tocar lightsObject ni intensidades.
    /// Úsalo cuando el trigger debe adelantarse al cambio de luces (ej: estado 4).</summary>
    public void TriggerSkyOnly(LightingState state)
    {
        int idx = (int)state;
        if (idx < 0 || idx >= states.Length) return;
        _currentSkyIndex = idx;
        var s = states[idx];
        if (skyAnimator != null && !string.IsNullOrEmpty(s.skyAnimatorTrigger))
            skyAnimator.SetTrigger(s.skyAnimatorTrigger);
    }

    private void SwitchSky(int stateIndex)
    {
        bool isBlue = stateIndex == (int)LightingState.Blue;

        if (redLightsObject  != null) redLightsObject.SetActive(!isBlue);
        if (blueLightsObject != null) blueLightsObject.SetActive(isBlue);
        if (redSource        != null) redSource.SetActive(!isBlue);
        if (blueSource       != null) blueSource.SetActive(isBlue);

        // Si TriggerSkyOnly ya disparó el trigger de este estado, no lo repetimos.
        if (_currentSkyIndex == stateIndex) return;

        _currentSkyIndex = stateIndex;
        var current = states[stateIndex];
        if (skyAnimator != null && !string.IsNullOrEmpty(current.skyAnimatorTrigger))
            skyAnimator.SetTrigger(current.skyAnimatorTrigger);
    }

    private void ApplyBakeForState(LightingState state)
    {
        if (lightmapStateManager == null)
            return;

        switch (state)
        {
            case LightingState.Tapat:
                lightmapStateManager.ApplyWarm1();
                break;
            case LightingState.Radiografia:
            case LightingState.Lluna:
                lightmapStateManager.ApplyWarm2();
                break;
            case LightingState.Blue:
                lightmapStateManager.ApplyBlue();
                break;
        }
    }

    [ContextMenu("Apply State")]
    private void ApplyPreviewState() => TransitionTo(_previewState);

    private void DeactivatePreviousActivations()
    {
        foreach (var go in _activatedByManager)
            if (go != null) go.SetActive(false);
        _activatedByManager.Clear();
    }

    private void ActivateStateObjects(LightingStateData target)
    {
        foreach (var entry in target.entries)
        {
            if (entry.light == null) continue;
            var go = entry.light.gameObject;
            if (!go.activeSelf)
            {
                go.SetActive(true);
                _activatedByManager.Add(go);
            }
        }
    }

    private IEnumerator TransitionCoroutine(LightingStateData target, float duration)
    {
        DeactivatePreviousActivations();
        ActivateStateObjects(target);
        ApplyStartIntensities(target);

        if (duration <= 0f)
        {
            ApplyTargetIntensities(target);
            _activeTransition = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < target.entries.Length; i++)
            {
                var entry = target.entries[i];
                if (entry.light != null)
                {
                    float intensity = Mathf.Lerp(
                        entry.startIntensity,
                        entry.targetIntensity,
                        t);
                    ApplyLightIntensity(entry.light, intensity);
                }
            }

            yield return null;
        }

        ApplyTargetIntensities(target);
        _activeTransition = null;
    }

    private void ApplyStartIntensities(LightingStateData target)
    {
        foreach (var entry in target.entries)
        {
            if (entry.light != null)
                ApplyLightIntensity(entry.light, entry.startIntensity);
        }
    }

    private void ApplyTargetIntensities(LightingStateData target)
    {
        foreach (var entry in target.entries)
        {
            if (entry.light != null)
                ApplyLightIntensity(entry.light, entry.targetIntensity);
        }
    }

    private void CacheFireVisuals()
    {
        _fireVisualsByLight.Clear();

        if (timedLight1 != null && timedLight1.TryGetComponent(out FireVisualScript fv1))
            _fireVisualsByLight[timedLight1] = fv1;

        if (timedLight2 != null && timedLight2.TryGetComponent(out FireVisualScript fv2))
            _fireVisualsByLight[timedLight2] = fv2;

        foreach (var state in states)
        {
            if (state.entries == null)
                continue;

            foreach (var entry in state.entries)
            {
                if (entry.light == null || _fireVisualsByLight.ContainsKey(entry.light))
                    continue;

                if (entry.light.TryGetComponent(out FireVisualScript fireVisual))
                    _fireVisualsByLight.Add(entry.light, fireVisual);
            }
        }
    }

    private void ApplyLightIntensity(Light targetLight, float intensity)
    {
        if (_fireVisualsByLight.TryGetValue(targetLight, out FireVisualScript fireVisual)
            && fireVisual != null)
        {
            fireVisual.SetBaseIntensity(intensity);
            return;
        }

        targetLight.intensity = intensity;
    }

    private void StopActiveTransition()
    {
        if (_activeTransition == null)
            return;

        StopCoroutine(_activeTransition);
        _activeTransition = null;
    }

    private void CancelBlueTransition()
    {
        if (_blueTransitionCoroutine == null)
            return;

        StopCoroutine(_blueTransitionCoroutine);
        _blueTransitionCoroutine = null;
    }
}
