using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.Video;

/// <summary>
/// Gestiona la ventisca con VideoPlayers fijos por clip.
///
/// Idea clave:
/// - Cada clip tiene su propio VideoPlayer.
/// - Los VideoPlayers, clips y quads se configuran en escena mediante BlizzardVideoRig.
/// - Todos los players se preparan una vez y los que no están visibles quedan pausados.
/// - Al cambiar de estado NO se cambia clip, NO se llama Prepare, NO se llama Stop.
/// - Tampoco se hace seek/time=0/frame=0 en runtime.
/// - Al cambiar de estado se pausa el anterior, se reproduce el activo y se actualiza el alpha.
///
/// Estructura por tecla:
///   Tecla 1 → Transition_1 → IdleLoop_Fast (si se solicita transición)
///   Tecla 2 → Transition_2 → IdleLoop_2
///   Tecla 3 → Transition_3 → IdleLoop_3
///   Tecla 4 → Transition_4 → IdleLoop_4
///
/// Mapeo wind → vegetación VAT:
///   W1 / W4 → fast
///   W2 / W3 → slow
/// </summary>
public class WindStateManager : MonoBehaviour
{
    // ── Enum público (mantiene compatibilidad con EnvironmentStateManager) ────

    public enum WindPreset { W1_MaxIdle = 0, W2_MaxToMedium = 1, W3_MediumToMin = 2, W4_MinToMedium = 3 }

    // ── Datos serializables por estado ────────────────────────────────────────

    [System.Serializable]
    public struct WindStateData
    {
        [Range(0f, 1f)]
        [Tooltip("Opacidad del vídeo sobre la cámara durante la transición.")]
        public float transitionOpacity;

        [Range(0f, 1f)]
        [Tooltip("Opacidad del vídeo sobre la cámara durante el idle loop.")]
        public float idleOpacity;

        [HideInInspector] public float videoOpacity;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Estados (0=W1, 1=W2, 2=W3, 3=W4)")]
    [SerializeField] private WindStateData[] windStates = new WindStateData[]
    {
        // W1: solo idle fast, sin transición
        new WindStateData { transitionOpacity = 0f,   idleOpacity = 0.36f },
        // W2
        new WindStateData { transitionOpacity = 0.24f, idleOpacity = 0.22f },
        // W3
        new WindStateData { transitionOpacity = 0.12f, idleOpacity = 0.08f },
        // W4
        new WindStateData { transitionOpacity = 0.18f, idleOpacity = 0.16f },
    };

    [Header("Cámara")]
    [Tooltip("Cámara sobre la que se dibuja el vídeo. Si queda vacía se busca CAM_Main o MainCamera.")]
    [SerializeField] private Camera blizzardVideoCamera;

    [Header("Vegetación VAT - Viento dual")]
    [Tooltip("Duración global de la mezcla entre viento débil y fuerte de la vegetación VAT.")]
    [SerializeField, Min(0f)] private float vatWindBlendDuration = 1.5f;

    [Header("Planta_MJ Alembic - Distance Culling")]
    [Tooltip("Distancia de animación de Planta_MJ durante la caminata inicial. 0 = sin límite.")]
    [SerializeField, Min(0f)] private float initialWalkAlembicDistance = 30f;
    [Tooltip("Distancia de animación al aproximarse y entrar en la iglesia. 0 = sin límite.")]
    [SerializeField, Min(0f)] private float churchAlembicDistance = 100f;
    [Tooltip("Tiempo que tarda en crecer desde la distancia inicial hasta la distancia de iglesia. 0 = instantaneo.")]
    [SerializeField, Min(0f)] private float churchAlembicDistanceTransitionDuration = 2f;
    [Tooltip("Cada cuantos segundos se recalcula la visibilidad de Planta_MJ.")]
    [FormerlySerializedAs("standaloneAlembicVisibilityCheckInterval")]
    [SerializeField, Min(0.02f)] private float alembicVisibilityCheckInterval = 0.2f;
    [Header("Planta_MJ Alembic - Playback")]
    [Tooltip("Reproduce en loop el Alembic de la planta colocado en escena como Planta_MJ.")]
    [SerializeField] private bool playPlantaMjAlembic = true;
    [SerializeField, Min(0f)] private float plantaMjPlaybackSpeed = 1f;

    [Header("Preload / Playback")]
    [Tooltip("Enlaza y prepara en Awake los VideoPlayers colocados en escena.")]
    [SerializeField] private bool preloadPlayersOnAwake = true;

    [Tooltip("Si está desactivado, los vídeos permanecen inactivos hasta entrar en la iglesia.")]
    [SerializeField] private bool videoPlayersRootStartsActive = false;

    [Tooltip("Prepara los VideoPlayers ocultos para evitar tirones al mostrarlos. Solo el video visible se reproduce.")]
    [SerializeField] private bool prewarmPlayersWhileHidden = true;

    [Tooltip("Duracion del fundido simultaneo entre el final de una transicion y su idle.")]
    [SerializeField, Min(0f)] private float transitionToIdleCrossFadeDuration = 0.6f;

    // Estado interno

    private int  _currentStateIndex = -1;
    private bool _missingCameraWarningShown;

    private BlizzardVideoRig _videoRig;
    private Transform _playersRoot;
    private VideoPlayer[] _idlePlayers;
    private VideoPlayer[] _transitionPlayers;
    private readonly List<VideoPlayer> _allPlayers = new List<VideoPlayer>();
    private readonly HashSet<VideoPlayer> _prepareRequestedPlayers = new HashSet<VideoPlayer>();
    private bool _missingVideoRigWarningShown;
    private bool _hasPendingWindRequest;
    private WindPreset _pendingWindPreset;
    private bool _pendingWindRequestUsesTransition;
    private bool _pendingWindRequestForcesTransition;
    private float _pendingWindCrossFadeOverride = -1f;

