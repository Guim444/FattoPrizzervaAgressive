using System.Collections;
using UnityEngine;

public class ChurchDoorTrigger : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private IntroSequenceManager introSequenceManager;
    [SerializeField] private Transform firstAutoMoveTarget;
    [SerializeField, Min(0f)] private float firstAutoMoveDuration = 2f;
    [SerializeField] private Transform secondAutoMoveTarget;
    [SerializeField, Min(0f)] private float secondAutoMoveDuration = 3f;

    [Header("RioTutte Visuals")]
    [SerializeField] private Transform rioTutteStandard;
    [SerializeField] private Transform rioTutteTransformation;

    [Header("Camera")]
    [Tooltip("Altura temporal de cámara durante los automoves.")]
    [SerializeField] private float autoMoveCameraY = 3f;
    [Tooltip("Activa la transición automática hacia la vista de prueba/gameplay tras un tiempo determinado.")]
    [SerializeField] private bool enableTimedCameraTransition = true;
    [Tooltip("Tiempo en segundos desde que se entra al trigger para que la cámara empiece a ir hacia la posición de prueba/gameplay.")]
    [SerializeField, Min(0f)] private float cameraTransitionDelay = 0f;

    [Header("Dialogue Layout")]
    [Tooltip("Escena que contiene únicamente el canvas usado durante el diálogo.")]
    [SerializeField] private string dialogueSceneName = "DialogueScene";
    [Tooltip("Escena que permanece activa y contiene DialogueLayoutManager.")]
    [SerializeField] private string lightingSceneName = "LightingScene";

    [SerializeField] private GameObject _blizzardVideos;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (_blizzardVideos)
        {
            _blizzardVideos.transform.parent = null;
        }  

        if (introSequenceManager == null)
        {
            Debug.LogError("[ChurchDoorTrigger] IntroSequenceManager no está asignado.", this);
            return;
        }

        float transitionDelay = enableTimedCameraTransition ? cameraTransitionDelay : -1f;

        triggered = introSequenceManager.TryStartChurchSequence(
            firstAutoMoveTarget,
            firstAutoMoveDuration,
            secondAutoMoveTarget,
            secondAutoMoveDuration,
            rioTutteStandard,
            rioTutteTransformation,
            autoMoveCameraY,
            dialogueSceneName,
            lightingSceneName,
            transitionDelay);
    }
}
