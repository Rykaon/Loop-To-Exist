using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.Windows;
using static Cinemachine.AxisState;

public class CustomInputProvider : MonoBehaviour, IInputAxisProvider
{
    [SerializeField] GameManager gameManager;
    [SerializeField] float rotationSpeed;

    public float GetAxisValue(int axis)
    {
        if (axis == 0)
        {
            return gameManager.playerControls.Player.RightStick.ReadValue<Vector2>().x * rotationSpeed;
        }
        else if (axis == 1)
        {
            return gameManager.playerControls.Player.RightStick.ReadValue<Vector2>().y * rotationSpeed;
        }
        else
        {
            return 0;
        }
    }
}
