using UnityEngine;

// Controla las fases del combate contra el boss 0
// Fase 1: el boss empieza dentro del cuadrado y hay que sacarlo por el back
// Fase 2: el cuadrado se mueve en Z y el boss vuelve a aparecer dentro
// El combate de la fase 2 no empieza hasta que el jugador entra en el cuadrado
public sealed class BossFightPhaseController : MonoBehaviour
{
    // Cuanto se mueve el cuadrado en Z al pasar a la fase 2
    [SerializeField] private float phase2OffsetZ = 14f;

    // El boss aparece a esta fraccion del cuadrado empezando desde el back
    [SerializeField] private float spawnDepthRatio = 0.25f;

    [SerializeField] private float messageSeconds = 3f;

    private TutorialRingManager _area;
    private Transform _areaRoot;
    private Transform _boss;
    private Vector3 _originalRootPosition;
    private bool _hasOriginalPosition;
    private EnemyBase _bossAI;
    private int _phase;
    private bool _waitingForPlayer;
    private float _messageUntil;

    public void Begin(TutorialRingManager area, Transform boss)
    {
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

        _bossAI = boss.GetComponent<EnemyBase>();
        if (_bossAI != null) _bossAI.activeAI = true;

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
        else if (_phase == 2 && _waitingForPlayer && PlayerIsInsideArea())
        {
            // El jugador ha llegado, el boss empieza a moverse
            _waitingForPlayer = false;
            if (_bossAI != null) _bossAI.activeAI = true;
        }
    }

    private void FinishPhase1()
    {
        _phase = 2;
        _waitingForPlayer = true;
        _messageUntil = Time.unscaledTime + messageSeconds;

        // El boss se queda quieto en su sitio hasta que llegue el jugador
        if (_bossAI != null) _bossAI.activeAI = false;

        _areaRoot.position += new Vector3(0f, 0f, phase2OffsetZ);
        PlaceBossAtSpawn();
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

        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height * 0.4f), "Finish Phase1", style);
    }
}
