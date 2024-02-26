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
using System.Linq;

public class GameManager : MonoBehaviour
{
    public enum ControlState
    {
        Menu,
        World
    }

    public ControlState controlState { get; private set; }

    [Header("Cinemachine Properties")]
    [SerializeField] public CameraManager cameraManager;
    [HideInInspector] public Camera _camera;

    public PlayerControls playerControls { get; private set; }

    public PlayerManager mainPlayer { get; private set; }

    [Header("Menu References")]
    [SerializeField] private RadialMenu PlayerMenu;

    [Header("Entities References")]
    [SerializeField] private List<StateManager> entities;

    public RadialMenu playerMenu { get; private set; }
    public List<PlayerManager> playerList { get; private set; }
    public List<ItemManager> itemList { get; private set; }
    //public List<ObjectManager> objectList { get; private set; }

    [HideInInspector] public float recordingEndTime;
    private int playerIndex = 0;
    private int playerMaxIndex;
    private InputActionReference action;
    private CinemachineInputProvider inputProvider;

    private bool firstRun = true;

    public float elapsedTime { get; private set; }

    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftShoulderisPressed = false;
    [HideInInspector] public Coroutine loop;


    private void Awake()
    {

        _camera = GetComponent<Camera>();
        controlState = ControlState.Menu;
        playerControls = new PlayerControls();
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        playerList = entities.OfType<PlayerManager>().ToList();
        itemList = entities.OfType<ItemManager>().ToList();
        //objectList = entities.OfType<ObjectManager>().ToList();

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].Initialize(this);
        }

        for (int i = 0; i < itemList.Count; i++)
        {
            itemList[i].Initialize(this);
        }

        /*for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].Initialize(this);
        }*/

        playerList[0].SetIsMainPlayer(true);
        mainPlayer = playerList[0];
        SetCameraTarget(mainPlayer.cameraTarget, mainPlayer.cameraTarget);

        playerMenu = PlayerMenu;
        playerMaxIndex = playerList.Count - 1;
        elapsedTime = 0f;
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
        if (cameraManager.cameraTransition != null)
        {
            StopCoroutine(cameraManager.cameraTransition);
        }

        cameraManager.cameraTransition = StartCoroutine(cameraManager.SetCameraTarget(follow, look));
    }

    /*public void SetCameraAim(bool value)
    {
        if (cameraManager.aimTransition != null)
        {
            StopCoroutine(cameraManager.aimTransition);
        }

        cameraManager.aimTransition = StartCoroutine(cameraManager.SetCameraAim(value));
    }*/

    public void EraseRunRecord(PlayerManager player)
    {
        int index = Utilities.FindIndexInList(player, playerList);
        playerList[index].recorder.Clean();
        playerList[index].hasBeenRecorded = false;
        playerMenu.elements[index].SetColors();
    }

    public void SetMainPlayer(PlayerManager player)
    {
        int previous = Utilities.FindIndexInList(mainPlayer, playerList);
        int next = Utilities.FindIndexInList(player, playerList);

        playerList[previous].SetIsMainPlayer(false);
        playerMenu.elements[previous].SetColors();
        mainPlayer = player;
        playerList[next].SetIsMainPlayer(true);
        playerMenu.elements[next].SetColors();
    }

    public void StartRun()
    {
        SetCameraTarget(mainPlayer.cameraTarget, mainPlayer.cameraTarget);
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
        int index = Utilities.FindIndexInList(mainPlayer, playerList);

        playerList[index].hasBeenRecorded = true;
        playerList[index].isRecording = false;
        playerMenu.elements[index].SetColors();
        elapsedTime = 0;
    }

    public void ResetLoop()
    {
        elapsedTime = 0;

        cameraManager.SetCameraAim(false);

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].Reset();
        }
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
            elapsedTime += 1f;

            if (playerControls.Player.Y.IsPressed() && !buttonNorthIsPressed)
            {
                buttonNorthIsPressed = true;
                SetRunRecord();
                playerMenu.gameObject.SetActive(true);
                ResetLoop();
                return;
            }

            if (playerControls.Player.LeftShoulder.IsPressed())
            {
                leftShoulderisPressed = true;
                EraseRunRecord(mainPlayer);
                mainPlayer.isRecording = true;
                ResetLoop();
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

                        if (cameraManager.currentTarget != playerMenu.elements[playerMenu.index].player.transform)
                        {
                            SetCameraTarget(playerMenu.elements[playerMenu.index].player.cameraTarget, playerMenu.elements[playerMenu.index].player.cameraTarget);
                        }

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
