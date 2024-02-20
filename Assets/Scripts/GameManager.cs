using Cinemachine;
using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static Cinemachine.CinemachineFreeLook;
using static UnityEngine.Rendering.VolumeComponent;

public class GameManager : MonoBehaviour
{
    //public static GameManager instance;
    public enum ControlState
    {
        Menu,
        World
    }

    public ControlState controlState { get; private set; }

    [SerializeField] private CinemachineFreeLook cinemachine;

    public PlayerControls playerControls { get; private set; }

    [SerializeField] private PlayerController MainPlayer;
    public PlayerController mainPlayer { get; private set; }

    [SerializeField] private List<PlayerController> PlayerList;
    public List<PlayerController> playerList { get; private set; }

    [SerializeField] private RadialMenu PlayerMenu;
    public RadialMenu playerMenu { get; private set; }

    [SerializeField] private List<InteractibleObject> ObjectList;
    public List<InteractibleObject> objectList { get; private set; }

    [Header("Status")]
    public bool isRecording;

    [Header("Properties")]
    public float recordingEndTime;
    private int playerIndex = 0;
    private int playerMaxIndex;
    private InputActionReference action;
    private CinemachineInputProvider inputProvider;

    private bool firstRun = true;

    public float elapsedTime { get; private set; }

    public bool buttonNorthIsPressed = false;
    public bool leftShoulderisPressed = false;

    private void Awake()
    {
        //instance = this;
        controlState = ControlState.Menu;
        playerControls = new PlayerControls();
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        mainPlayer = MainPlayer;
        playerList = PlayerList;
        playerMenu = PlayerMenu;
        objectList = ObjectList;
        playerMaxIndex = playerList.Count - 1;
        elapsedTime = 0f;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            playerList[i].Initialize();
        }

