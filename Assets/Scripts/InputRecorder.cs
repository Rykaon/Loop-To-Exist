using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.Profiling;
using static UnityEditor.FilePathAttribute;
using Cinemachine;

public class InputRecorder
{
    public PlayerManager player;// {  get; private set; }
    public List<float> timeList { get; private set; }
    public List<InputLog<Vector2>> moveLogs { get; private set; }
    public List<InputLog<Vector3>> cameraPosLogs { get; private set; }
    public List<InputLog<Vector3>> cameraRotLogs { get; private set; }
    public List<InputLog<float>> jumpLogs { get; private set; }
    public List<InputLog<float>> catchLogs { get; private set; }
    public List<InputLog<float>> throwLogs { get; private set; }
    public List<InputLog<float>> shotLogs { get; private set; }
    public List<List<InputLog<float>>> floatLogs { get; private set; }

    public InputRecorder(PlayerManager playerRecorder)
    {
        player = playerRecorder;
        timeList = new List<float>();
        moveLogs = new List<InputLog<Vector2>>();
        cameraPosLogs = new List<InputLog<Vector3>>();
        cameraRotLogs   = new List<InputLog<Vector3>>();
        jumpLogs = new List<InputLog<float>>();
        catchLogs = new List<InputLog<float>>();
        throwLogs = new List<InputLog<float>>();
        shotLogs = new List<InputLog<float>>();

        floatLogs = new List<List<InputLog<float>>>();
        floatLogs.Add(jumpLogs);
        floatLogs.Add(catchLogs);
        floatLogs.Add(throwLogs);
        floatLogs.Add(shotLogs);
    }

    public void Clean()
    {
        moveLogs.Clear();
        cameraPosLogs.Clear();
        cameraRotLogs.Clear();
        jumpLogs.Clear();
        catchLogs.Clear();
        throwLogs.Clear();
        shotLogs.Clear();
    }

    public void AddFloatLogs(InputLog<float> log, List<InputLog<float>> logs)
    {
        logs.Add(log);
        timeList.Add(log.time);
    }

    public void AddVectorLogs(InputLog<Vector2> log, List<InputLog<Vector2>> logs)
    {
        logs.Add(log);
        timeList.Add(log.time);
    }

    public void AddCameraLogs(InputLog<Vector3> log, List<InputLog<Vector3>> logs)
    {
        logs.Add(log);
    }

    public InputLog<float> GetFloatInputLogs(float time, List<InputLog<float>> logs)
    {
        for (int i = 0; i < logs.Count; ++i)
        {
            if ((logs[i].time - time) is >= -0.001f and <= 0.001f)
            {
                return logs[i];
            }
        }
        return null;
    }

    public InputLog<Vector2> GetVectorInputLogs(float time, List<InputLog<Vector2>> logs)
    {
        for (int i = 0; i < logs.Count; ++i)
        {
            if (logs[i].time == time)
            {
                return logs[i];
            }
        }

        return null;
    }

    public InputLog<Vector3> GetCameraInputLogs(float time, List<InputLog<Vector3>> logs)
    {
        for (int i = 0; i < logs.Count; ++i)
        {
            if (logs[i].time == time)
            {
                return logs[i];
            }
        }

        return null;
    }

    public List<InputAction> GetInputActions(float time)
    {
        List<InputAction> actions = new List<InputAction>();
        for (int i = 0; i < floatLogs.Count + 1; ++i)
        {
            if (i == floatLogs.Count)
            {
                for (int j = 0; j < moveLogs.Count; ++j)
                {
                    if (moveLogs[j].time == time)
                    {
                        actions.Add(moveLogs[j].action);
                    }
                }
            }
            else
            {
                for (int j = 0; j < floatLogs[i].Count; ++j)
                {
                    if (floatLogs[i][j].time == time)
                    {
                        actions.Add(floatLogs[i][j].action);
                    }
                }
            }
        }

        if(actions.Count > 0)
        {
            return actions;
        }
        else
        {
            return null;
        }
    }

