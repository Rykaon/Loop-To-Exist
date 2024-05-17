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
    [SerializeField] private AnimationCurve openCurve;
    private Vector3 openPosition;
    private Coroutine coroutine;
    private Tween open;

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
        if (state == State.Open)
        {
            gateBody.useGravity = false;
            audioSource[2].Play();
            open = gate.DOMove(openPosition, 1.5f).SetEase(openCurve);
            yield return new WaitForSecondsRealtime(1.5f);
            gateBody.isKinematic = true;
        }
        else
        {
            audioSource[1].Play();
            gateBody.isKinematic = false;
            gateBody.useGravity = true;
            gateBody.AddForce(Vector3.down * 10, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "TriggerSound")
        {
            audioSource[0].Play();

        }
    }
}
