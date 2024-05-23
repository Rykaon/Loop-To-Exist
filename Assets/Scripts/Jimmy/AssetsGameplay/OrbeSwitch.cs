using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OrbeSwitch : DoorSwitch
{
    [HideInInspector] protected Transform orbe = null;
    [SerializeField] protected string nameToCheck;
    [SerializeField] protected Transform orbeStartPos;
    [SerializeField] protected Transform orbeEndPos;
    [SerializeField] protected GameObject vfxHolder;
    [SerializeField] protected HDRColorspace glowUp;
    [SerializeField] private HDRColorspace glowDown;
    [SerializeField] protected Animator animator;
    [SerializeField] protected TextAsset inkJSON;
    [SerializeField] protected Renderer doorRenderer;
    [HideInInspector] protected Renderer orbRenderer;
    [HideInInspector] protected Vector3 startPos;
    [HideInInspector] protected Color currentEmissionColor;
    [HideInInspector] protected Color currentDoorEmissionColor;

    protected override void Awake()
    {
        state = State.Inactive;
    }

    protected void OnTriggerStay(Collider other)
    {
        if (orbe == null)
        {
            if (other.TryGetComponent<ObjectManager>(out ObjectManager objectManager))
            {
                if (!objectManager.isHeld && !objectManager.isEquipped && objectManager.type == StateManager.Type.Orbe && objectManager.name.Contains(nameToCheck))
                {
                    orbe = objectManager.transform;
                    orbRenderer = objectManager.renderer;

                    if (GameManager.instance.mainPlayer.trigger.triggeredObjectsList.Contains(objectManager))
                    {
                        if (GameManager.instance.mainPlayer.trigger.current == objectManager)
                        {
                            GameManager.instance.mainPlayer.trigger.current = null;
                        }

                        GameManager.instance.mainPlayer.trigger.triggeredObjectsList.Remove(objectManager);
                    }

                    Destroy(objectManager.outline);
                    Destroy(objectManager.rigidBody);
                    Destroy(objectManager);
                    StartCoroutine(SetActive());
                }
            }
        }
    }

    protected IEnumerator SetActive()
    {
        Vector3 transiPos = new Vector3(orbe.transform.position.x, orbeStartPos.position.y, orbe.transform.position.z);
        Vector3[] path = new Vector3[4];
        path[0] = orbe.transform.position;
        path[1] = transiPos;
        path[2] = orbeStartPos.position;
        path[3] = orbeEndPos.position;

        orbe.DOPath(path, 1.25f).SetEase(Ease.OutSine);
        vfxHolder.SetActive(true);
        animator.Play("Open");

        yield return new WaitForSecondsRealtime(1.25f);

        if (inkJSON != null)
        {
            DialogueManager.instance.EnterDialogueMode(inkJSON, false, true);
        }

        state = State.Active;

        startPos = orbe.transform.position;
        currentEmissionColor = orbRenderer.material.GetColor("_EmissionColor");
        currentDoorEmissionColor = doorRenderer.material.GetColor("_EmissionColor");
        StartCoroutine(Bounce());
        StartCoroutine(Glow(1));
        StartCoroutine(DoorGlow(1));

        doorController.CheckSwitches();
    }

    private IEnumerator Bounce()
    {
        float duration = 0.75f;
        Vector3 up = startPos + Vector3.up * 0.2f;
        Vector3 down = startPos + Vector3.down * 0.1f;

        orbe.DOMove(up, duration).SetEase(Ease.InOutSine);
        yield return new WaitForSecondsRealtime(duration);
        orbe.DOMove(down, duration).SetEase(Ease.InOutSine);
        yield return new WaitForSecondsRealtime(duration);

        StartCoroutine(Bounce());
    }

    private IEnumerator Glow(float start)
    {
        float duration = 0.45f;
        float startIntensity = start;
        float endIntensity = 1.5f;

        orbRenderer.material.SetColor("_EmissionColor", currentEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentEmissionColor * x;
            orbRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(orbRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        startIntensity = endIntensity;
        endIntensity = 0.5f;

        orbRenderer.material.SetColor("_EmissionColor", currentEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentEmissionColor * x;
            orbRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(orbRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        StartCoroutine(Glow(endIntensity));
    }

    private IEnumerator DoorGlow(float start)
    {
        float duration = 0.45f;
        float startIntensity = start;
        float endIntensity = 3f;

        doorRenderer.material.SetColor("_EmissionColor", currentDoorEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentDoorEmissionColor * x;
            doorRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(doorRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        startIntensity = endIntensity;
        endIntensity = 0.5f;

        doorRenderer.material.SetColor("_EmissionColor", currentDoorEmissionColor * startIntensity);

        DOTween.To(() => startIntensity, x => {
            Color newEmissionColor = currentDoorEmissionColor * x;
            doorRenderer.material.SetColor("_EmissionColor", newEmissionColor);
            DynamicGI.SetEmissive(doorRenderer, newEmissionColor);
        }, endIntensity, duration);

        yield return new WaitForSecondsRealtime(duration);

        StartCoroutine(DoorGlow(endIntensity));
    }
}
