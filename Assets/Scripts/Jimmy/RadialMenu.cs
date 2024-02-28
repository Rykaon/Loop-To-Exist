using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class RadialMenu : MonoBehaviour
{
    [SerializeField] public GameManager gameManager;
    [SerializeField] public bool isActive = false;

    [Tooltip("Adjusts the radial menu for use with a gamepad or joystick. You might need to edit this script if you're not using the default horizontal and vertical input axes.")]
    public bool useGamepad = false;
    [Tooltip("With lazy selection, you only have to point your mouse (or joystick) in the direction of an element to select it, rather than be moused over the element entirely.")]
    public bool useLazySelection = true;
    [Tooltip("If set to true, a pointer with a graphic of your choosing will aim in the direction of your mouse. You will need to specify the container for the selection follower.")]
    public bool useSelectionFollower = true;
    [Tooltip("If using the selection follower, this must point to the rect transform of the selection follower's container.")]
    public RectTransform selectionFollowerContainer;
    [Tooltip("This is the text object that will display the labels of the radial elements when they are being hovered over. If you don't want a label, leave this blank.")]
    public Text textLabel;
    [Tooltip("This is the list of radial menu elements. This is order-dependent. The first element in the list will be the first element created, and so on.")]
    public List<RadialMenuElement> elements = new List<RadialMenuElement>();
    [Tooltip("Controls the total angle offset for all elements. For example, if set to 45, all elements will be shifted +45 degrees. Good values are generally 45, 90, or 180")]
    public float globalOffset = 0f;

    [HideInInspector]
    public RectTransform rt;
    [HideInInspector]
    public float currentAngle = 0f; //Our current angle from the center of the radial menu.
    [HideInInspector]
    public int index = 0; //The current index of the element we're pointing at.
    [HideInInspector]
    public int elementCount;
    [HideInInspector]
    public float angleOffset; //The base offset. For example, if there are 4 elements, then our offset is 360/4 = 90
    [HideInInspector]
    public int previousActiveIndex = 0; //Used to determine which buttons to unhighlight in lazy selection.
    [HideInInspector]
    public PointerEventData pointer;

    private void Awake()
    {
        pointer = new PointerEventData(EventSystem.current);

        rt = GetComponent<RectTransform>();

        if (rt == null)
        {
            Debug.LogError("Radial Menu: Rect Transform for radial menu " + gameObject.name + " could not be found. Please ensure this is an object parented to a canvas.");
        }

        if (useSelectionFollower && selectionFollowerContainer == null)
        {
            Debug.LogError("Radial Menu: Selection follower container is unassigned on " + gameObject.name + ", which has the selection follower enabled.");
        }

        elementCount = elements.Count;

        angleOffset = (360f / (float)elementCount);

        //Loop through and set up the elements.
        for (int i = 0; i < elementCount; i++)
        {
            if (elements[i] == null)
            {
                Debug.LogError("Radial Menu: element " + i.ToString() + " in the radial menu " + gameObject.name + " is null!");
                continue;
            }
            elements[i].parentRM = this;

            elements[i].SetAllAngles((angleOffset * i) + globalOffset, angleOffset);

            elements[i].assignedIndex = i;
        }

        if (useGamepad)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, null); //We'll make this the active object when we start it. Comment this line to set it manually from another script.
            if (useSelectionFollower && selectionFollowerContainer != null)
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    if (elements[i].active)
                    {
                        selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -globalOffset * i); //Point the selection follower at the first active element.
                        break;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        EnableDisable(true);
    }

    private void OnDisable()
    {
        EnableDisable(false);
    }

    public void EnableDisable(bool enabled)
    {
        if (enabled && gameManager.controlState == GameManager.ControlState.World)
        {
            gameManager.ChangeState(GameManager.ControlState.Menu);
        }
        else if (!enabled && gameManager.controlState == GameManager.ControlState.Menu)
        {
            gameManager.ChangeState(GameManager.ControlState.World);
        }
    }

    public float CalculateRawAngles()
    {
        if (!useGamepad)
        {
            return Mathf.Atan2(Input.mousePosition.y - rt.position.y, Input.mousePosition.x - rt.position.x) * Mathf.Rad2Deg;
        }
        else
        {
            return Mathf.Atan2(gameManager.playerControls.UI.LeftStick.ReadValue<Vector2>().y, gameManager.playerControls.UI.LeftStick.ReadValue<Vector2>().x) * Mathf.Rad2Deg;
        }
    }

    void Update()
    {
        if (isActive)
        {
            

            
        } 
    }


    //Selects the button with the specified index.
    public void SelectButton(int i)
    {
        if (elements[i].selected == false)
        {
            
        }

        elements[i].HighlightThisElement(); //Select this one

        if (previousActiveIndex != i)
        {
            elements[previousActiveIndex].UnHighlightThisElement(); //Deselect the last one.
        }

        previousActiveIndex = i;
    }

    //Keeps angles between 0 and 360.
    public float NormalizeAngle(float angle)
    {
        angle = angle % 360f;

        if (angle < 0)
            angle += 360;

        return angle;
    }
}
