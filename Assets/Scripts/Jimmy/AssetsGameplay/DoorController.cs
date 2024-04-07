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
    [SerializeField] private Animator animator;

    private void Awake()
    {
        state = State.Close;
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
            OpenClose();
        }
        else if (state == State.Close && allSwitchesActive)
        {
            state = State.Open;
            OpenClose();
        }
    }

    private void OpenClose()
    {
        if (state == State.Open)
        {
            animator.Play("Open");
        }
        else
        {
            animator.Play("Close");
        }
    }
}