    private int   _activeIdleIndex       = -1;
    private int   _activeTransitionIndex = -1;
    private int   _pendingIdleAfterTransitionIndex = -1;
    private float _transitionEndsAtUnscaled = -1f;
    private float _activeTransitionOpacity = 0f;
    private bool _transitionToIdleCrossFadeActive;
    private float _transitionToIdleCrossFadeStartedAt;
    private float _activeTransitionToIdleCrossFadeDuration;
    private bool  _videoPlayersVisible;
    private float _videoOpacityMultiplier = 1f;
    private bool _videoOpacityFadeActive;
    private float _videoOpacityFadeStartedAt;
    private float _videoOpacityFadeDuration;
    private readonly List<AlembicStreamPlayer> _plantaMjPlayers = new List<AlembicStreamPlayer>();
    private readonly List<float> _plantaMjTimes = new List<float>();
    private readonly List<Renderer[]> _plantaMjRenderers = new List<Renderer[]>();
    private readonly List<bool> _plantaMjVisible = new List<bool>();
    private bool _plantaMjAutoPlaybackReported;
    private bool _standaloneVegetationPlaybackEnabled = true;
    private bool _blizzardVideoPlaybackEnabled = true;
    private float _currentAlembicMaxDistance = 30f;
    private float _nextAlembicVisibilityCheck;
    private bool _usesChurchAlembicDistance;
    private bool _alembicDistanceTransitionActive;
    private float _alembicDistanceTransitionStartedAt;
    private float _alembicDistanceTransitionFrom;
    private float _alembicDistanceTransitionTo;
    private static readonly int VatWindBlendId = Shader.PropertyToID("_VAT_WindBlend");
    private float _vatWindBlend = 1f;
    private float _vatWindBlendTarget = 1f;

    // Lifecycle 

    private void Awake()
    {
        Shader.SetGlobalFloat(VatWindBlendId, _vatWindBlend);

        BlizzardVideoRig.RigAvailable += HandleVideoRigAvailable;
        BlizzardVideoRig.RigUnavailable += HandleVideoRigUnavailable;

        ResolvePlantaMjAlembicPlayers();
        UseInitialWalkAlembicDistance();

        if (preloadPlayersOnAwake)
            EnsureVideoRig(logWarning: false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Si otro manager llama SnapToState/TransitionTo en Awake, respetamos eso.
        // Si nadie llamó nada, dejamos W1 visible por defecto cuando exista.
        if (_currentStateIndex < 0 &&
            !_hasPendingWindRequest &&
            windStates != null &&
            windStates.Length > 0)
            SnapToState(WindPreset.W1_MaxIdle);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        BlizzardVideoRig.RigAvailable -= HandleVideoRigAvailable;
        BlizzardVideoRig.RigUnavailable -= HandleVideoRigUnavailable;
        UnsubscribeAll();
        HideAllVideos();
        // No llamamos Stop() durante cambios de estado.
        // En OnDestroy ya no importa, pero tampoco hace falta detener explícitamente.
    }

    private void Update()
    {
        if (_videoRig == null && preloadPlayersOnAwake)
            EnsureVideoRig(logWarning: false);

        RefreshVideoCamera();
        UpdateTransitionTimer();
        UpdateTransitionToIdleCrossFade();
        UpdateAlembicDistanceTransition();
        UpdateAlembicCulling();
        UpdateVideoOpacityFade();
        UpdateVatWindBlend();

        // Mantiene el alpha correcto aunque algún prepareCompleted tardío,
        // recarga de escena o llamada repetida apague el player activo.
        MaintainActiveVideoAlpha();

        AdvancePlantaMjAlembicPlayers();
    }

    /// <summary>
    /// Tecla 1: salta directamente al idle fast sin transición.
    /// No detiene ni prepara players; solo cambia opacidades.
    /// </summary>
    public void SnapToState(WindPreset preset)
    {
        int idx = (int)preset;
        if (!IsValidStateIndex(idx)) return;

        if (!EnsureVideoRig(logWarning: false))
        {
            QueuePendingWindRequest(
                preset,
                useTransition: false,
                forceTransition: false,
                crossFadeOverride: -1f);
            return;
        }

        ClearPendingWindRequest();

        CancelTransitionToIdleCrossFade();

        _currentStateIndex = idx;
        _activeTransitionIndex = -1;
        _pendingIdleAfterTransitionIndex = -1;
        _transitionEndsAtUnscaled = -1f;
        _activeTransitionOpacity = 0f;

        SetVegetationWindState(preset);

        ShowIdle(idx);
    }

    /// <summary>
    /// Muestra la transición del estado y, al cumplirse su duración,
    /// cambia al idle. No toca clip, no llama Prepare(), no llama Stop().
    /// </summary>
    public void TransitionTo(
        WindPreset preset,
        float _crossFadeOverride = -1f,
        bool forceTransition = false)
    {
        int idx = (int)preset;
        if (!IsValidStateIndex(idx)) return;

        if (!EnsureVideoRig(logWarning: false))
        {
            QueuePendingWindRequest(
                preset,
                useTransition: true,
                forceTransition: forceTransition,
                crossFadeOverride: _crossFadeOverride);
            return;
        }

        ClearPendingWindRequest();

        if (!forceTransition &&
            idx == _currentStateIndex &&
            _activeTransitionIndex < 0)
        {
            // Una petición repetida conserva el idle actual. La intro puede forzar
            // la transición aunque W1 ya se haya seleccionado durante Start().
            ShowIdle(idx);
            return;
        }

        CancelTransitionToIdleCrossFade();

        _currentStateIndex = idx;

        SetVegetationWindState(preset);

        VideoPlayer transition = GetTransitionPlayer(idx);

        // Todos los estados, incluido W1, usan su transición cuando existe.
        // Si la escena todavía no la tiene asignada, conservamos el fallback al idle.
        if (!HasVideoSource(transition))
        {
            ShowIdle(idx);
            return;
        }

        HideAllVideos();

        _activeIdleIndex = -1;
        _activeTransitionIndex = idx;
        _pendingIdleAfterTransitionIndex = idx;

        // No hacemos seek/restart aquí. time=0/frame=0 también puede congelar el decoder.
        // El player ya fue preparado al cargar. Pausamos el anterior y reanudamos
        // únicamente esta transición, sin preparar ni reasignar clips.
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);

        float opacity = _crossFadeOverride >= 0f ? _crossFadeOverride : GetTransitionOpacity(idx);
        _activeTransitionOpacity = opacity;
        SetVideoOpacity(transition, ShouldShowVideos ? opacity : 0f);

        float clipDuration = GetClipDurationSafe(transition);
        float crossFadeDuration = Mathf.Min(transitionToIdleCrossFadeDuration, clipDuration);
        _transitionEndsAtUnscaled = Time.unscaledTime + Mathf.Max(0f, clipDuration - crossFadeDuration);
    }