    public bool CheckLog(float time, List<InputLog<Vector2>> vectorLogs, List<InputLog<float>> floatLogs, List<InputLog<Vector3>> cameraLogs)
    {
        if (floatLogs == null && vectorLogs != null && cameraLogs == null)
        {
            for (int i = 0; i < vectorLogs.Count; ++i)
            {
                if (vectorLogs[i].time == time)
                {
                    return true;
                }
            }

            return false;
        }
        else if (floatLogs != null && vectorLogs == null && cameraLogs == null)
        {
            for (int i = 0; i < floatLogs.Count; ++i)
            {
                if (floatLogs[i].time == time)
                {
                    return true;
                }
            }

            return false;
        }
        else if (floatLogs == null && vectorLogs == null && cameraLogs != null)
        {
            for (int i = 0; i < cameraLogs.Count; ++i)
            {
                if (cameraLogs[i].time == time)
                {
                    return true;
                }
            }

            return false;
        }
        else
        {
            Debug.Log("ERROR : Function Parameters hasn't been correctly setup");
            return false;
        }
    }

    public void RecordInput(InputAction action)
    {
        if (action == player.moveAction)
        {
            InputLog<Vector2> log = new InputLog<Vector2>(action.ReadValue<Vector2>(), player.gameManager.elapsedTime, action);
            AddVectorLogs(log, moveLogs);
        }
        else if (action == player.jumpAction)
        {
            InputLog<float> log = new InputLog<float>(action.ReadValue<float>(), player.gameManager.elapsedTime, action);
            AddFloatLogs(log, jumpLogs);
        }
        else if (action == player.grabAction)
        {
            InputLog<float> log = new InputLog<float>(action.ReadValue<float>(), player.gameManager.elapsedTime, action);
            AddFloatLogs(log, catchLogs);
        }
        else if (action == player.throwAction)
        {
            InputLog<float> log = new InputLog<float>(action.ReadValue<float>(), player.gameManager.elapsedTime, action);
            AddFloatLogs(log, throwLogs);
        }
        else if (action == player.shotAction)
        {
            InputLog<float> log = new InputLog<float>(action.ReadValue<float>(), player.gameManager.elapsedTime, action);
            AddFloatLogs(log, shotLogs);
        }
        else if (action == null)
        {
            InputLog<Vector3> log = new InputLog<Vector3>(player.playerCamera.position, player.gameManager.elapsedTime, action);
            AddCameraLogs(log, cameraPosLogs);
            log = new InputLog<Vector3>(player.playerCamera.rotation.eulerAngles, player.gameManager.elapsedTime, action);
            AddCameraLogs(log, cameraRotLogs);
        }
    }

    public void ExecuteVectorLog(InputLog<Vector2> log)
    {
        if (!log.hasBeenExecuted)
        {
            if (log.action == player.moveAction)
            {
                player.Move(log.value);
            }

            log.SetExexcuted(true);
        }
    }

    public void ExecuteFloatLog(InputLog<float> log)
    {
        if (!log.hasBeenExecuted)
        {
            if (log.action == player.jumpAction)
            {
                player.Jump();
            }
            else if (log.action == player.grabAction)
            {
                player.Grab();
            }
            else if (log.action == player.throwAction)
            {
                player.Throw();
            }
            else if (log.action == player.shotAction)
            {
                player.Shot();
            }

            log.SetExexcuted(true);
        }
    }

    public void ExecuteCameraLog(InputLog<Vector3> log, bool isQuaternion)
    {
        if (!log.hasBeenExecuted)
        {
            if (isQuaternion)
            {
                player.playerCamera.rotation = Quaternion.Euler(log.value);
            }
            else
            {
                player.playerCamera.position = log.value;
            }

            log.SetExexcuted(true);
        }
    }

    public void ResetExecution()
    {
        for (int i = 0; i < floatLogs.Count; ++i)
        {
            for (int j = 0; j < floatLogs[i].Count; ++j)
            {
                floatLogs[i][j].SetExexcuted(false);
            }
        }

        for (int i = 0; i < moveLogs.Count; ++i)
        {
            moveLogs[i].SetExexcuted(false);
        }

        for (int i = 0; i < cameraPosLogs.Count; ++i)
        {
            cameraPosLogs[i].SetExexcuted(false);
            cameraRotLogs[i].SetExexcuted(false);
        }
    }
}
