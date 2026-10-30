using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Minijuego de pulsar un boton muchas veces antes de que se acabe el tiempo
// Por ahora usa la barra espaciadora, que es el slot de salto
public sealed class SmashButtonChallenge : MonoBehaviour
{
    // Opcion para desactivar el minijuego, con ella basta una pulsacion
    public static bool simplified;

    private bool _active;
    private int _pressesNeeded;
    private int _presses;
    private float _endTime;
    private float _seconds;
    private string _label;
    private Action<bool> _onFinished;

    public bool Active => _active;

    // onFinished recibe true si se consiguio y false si se acabo el tiempo
    public void Begin(string label, int pressesNeeded, float seconds, Action<bool> onFinished)
    {
        _label = label;
        _pressesNeeded = simplified ? 1 : Mathf.Max(1, pressesNeeded);
        _presses = 0;
        _seconds = Mathf.Max(0.1f, seconds);
        _endTime = Time.time + _seconds;
        _onFinished = onFinished;
        _active = true;
    }

    public void Cancel()
    {
        _active = false;
        _onFinished = null;
    }

    private void Update()
    {
        if (!_active) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            _presses++;

        if (_presses >= _pressesNeeded)
            Finish(true);
        else if (Time.time >= _endTime)
            Finish(false);
    }

    private void Finish(bool success)
    {
        _active = false;
        Action<bool> callback = _onFinished;
        _onFinished = null;
        callback?.Invoke(success);
    }

    private void OnGUI()
    {
        if (!_active) return;

        float width = 360f;
        Rect area = new Rect((Screen.width - width) * 0.5f, Screen.height - 140f, width, 90f);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label($"{_label}: pulsa ESPACIO  {_presses}/{_pressesNeeded}");
        Rect bar = GUILayoutUtility.GetRect(width - 20f, 18f);
        GUI.Box(bar, GUIContent.none);
        GUI.Box(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(_presses / (float)_pressesNeeded), bar.height), GUIContent.none);
        GUILayout.Label($"Tiempo: {Mathf.Max(0f, _endTime - Time.time):F1} s");
        GUILayout.EndArea();
    }
}
