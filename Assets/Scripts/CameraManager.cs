using Cinemachine;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("General Properties")]
    [SerializeField] protected GameManager gameManager;
    [HideInInspector] public CinemachineVirtualCamera currentCamera;
    [SerializeField] protected CinemachineVirtualCamera worldCamera;
    [SerializeField] protected CinemachineVirtualCamera aimCamera;
    [HideInInspector] protected Cinemachine3rdPersonFollow thirdPersonFollow;
    [SerializeField] protected Transform followTransitionTarget;
    [SerializeField] protected Transform lookTransitionTarget;
    [SerializeField] protected float cameraTransitionDuration;
    [SerializeField] protected float aimTransitionDuration;
    [HideInInspector] public bool isCameraSet = true;

    [HideInInspector] public Transform previousTarget;
    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public Coroutine cameraTransition = null;

    [Header("Aim Properties")]
    [HideInInspector] protected float cameraSide;
    [HideInInspector] protected float cameraDistance;
    [HideInInspector] protected Vector3 cameraDamping;
    [HideInInspector] protected Vector3 cameraShoulderOffset;
    [HideInInspector] protected float cameraFOV;

    [SerializeField] protected float aimCameraSide;
    [SerializeField] protected float aimCameraDistance;
    [SerializeField] protected Vector3 aimCameraDamping;
    [SerializeField] protected Vector3 aimCameraShoulderOffset;
    [SerializeField] protected float aimCameraFOV;
    [SerializeField] protected RectTransform aimCursor;
    [HideInInspector] public Coroutine aimTransition = null;

    private void Awake()
    {
        currentCamera = worldCamera;

        /*thirdPersonFollow = currentCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        cameraSide = thirdPersonFollow.CameraSide;
        cameraDistance = thirdPersonFollow.CameraDistance;
        cameraDamping = thirdPersonFollow.Damping;
        cameraShoulderOffset = thirdPersonFollow.ShoulderOffset;
        cameraFOV = currentCamera.m_Lens.FieldOfView;*/
    }

    public IEnumerator SetCameraTarget(Transform follow, Transform look)
    {
        isCameraSet = false;
        previousTarget = currentCamera.m_Follow;
        currentTarget = follow;

        float elapsedTime = 0f;
        Transform startFollow = currentCamera.m_Follow;
        Transform startLook = currentCamera.m_LookAt;
        followTransitionTarget.position = startFollow.position;
        followTransitionTarget.rotation = startFollow.rotation;
        lookTransitionTarget.position = startLook.position;
        lookTransitionTarget.rotation = startLook.rotation;
        currentCamera.m_Follow = followTransitionTarget;
        currentCamera.m_LookAt = lookTransitionTarget;

        while (elapsedTime < cameraTransitionDuration)
        {
            float time = elapsedTime / cameraTransitionDuration;

            currentCamera.m_Follow.position = Vector3.Lerp(startFollow.position, follow.position, time);
            currentCamera.m_Follow.rotation = Quaternion.Slerp(startFollow.rotation, follow.rotation, time);

            currentCamera.m_LookAt.position = Vector3.Lerp(startLook.position, look.position, time);
            currentCamera.m_LookAt.rotation = Quaternion.Slerp(startLook.rotation, look.rotation, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        currentCamera.m_Follow = follow;
        currentCamera.m_LookAt = look;
        aimCamera.m_Follow = follow;
        aimCamera.m_LookAt = look;
        isCameraSet = true;
        cameraTransition = null;
    }

    public void SetCameraAim(bool value)
    {
        if (value)
        {
            worldCamera.Priority = 0;
            aimCamera.Priority = 100;
            currentCamera = aimCamera;
            StartCoroutine(ClampAimTargetRotation());
            StartCoroutine(ShowHideCursor(aimTransitionDuration, true));
        }
        else if (!value)
        {
            worldCamera.Priority = 100;
            aimCamera.Priority = 0;
            currentCamera = worldCamera;
            StartCoroutine(ShowHideCursor(0f, false));
        }
    }

    private IEnumerator ShowHideCursor(float time, bool value)
    {
        yield return new WaitForSecondsRealtime(time);

        aimCursor.gameObject.SetActive(value);

    }

    private IEnumerator ClampAimTargetRotation()
    {
        float elapsedTime = 0f;

        while (elapsedTime < aimTransitionDuration)
        {
            float time = elapsedTime / cameraTransitionDuration;

            gameManager.mainPlayer.cameraTarget.rotation = Quaternion.Slerp(gameManager.mainPlayer.cameraTarget.rotation, gameManager.mainPlayer.transform.rotation, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    /*public IEnumerator SetCameraAim(bool value)
    {
        float elapsedTime = 0f;

        if (!value)
        {
            aimCursor.gameObject.SetActive(false);
        }

        while (elapsedTime < aimTransitionDuration)
        {
            float time = elapsedTime / aimTransitionDuration;

            if (value)
            {
                thirdPersonFollow.CameraSide = Mathf.Lerp(cameraSide, aimCameraSide, time);
                thirdPersonFollow.CameraDistance = Mathf.Lerp(cameraDistance, aimCameraDistance, time);
                thirdPersonFollow.Damping = Vector3.Lerp(cameraDamping, aimCameraDamping, time);
                thirdPersonFollow.ShoulderOffset = Vector3.Lerp(cameraShoulderOffset, aimCameraShoulderOffset, time);
                currentCamera.m_Lens.FieldOfView = Mathf.Lerp(cameraFOV, aimCameraFOV, time);
            }
            else
            {
                thirdPersonFollow.CameraSide = Mathf.Lerp(aimCameraSide, cameraSide, time);
                thirdPersonFollow.CameraDistance = Mathf.Lerp(aimCameraDistance, cameraDistance, time);
                thirdPersonFollow.Damping = Vector3.Lerp(aimCameraDamping, cameraDamping, time);
                thirdPersonFollow.ShoulderOffset = Vector3.Lerp(aimCameraShoulderOffset, cameraShoulderOffset, time);
                currentCamera.m_Lens.FieldOfView = Mathf.Lerp(aimCameraFOV, cameraFOV, time);
            }

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (value)
        {
            aimCursor.gameObject.SetActive(true);
        }

        aimTransition = null;
    }*/
}
