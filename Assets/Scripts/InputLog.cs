using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputLog<TLog>
{
    public TLog value {  get; private set; }
    public float time { get; private set; }
    public float deltaTime { get; private set; }
    public InputAction action { get; private set; }

    public bool hasBeenExecuted { get; private set; }

    public InputLog(TLog value, float time, InputAction action)
    {
        this.value = value; this.time = time; deltaTime = Time.fixedDeltaTime; this.action = action; hasBeenExecuted = false;
    }

    public void SetExexcuted(bool hasBeenExecuted)
    {
        this.hasBeenExecuted = hasBeenExecuted;
    }
}
