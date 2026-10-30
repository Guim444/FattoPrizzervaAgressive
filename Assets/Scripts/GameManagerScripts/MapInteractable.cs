using System;
using System.Collections.Generic;
using UnityEngine;

// Objeto del mapa contra el que se puede empujar al boss
// Cuenta una vez cuando el boss lo toca mientras sale empujado
public sealed class MapInteractable : MonoBehaviour
{
    public Color usedColor = new Color(1f, 0.55f, 0.1f);

    // Cuanto se ensancha el objeto para detectar el contacto
    [SerializeField] private float contactMargin = 0.25f;

    public bool Used { get; private set; }
    public event Action<MapInteractable> Triggered;

    private EnemyBase _boss;
    private CharacterController _bossController;
    private Collider _collider;
    private Renderer _renderer;

    private enum Kind { SquareRock, LongRock, Log }

    // Posiciones como fraccion del cuadrado, x de 0 a 1 en Left a Right, z de 0 a 1 en Front a Back
    private static readonly Vector2[] SpreadSpots =
    {
        new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.35f), new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.9f)
    };

    private static readonly Kind[] SpotKinds = { Kind.SquareRock, Kind.Log, Kind.LongRock, Kind.Log };

    // Objetos de relleno repartidos por el cuadrado, luego cada uno sera la pieza de arte final
    public static List<MapInteractable> SpawnBoxes(TutorialRingManager area, EnemyBase boss)
    {
        float frontZ = area.frontBound.position.z;
        float backZ = area.backBound.position.z;
        float leftX = area.leftBound.position.x;
        float rightX = area.rightBound.position.x;
        float groundY = boss.GetComponent<CharacterController>().bounds.min.y;

        List<MapInteractable> items = new List<MapInteractable>();
        for (int i = 0; i < SpreadSpots.Length; i++)
        {
            GameObject obj = CreateObject(SpotKinds[i], out float height);
            obj.name = $"{SpotKinds[i]}_{i + 1}";
            obj.transform.position = new Vector3(
                Mathf.Lerp(leftX, rightX, SpreadSpots[i].x),
                groundY + height * 0.5f,
                Mathf.Lerp(frontZ, backZ, SpreadSpots[i].y));

            MapInteractable item = obj.AddComponent<MapInteractable>();
            item.Init(boss);
            items.Add(item);
        }

        return items;
    }

    // Roca cuadrada, roca alargada en el suelo o tronco caido tumbado a lo largo de Z
    private static GameObject CreateObject(Kind kind, out float height)
    {
        GameObject obj;
        Color color;

        switch (kind)
        {
            case Kind.Log:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                height = 0.5f;
                color = new Color(0.45f, 0.28f, 0.12f);
                break;
            case Kind.LongRock:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = new Vector3(0.6f, 0.5f, 1.8f);
                height = 0.5f;
                color = new Color(0.4f, 0.4f, 0.42f);
                break;
            default:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                height = 0.8f;
                color = new Color(0.55f, 0.55f, 0.58f);
                break;
        }

        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    public void Init(EnemyBase boss)
    {
        _boss = boss;
        _bossController = boss.GetComponent<CharacterController>();
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<Renderer>();
        Used = false;
    }

    private void Update()
    {
        if (Used || _boss == null || !_boss.HasKnockback || !_bossController.enabled) return;

        Bounds area = _collider.bounds;
        area.Expand(contactMargin * 2f);
        if (!area.Intersects(_bossController.bounds)) return;

        Used = true;
        if (_renderer != null) _renderer.material.color = usedColor;
        Triggered?.Invoke(this);
    }
}
