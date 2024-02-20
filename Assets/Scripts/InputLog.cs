using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputLog<TLog>
{
    public TLog value {  get; private set; }
    public float time { get; private set; }
    public InputAction action { get; private set; }

    public bool hasBeenExecuted { get; private set; }

    public InputLog(TLog value, float time, InputAction action)
    {
        this.value = value; this.time = time; this.action = action; hasBeenExecuted = false;
    }

    public void SetValue(TLog value)
    {
        this.value = value;
    }

    public void SetTime(float time)
    {
        this.time = time;
    }

    public void SetAction(InputAction action)
    {
        this.action = action;
    }

    public void SetExexcuted(bool hasBeenExecuted)
    {
        this.hasBeenExecuted = hasBeenExecuted;
    }
}
