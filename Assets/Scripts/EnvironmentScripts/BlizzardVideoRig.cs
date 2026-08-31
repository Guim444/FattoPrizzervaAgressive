using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Referencias de escena para los VideoPlayers y quads de la ventisca.
///
/// Este componente debe vivir en un objeto que permanezca activo (por ejemplo CAM_Player),
/// mientras que <see cref="playersRoot"/> puede empezar desactivado. De este modo el rig
/// puede registrarse aunque las superficies de vídeo todavía estén ocultas.
/// </summary>
[DisallowMultipleComponent]
public sealed class BlizzardVideoRig : MonoBehaviour
{
    [Serializable]
    public sealed class SurfaceBinding
    {
        [Tooltip("Renderer del quad que muestra este vídeo.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Multiplicador respecto a la opacidad del estado. Principal=1; copias de fondo=0.35.")]
        [SerializeField, Range(0f, 1f)] private float opacityMultiplier = 1f;

        [Tooltip("Propiedad float opcional para controlar opacidad. Se usa antes que _BaseColor/_Color.")]
        [SerializeField] private string opacityProperty = "_Opacity";

        [Tooltip("Desactiva el Renderer cuando la opacidad llega a cero.")]
        [SerializeField] private bool disableRendererWhenHidden = true;

        [NonSerialized] private MaterialPropertyBlock _propertyBlock;
        [NonSerialized] private bool _baseColorCached;
        [NonSerialized] private Color _baseColor = Color.white;
        [NonSerialized] private int _colorPropertyId;
        [NonSerialized] private int _opacityPropertyId;
        [NonSerialized] private bool _usesColorProperty;
        [NonSerialized] private bool _usesOpacityProperty;
        [NonSerialized] private bool _missingOpacityPropertyReported;
        [NonSerialized] private bool _opaqueMaterialReported;

        public Renderer TargetRenderer => targetRenderer;
        public float OpacityMultiplier => opacityMultiplier;

        internal void SetOpacity(float stateOpacity, UnityEngine.Object logContext)
        {
            if (targetRenderer == null)
                return;

            float alpha = Mathf.Clamp01(stateOpacity * opacityMultiplier);
            EnsureMaterialProperties(logContext);

            _propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_propertyBlock);

            if (_usesOpacityProperty)
                _propertyBlock.SetFloat(_opacityPropertyId, alpha);

            if (_usesColorProperty)
            {
                Color color = _baseColor;
                color.a = alpha;
                _propertyBlock.SetColor(_colorPropertyId, color);
            }

            targetRenderer.SetPropertyBlock(_propertyBlock);

            if (disableRendererWhenHidden)
                targetRenderer.enabled = alpha > 0.001f;
            else if (!targetRenderer.enabled && alpha > 0.001f)
                targetRenderer.enabled = true;
        }

        internal void ClearRuntimeCache()
        {
            _propertyBlock = null;
            _baseColorCached = false;
            _usesColorProperty = false;
            _usesOpacityProperty = false;
            _missingOpacityPropertyReported = false;
            _opaqueMaterialReported = false;
        }

