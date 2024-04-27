using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TutorialBark : MonoBehaviour
{
    private Camera mainCamera;
    public Transform textMeshTransform;

    private bool isActivate;

    void Start()
    {
        isActivate = false;

        textMeshTransform.localScale = new Vector3(-0.1f, 0f, 1f);
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera != null &&isActivate)
        {
            // Faites en sorte que le GameObject regarde vers la caméra en utilisant la méthode LookAt
            textMeshTransform.LookAt(mainCamera.transform.position, Vector3.up);

            /*// Gardez l'enfant TextMeshPro aligné avec le plan de la caméra
            textMeshTransform.forward = -mainCamera.transform.forward;*/
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<PlayerManager>(out PlayerManager player))
        {
            if (player.isMainPlayer)
            {
                isActivate = true;

                textMeshTransform.DOScale(new Vector3(-0.6f, 0.6f, 0.6f), 0.5f).SetEase(Ease.InOutSine, 0.9f);
                textMeshTransform.DOLocalMoveY(6.48f, 0.5f).SetEase(Ease.InOutSine, 0.3f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager player))
        {
            if (player.isMainPlayer)
            {
                isActivate = false;

                textMeshTransform.DOScale(new Vector3(-0.1f, 0f, 1f), 0.5f).SetEase(Ease.InOutSine, 0.9f);
                textMeshTransform.DOLocalMoveY(4.27f, 0.5f).SetEase(Ease.InOutSine, 0.3f);
            }
        }
    }
}
