using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class DoorSwitch : MonoBehaviour
{
    public enum State
    {
        Active,
        Inactive
    }
    public State state;
    
    [SerializeField] protected List<string> tagsToCheck;
    [SerializeField] protected DoorController doorController;
    [SerializeField] protected int nbrOfEntity;
    [SerializeField] protected Transform pressurePlate;
    [SerializeField] protected MeshRenderer pressurePlateRenderer;
    [SerializeField] protected Color colorActive;
    [SerializeField] protected Color colorInactive;
    private float intensity = 3f;
    private Coroutine effectRoutine = null;
    protected List<GameObject> objects = new List<GameObject>();
    [SerializeField] private bool isOrbe;

    protected virtual void Awake()
    {
        if (!isOrbe)
        {
            state = State.Inactive;

            tagsToCheck = new List<string>();
            tagsToCheck.Add("Player");
            tagsToCheck.Add("Mushroom");
            tagsToCheck.Add("Object");
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isOrbe)
        {
            if (other.TryGetComponent<StateManager>(out StateManager manager))
            {
                if (!manager.isHeld && !manager.isEquipped)
                {
                    List<GameObject> list = new List<GameObject>();
                    list.Add(other.gameObject);
                    if (manager.GetType() == typeof(PlayerManager))
                    {
                        PlayerManager player = (PlayerManager)manager;

                        foreach (GameObject go in player.stickedObjects)
                        {
                            list.Add(go);
                        }

                        if (player.heldObject != null)
                        {
                            list.Add(player.heldObject.gameObject);
                            foreach (GameObject go in player.heldObject.stickedObjects)
                            {
                                list.Add(go);
                            }
                        }

                        if (player.equippedObject != null)
                        {
                            list.Add(player.equippedObject.gameObject);
                            foreach (GameObject go in player.equippedObject.stickedObjects)
                            {
                                list.Add(go);
                            }
                        }
                    }
                    else
                    {
                        foreach (GameObject go in manager.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (tagsToCheck.Contains(list[i].tag) && !objects.Contains(list[i]))
                        {
                            objects.Add(list[i]);
                        }
                    }

                    if (objects.Count >= nbrOfEntity)
                    {
                        Debug.Log(transform.parent.name + " is Active");
                        state = State.Active;

                        if (effectRoutine != null)
                        {
                            StopCoroutine(effectRoutine);
                            effectRoutine = null;
                        }

                        effectRoutine = StartCoroutine(SetActive(true));

                        if (doorController.state == DoorController.State.Close)
                        {
                            doorController.CheckSwitches();
                        }
                    }
                }
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (!isOrbe)
        {
            if (other.TryGetComponent<StateManager>(out StateManager manager))
            {
                if (!manager.isHeld && !manager.isEquipped)
                {
                    List<GameObject> list = new List<GameObject>();
                    list.Add(other.gameObject);
                    if (manager.GetType() == typeof(PlayerManager))
                    {
                        PlayerManager player = (PlayerManager)manager;

                        foreach (GameObject go in player.stickedObjects)
                        {
                            list.Add(go);
                        }

                        if (player.heldObject != null)
                        {
                            list.Add(player.heldObject.gameObject);
                            foreach (GameObject go in player.heldObject.stickedObjects)
                            {
                                list.Add(go);
                            }
                        }

                        if (player.equippedObject != null)
                        {
                            list.Add(player.equippedObject.gameObject);
                            foreach (GameObject go in player.equippedObject.stickedObjects)
                            {
                                list.Add(go);
                            }
                        }
                    }
                    else
                    {
                        foreach (GameObject go in manager.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (tagsToCheck.Contains(list[i].tag) && objects.Contains(list[i]))
                        {
                            objects.Remove(list[i]);
                        }
                    }

                    if (objects.Count < nbrOfEntity)
                    {
                        Debug.Log(transform.parent.name + " is Inactive");
                        state = State.Inactive;

                        if (effectRoutine != null)
                        {
                            StopCoroutine(effectRoutine);
                            effectRoutine = null;
                        }

                        effectRoutine = StartCoroutine(SetActive(false));

                        if (doorController.state == DoorController.State.Open)
                        {
                            doorController.CheckSwitches();
                        }
                    }
                }
            }
        }
    }

    private IEnumerator SetActive(bool value)
    {
        float elapsedTime = 0f;
        float duration = 0.5f;
        Color colorToSet;
        AnimationCurve curve;

        Color startColor = pressurePlateRenderer.material.GetColor("_EmissionColor");

        if (value)
        {
            colorToSet = colorActive;
            curve = Utilities.ConvertEaseToCurve(Ease.OutSine);
            
            pressurePlate.DOLocalMove(Vector3.zero + Vector3.down * 0.04f, duration).SetEase(Ease.OutSine);
        }
        else
        {
            colorToSet = colorInactive;
            curve = Utilities.ConvertEaseToCurve(Ease.InSine);
            
            pressurePlate.DOLocalMove(Vector3.zero, duration).SetEase(Ease.InSine);
        }

        

        while (elapsedTime < duration)
        {
            float time = elapsedTime / duration;

            pressurePlateRenderer.material.SetColor("_EmissionColor", new Color(Mathf.Lerp(startColor.r, colorToSet.r, curve.Evaluate(time)), Mathf.Lerp(startColor.g, colorToSet.g, curve.Evaluate(time)), Mathf.Lerp(startColor.b, colorToSet.b, curve.Evaluate(time)) * intensity));

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
