using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EntityCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform camPoint;
    [SerializeField] private Outline outline;
    [SerializeField] private Renderer entityRenderer;
    [HideInInspector] protected Color currentEmissionColor;
    public List<PlayerManager> players = new List<PlayerManager>();
    public bool isCamActive = false;
    private Coroutine camRoutine = null;

    private void Start()
    {
        currentEmissionColor = entityRenderer.material.GetColor("_EmissionColor");
        StartCoroutine(EntityGlow(1));
    }

    private IEnumerator EntityGlow(float start)
    {
        float duration = 0.45f;
        float startIntensity = start;
        float endIntensity = 1.5f;

        entityRenderer.material.SetColor("_EmissionColor", currentEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentEmissionColor * x;
            entityRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(entityRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        startIntensity = endIntensity;
        endIntensity = 0.5f;

        entityRenderer.material.SetColor("_EmissionColor", currentEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentEmissionColor * x;
            entityRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(entityRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        StartCoroutine(EntityGlow(endIntensity));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
        {
            if (!players.Contains(playerManager))
            {
                players.Add(playerManager);
                playerManager.isLadderTrigger = true;
                playerManager.isLookingEntity = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
        {
            if (players.Contains(playerManager))
            {
                players.Remove(playerManager);
                playerManager.isLadderTrigger = false;
                playerManager.isLookingEntity = false;
            }
        }
    }

    private IEnumerator SetEntityCamera(bool value)
    {
        float duration = 2f;
        AnimationCurve curve = Utilities.ConvertEaseToCurve(Ease.InOutSine);

        outline.enabled = value;

        if (value)
        {
            if (GameManager.instance.mainPlayer.isAiming)
            {
                GameManager.instance.mainPlayer.Aim(false);
            }

            cameraTarget.position = camPoint.position;
            cameraTarget.rotation = camPoint.rotation;

            GameManager.instance.cameraManager.ChangeCamera(GameManager.instance.cameraManager.cinematicCamera, 2f);
        }
        else
        {
            GameManager.instance.cameraManager.ChangeCamera(GameManager.instance.cameraManager.worldCamera, 2f);
        }

        yield return new WaitForSeconds(duration);
        GameManager.instance.cameraManager.brain.m_DefaultBlend.m_Time = duration;
    }

    private void Update()
    {
        if (!isCamActive)
        {
            if (players.Count > 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].isMainPlayer && players[i].isActive)
                    {
                        isCamActive = true;

                        if (camRoutine != null)
                        {
                            StopCoroutine(camRoutine);
                            camRoutine = null;
                        }
                        camRoutine = StartCoroutine(SetEntityCamera(true));
                        break;
                    }
                }
            }
        }
        else
        {
            bool isPlayer = false;

            if (players.Count > 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].isMainPlayer && players[i].isActive)
                    {
                        isPlayer = true;
                        break;
                    }
                }
            }

            if (!isPlayer)
            {
                isCamActive = false;

                if (camRoutine != null)
                {
                    StopCoroutine(camRoutine);
                    camRoutine = null;
                }

                camRoutine = StartCoroutine(SetEntityCamera(false));
            }
        }
    }
}
