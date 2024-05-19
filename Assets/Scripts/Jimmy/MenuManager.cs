using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("Cinemachine Properties")]
    [SerializeField] private Volume GlobalVolume;
    [HideInInspector] public Camera _camera;

    public PlayerControls playerControls { get; private set; }

    [Header("Input References")]
    [SerializeField] private PlayerInput playerInput;
    public string previousControlScheme = "";
    private const string gamepadScheme = "Gamepad";
    private const string mouseScheme = "Keyboard&Mouse";
    public string gamepad { get { return gamepadScheme; } private set { } }
    public string mouse { get { return mouseScheme; } private set { } }

    [Header("UI References")]
    [SerializeField] private UIManager uiManager;

    [Header("Entities References")]
    [SerializeField] private List<StateManager> entities;

    public Volume globalVolume { get; private set; }
    public UIManager UIManager { get; private set; }
    public List<PlayerManager> playerList { get; private set; }
    public List<MushroomManager> mushroomList { get; private set; }
    public List<ObjectManager> objectList { get; private set; }
    public List<CreatureManager> creatureList { get; private set; }

    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftShoulderisPressed = false;

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
        playerControls = new PlayerControls();
        playerControls.Player.Disable();
        playerControls.UI.Enable();

        playerList = entities.OfType<PlayerManager>().ToList();
        mushroomList = entities.OfType<MushroomManager>().ToList();
        objectList = entities.OfType<ObjectManager>().ToList();
        creatureList = entities.OfType<CreatureManager>().ToList();

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].Initialize(null);
        }

        for (int i = 0; i < mushroomList.Count; i++)
        {
            mushroomList[i].Initialize(null);
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].Initialize(null);
        }

        for (int i = 0; i < creatureList.Count; i++)
        {
            creatureList[i].Initialize(null);
        }

        AudioManager.instance.Play("Music_Main_Menu");
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
            }
            else if (playerInput.currentControlScheme == gamepadScheme && previousControlScheme != gamepadScheme)
            {
                previousControlScheme = gamepadScheme;
            }
        }

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

    private void FixedUpdate()
    {
        ResetInputState();

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
    }
}
