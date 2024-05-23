using Cinemachine;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Cinematic;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;
using Ink.Parsed;

public class CameraManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameManager gameManager;
    public CinemachineFreeLook worldCamera;
    public CinemachineFreeLook aimCamera;
    public CinemachineVirtualCamera cinematicCamera;
    [HideInInspector] private Cinemachine3rdPersonFollow thirdPersonFollow;
    public CinemachineBrain brain;
    private float brainTransitionDuration;
    
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
    [SerializeField] private AnimationCurve warpCurve;
    [SerializeField] private AnimationCurve fovCurve;
    private Coroutine aimRoutine;
    private Coroutine cursorRoutine;
    
    [Header("UI References")]
    [SerializeField] private Image blackScreen;

    [Header("Cinematics")]
    public CinematicSequence intro;

    private void Awake()
    {
        currentCam = worldCamera;
        brain = Camera.main.GetComponent<CinemachineBrain>();
        brainTransitionDuration = brain.m_DefaultBlend.m_Time;
    }

    public void ChangeCamera(CinemachineVirtualCameraBase newCam)
    {
        if (currentCam != null)
        {
            previousCam = currentCam;
            currentCam.Priority = 0;
        }

        currentCam = newCam;
        brain.m_DefaultBlend.m_Time = brainTransitionDuration;
        currentCam.Priority = 100;
    }

    public void ChangeCamera(CinemachineVirtualCameraBase newCam, float transitionDuration)
    {
        if (currentCam != null)
        {
            previousCam = currentCam;
            currentCam.Priority = 0;
        }

        currentCam = newCam;
        brain.m_DefaultBlend.m_Time = transitionDuration;
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

    public IEnumerator SetCameraTarget(Transform follow, Transform look, bool isWarp)
    {
        if (isWarp)
        {
            StartCoroutine(SetWarp(follow));
        }
        
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

            Vector3 newFollowPos = Vector3.Lerp(startFollow.position, follow.position, warpCurve.Evaluate(time));
            Vector3 newLookPos = Vector3.Lerp(startLook.position, look.position, warpCurve.Evaluate(time));

            worldCamera.m_Follow.position = newFollowPos;
            worldCamera.m_Follow.rotation = Quaternion.Slerp(startFollow.rotation, follow.rotation, time);

            worldCamera.m_LookAt.position = newLookPos;
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

    private IEnumerator SetWarp(Transform target)
    {
        float elapsedTime = 0f;
        float segmentTime = cameraTransitionDuration / 3;
        float fovToSet = 80f;
        float lensToSet = -0.25f;
        
        if (!gameManager.globalVolume.profile.TryGet<LensDistortion>(out LensDistortion lensDistortion))
        {
            Debug.LogError("Lens Distortion effect not found in the Global Volume!");
            yield break;
        }
        
        while (elapsedTime < segmentTime)
        {
            float time = elapsedTime / segmentTime;
            
            float fov = Mathf.Lerp(worldCamera.m_Lens.FieldOfView, fovToSet, fovCurve.Evaluate(time));
            worldCamera.m_Lens.FieldOfView = fov;
            float lens = Mathf.Lerp(lensDistortion.intensity.value, lensToSet, warpCurve.Evaluate(time));
            lensDistortion.intensity.value = lens;

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        elapsedTime = 0f;
        lensToSet = -0.75f;

        while (elapsedTime < segmentTime)
        {
            float time = elapsedTime / segmentTime;
            
            float fov = Mathf.Lerp(worldCamera.m_Lens.FieldOfView, fovToSet, warpCurve.Evaluate(time));
            worldCamera.m_Lens.FieldOfView = fov;
            float lens = Mathf.Lerp(lensDistortion.intensity.value, lensToSet, fovCurve.Evaluate(time));
            lensDistortion.intensity.value = lens;

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        elapsedTime = 0f;
        fovToSet = 45f;
        lensToSet = 0f;
        
        while (elapsedTime < segmentTime)
        {
            float time = elapsedTime / segmentTime;
            
            float fov = Mathf.Lerp(worldCamera.m_Lens.FieldOfView, fovToSet, fovCurve.Evaluate(time));
            worldCamera.m_Lens.FieldOfView = fov;
            float lens = Mathf.Lerp(lensDistortion.intensity.value, lensToSet, fovCurve.Evaluate(time));
            lensDistortion.intensity.value = lens;

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        elapsedTime = 0f;
        while (elapsedTime < cameraTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
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
            float time = elapsedTime / aimTransitionDuration;

            gameManager.mainPlayer.cameraTarget.localPosition = Vector3.Slerp(gameManager.mainPlayer.cameraTarget.localPosition, targetPos, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