    private void QueuePendingWindRequest(
        WindPreset preset,
        bool useTransition,
        bool forceTransition,
        float crossFadeOverride)
    {
        _hasPendingWindRequest = true;
        _pendingWindPreset = preset;
        _pendingWindRequestUsesTransition = useTransition;
        _pendingWindRequestForcesTransition = forceTransition;
        _pendingWindCrossFadeOverride = crossFadeOverride;
    }

    private void ApplyPendingWindRequest()
    {
        if (!_hasPendingWindRequest)
            return;

        WindPreset preset = _pendingWindPreset;
        bool useTransition = _pendingWindRequestUsesTransition;
        bool forceTransition = _pendingWindRequestForcesTransition;
        float crossFadeOverride = _pendingWindCrossFadeOverride;
        ClearPendingWindRequest();

        if (useTransition)
            TransitionTo(preset, crossFadeOverride, forceTransition);
        else
            SnapToState(preset);
    }

    private void ClearPendingWindRequest()
    {
        _hasPendingWindRequest = false;
        _pendingWindRequestForcesTransition = false;
        _pendingWindCrossFadeOverride = -1f;
    }

    /// <summary>
    /// Solicita la misma transición en todos los gestores activos.
    /// Se usa en la intro para reproducir la transición de W1 aunque Start()
    /// ya haya dejado seleccionado su idle.
    /// </summary>
    public static int TransitionAllTo(
        WindPreset preset,
        bool forceTransition = false,
        float crossFadeOverride = -1f)
    {
        WindStateManager[] managers = Object.FindObjectsByType<WindStateManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (WindStateManager manager in managers)
            manager.TransitionTo(preset, crossFadeOverride, forceTransition);

        return managers.Length;
    }

    public static int ActivateAllVideoPlayersRoots()
    {
        WindStateManager[] managers = Object.FindObjectsByType<WindStateManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (WindStateManager manager in managers)
            manager.ActivateVideoPlayersRoot();

        return managers.Length;
    }

    public static int FadeInAllVideoPlayers(float duration)
    {
        WindStateManager[] managers = Object.FindObjectsByType<WindStateManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (WindStateManager manager in managers)
            manager.FadeInVideos(duration);

        return managers.Length;
    }

    public void FadeInVideos(float duration)
    {
        _videoOpacityFadeDuration = Mathf.Max(0f, duration);
        _videoOpacityFadeStartedAt = Time.unscaledTime;
        _videoOpacityFadeActive = _videoOpacityFadeDuration > 0f;
        _videoOpacityMultiplier = _videoOpacityFadeActive ? 0f : 1f;

        ApplyCurrentVideoAlphas();
    }

    /// <summary>
    /// Amplia la distancia de actualizacion de los Alembic en todos los managers activos.
    /// Permite que entradas sin desplazamiento fisico, como un teletransporte, produzcan
    /// el mismo cambio de estado que el trigger del recorrido inicial.
    /// </summary>
    public static int UseChurchAlembicDistanceOnAll()
    {
        WindStateManager[] managers = Object.FindObjectsByType<WindStateManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (WindStateManager manager in managers)
            manager.UseChurchAlembicDistance();

        return managers.Length;
    }

    public static int PrewarmAllVideoPlayersHidden()
    {
        WindStateManager[] managers = Object.FindObjectsByType<WindStateManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (WindStateManager manager in managers)
            manager.PrewarmVideoPlayersHidden();

        return managers.Length;
    }

    public void ActivateVideoPlayersRoot()
    {
        // Conserva la intención aunque GameplayScene todavía no haya publicado el rig.
        _videoPlayersVisible = true;

        if (!EnsureVideoRig(logWarning: false))
            return;

        _videoRig.SetPlayersRootActive(true);
        RefreshVideoCamera();

        if (!_blizzardVideoPlaybackEnabled)
        {
            HideAllVideos();
            PauseAllVideoPlayers();
            return;
        }

        PrepareAllFixedPlayers();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
        ApplyCurrentVideoAlphas();
    }

    public void UseInitialWalkAlembicDistance()
    {
        _usesChurchAlembicDistance = false;
        _alembicDistanceTransitionActive = false;
        ApplyAlembicCullingDistance(initialWalkAlembicDistance);
        _nextAlembicVisibilityCheck = 0f;
    }

    public void UseChurchAlembicDistance()
    {
        _usesChurchAlembicDistance = true;
        _alembicDistanceTransitionFrom = _currentAlembicMaxDistance;
        _alembicDistanceTransitionTo = Mathf.Max(0f, churchAlembicDistance);
        _alembicDistanceTransitionStartedAt = Time.unscaledTime;
        _alembicDistanceTransitionActive = churchAlembicDistanceTransitionDuration > 0f
            && !Mathf.Approximately(_alembicDistanceTransitionFrom, _alembicDistanceTransitionTo);

        if (!_alembicDistanceTransitionActive)
            ApplyAlembicCullingDistance(_alembicDistanceTransitionTo);
        else
            _nextAlembicVisibilityCheck = 0f;
    }

