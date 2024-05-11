using Cinemachine;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Cinematic;

public class CameraManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameManager gameManager;
    public CinemachineFreeLook worldCamera;
    public CinemachineFreeLook aimCamera;
    public CinemachineVirtualCamera cinematicCamera;
    [HideInInspector] private Cinemachine3rdPersonFollow thirdPersonFollow;
    
    [SerializeField] private RectTransform aimCursor;
    [SerializeField] private Transform followTransitionTarget;
    [SerializeField] private Transform lookTransitionTarget;
    [SerializeField] private Transform cinematicTarget;

    [Header("General References")]
    [HideInInspector] public CinemachineVirtualCameraBase currentCam;
    [HideInInspector] public CinemachineVirtualCameraBase previousCam;
    [SerializeField] private float cameraTransitionDuration;
    [SerializeField] private float aimTransitionDuration;
    [HideInInspector] public bool isCameraSet = true;
    [HideInInspector] public Transform previousTarget;
    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public Coroutine cameraTransition = null;
    private Coroutine aimRoutine;
    private Coroutine cursorRoutine;
    
    [Header("UI References")]
    [SerializeField] private Image blackScreen;

    [Header("Cinematics")]
    public CinematicSequence intro;

    private void Awake()
    {
        currentCam = worldCamera;
    }

    public void ChangeCamera(CinemachineVirtualCameraBase newCam)
    {
        if (currentCam !=  null)
        {
            previousCam = currentCam;
            currentCam.Priority = 0;
        }

        currentCam = newCam;
        currentCam.Priority = 100;
    }

    public void ExecuteCinematic(CinematicSequence sequence)
    {
        sequence.Execute(this, gameManager, DialogueManager.instance, cinematicCamera, cinematicTarget);
    }

    public void BlackScreen(float fadeTime, float blackTime)
    {
        StartCoroutine(BlackScreenRoutine(fadeTime, blackTime));
    }

    private IEnumerator BlackScreenRoutine(float fadeTime, float blackTime)
    {
        blackScreen.DOFade(1f, fadeTime);
        gameManager.mainPlayer.isActive = false;
        yield return new WaitForSecondsRealtime(fadeTime);
        yield return new WaitForSecondsRealtime(blackTime);
        blackScreen.DOFade(0f, fadeTime);
        yield return new WaitForSecondsRealtime(fadeTime);
        gameManager.mainPlayer.isActive = true;
    }

    public IEnumerator SetCameraTarget(Transform follow, Transform look)
    {
        isCameraSet = false;
        previousTarget = worldCamera.m_Follow;
        currentTarget = follow;

        float elapsedTime = 0f;
        Transform startFollow = worldCamera.m_Follow;
        Transform startLook = worldCamera.m_LookAt;
        followTransitionTarget.position = startFollow.position;
        followTransitionTarget.rotation = startFollow.rotation;
        lookTransitionTarget.position = startLook.position;
        lookTransitionTarget.rotation = startLook.rotation;
        worldCamera.m_Follow = followTransitionTarget;
        worldCamera.m_LookAt = lookTransitionTarget;

        while (elapsedTime < cameraTransitionDuration)
        {
            float time = elapsedTime / cameraTransitionDuration;

            worldCamera.m_Follow.position = Vector3.Lerp(startFollow.position, follow.position, time);
            worldCamera.m_Follow.rotation = Quaternion.Slerp(startFollow.rotation, follow.rotation, time);

            worldCamera.m_LookAt.position = Vector3.Lerp(startLook.position, look.position, time);
            worldCamera.m_LookAt.rotation = Quaternion.Slerp(startLook.rotation, look.rotation, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        worldCamera.m_Follow = follow;
        worldCamera.m_LookAt = look;
        aimCamera.m_Follow = follow;
        aimCamera.m_LookAt = look;
        isCameraSet = true;
        cameraTransition = null;
    }

    public void SetCameraAim(bool value, Vector3 targetPos)
    {
        if (value)
        {
            ChangeCamera(aimCamera);
            if (cursorRoutine != null)
            {
                StopCoroutine(cursorRoutine);
            }

            cursorRoutine = StartCoroutine(ShowHideCursor(aimTransitionDuration, true));
        }
        else if (!value)
        {
            ChangeCamera(worldCamera);
            if (cursorRoutine != null)
            {
                StopCoroutine(cursorRoutine);
            }

            cursorRoutine = StartCoroutine(ShowHideCursor(0f, false));
        }

        if (aimRoutine != null)
        {
            StopCoroutine(aimRoutine);
        }

        aimRoutine = StartCoroutine(SetAimTarget(targetPos));
    }

    private IEnumerator ShowHideCursor(float time, bool value)
    {
        yield return new WaitForSecondsRealtime(time);

        aimCursor.gameObject.SetActive(value);

    }

    private IEnumerator SetAimTarget(Vector3 targetPos)
    {
        float elapsedTime = 0f;

        while (elapsedTime < aimTransitionDuration)
        {
            float time = elapsedTime / cameraTransitionDuration;

            gameManager.mainPlayer.cameraTarget.localPosition = Vector3.Slerp(gameManager.mainPlayer.cameraTarget.localPosition, targetPos, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
