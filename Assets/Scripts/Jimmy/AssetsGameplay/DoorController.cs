using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum State
    {
        Open,
        Close
    }

    public State state;
    [SerializeField] private List<DoorSwitch> switchs;
    [SerializeField] private Transform gate;
    [SerializeField] private Rigidbody gateBody;
    [SerializeField] private MeshRenderer motifRenderer;
    [SerializeField] private AnimationCurve openCurve;
    private Vector3 openPosition;
    private Coroutine coroutine;
    private Tween open;
    private bool isFirst = true;
    
    [SerializeField] private AudioSource[] audioSource;

    private void Awake()
    {
        state = State.Close;
        openPosition = gate.position;
        coroutine = StartCoroutine(OpenClose());
    }

    public void CheckSwitches()
    {
        bool allSwitchesActive = true;

        for (int i = 0; i < switchs.Count; i++)
        {
            if (switchs[i].state == DoorSwitch.State.Inactive)
            {
                allSwitchesActive = false;
                break;
            }
        }

        if (state == State.Open && !allSwitchesActive)
        {
            state = State.Close;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                if (open != null)
                {
                    open.Kill();
                }
            }
            coroutine = StartCoroutine(OpenClose());
        }
        else if (state == State.Close && allSwitchesActive)
        {
            state = State.Open;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                if (open != null)
                {
                    open.Kill();
                }
            }
            coroutine = StartCoroutine(OpenClose());
        }
    }

    private IEnumerator OpenClose()
    {
        float duration = 1.5f;

        if (state == State.Open)
        {
            if (!isFirst)
            {
                audioSource[2].Play();
            }

            gateBody.useGravity = false;
            open = gate.DOMove(openPosition, duration).SetEase(openCurve);
            yield return new WaitForSecondsRealtime(duration);
            gateBody.isKinematic = true;

            yield return new WaitForSecondsRealtime(duration);
            gateBody.isKinematic = true;
        }
        else
        {
            if (!isFirst)
            {
                audioSource[1].Play();
            }
            
            gateBody.isKinematic = false;
            gateBody.useGravity = true;
            gateBody.AddForce(Vector3.down * 10, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "TriggerSound")
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else if (!isFirst)
            {
                audioSource[0].Play();
            }
        }
    }
}