    private void ApplyAlembicCullingDistance(float distance)
    {
        _currentAlembicMaxDistance = Mathf.Max(0f, distance);
    }

    private void UpdateAlembicDistanceTransition()
    {
        if (!_alembicDistanceTransitionActive)
            return;

        float duration = Mathf.Max(0f, churchAlembicDistanceTransitionDuration);
        float progress = duration > 0f
            ? Mathf.Clamp01((Time.unscaledTime - _alembicDistanceTransitionStartedAt) / duration)
            : 1f;

        ApplyAlembicCullingDistance(Mathf.Lerp(
            _alembicDistanceTransitionFrom,
            _alembicDistanceTransitionTo,
            progress));

        if (progress >= 1f)
            _alembicDistanceTransitionActive = false;
    }

    private void UpdateAlembicCulling()
    {
        if (Time.unscaledTime < _nextAlembicVisibilityCheck)
            return;

        _nextAlembicVisibilityCheck = Time.unscaledTime
            + Mathf.Max(0.02f, alembicVisibilityCheckInterval);

        Camera camera = IsUsableCamera(blizzardVideoCamera)
            ? blizzardVideoCamera
            : ResolveCamera();

        UpdatePlantaMjVisibility(camera);

    }

    public void PrewarmVideoPlayersHidden()
    {
        bool wasVisible = _videoPlayersVisible;

        if (!EnsureVideoRig())
            return;

        _videoRig.SetPlayersRootActive(true);
        _videoPlayersVisible = false;
        RefreshVideoCamera();

        if (!_blizzardVideoPlaybackEnabled)
        {
            HideAllVideos();
            PauseAllVideoPlayers();
            _videoPlayersVisible = wasVisible;
            ApplyCurrentVideoAlphas();
            return;
        }

        PrepareAllFixedPlayers();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: false);

        _videoPlayersVisible = wasVisible;
        ApplyCurrentVideoAlphas();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
        PrewarmPlantaMjAlembicPlayers(0.05f);
    }

    public void SetStandaloneVegetationPlaybackEnabled(bool shouldRun)
    {
        _standaloneVegetationPlaybackEnabled = shouldRun;

        if (shouldRun)
        {
            foreach (AlembicStreamPlayer player in _plantaMjPlayers)
                if (player != null && !player.enabled) player.enabled = true;

            _nextAlembicVisibilityCheck = 0f;
        }
    }

    public void SetBlizzardVideoPlaybackEnabled(bool shouldRun)
    {
        _blizzardVideoPlaybackEnabled = shouldRun;

        if (!shouldRun)
        {
            HideAllVideos();
            PauseAllVideoPlayers();
            return;
        }

        if (_videoRig == null && preloadPlayersOnAwake)
            EnsureVideoRig();

        if (_playersRoot == null || !_videoPlayersVisible)
        {
            ApplyCurrentVideoAlphas();
            return;
        }

        _videoRig.SetPlayersRootActive(true);
        PrepareAllFixedPlayers();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
        ApplyCurrentVideoAlphas();
    }

    private bool EnsureVideoRig(bool logWarning = true)
    {
        if (_videoRig != null && _playersRoot != null)
            return true;

        BlizzardVideoRig rig = BlizzardVideoRig.ActiveRig;
        if (rig == null)
        {
            rig = Object.FindFirstObjectByType<BlizzardVideoRig>(FindObjectsInactive.Include);
        }

        if (rig == null)
        {
            if (logWarning && !_missingVideoRigWarningShown)
            {
                _missingVideoRigWarningShown = true;
                Debug.LogWarning(
                    $"[{nameof(WindStateManager)}] No se encontró BlizzardVideoRig. " +
                    "Añádelo a CAM_Player y asigna su Players Root.",
                    this);
            }

            return false;
        }

        BindVideoRig(rig);
        return _videoRig != null && _playersRoot != null;
    }

    private void BindVideoRig(BlizzardVideoRig rig)
    {
        if (rig == null)
            return;

        UnsubscribeAll();
        _allPlayers.Clear();
        _videoRig = rig;
        _playersRoot = rig.PlayersRoot != null ? rig.PlayersRoot.transform : null;
        _missingVideoRigWarningShown = false;

        if (_playersRoot == null || rig.PlayersRoot == rig.gameObject)
        {
            _playersRoot = null;
            Debug.LogWarning($"[{nameof(WindStateManager)}] BlizzardVideoRig necesita un Players Root hijo y distinto del objeto del rig.", rig);
            return;
        }

        int stateCount = windStates?.Length ?? 0;
        _idlePlayers = new VideoPlayer[stateCount];
        _transitionPlayers = new VideoPlayer[stateCount];

        _videoPlayersVisible = _videoPlayersVisible || videoPlayersRootStartsActive;
        rig.SetPlayersRootActive(_videoPlayersVisible || prewarmPlayersWhileHidden);

        for (int i = 0; i < stateCount; i++)
        {
            _idlePlayers[i] = rig.GetIdlePlayer(i);
            _transitionPlayers[i] = rig.GetTransitionPlayer(i);
            RegisterScenePlayer(_idlePlayers[i]);
            RegisterScenePlayer(_transitionPlayers[i]);
        }

        rig.HideAllSurfaces();

        if (CanRunVideoPlayers)
            PrepareAllFixedPlayers();

        if (_hasPendingWindRequest)
        {
            ApplyPendingWindRequest();
            return;
        }

        ApplyCurrentVideoAlphas();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
    }

