using Cinemachine;
using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using static Cinemachine.CinemachineFreeLook;
using static UnityEngine.Rendering.VolumeComponent;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum ControlState
    {
        UI,
        World
    }

    public ControlState controlState { get; private set; }

    [Header("Cinemachine Properties")]
    [SerializeField] private Volume GlobalVolume;
    [SerializeField] public CameraManager cameraManager;
    [HideInInspector] public Camera _camera;

    public PlayerControls playerControls { get; private set; }
    public PlayerManager mainPlayer { get; set; }

    [Header ("Input References")]
    [SerializeField] private PlayerInput playerInput;
    public string previousControlScheme = "";
    private const string gamepadScheme = "Gamepad";
    private const string mouseScheme = "Keyboard&Mouse";
    public string gamepad { get { return gamepadScheme; } private set { } }
    public string mouse { get { return mouseScheme; } private set { } }

    [Header("UI References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private RadialMenu PlayerMenu;

    [Header("Entities References")]
    [SerializeField] private List<StateManager> entities;

    [Header("Progression Status")]
    public bool isInCinematic = false;
    public bool showCinematics = true;
    public bool hasFinishedTutorial = false;
   
    public bool hasSetFirstOrb = false;
    public bool hasSetSecondOrb = false;
    public bool hasSetLastOrb = false;

    public int nbrOfDeath = 0;
    public bool hasSeenAllDeath = false;

    public int talkedToEntity = 0;
    public bool hasGotToTop = false;
    public bool hasGotToBottom = false;

    public Volume globalVolume { get; private set; }
    public UIManager UIManager { get; private set; }
    public RadialMenu playerMenu { get; private set; }
    public List<PlayerManager> playerList { get; private set; }
    public List<MushroomManager> mushroomList { get; private set; }
    public List<ObjectManager> objectList { get; private set; }
    public List<CreatureManager> creatureList { get; private set; }

    private int playerIndex = 0;
    private int playerMaxIndex;
    private InputActionReference action;
    private CinemachineInputProvider inputProvider;

    private bool firstRun = true;

    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftShoulderisPressed = false;
    [HideInInspector] public Coroutine loop;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        globalVolume = GlobalVolume;
        UIManager = uiManager;
        UIManager.gameManager = this;
        controlState = ControlState.UI;
        playerControls = new PlayerControls();
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        playerList = entities.OfType<PlayerManager>().ToList();
        mushroomList = entities.OfType<MushroomManager>().ToList();
        objectList = entities.OfType<ObjectManager>().ToList();
        creatureList = entities.OfType<CreatureManager>().ToList();

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].Initialize(this);
        }

        for (int i = 0; i < mushroomList.Count; i++)
        {
            mushroomList[i].Initialize(this);
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].Initialize(this);
        }

        for (int i = 0; i < creatureList.Count; i++)
        {
            creatureList[i].Initialize(this);
        }

        mainPlayer = playerList[0];

        playerMenu = PlayerMenu;
        playerMaxIndex = playerList.Count - 1;
        SetMainPlayer(mainPlayer, true, false);

        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        yield return new WaitForSecondsRealtime(0.25f);

        if (showCinematics)
        {
            cameraManager.ExecuteCinematic(cameraManager.intro);
        }
        else
        {
            cameraManager.BlackScreen(0.5f, 0f);
        }
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnControlsChanged;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnControlsChanged;
    }

    private void OnControlsChanged(object input, InputActionChange inputActionChange)
    {
        if (inputActionChange == InputActionChange.BoundControlsChanged)
        {
            if (playerInput.currentControlScheme == mouseScheme && previousControlScheme != mouseScheme)
            {
                previousControlScheme = mouseScheme;
                playerMenu.useGamepad = false;
            }
            else if (playerInput.currentControlScheme == gamepadScheme && previousControlScheme != gamepadScheme)
            {
                previousControlScheme = gamepadScheme;
                playerMenu.useGamepad = true;
            }
        }

    }

    public void ChangeState(ControlState state)
    {
        if (state == ControlState.UI)
        {
            playerControls.Player.Disable();
            playerControls.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
        }
        else if (state == ControlState.World)
        {
            playerControls.UI.Disable();
            playerControls.Player.Enable();
            Cursor.lockState = CursorLockMode.Locked;
        }

        controlState = state;
    }

    public void SetCameraTarget(Transform follow, Transform look, bool isWarp)
    {
        if (cameraManager.cameraTransition != null)
        {
            StopCoroutine(cameraManager.cameraTransition);
        }

        cameraManager.cameraTransition = StartCoroutine(cameraManager.SetCameraTarget(follow, look, isWarp));
    }

    public void SetMainPlayer(PlayerManager player, bool startRun, bool isWarp)
    {
        int previous = Utilities.FindIndexInList(mainPlayer, playerList);
        int next = Utilities.FindIndexInList(player, playerList);

        if (previous != -1)
        {
            playerList[previous].SetIsMainPlayer(false);
            playerMenu.elements[previous].SetColors();
        }
        
        mainPlayer = player;
        playerList[next].SetIsMainPlayer(true);
        playerMenu.elements[next].SetColors();

        if (startRun)
        {
            ChangeState(ControlState.World);
            StartRun(isWarp);
        }
        else
        {
            SetCameraTarget(mainPlayer.cameraTarget, mainPlayer.cameraTarget, isWarp);
        }
    }

    public void StartRun(bool isWarp)
    {
        SetCameraTarget(mainPlayer.cameraTarget, mainPlayer.cameraTarget, isWarp);
        
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i] != mainPlayer)
            {
                playerList[i].isActive = false;
            }
        }

        mainPlayer.isActive = true;
    }

    private void ResetInputState()
    {
        if (buttonNorthIsPressed && !playerControls.Player.Y.IsPressed())
        {
            buttonNorthIsPressed = false;
        }

        if (leftShoulderisPressed && !playerControls.Player.LB.IsPressed())
        {
            leftShoulderisPressed = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene(1);
        }


        ResetInputState();

        if (controlState == ControlState.World)
        {
            if (playerControls.Player.Select.WasPressedThisFrame())
            {
                if (UIManager.toggleMenuPause == null)
                {
                    ChangeState(ControlState.UI);
                    UIManager.SetGameMenuActive(true);
                }
            }

            if (playerControls.Player.LB.IsPressed() && !leftShoulderisPressed)
            {
                leftShoulderisPressed = true;
                playerMenu.gameObject.SetActive(true);
                cameraManager.SetCameraAim(false, Vector3.zero);
                return;
            }
        }
        else if (controlState == ControlState.UI)
        {
            Vector2 leftStick = playerControls.UI.LeftStick.ReadValue<Vector2>();
            float x = Mathf.Abs(leftStick.x);
            float y = Mathf.Abs(leftStick.y);

            if (playerControls.UI.A.IsPressed())
            {
                UIManager.ExecuteMenuButton();
            }

            if (leftStick != Vector2.zero && y > x)
            {
                if (leftStick.y > 0)
                {
                    UIManager.NavigateMenu(-1);
                }
                else
                {
                    UIManager.NavigateMenu(1);
                }
            }

            if (playerMenu.isActive)
            {
                bool joystickMoved = playerControls.UI.LeftStick.ReadValue<Vector2>() != Vector2.zero;
                Debug.Log(joystickMoved);
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
                                SetCameraTarget(playerMenu.elements[playerMenu.index].player.cameraTarget, playerMenu.elements[playerMenu.index].player.cameraTarget, false);
                            }

                            if (playerControls.UI.X.IsPressed())
                            {
                                SetMainPlayer(playerMenu.elements[playerMenu.index].player, false, false);
                            }

                            if (playerControls.UI.Y.IsPressed())
                            {
                                playerMenu.gameObject.SetActive(false);
                                StartRun(false);
                            }

                            if (playerControls.UI.A.IsPressed())
                            {
                                playerMenu.gameObject.SetActive(false);
                                SetMainPlayer(playerMenu.elements[playerMenu.index].player, true, false);
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
}
