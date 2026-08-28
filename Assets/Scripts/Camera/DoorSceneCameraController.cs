using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DoorSceneCameraController : MonoBehaviour
{
    [System.Serializable]
    public class CameraShot
    {
        [Header("Movement")]
        public Transform targetPosition;

        [Min(0.01f)]
        public float moveDuration = 1f;

        [Tooltip("Movement curve for this shot.")]
        public AnimationCurve moveCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        [Header("Rotation")]
        public bool animateRotation = true;

        [Tooltip("If assigned, the camera will look at this object.")]
        public Transform lookTarget;


        [Header("Field Of View")]
        public bool changeFOV = false;

        [Range(1f, 179f)]
        public float targetFOV = 60f;

        [Min(0.01f)]
        public float fovDuration = 1f;

        [Tooltip("FOV transition curve.")]
        public AnimationCurve fovCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        [Header("Shake")]
        public bool useShake = false;

        [Tooltip("Maximum positional shake intensity.")]
        [Min(0f)]
        public float shakePositionIntensity = 0.1f;

        [Tooltip("Maximum rotational shake intensity in degrees.")]
        [Min(0f)]
        public float shakeRotationIntensity = 1f;

        [Tooltip("Base shake frequency.")]
        [Min(0f)]
        public float shakeFrequency = 20f;

        [Tooltip("Shake intensity multiplier over the duration of the shot.")]
        public AnimationCurve shakeIntensityCurve =
            AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip("Shake frequency multiplier over the duration of the shot.")]
        public AnimationCurve shakeFrequencyCurve =
            AnimationCurve.Linear(0f, 1f, 1f, 1f);


        [Header("Timing")]
        [Tooltip("Time to wait after the camera reaches the end of the shot.")]
        [Min(0f)]
        public float holdTime = 0f;
    }


    [Header("Sequence")]
    [Tooltip("Camera shots played in order.")]
    public CameraShot[] shots;


    [Header("Start Settings")]
    public bool playOnStart = true;

    [Min(0f)]
    public float startDelay = 0f;


    [Header("Final Fade")]
    public bool fadeToBlackAtEnd = true;

    [Min(0.01f)]
    public float fadeDuration = 1.5f;

    [Tooltip("Fade animation curve.")]
    public AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("Debug")]
    [SerializeField]
    private int currentShot = -1;

    public bool drawGizmos = true;


    private Camera cam;


    private Vector3 basePosition;
    private Quaternion baseRotation;


    private bool shakeActive;

    private float currentPositionShake;
    private float currentRotationShake;
    private float currentShakeFrequency;

    private float shakeTime;


    private Texture2D blackTexture;
    private float fadeAlpha;


    private Coroutine sequenceRoutine;


    private void Awake()
    {
        cam = GetComponent<Camera>();

        basePosition = transform.position;
        baseRotation = transform.rotation;

        CreateBlackTexture();
    }


    private void Start()
    {
        if (playOnStart)
        {
            PlaySequence();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 1. Atura la seqüència, el sacseig i torna a la posició inicial
            StopSequence();

            // 2. Elimina el fos a negre per netejar la pantalla
            ResetFade();

            // 3. Torna a arrencar tota la seqüència de plans des de l'inici
            PlaySequence();

            Debug.Log("🔄 Seqüència reiniciada directament des del controlador!");
        }
    }

    private void LateUpdate()
    {
        if (!shakeActive)
        {
            return;
        }

        shakeTime += Time.deltaTime * currentShakeFrequency;


        float positionX =
            (Mathf.PerlinNoise(shakeTime, 0.17f) - 0.5f) * 2f;

        float positionY =
            (Mathf.PerlinNoise(0.43f, shakeTime) - 0.5f) * 2f;


        Vector3 positionOffset =
            new Vector3(
                positionX,
                positionY,
                0f
            ) * currentPositionShake;


        float rotationX =
            (Mathf.PerlinNoise(shakeTime, 4.17f) - 0.5f) * 2f;

        float rotationY =
            (Mathf.PerlinNoise(7.31f, shakeTime) - 0.5f) * 2f;

        float rotationZ =
            (Mathf.PerlinNoise(shakeTime, 9.73f) - 0.5f) * 2f;


        Vector3 rotationOffset =
            new Vector3(
                rotationX,
                rotationY,
                rotationZ
            ) * currentRotationShake;


        transform.position =
            basePosition +
            baseRotation * positionOffset;


        transform.rotation =
            baseRotation *
            Quaternion.Euler(rotationOffset);
    }


    public void PlaySequence()
    {
        StopCurrentSequence();

        fadeAlpha = 0f;

        sequenceRoutine =
            StartCoroutine(SequenceRoutine());
    }


    public void StopSequence()
    {
        StopCurrentSequence();

        StopShake();

        transform.position = basePosition;
        transform.rotation = baseRotation;
    }


    public void PlayShot(int index)
    {
        if (shots == null)
        {
            return;
        }

        if (index < 0 || index >= shots.Length)
        {
            return;
        }

        StopCurrentSequence();

        fadeAlpha = 0f;

        sequenceRoutine =
            StartCoroutine(PlaySingleShot(index));
    }


    public void ResetFade()
    {
        fadeAlpha = 0f;
    }


    private void StopCurrentSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }


    private IEnumerator SequenceRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }


        for (int i = 0; i < shots.Length; i++)
        {
            currentShot = i;

            yield return StartCoroutine(
                ExecuteShot(shots[i])
            );
        }


        currentShot = -1;

        StopShake();


        if (fadeToBlackAtEnd)
        {
            yield return StartCoroutine(
                FadeToBlack()
            );
        }


        sequenceRoutine = null;
    }


    private IEnumerator PlaySingleShot(int index)
    {
        currentShot = index;

        yield return StartCoroutine(
            ExecuteShot(shots[index])
        );


        currentShot = -1;

        StopShake();

        sequenceRoutine = null;
    }


    private IEnumerator ExecuteShot(CameraShot shot)
    {
        if (shot == null)
        {
            yield break;
        }


        if (shot.targetPosition == null)
        {
            Debug.LogWarning(
                "CameraShot does not have a Target Position."
            );

            yield break;
        }


        Vector3 startPosition =
            transform.position;

        Quaternion startRotation =
            transform.rotation;

        float startFOV =
            cam.fieldOfView;


        Vector3 destination =
            shot.targetPosition.position;


        float duration =
            Mathf.Max(
                0.01f,
                shot.moveDuration
            );


        float elapsed = 0f;


        shakeActive =
            shot.useShake;

        shakeTime = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / duration
                );


            float movementT =
                shot.moveCurve.Evaluate(
                    normalizedTime
                );


            basePosition =
                Vector3.LerpUnclamped(
                    startPosition,
                    destination,
                    movementT
                );


            if (shot.animateRotation)
            {
                Quaternion desiredRotation;


                if (shot.lookTarget != null)
                {
                    Vector3 direction =
                        shot.lookTarget.position -
                        basePosition;


                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        desiredRotation =
                            Quaternion.LookRotation(
                                direction.normalized,
                                Vector3.up
                            );
                    }
                    else
                    {
                        desiredRotation =
                            startRotation;
                    }
                }
                else
                {
                    desiredRotation =
                        shot.targetPosition.rotation;
                }


                baseRotation =
                    Quaternion.SlerpUnclamped(
                        startRotation,
                        desiredRotation,
                        movementT
                    );
            }
            else
            {
                baseRotation =
                    startRotation;
            }


            if (shot.changeFOV)
            {
                float fovNormalizedTime =
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(
                            0.01f,
                            shot.fovDuration
                        )
                    );


                float fovT =
                    shot.fovCurve.Evaluate(
                        fovNormalizedTime
                    );


                cam.fieldOfView =
                    Mathf.LerpUnclamped(
                        startFOV,
                        shot.targetFOV,
                        fovT
                    );
            }


            if (shot.useShake)
            {
                float intensityMultiplier =
                    shot.shakeIntensityCurve.Evaluate(
                        normalizedTime
                    );


                float frequencyMultiplier =
                    shot.shakeFrequencyCurve.Evaluate(
                        normalizedTime
                    );


                currentPositionShake =
                    shot.shakePositionIntensity *
                    intensityMultiplier;


                currentRotationShake =
                    shot.shakeRotationIntensity *
                    intensityMultiplier;


                currentShakeFrequency =
                    Mathf.Max(
                        0f,
                        shot.shakeFrequency *
                        frequencyMultiplier
                    );
            }


            if (!shot.useShake)
            {
                transform.position =
                    basePosition;

                transform.rotation =
                    baseRotation;
            }


            yield return null;
        }


        basePosition =
            destination;


        if (shot.animateRotation)
        {
            if (shot.lookTarget != null)
            {
                Vector3 direction =
                    shot.lookTarget.position -
                    destination;


                if (direction.sqrMagnitude > 0.0001f)
                {
                    baseRotation =
                        Quaternion.LookRotation(
                            direction.normalized,
                            Vector3.up
                        );
                }
            }
            else
            {
                baseRotation =
                    shot.targetPosition.rotation;
            }
        }


        transform.position =
            basePosition;

        transform.rotation =
            baseRotation;


        if (shot.changeFOV)
        {
            cam.fieldOfView =
                shot.targetFOV;
        }


        StopShake();


        if (shot.holdTime > 0f)
        {
            yield return new WaitForSeconds(
                shot.holdTime
            );
        }
    }


    private void StopShake()
    {
        shakeActive = false;

        currentPositionShake = 0f;
        currentRotationShake = 0f;
        currentShakeFrequency = 0f;

        transform.position =
            basePosition;

        transform.rotation =
            baseRotation;
    }


    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        0.01f,
                        fadeDuration
                    )
                );


            fadeAlpha =
                fadeCurve.Evaluate(
                    normalizedTime
                );


            yield return null;
        }


        fadeAlpha = 1f;
    }


    private void CreateBlackTexture()
    {
        blackTexture =
            new Texture2D(1, 1);


        blackTexture.SetPixel(
            0,
            0,
            Color.black
        );


        blackTexture.Apply();
    }


    private void OnGUI()
    {
        if (fadeAlpha <= 0f)
        {
            return;
        }


        Color previousColor =
            GUI.color;


        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                Mathf.Clamp01(fadeAlpha)
            );


        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height
            ),
            blackTexture
        );


        GUI.color =
            previousColor;
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        if (shots == null)
        {
            return;
        }


        Vector3 previousPosition =
            transform.position;


        foreach (CameraShot shot in shots)
        {
            if (shot == null)
            {
                continue;
            }

            if (shot.targetPosition == null)
            {
                continue;
            }


            Gizmos.DrawWireSphere(
                shot.targetPosition.position,
                0.15f
            );


            Gizmos.DrawLine(
                previousPosition,
                shot.targetPosition.position
            );


            if (shot.lookTarget != null)
            {
                Gizmos.DrawLine(
                    shot.targetPosition.position,
                    shot.lookTarget.position
                );
            }


            previousPosition =
                shot.targetPosition.position;
        }
    }
}
