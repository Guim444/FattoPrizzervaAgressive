using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Dibuja los colliders en el Game para depurar
// Verde: jugador, rojo: enemigos, amarillo: triggers, celeste: mapa, magenta: caja del puñetazo
// Usa el shader AT/DebugLines que esta en la carpeta AT
public sealed class ColliderDebugView : MonoBehaviour
{
    public bool show;

    // Los colliders del mapa mas lejos que esto del jugador no se dibujan
    [SerializeField] private float mapDrawDistance = 40f;

    private struct Entry
    {
        public Collider collider;
        public Color color;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Color32> _colors = new List<Color32>();
    private readonly List<int> _indices = new List<int>();

    private Mesh _mesh;
    private Material _material;
    private Transform _player;
    private Color32 _currentColor;
    private float _nextRefresh;

    private void Awake()
    {
        _mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        _mesh.MarkDynamic();
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }

    private void LateUpdate()
    {
        if (!show) return;

        if (_material == null)
        {
            Shader shader = Shader.Find("AT/DebugLines");
            if (shader == null)
            {
                Debug.LogWarning("[ColliderDebugView] No se encuentra el shader AT/DebugLines");
                show = false;
                return;
            }

            _material = new Material(shader);
        }

        RefreshEntries();

        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();

        foreach (Entry entry in _entries)
        {
            Collider collider = entry.collider;
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                continue;

            // El mapa lejano no se dibuja, jugador y enemigos siempre
            bool isMap = entry.color.g > 0.8f && entry.color.b > 0.9f;
            if (isMap && _player != null &&
                (collider.bounds.center - _player.position).sqrMagnitude > mapDrawDistance * mapDrawDistance)
                continue;

            _currentColor = entry.color;
            DrawCollider(collider);
        }

        // Caja del ultimo puñetazo, se ve un rato despues de pegar
        if (Time.time - CombatAttackHandler.DebugBoxTime < 0.8f)
        {
            _currentColor = Color.magenta;
            DrawWorldBox(CombatAttackHandler.DebugBoxCenter, CombatAttackHandler.DebugBoxHalf);
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetIndices(_indices, MeshTopology.Lines, 0);
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
    }

    // La lista se recalcula cada medio segundo para no gastar de mas
    private void RefreshEntries()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.5f;

        if (_player == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) _player = player.transform;
        }

        _entries.Clear();
        foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            Color color = new Color(0.2f, 0.9f, 1f);
            if (collider.GetComponentInParent<PlayerController>() != null) color = Color.green;
            else if (collider.GetComponentInParent<EnemyBase>() != null) color = Color.red;
            else if (collider.isTrigger) color = Color.yellow;

            _entries.Add(new Entry { collider = collider, color = color });
        }
    }

    private void DrawCollider(Collider collider)
    {
        Transform t = collider.transform;

        if (collider is BoxCollider box)
        {
            DrawBox(t, box.center, box.size);
        }
        else if (collider is SphereCollider sphere)
        {
            Vector3 scale = t.lossyScale;
            float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            DrawSphere(t.TransformPoint(sphere.center), radius);
        }
        else if (collider is CapsuleCollider capsule)
        {
            Vector3 axis = capsule.direction == 0 ? t.right : capsule.direction == 1 ? t.up : t.forward;
            Vector3 scale = t.lossyScale;
            float axisScale = capsule.direction == 0 ? scale.x : capsule.direction == 1 ? scale.y : scale.z;
            float radiusScale = Mathf.Max(
                capsule.direction == 0 ? 0f : Mathf.Abs(scale.x),
                capsule.direction == 1 ? 0f : Mathf.Abs(scale.y),
                capsule.direction == 2 ? 0f : Mathf.Abs(scale.z));
            DrawCapsule(t.TransformPoint(capsule.center), axis, capsule.height * Mathf.Abs(axisScale),
                capsule.radius * radiusScale);
        }
        else if (collider is CharacterController cc)
        {
            Vector3 scale = t.lossyScale;
            DrawCapsule(t.TransformPoint(cc.center), Vector3.up, cc.height * Mathf.Abs(scale.y),
                cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
        }
        else
        {
            // Cualquier otro collider se dibuja como su caja envolvente
            Bounds b = collider.bounds;
            DrawWorldBox(b.center, b.extents);
        }
    }

    private void DrawBox(Transform t, Vector3 center, Vector3 size)
    {
        Vector3 h = size * 0.5f;
        Vector3[] c = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3(
                (i & 1) == 0 ? -h.x : h.x,
                (i & 2) == 0 ? -h.y : h.y,
                (i & 4) == 0 ? -h.z : h.z);
            c[i] = t.TransformPoint(corner);
        }

        DrawBoxEdges(c);
    }

    private void DrawWorldBox(Vector3 center, Vector3 half)
    {
        Vector3[] c = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            c[i] = center + new Vector3(
                (i & 1) == 0 ? -half.x : half.x,
                (i & 2) == 0 ? -half.y : half.y,
                (i & 4) == 0 ? -half.z : half.z);
        }

        DrawBoxEdges(c);
    }

    // Une las esquinas que solo cambian en un eje
    private void DrawBoxEdges(Vector3[] c)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int bit = 1; bit <= 4; bit <<= 1)
            {
                if ((i & bit) == 0)
                    Line(c[i], c[i | bit]);
            }
        }
    }

    private void DrawSphere(Vector3 center, float radius)
    {
        DrawCircle(center, Vector3.right, Vector3.up, radius);
        DrawCircle(center, Vector3.right, Vector3.forward, radius);
        DrawCircle(center, Vector3.up, Vector3.forward, radius);
    }

    private void DrawCapsule(Vector3 center, Vector3 axis, float height, float radius)
    {
        axis = axis.normalized;
        float half = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 top = center + axis * half;
        Vector3 bottom = center - axis * half;

        Vector3 a = Vector3.Cross(axis, Vector3.up);
        if (a.sqrMagnitude < 0.001f) a = Vector3.Cross(axis, Vector3.right);
        a.Normalize();
        Vector3 b = Vector3.Cross(axis, a).normalized;

        DrawCircle(top, a, b, radius);
        DrawCircle(bottom, a, b, radius);
        Line(top + a * radius, bottom + a * radius);
        Line(top - a * radius, bottom - a * radius);
        Line(top + b * radius, bottom + b * radius);
        Line(top - b * radius, bottom - b * radius);

        // Las puntas redondeadas se marcan con un circulo extra
        DrawCircle(top, a, axis, radius);
        DrawCircle(bottom, a, axis, radius);
    }

    private void DrawCircle(Vector3 center, Vector3 a, Vector3 b, float radius)
    {
        const int segments = 24;
        Vector3 previous = center + a * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + (a * Mathf.Cos(angle) + b * Mathf.Sin(angle)) * radius;
            Line(previous, next);
            previous = next;
        }
    }

    private void Line(Vector3 from, Vector3 to)
    {
        int index = _vertices.Count;
        _vertices.Add(from);
        _vertices.Add(to);
        _colors.Add(_currentColor);
        _colors.Add(_currentColor);
        _indices.Add(index);
        _indices.Add(index + 1);
    }
}
