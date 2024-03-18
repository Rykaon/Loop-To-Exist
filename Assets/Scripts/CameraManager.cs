using Cinemachine;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] protected GameManager gameManager;
    [SerializeField] public CinemachineFreeLook worldCamera;
    [SerializeField] public CinemachineFreeLook aimCamera;
    [HideInInspector] protected Cinemachine3rdPersonFollow thirdPersonFollow;
    
    [SerializeField] protected RectTransform aimCursor;
    [SerializeField] protected Transform followTransitionTarget;
    [SerializeField] protected Transform lookTransitionTarget;

    [Header("General References")]
    [SerializeField] protected float cameraTransitionDuration;
    [SerializeField] protected float aimTransitionDuration;
    [HideInInspector] public bool isCameraSet = true;
    [HideInInspector] public Transform previousTarget;
    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public Coroutine cameraTransition = null;

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
            worldCamera.Priority = 0;
            aimCamera.Priority = 100;
            StartCoroutine(ShowHideCursor(aimTransitionDuration, true));
        }
        else if (!value)
        {
            worldCamera.Priority = 100;
            aimCamera.Priority = 0;
            StartCoroutine(ShowHideCursor(0f, false));
        }

        StartCoroutine(SetAimTarget(targetPos));
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