        private void EnsureMaterialProperties(UnityEngine.Object logContext)
        {
            if (_baseColorCached)
                return;

            _baseColorCached = true;
            Material material = targetRenderer.sharedMaterial;
            if (material == null)
                return;

            if (!string.IsNullOrWhiteSpace(opacityProperty) && material.HasProperty(opacityProperty))
            {
                _opacityPropertyId = Shader.PropertyToID(opacityProperty);
                _usesOpacityProperty = true;
            }

            if (material.HasProperty("_BaseColor"))
            {
                _colorPropertyId = Shader.PropertyToID("_BaseColor");
                _baseColor = material.GetColor(_colorPropertyId);
                _usesColorProperty = true;
            }
            else if (material.HasProperty("_Color"))
            {
                _colorPropertyId = Shader.PropertyToID("_Color");
                _baseColor = material.GetColor(_colorPropertyId);
                _usesColorProperty = true;
            }

            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") < 0.5f && !_opaqueMaterialReported)
            {
                _opaqueMaterialReported = true;
                Debug.LogWarning(
                    $"[{nameof(BlizzardVideoRig)}] El material '{material.name}' de '{targetRenderer.name}' está en modo Opaque. " +
                    "Configúralo como Transparent para conservar el alpha del vídeo y las transiciones de opacidad.",
                    logContext);
            }

            if (!_usesOpacityProperty && !_usesColorProperty && !_missingOpacityPropertyReported)
            {
                _missingOpacityPropertyReported = true;
                Debug.LogWarning(
                    $"[{nameof(BlizzardVideoRig)}] El material '{material.name}' de '{targetRenderer.name}' " +
                    "no expone _Opacity, _BaseColor ni _Color. Podrá ocultarse, pero no aplicar opacidad gradual.",
                    logContext);
            }
        }
    }

    [Serializable]
    public sealed class PlayerBinding
    {
        [Tooltip("VideoPlayer ya creado y configurado en la escena.")]
        [SerializeField] private VideoPlayer player;

        [Tooltip("Quads que muestran la salida de este VideoPlayer.")]
        [SerializeField] private SurfaceBinding[] surfaces = Array.Empty<SurfaceBinding>();

        public VideoPlayer Player => player;
        public IReadOnlyList<SurfaceBinding> Surfaces => surfaces ?? Array.Empty<SurfaceBinding>();

        internal void SetOpacity(float opacity, UnityEngine.Object logContext)
        {
            if (surfaces == null)
                return;

            for (int i = 0; i < surfaces.Length; i++)
                surfaces[i]?.SetOpacity(opacity, logContext);
        }

        internal void ClearRuntimeCache()
        {
            if (surfaces == null)
                return;

            for (int i = 0; i < surfaces.Length; i++)
                surfaces[i]?.ClearRuntimeCache();
        }
    }

    [Serializable]
    public sealed class StateBinding
    {
        [SerializeField] private string stateName;
        [SerializeField] private PlayerBinding idle;
        [SerializeField] private PlayerBinding transition;

        public PlayerBinding Idle => idle;
        public PlayerBinding Transition => transition;

        public StateBinding(string stateName)
        {
            this.stateName = stateName;
            idle = new PlayerBinding();
            transition = new PlayerBinding();
        }
    }

    public static event Action<BlizzardVideoRig> RigAvailable;
    public static event Action<BlizzardVideoRig> RigUnavailable;
    public static BlizzardVideoRig ActiveRig { get; private set; }

    [Header("Jerarquía")]
    [Tooltip("Root que contiene todos los players y quads. Debe ser hijo de CAM_Player y distinto del objeto que contiene este componente.")]
    [SerializeField] private GameObject playersRoot;

    [Header("Estados (0=W1, 1=W2, 2=W3, 3=W4)")]
    [SerializeField] private StateBinding[] states =
    {
        new StateBinding("W1 - Max Idle"),
        new StateBinding("W2 - Max To Medium"),
        new StateBinding("W3 - Medium To Min"),
        new StateBinding("W4 - Min To Medium")
    };

    private readonly Dictionary<VideoPlayer, PlayerBinding> _bindingsByPlayer =
        new Dictionary<VideoPlayer, PlayerBinding>();

    public GameObject PlayersRoot => playersRoot;
    public int StateCount => states?.Length ?? 0;
    public bool IsPlayersRootActive => playersRoot != null && playersRoot.activeInHierarchy;

    private void OnEnable()
    {
        RebuildPlayerLookup();
        ActiveRig = this;
        RigAvailable?.Invoke(this);
    }

    private void OnDisable()
    {
        RigUnavailable?.Invoke(this);
        if (ActiveRig == this)
            ActiveRig = null;
    }

    private void OnValidate()
    {
        RebuildPlayerLookup();
    }

    public PlayerBinding GetIdleBinding(int stateIndex)
        => IsValidStateIndex(stateIndex) ? states[stateIndex]?.Idle : null;

    public PlayerBinding GetTransitionBinding(int stateIndex)
        => IsValidStateIndex(stateIndex) ? states[stateIndex]?.Transition : null;

    public VideoPlayer GetIdlePlayer(int stateIndex)
        => GetIdleBinding(stateIndex)?.Player;

    public VideoPlayer GetTransitionPlayer(int stateIndex)
        => GetTransitionBinding(stateIndex)?.Player;

    public void SetPlayersRootActive(bool active)
    {
        if (playersRoot != null && playersRoot.activeSelf != active)
            playersRoot.SetActive(active);
    }

    public void SetPlayerOpacity(VideoPlayer player, float opacity)
    {
        if (player == null)
            return;

        if (_bindingsByPlayer.Count == 0)
            RebuildPlayerLookup();

        if (_bindingsByPlayer.TryGetValue(player, out PlayerBinding binding))
            binding.SetOpacity(opacity, this);
    }

    public void HideAllSurfaces()
    {
        if (states == null)
            return;

        for (int i = 0; i < states.Length; i++)
        {
            states[i]?.Idle?.SetOpacity(0f, this);
            states[i]?.Transition?.SetOpacity(0f, this);
        }
    }

    [ContextMenu("Validar configuración de ventisca")]
    public void ValidateConfiguration()
    {
        RebuildPlayerLookup();

        if (playersRoot == null)
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] Falta asignar Players Root.", this);
        else if (playersRoot == gameObject)
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] Players Root debe ser un hijo distinto para poder ocultarlo sin desactivar el rig.", this);

        if (StateCount != 4)
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] Se esperaban 4 estados y hay {StateCount}.", this);

        for (int i = 0; i < StateCount; i++)
        {
            VideoPlayer idlePlayer = GetIdlePlayer(i);
            if (idlePlayer == null)
                Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] W{i + 1} no tiene VideoPlayer idle.", this);
            else
                ValidatePlayer(idlePlayer, $"W{i + 1} idle");

            VideoPlayer transitionPlayer = GetTransitionPlayer(i);
            if (i == 0 && transitionPlayer != null)
                Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] W1 no necesita VideoPlayer de transición.", this);
            else if (i > 0 && transitionPlayer == null)
                Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] W{i + 1} no tiene VideoPlayer de transición.", this);
            else if (transitionPlayer != null)
                ValidatePlayer(transitionPlayer, $"W{i + 1} transición");
        }
    }

    private void ValidatePlayer(VideoPlayer player, string label)
    {
        if (player.clip == null && string.IsNullOrWhiteSpace(player.url))
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] {label} no tiene clip ni URL.", player);

        if (!_bindingsByPlayer.TryGetValue(player, out PlayerBinding binding) || binding.Surfaces.Count == 0)
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] {label} no tiene quads asignados en Surfaces.", player);
    }

    private void RebuildPlayerLookup()
    {
        _bindingsByPlayer.Clear();

        if (states == null)
            return;

        for (int i = 0; i < states.Length; i++)
        {
            RegisterBinding(states[i]?.Idle, i, "idle");
            RegisterBinding(states[i]?.Transition, i, "transición");
        }
    }

    private void RegisterBinding(PlayerBinding binding, int stateIndex, string kind)
    {
        if (binding == null || binding.Player == null)
            return;

        binding.ClearRuntimeCache();

        if (_bindingsByPlayer.ContainsKey(binding.Player))
        {
            Debug.LogWarning($"[{nameof(BlizzardVideoRig)}] El VideoPlayer '{binding.Player.name}' está asignado más de una vez (W{stateIndex + 1} {kind}).", this);
            return;
        }

        _bindingsByPlayer.Add(binding.Player, binding);
    }

    private bool IsValidStateIndex(int stateIndex)
        => states != null && stateIndex >= 0 && stateIndex < states.Length;
}
