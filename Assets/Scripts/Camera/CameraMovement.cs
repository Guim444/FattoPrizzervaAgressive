using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Smoothness")]
    public float smoothTime  = 0.3f;
    public float ySmoothTime = 0.4f;

    [Header("Zoom X")]
    public bool enableZoom = true;
    public float zoomDistance;
    [SerializeField] private float zoomInThreshold  =  0.5f;
    [SerializeField] private float zoomOutThreshold = -0.5f;

    [Header("Z Zones")]
    public float zTransitionMin  = 6f;
    public float zTransitionMax  = 10f;
    public float zTargetInside   = 4f;
    public float zTargetOutside  = 14f;

    [Header("Y Position")]
    public float combatY      = 1.5f;
    public bool  followPlayerY = false;
    public float yOffset      = 1.5f;

    [Header("Camera")]
    [SerializeField] private float defaultFieldOfView = 45f;
    [SerializeField] private float farClipPlane = 40f;
    [SerializeField] private float rotationX = 6.234f;
    [SerializeField] private float rotationY = 89.006f;
    [SerializeField] private float rotationZ = 359.892f;

    public bool zoomed;
    private float xZoomTarget;
    private float xVel;
    private float zVel;
    private float _yVel;
    private float _fovVel;
    private Camera _cam;
    private float _pendingTargetX;
    private bool _preserveTransformOnNextEnable;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null)
        {
            _cam.farClipPlane = farClipPlane;
            if (_cam.fieldOfView > 1f)
                defaultFieldOfView = _cam.fieldOfView;
        }
    }

    public void PrepareSmoothTransition(float targetX, float targetY)
    {
        combatY = targetY;
        _pendingTargetX = targetX;
        _preserveTransformOnNextEnable = true;

        xZoomTarget = targetX;

        xVel = 0f;
        zVel = 0f;
        _yVel = 0f;
        _fovVel = 0f;
        zoomed = false;
        enableZoom = false;
    }

    public void SnapToTestPosition(float targetX, float targetY)
    {
        combatY = targetY;
        float targetZ = GetTargetZ();

        transform.position = new Vector3(targetX, targetY, targetZ);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);

        if (_cam != null)
            _cam.fieldOfView = defaultFieldOfView;

        xZoomTarget = targetX;
        xVel  = 0f;
        zVel  = 0f;
        _yVel = 0f;
        _fovVel = 0f;
        zoomed = false;
        enableZoom = true;
    }

    private void OnEnable()
    {
        if (player == null) return;

        if (_preserveTransformOnNextEnable)
        {
            _preserveTransformOnNextEnable = false;
            xZoomTarget = _pendingTargetX;
            xVel = 0f;
            zVel = 0f;
            _yVel = 0f;
            _fovVel = 0f;
            zoomed = false;
            enableZoom = false;
            return;
        }

        // Immediate snap to position relative to player
        float targetX = player.position.x;
        float targetY = followPlayerY ? player.position.y + yOffset : combatY;
        float targetZ = GetTargetZ();

        transform.position = new Vector3(targetX, targetY, targetZ);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);

        if (_cam != null)
            _cam.fieldOfView = defaultFieldOfView;

        xZoomTarget = targetX;
        xVel  = 0f;
        zVel  = 0f;
        _yVel = 0f;
        _fovVel = 0f;
        zoomed = false;
        enableZoom = true;
    }

    private void Update()
    {
        float targetY = followPlayerY ? player.position.y + yOffset : combatY;

        float st  = Mathf.Max(smoothTime,  0.01f);
        float sty = Mathf.Max(ySmoothTime, 0.01f);

        float newX = Mathf.SmoothDamp(transform.position.x, xZoomTarget,  ref xVel,  st);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY,       ref _yVel, sty);
        float newZ = Mathf.SmoothDamp(transform.position.z, GetTargetZ(),  ref zVel,  st);

        if (float.IsNaN(newX) || float.IsNaN(newY) || float.IsNaN(newZ))
        {
            xVel = 0f; zVel = 0f; _yVel = 0f;
            return;
        }

        transform.position = new Vector3(newX, newY, newZ);

        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            float rotDamp = 1f - Mathf.Exp(-Time.deltaTime * (2.5f / st));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotDamp);
        }
        else
        {
            transform.rotation = targetRotation;
        }

        if (_cam != null)
        {
            if (Mathf.Abs(_cam.fieldOfView - defaultFieldOfView) > 0.05f)
                _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, defaultFieldOfView, ref _fovVel, st);
            else
                _cam.fieldOfView = defaultFieldOfView;
        }
    }

    private void LateUpdate()
    {
        if (!enableZoom) return;

        if (player.position.x > zoomInThreshold && !zoomed)
            Zoom(1);
        else if (player.position.x < zoomOutThreshold && zoomed)
            Zoom(-1);
    }

    public void Zoom(int zoom)
    {
        xZoomTarget += zoomDistance * zoom;
        zoomed = zoom == 1;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        xZoomTarget = position.x;
        xVel  = 0f;
        zVel  = 0f;
        _yVel = 0f;
        combatY = position.y;
        zoomed  = false;
    }

    private float GetTargetZ()
    {
        float z = player.position.z;
        if (z < zTransitionMin) return zTargetInside;
        if (z > zTransitionMax) return zTargetOutside;
        return z;
    }
}