    private void RegisterScenePlayer(VideoPlayer player)
    {
        if (player == null || _allPlayers.Contains(player))
            return;

        ConfigureBasePlayer(player);
        player.prepareCompleted -= OnFixedPlayerPrepared;
        player.errorReceived -= OnVideoError;
        player.prepareCompleted += OnFixedPlayerPrepared;
        player.errorReceived += OnVideoError;
        _allPlayers.Add(player);
    }

    private void HandleVideoRigAvailable(BlizzardVideoRig rig)
    {
        if (_videoRig == null || _videoRig == rig)
            BindVideoRig(rig);
    }

    private void HandleVideoRigUnavailable(BlizzardVideoRig rig)
    {
        if (_videoRig != rig)
            return;

        UnsubscribeAll();
        _allPlayers.Clear();
        _idlePlayers = null;
        _transitionPlayers = null;
        _playersRoot = null;
        _videoRig = null;
    }

    private void OnFixedPlayerPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnFixedPlayerPrepared;
        _prepareRequestedPlayers.Remove(source);

        ApplyCurrentVideoAlphas();
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: false);
    }

    private void ShowIdle(int stateIdx)
    {
        if (!IsValidStateIndex(stateIdx)) return;

        VideoPlayer idle = GetIdlePlayer(stateIdx);
        if (idle == null)
        {
            Debug.LogWarning($"[{nameof(WindStateManager)}] W{stateIdx + 1} no tiene VideoPlayer idle asignado en BlizzardVideoRig.", this);
            return;
        }

        _activeIdleIndex = stateIdx;
        _activeTransitionIndex = -1;
        _pendingIdleAfterTransitionIndex = -1;
        _transitionEndsAtUnscaled = -1f;
        _activeTransitionOpacity = 0f;

        HideAllVideos();
        SetVideoOpacity(idle, ShouldShowVideos ? GetIdleOpacity(stateIdx) : 0f);
        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
    }

    private void HideAllVideos()
    {
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player != null)
                SetVideoOpacity(player, 0f);
        }
    }

    private void PrepareAllFixedPlayers()
    {
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player == null) continue;

            RequestPrepare(player);
        }
    }

    private void RequestPrepare(VideoPlayer player)
    {
        if (!HasVideoSource(player) || player.isPrepared || _prepareRequestedPlayers.Contains(player))
            return;

        _prepareRequestedPlayers.Add(player);
        player.Prepare();
    }

    private VideoPlayer GetActiveFixedPlayer()
    {
        if (_activeTransitionIndex >= 0)
            return GetTransitionPlayer(_activeTransitionIndex);

        if (_activeIdleIndex >= 0)
            return GetIdlePlayer(_activeIdleIndex);

        return null;
    }

    private void PlayOnlyActiveFixedPlayer(bool requestPrepareIfNeeded)
    {
        if (_transitionToIdleCrossFadeActive)
        {
            MaintainTransitionToIdleCrossFade(requestPrepareIfNeeded);
            return;
        }

        VideoPlayer activePlayer = ShouldShowVideos ? GetActiveFixedPlayer() : null;

        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player == null) continue;

            if (player != activePlayer)
            {
                if (player.isPlaying)
                    PauseFixedPlayer(player);

                continue;
            }

            if (player.isPrepared)
            {
                if (!player.isPlaying)
                    player.Play();
            }
            else if (requestPrepareIfNeeded)
            {
                RequestPrepare(player);
            }
        }
    }

    private void PauseAllVideoPlayers()
    {
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player != null && player.isPlaying)
                PauseFixedPlayer(player);
        }

    }

    private void PauseFixedPlayer(VideoPlayer player)
    {
        if (player == null || !player.isPlaying)
            return;

        player.Pause();

        // Reiniciar clips de transicion
        if (IsTransitionPlayer(player))
            RewindTransitionPlayer(player);
    }

    private static void RewindTransitionPlayer(VideoPlayer player)
    {
        if (player != null && player.canSetTime)
            player.time = 0d;
    }

    private bool IsTransitionPlayer(VideoPlayer player)
    {
        if (player == null || _transitionPlayers == null)
            return false;

        for (int i = 0; i < _transitionPlayers.Length; i++)
        {
            if (_transitionPlayers[i] == player)
                return true;
        }

        return false;
    }

    private void ApplyCurrentVideoAlphas()
    {
        HideAllVideos();

        if (!ShouldShowVideos)
            return;

        if (_transitionToIdleCrossFadeActive)
        {
            ApplyTransitionToIdleCrossFadeAlphas();
            return;
        }

        if (_activeTransitionIndex >= 0)
        {
            VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
            if (transition != null)
            {
                float opacity = _activeTransitionOpacity > 0f
                    ? _activeTransitionOpacity
                    : GetTransitionOpacity(_activeTransitionIndex);

                SetVideoOpacity(transition, opacity);
            }

            return;
        }

        if (_activeIdleIndex >= 0)
        {
            VideoPlayer idle = GetIdlePlayer(_activeIdleIndex);
            if (idle != null)
                SetVideoOpacity(idle, GetIdleOpacity(_activeIdleIndex));
        }
    }

    private void MaintainActiveVideoAlpha()
    {
        if (!CanRunVideoPlayers)
            return;

        if (_transitionToIdleCrossFadeActive)
        {
            MaintainTransitionToIdleCrossFade(requestPrepareIfNeeded: true);
            return;
        }

        if (_activeTransitionIndex >= 0)
        {
            VideoPlayer activeTransition = GetTransitionPlayer(_activeTransitionIndex);

            for (int i = 0; i < _allPlayers.Count; i++)
            {
                VideoPlayer player = _allPlayers[i];
                if (player == null) continue;

                SetVideoOpacity(player, ShouldShowVideos && player == activeTransition ? _activeTransitionOpacity : 0f);

                if (ShouldShowVideos && player == activeTransition)
                {
                    if (player.isPrepared && !player.isPlaying)
                        player.Play();
                }
                else if (player.isPlaying)
                {
                    PauseFixedPlayer(player);
                }
            }

            return;
        }

        if (_activeIdleIndex >= 0)
        {
            VideoPlayer activeIdle = GetIdlePlayer(_activeIdleIndex);
            float idleOpacity = IsValidStateIndex(_activeIdleIndex) ? GetIdleOpacity(_activeIdleIndex) : 0f;

            for (int i = 0; i < _allPlayers.Count; i++)
            {
                VideoPlayer player = _allPlayers[i];
                if (player == null) continue;

                SetVideoOpacity(player, ShouldShowVideos && player == activeIdle ? idleOpacity : 0f);

                if (ShouldShowVideos && player == activeIdle)
                {
                    if (player.isPrepared && !player.isPlaying)
                        player.Play();
                }
                else if (player.isPlaying)
                {
                    PauseFixedPlayer(player);
                }
            }
        }
    }

    private void MaintainTransitionToIdleCrossFade(bool requestPrepareIfNeeded)
    {
        VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
        VideoPlayer idle = GetIdlePlayer(_activeIdleIndex);

        ApplyTransitionToIdleCrossFadeAlphas();

        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player == null) continue;

            bool shouldPlay = ShouldShowVideos && (player == transition || player == idle);
            if (shouldPlay)
            {
                if (player.isPrepared)
                {
                    if (!player.isPlaying)
                        player.Play();
                }
                else if (requestPrepareIfNeeded)
                {
                    RequestPrepare(player);
                }
            }
            else if (player.isPlaying)
            {
                PauseFixedPlayer(player);
            }
        }
    }

    private void ApplyTransitionToIdleCrossFadeAlphas()
    {
        if (!_transitionToIdleCrossFadeActive)
            return;

        float progress = GetTransitionToIdleCrossFadeProgress();
        float blend = Mathf.SmoothStep(0f, 1f, progress);

        VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
        VideoPlayer idle = GetIdlePlayer(_activeIdleIndex);

        SetVideoOpacity(transition, Mathf.Lerp(_activeTransitionOpacity, 0f, blend));
        SetVideoOpacity(idle, Mathf.Lerp(0f, GetIdleOpacity(_activeIdleIndex), blend));
    }

    private float GetTransitionToIdleCrossFadeProgress()
    {
        if (_activeTransitionToIdleCrossFadeDuration <= 0f)
            return 1f;

        return Mathf.Clamp01(
            (Time.unscaledTime - _transitionToIdleCrossFadeStartedAt)
            / _activeTransitionToIdleCrossFadeDuration);
    }

    private void UpdateTransitionToIdleCrossFade()
    {
        if (!_transitionToIdleCrossFadeActive)
            return;

        ApplyTransitionToIdleCrossFadeAlphas();

        if (GetTransitionToIdleCrossFadeProgress() >= 1f)
            CompleteTransitionToIdleCrossFade();
    }

    private void CompleteTransitionToIdleCrossFade()
    {
        VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
        VideoPlayer idle = GetIdlePlayer(_activeIdleIndex);

        SetVideoOpacity(transition, 0f);
        SetVideoOpacity(idle, ShouldShowVideos ? GetIdleOpacity(_activeIdleIndex) : 0f);

        if (transition != null)
        {
            if (transition.isPlaying)
                PauseFixedPlayer(transition);
            else
                RewindTransitionPlayer(transition);
        }

        _transitionToIdleCrossFadeActive = false;
        _activeTransitionIndex = -1;
        _pendingIdleAfterTransitionIndex = -1;
        _transitionEndsAtUnscaled = -1f;
        _activeTransitionOpacity = 0f;

        PlayOnlyActiveFixedPlayer(requestPrepareIfNeeded: true);
    }

    private void CancelTransitionToIdleCrossFade()
    {
        if (!_transitionToIdleCrossFadeActive)
            return;

        VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
        VideoPlayer idle = GetIdlePlayer(_activeIdleIndex);

        if (transition != null)
        {
            if (transition.isPlaying)
                PauseFixedPlayer(transition);
            else
                RewindTransitionPlayer(transition);
        }

        if (idle != null && idle.isPlaying)
            PauseFixedPlayer(idle);

        _transitionToIdleCrossFadeActive = false;
        _activeTransitionToIdleCrossFadeDuration = 0f;
    }

    private void UpdateVideoOpacityFade()
    {
        if (!_videoOpacityFadeActive)
            return;

        float progress = Mathf.Clamp01(
            (Time.unscaledTime - _videoOpacityFadeStartedAt) / _videoOpacityFadeDuration);

        _videoOpacityMultiplier = Mathf.SmoothStep(0f, 1f, progress);

        if (progress >= 1f)
        {
            _videoOpacityMultiplier = 1f;
            _videoOpacityFadeActive = false;
        }
    }

    private void UpdateTransitionTimer()
    {
        if (_transitionToIdleCrossFadeActive ||
            _activeTransitionIndex < 0 ||
            _transitionEndsAtUnscaled < 0f)
            return;

        if (Time.unscaledTime < _transitionEndsAtUnscaled)
            return;

        int idleIdx = _pendingIdleAfterTransitionIndex;
        VideoPlayer idle = GetIdlePlayer(idleIdx);

        // No empezamos el fundido hasta que el idle pueda mostrar su primer frame.
        // Mientras tanto la transicion sigue visible y reproduciendose.
        if (idle == null)
        {
            ShowIdle(idleIdx);
            return;
        }

        if (!idle.isPrepared)
        {
            RequestPrepare(idle);
            return;
        }

        _activeIdleIndex = idleIdx;
        _pendingIdleAfterTransitionIndex = -1;
        _transitionEndsAtUnscaled = -1f;
        VideoPlayer transition = GetTransitionPlayer(_activeTransitionIndex);
        _activeTransitionToIdleCrossFadeDuration = Mathf.Min(
            Mathf.Max(0f, transitionToIdleCrossFadeDuration),
            GetClipDurationSafe(transition));
        _transitionToIdleCrossFadeStartedAt = Time.unscaledTime;
        _transitionToIdleCrossFadeActive = true;

        if (ShouldShowVideos && !idle.isPlaying)
            idle.Play();

        if (_activeTransitionToIdleCrossFadeDuration <= 0f)
            CompleteTransitionToIdleCrossFade();
    }


    private float GetClipDurationSafe(VideoPlayer player)
    {
        if (player != null && player.clip != null && player.clip.length > 0.01)
        {
            float speed = Mathf.Max(0.01f, player.playbackSpeed);
            return (float)(player.clip.length / speed);
        }

        return 0.1f;
    }

    private VideoPlayer GetIdlePlayer(int stateIdx)
    {
        if (_idlePlayers == null || stateIdx < 0 || stateIdx >= _idlePlayers.Length)
            return null;

        return _idlePlayers[stateIdx];
    }

    private float GetIdleOpacity(int stateIdx)
    {
        if (!IsValidStateIndex(stateIdx)) return 0f;

        float opacity = windStates[stateIdx].idleOpacity;
        if (opacity <= 0f && windStates[stateIdx].videoOpacity > 0f)
            opacity = windStates[stateIdx].videoOpacity;

        return opacity;
    }

    private float GetTransitionOpacity(int stateIdx)
    {
        if (!IsValidStateIndex(stateIdx)) return 0f;

        float opacity = windStates[stateIdx].transitionOpacity;
        if (opacity <= 0f && windStates[stateIdx].videoOpacity > 0f)
            opacity = windStates[stateIdx].videoOpacity;

        return opacity;
    }

    private VideoPlayer GetTransitionPlayer(int stateIdx)
    {
        if (_transitionPlayers == null || stateIdx < 0 || stateIdx >= _transitionPlayers.Length)
            return null;

        return _transitionPlayers[stateIdx];
    }

    private bool CanRunVideoPlayers =>
        _blizzardVideoPlaybackEnabled && _videoRig != null && _videoRig.IsPlayersRootActive;

    private bool ShouldShowVideos =>
        CanRunVideoPlayers && _videoPlayersVisible;

    // Callbacks de VideoPlayer 

    private void OnVideoError(VideoPlayer source, string message)
    {
        _prepareRequestedPlayers.Remove(source);
        Debug.LogWarning($"[{nameof(WindStateManager)}] Error en VideoPlayer '{source.name}': {message}", this);
    }

    private void UnsubscribeAll()
    {
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            VideoPlayer player = _allPlayers[i];
            if (player == null) continue;

            player.prepareCompleted -= OnFixedPlayerPrepared;
            player.errorReceived -= OnVideoError;
        }

        _prepareRequestedPlayers.Clear();
    }

    //Configuración de VideoPlayer 

    private void ConfigureBasePlayer(VideoPlayer player)
    {
        if (player == null) return;

        player.playOnAwake       = false;
        player.waitForFirstFrame = false;
        player.skipOnDrop        = true;
        player.audioOutputMode   = VideoAudioOutputMode.None;
        player.timeUpdateMode    = VideoTimeUpdateMode.UnscaledGameTime;
        player.isLooping         = true;
    }

    //Cámara 

    private void RefreshVideoCamera()
    {
        bool cameraOk = IsUsableCamera(blizzardVideoCamera);

        if (!cameraOk)
        {
            Camera resolved = ResolveCamera();
            if (resolved == null)
            {
                WarnMissingCamera();
                return;
            }

            blizzardVideoCamera = resolved;
            _missingCameraWarningShown = false;
        }

    }

    private Camera ResolveCamera()
    {
        Camera fallback = null;

        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!IsUsableCamera(cam)) continue;
            if (cam.name == "CAM_Main") return cam;
            if (cam.CompareTag("MainCamera")) fallback = cam;
            else if (fallback == null) fallback = cam;
        }

        return fallback;
    }

    private static bool IsUsableCamera(Camera cam)
        => cam != null && cam.isActiveAndEnabled && cam.targetTexture == null;

    private void WarnMissingCamera()
    {
        if (_missingCameraWarningShown) return;
        _missingCameraWarningShown = true;
        Debug.LogWarning($"[{nameof(WindStateManager)}] No se encontró una cámara activa válida para el culling de vegetación.", this);
    }

    // ── Scene reload ──────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolvePlantaMjAlembicPlayers();
        if (!_alembicDistanceTransitionActive)
        {
            ApplyAlembicCullingDistance(
                _usesChurchAlembicDistance
                    ? churchAlembicDistance
                    : initialWalkAlembicDistance);
        }
        _nextAlembicVisibilityCheck = 0f;
        EnsureVideoRig(logWarning: false);
    }

    // ── Superficies colocadas en escena ────────────────────────────────────────

    private void SetVideoOpacity(VideoPlayer player, float opacity)
    {
        if (player == null || _videoRig == null)
            return;

        float alpha = Mathf.Clamp01(opacity * _videoOpacityMultiplier);
        _videoRig.SetPlayerOpacity(player, alpha);
    }

    private static bool HasVideoSource(VideoPlayer player)
        => player != null &&
           (player.clip != null || !string.IsNullOrWhiteSpace(player.url));

    // ── Vegetación VAT ────────────────────────────────────────────────────────

    private void SetVegetationWindState(WindPreset preset)
    {
        bool fast = preset == WindPreset.W1_MaxIdle || preset == WindPreset.W4_MinToMedium;

        _vatWindBlendTarget = fast ? 1f : 0f;
    }

    private void UpdateVatWindBlend()
    {
        if (Mathf.Approximately(_vatWindBlend, _vatWindBlendTarget))
            return;

        if (vatWindBlendDuration <= 0f)
        {
            _vatWindBlend = _vatWindBlendTarget;
        }
        else
        {
            _vatWindBlend = Mathf.MoveTowards(
                _vatWindBlend,
                _vatWindBlendTarget,
                Time.deltaTime / vatWindBlendDuration);
        }

        Shader.SetGlobalFloat(VatWindBlendId, _vatWindBlend);
    }

    // ── Alembic standalone ───────────────────────────────────────────────────

    private void ResolvePlantaMjAlembicPlayers()
    {
        _plantaMjPlayers.Clear();
        _plantaMjTimes.Clear();
        _plantaMjRenderers.Clear();
        _plantaMjVisible.Clear();

        if (!playPlantaMjAlembic)
            return;

        foreach (var player in Object.FindObjectsByType<AlembicStreamPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (player == null) continue;
            if (!IsInsideNamedHierarchy(player.transform, "planta_mj")) continue;

            _plantaMjPlayers.Add(player);
            _plantaMjTimes.Add(GetClampedAlembicTime(player));
            _plantaMjRenderers.Add(player.GetComponentsInChildren<Renderer>(true));
            _plantaMjVisible.Add(true);
        }

        if (_plantaMjPlayers.Count > 0 && !_plantaMjAutoPlaybackReported)
        {
            _plantaMjAutoPlaybackReported = true;
            Debug.Log($"[{nameof(WindStateManager)}] Planta_MJ Alembic enlazado para reproducción en loop: {_plantaMjPlayers.Count} player(s).", this);
        }
    }

    private void AdvancePlantaMjAlembicPlayers()
    {
        if (!playPlantaMjAlembic || _plantaMjPlayers.Count == 0)
            return;

        if (!_standaloneVegetationPlaybackEnabled)
            return;

        float speed = Mathf.Max(0f, plantaMjPlaybackSpeed);
        if (speed <= 0f)
            return;

        for (int i = _plantaMjPlayers.Count - 1; i >= 0; i--)
        {
            AlembicStreamPlayer player = _plantaMjPlayers[i];
            if (player == null)
            {
                _plantaMjPlayers.RemoveAt(i);
                _plantaMjTimes.RemoveAt(i);
                _plantaMjRenderers.RemoveAt(i);
                _plantaMjVisible.RemoveAt(i);
                continue;
            }

            if (!_plantaMjVisible[i])
                continue;

            float duration = player.Duration;
            if (duration <= 0f) continue;

            float time = _plantaMjTimes[i] + Time.deltaTime * speed;
            if (time > duration)
                time %= duration;

            _plantaMjTimes[i] = time;
            player.CurrentTime = time;
        }
    }

    private void UpdatePlantaMjVisibility(Camera camera)
    {
        for (int i = 0; i < _plantaMjPlayers.Count; i++)
        {
            AlembicStreamPlayer player = _plantaMjPlayers[i];
            if (player == null)
                continue;

            bool visible = camera == null || IsAlembicVisible(
                player.transform,
                _plantaMjRenderers[i],
                camera,
                _currentAlembicMaxDistance);
            _plantaMjVisible[i] = visible;
            SetRenderersEnabled(_plantaMjRenderers[i], visible);
        }
    }

    private static void SetRenderersEnabled(Renderer[] renderers, bool isEnabled)
    {
        if (renderers == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled != isEnabled)
                renderer.enabled = isEnabled;
        }
    }

    private static bool IsAlembicVisible(
        Transform root,
        Renderer[] renderers,
        Camera camera,
        float maxDistance)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        float maxDistanceSqr = maxDistance > 0f
            ? maxDistance * maxDistance
            : float.PositiveInfinity;
        bool foundRenderer = false;

        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                foundRenderer = true;
                Bounds bounds = renderer.bounds;
                Vector3 closestPoint = bounds.ClosestPoint(camera.transform.position);
                if ((closestPoint - camera.transform.position).sqrMagnitude > maxDistanceSqr)
                    continue;

                if (GeometryUtility.TestPlanesAABB(planes, bounds))
                    return true;
            }
        }

        if (foundRenderer)
            return false;

        return maxDistance <= 0f
            || (root.position - camera.transform.position).sqrMagnitude <= maxDistanceSqr;
    }

    private void PrewarmPlantaMjAlembicPlayers(float sampleTime)
    {
        if (!playPlantaMjAlembic)
            return;

        if (_plantaMjPlayers.Count == 0)
            ResolvePlantaMjAlembicPlayers();

        for (int i = 0; i < _plantaMjPlayers.Count; i++)
        {
            AlembicStreamPlayer player = _plantaMjPlayers[i];
            if (player == null) continue;

            float duration = player.Duration;
            if (duration <= 0f) continue;

            float time = Mathf.Clamp(sampleTime, 0f, duration);
            _plantaMjTimes[i] = time;
            player.CurrentTime = time;
        }
    }

    private static float GetClampedAlembicTime(AlembicStreamPlayer player)
    {
        if (player == null) return 0f;

        float duration = player.Duration;
        if (duration <= 0f) return 0f;

        return Mathf.Clamp((float)player.CurrentTime, 0f, duration);
    }

    private static bool IsInsideNamedHierarchy(Transform candidate, string normalizedName)
    {
        while (candidate != null)
        {
            if (candidate.name.ToLowerInvariant().Contains(normalizedName))
                return true;

            candidate = candidate.parent;
        }

        return false;
    }

    private bool IsValidStateIndex(int idx)
    {
        if (windStates == null || idx < 0 || idx >= windStates.Length)
        {
            Debug.LogWarning($"[{nameof(WindStateManager)}] Índice de viento inválido: {idx}.", this);
            return false;
        }

        return true;
    }

}