        mainPlayer.SetIsMainPlayer(true);
        SetCameraTarget(mainPlayer.transform, mainPlayer.transform);
    }

    public void ChangeState(ControlState state)
    {
        if (state == ControlState.Menu)
        {
            playerControls.Player.Disable();
            playerControls.UI.Enable();
        }
        else if (state == ControlState.World)
        {
            playerControls.UI.Disable();
            playerControls.Player.Enable();
        }

        controlState = state;
    }

    public void SetCameraTarget(Transform follow, Transform look)
    {
        cinemachine.Follow = follow;
        cinemachine.LookAt = look;
    }

    public void SetCameraAim(bool value)
    {
        if (value)
        {
            // top = 0, middle = 1, bottom = 2
            ChangeOrbitParameters(0f, 0f, 0);
            ChangeOrbitParameters(0f, 0f, 1);
            ChangeOrbitParameters(0f, 0f, 2);
        }
        else
        {
            ChangeOrbitParameters(3f, 9f, 0);
            ChangeOrbitParameters(2f, 6f, 1);
            ChangeOrbitParameters(1f, 3f, 2);
        }
    }

    private void ChangeOrbitParameters(float newHeight, float newRadius, int index)
    {
        cinemachine.m_Orbits[index].m_Height = newHeight;
        cinemachine.m_Orbits[index].m_Radius = newRadius;
    }

    public void EraseRunRecord(PlayerController player)
    {
        int index = GetIndexOfMainPlayer(player, playerList);
        playerList[index].recorder.Clean();
        playerList[index].hasBeenRecorded = false;
        playerMenu.elements[index].SetColors();
    }

    public void SetMainPlayer(PlayerController player)
    {
        int previous = GetIndexOfMainPlayer(mainPlayer, playerList);
        int next = GetIndexOfMainPlayer(player, playerList);

        playerList[previous].SetIsMainPlayer(false);
        playerMenu.elements[previous].SetColors();
        mainPlayer = player;
        playerList[next].SetIsMainPlayer(true);
        playerMenu.elements[next].SetColors();
    }

    public void StartRun()
    {
        SetCameraTarget(mainPlayer.transform, mainPlayer.transform);
        EraseRunRecord(mainPlayer);
        
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].hasBeenRecorded && playerList[i] != mainPlayer)
            {
                playerList[i].isActive = true;
            }
        }

        mainPlayer.isActive = true;
        mainPlayer.isRecording = true;
        elapsedTime = 0f;
    }

    public void SetRunRecord()
    {
        int index = GetIndexOfMainPlayer(mainPlayer, playerList);

        playerList[index].hasBeenRecorded = true;
        playerList[index].isRecording = false;
        playerMenu.elements[index].SetColors();
        elapsedTime = 0;
    }

    public void Replay()
    {
        elapsedTime = 0;
        
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].isActive)
            {
                playerList[i].Restart();
                playerList[i].isActive = true;
            }
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].Replay();
        }
    }

    public int GetIndexOfMainPlayer(PlayerController player, List<PlayerController> list)
    {
        for (int i = 0;i < list.Count; i++)
        {
            if (list[i] == player)
            {
                return i;
            }
        }

        return -1;
    }

    private void FixedUpdate()
    {
        if (buttonNorthIsPressed && !playerControls.Player.Y.IsPressed())
        {
            buttonNorthIsPressed = false;
        }

        if (leftShoulderisPressed && !playerControls.Player.LeftShoulder.IsPressed())
        {
            leftShoulderisPressed = false;
        }

        if (controlState == ControlState.World)
        {
            elapsedTime += Time.fixedDeltaTime;

            if (playerControls.Player.Y.IsPressed() && !buttonNorthIsPressed)
            {
                buttonNorthIsPressed = true;
                SetRunRecord();
                playerMenu.gameObject.SetActive(true);
                Replay();
                return;
            }

            if (playerControls.Player.LeftShoulder.IsPressed())
            {
                leftShoulderisPressed = true;
                EraseRunRecord(mainPlayer);
                mainPlayer.isRecording = true;
                Replay();
                return;
            }
        }
        else if (controlState == ControlState.Menu)
        {
            bool joystickMoved = playerControls.UI.LeftStick.ReadValue<Vector2>() != Vector2.zero;

            float rawAngle = playerMenu.CalculateRawAngles();

            if (!playerMenu.useGamepad)
            {
                playerMenu.currentAngle = playerMenu.NormalizeAngle(-rawAngle + 90 - playerMenu.globalOffset + (playerMenu.angleOffset / 2f));
            }
            else if (joystickMoved)
            {
                playerMenu.currentAngle = playerMenu.NormalizeAngle(-rawAngle + 90 - playerMenu.globalOffset + (playerMenu.angleOffset / 2f));
            }

            if (playerMenu.angleOffset != 0 && playerMenu.useLazySelection)
            {
                playerMenu.index = (int)(playerMenu.currentAngle / playerMenu.angleOffset);

                if (playerMenu.elements[playerMenu.index] != null)
                {
                    if (playerMenu.elements[playerMenu.index].active)
                    {
                        playerMenu.SelectButton(playerMenu.index);
                        SetCameraTarget(playerMenu.elements[playerMenu.index].player.transform, playerMenu.elements[playerMenu.index].player.transform);

                        if (playerControls.UI.B.IsPressed())
                        {
                            EraseRunRecord(playerMenu.elements[playerMenu.index].player);
                        }

                        if (playerControls.UI.X.IsPressed())
                        {
                            SetMainPlayer(playerMenu.elements[playerMenu.index].player);
                        }

                        if (playerControls.UI.Y.IsPressed())
                        {
                            playerMenu.gameObject.SetActive(false);
                            StartRun();
                        }

                        if (playerControls.UI.A.IsPressed())
                        {
                            SetMainPlayer(playerMenu.elements[playerMenu.index].player);
                            playerMenu.gameObject.SetActive(false);
                            StartRun();
                        }
                    }
                    else
                    {
                        if (playerMenu.previousActiveIndex != playerMenu.index)
                        {
                            if (playerMenu.elements[playerMenu.previousActiveIndex].active)
                            {
                                playerMenu.elements[playerMenu.previousActiveIndex].UnHighlightThisElement();
                            }
                        }
                    }
                }
            }

            if (playerMenu.useSelectionFollower && playerMenu.selectionFollowerContainer != null)
            {
                if (rawAngle != 0)
                {
                    playerMenu.selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, rawAngle + 270);
                }
                else
                {
                    playerMenu.selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, 90 + 270);
                    playerMenu.elements[playerMenu.previousActiveIndex].UnHighlightThisElement();
                    playerMenu.elements[playerMenu.index].UnHighlightThisElement();
                    playerMenu.previousActiveIndex = 0;
                    playerMenu.index = 0;
                }
            }
        }
    }
}
