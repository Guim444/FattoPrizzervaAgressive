using System.Collections.Generic;
using UnityEngine;

// Controla las fases del combate contra el boss 0
// Fase 1: tutorial, el boss solo camina hacia el jugador y hay que sacarlo por el back
// Fase 2: el cuadrado se mueve en Z, el boss aparece dentro y hay que pegarse a puñetazos
// Fase 3: con la mitad de vida el boss usa el Dash y hay que estrellarlo contra los objetos
// El combate de la fase 2 no empieza hasta que el jugador entra en el cuadrado
public sealed class BossFightPhaseController : MonoBehaviour
{
    // Cuanto se mueve el cuadrado en Z al pasar a la fase 2
    [SerializeField] private float phase2OffsetZ = 14f;

    // El boss aparece a esta fraccion del cuadrado empezando desde el back
    [SerializeField] private float spawnDepthRatio = 0.25f;

    [SerializeField] private float messageSeconds = 3f;

    // La fase 3 empieza cuando la vida baja de esta fraccion
    [SerializeField, Range(0f, 1f)] private float phase3LifeRatio = 0.5f;

    // Segundos de pausa entre dashes en la fase 3
    [SerializeField] private float phase3DashCooldown = 2f;

    private TutorialRingManager _area;
    private Transform _areaRoot;
    private Transform _boss;
    private Vector3 _originalRootPosition;
    private bool _hasOriginalPosition;
    private RioTutteEnemy _rio;
    private EnemyBase _bossAI;
    private int _phase;
    private bool _waitingForPlayer;
    private string _message = "";
    private float _messageUntil;
    private readonly List<MapInteractable> _objects = new List<MapInteractable>();
    private int _objectsUsed;

    public TutorialRingManager Area => _area;
    public RioTutteEnemy Boss => _rio;
    public int Phase => _phase;

    public void Begin(TutorialRingManager area, Transform boss)
    {
        ClearObjects();

        _area = area;
        _boss = boss;
        _areaRoot = area.backBound.parent;

        // Si ya se habia movido a la fase 2 lo devolvemos a su sitio
        if (!_hasOriginalPosition)
        {
            _originalRootPosition = _areaRoot.position;
            _hasOriginalPosition = true;
        }
        else
        {
            _areaRoot.position = _originalRootPosition;
        }

        _rio = boss.GetComponent<RioTutteEnemy>();
        _bossAI = boss.GetComponent<EnemyBase>();
        if (_bossAI != null) _bossAI.activeAI = true;

        // Fase 1: el boss solo camina hacia el jugador, sin atacar
        if (_rio != null)
        {
            _rio.attacksEnabled = false;
            _rio.facePlayerByZ = true;
            _rio.dashPackMode = false;
            _rio.grabEnabled = false;
            _rio.lockLife = false;
            _rio.ResetLife();
            if (_rio.Dash != null) _rio.Dash.Cancel();
        }

        _phase = 1;
        _waitingForPlayer = false;
        _messageUntil = 0f;
        PlaceBossAtSpawn();
    }

    private void Update()
    {
        if (_boss == null) return;

        if (_phase == 1)
        {
            // Fase 1 acaba cuando el boss pasa el back hacia fuera del cuadrado
            float side = Mathf.Sign(_area.backBound.position.z - _area.frontBound.position.z);
            if ((_boss.position.z - _area.backBound.position.z) * side > 0f)
                FinishPhase1();
        }
        else if (_phase == 2)
        {
            if (_waitingForPlayer && PlayerIsInsideArea())
            {
                // El jugador ha llegado, el boss empieza a moverse y a pegar
                _waitingForPlayer = false;
                if (_bossAI != null) _bossAI.activeAI = true;
                if (_rio != null) _rio.attacksEnabled = true;
            }
            else if (!_waitingForPlayer && _rio != null && _rio.Life <= _rio.maxLife * phase3LifeRatio)
            {
                StartPhase3();
            }
        }
    }

    private void FinishPhase1()
    {
        _phase = 2;
        _waitingForPlayer = true;
        ShowMessage("Finish Phase1");

        // El boss se queda quieto en su sitio hasta que llegue el jugador
        if (_bossAI != null) _bossAI.activeAI = false;

        _areaRoot.position += new Vector3(0f, 0f, phase2OffsetZ);
        PlaceBossAtSpawn();
    }

    // Fase 3: dash cada pocos segundos y objetos repartidos por la zona
    private void StartPhase3()
    {
        _phase = 3;
        ShowMessage("Phase 3");

        RioTutteDashAttack dash = _rio.Dash;
        if (dash == null) dash = _rio.gameObject.AddComponent<RioTutteDashAttack>();
        dash.cooldownSeconds = phase3DashCooldown;

        _rio.lockLife = true;
        _rio.attacksEnabled = true;
        _rio.dashPackMode = true;
        _rio.grabEnabled = false;

        _objectsUsed = 0;
        foreach (MapInteractable item in MapInteractable.SpawnBoxes(_area, _rio))
        {
            item.Triggered += OnObjectTriggered;
            _objects.Add(item);
        }
    }

    private void OnObjectTriggered(MapInteractable item)
    {
        _objectsUsed++;
        if (_objectsUsed < _objects.Count) return;

        _phase = 4;
        ShowMessage("Fase 3 superada");
        Debug.Log("[BossFight] Fase 3 superada");

        _rio.dashPackMode = false;
        _rio.activeAI = false;
        if (_rio.Dash != null) _rio.Dash.Cancel();
    }

    private void ClearObjects()
    {
        foreach (MapInteractable item in _objects)
        {
            if (item == null) continue;
            item.Triggered -= OnObjectTriggered;
            Destroy(item.gameObject);
        }
        _objects.Clear();
    }

    private void ShowMessage(string text)
    {
        _message = text;
        _messageUntil = Time.unscaledTime + messageSeconds;
    }

    private bool PlayerIsInsideArea()
    {
        if (_area.player == null) return false;

        Vector3 p = _area.player.transform.position;
        return p.x >= Mathf.Min(_area.leftBound.position.x, _area.rightBound.position.x)
            && p.x <= Mathf.Max(_area.leftBound.position.x, _area.rightBound.position.x)
            && p.z >= Mathf.Min(_area.frontBound.position.z, _area.backBound.position.z)
            && p.z <= Mathf.Max(_area.frontBound.position.z, _area.backBound.position.z);
    }

    // Pone al boss un cuarto del cuadrado hacia dentro empezando desde el back
    private void PlaceBossAtSpawn()
    {
        float backZ = _area.backBound.position.z;
        float frontZ = _area.frontBound.position.z;
        float side = Mathf.Sign(backZ - frontZ);
        float depth = Mathf.Abs(backZ - frontZ);

        Vector3 position = _boss.position;
        position.x = (_area.leftBound.position.x + _area.rightBound.position.x) * 0.5f;
        position.z = backZ - side * depth * spawnDepthRatio;

        CharacterController cc = _boss.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        _boss.position = position;
        if (cc != null) cc.enabled = true;
    }

    private void OnDisable()
    {
        ClearObjects();
    }

    private void OnGUI()
    {
        if (Time.unscaledTime >= _messageUntil) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height * 0.4f), _message, style);
    }
}
