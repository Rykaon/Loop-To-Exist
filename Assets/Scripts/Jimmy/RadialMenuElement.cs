using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Rendering;

public class RadialMenuElement : MonoBehaviour
{
    public bool active = false;
    [SerializeField]
    private Sprite defaultSprite;
    [SerializeField]
    private Sprite selectedSprite;
    [HideInInspector]
    public RectTransform rt;
    [SerializeField]
    public RadialMenu parentRM;

    [Tooltip("Each radial element needs a button. This is generally a child one level below this primary radial element game object.")]
    public Button button;

    [Tooltip("This is the text label that will appear in the center of the radial menu when this option is moused over. Best to keep it short.")]
    public string label;

    [HideInInspector]
    public float angleMin, angleMax;

    [HideInInspector]
    public float angleOffset;

    //[HideInInspector]
    public bool selected = false;

    public PlayerController player;

    [HideInInspector]
    public int assignedIndex = 0;
    // Use this for initialization

    private CanvasGroup cg;

    private Image image;

    [SerializeField] Color m_recorded;
    [SerializeField] Color d_recorded;
    [SerializeField] Color m_unrecorded;
    [SerializeField] Color d_unrecorded;

    void Awake()
    {
        rt = gameObject.GetComponent<RectTransform>();
        image = button.GetComponent<Image>();

        if (gameObject.GetComponent<CanvasGroup>() == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }
        else
        {
            cg = gameObject.GetComponent<CanvasGroup>();
        }


        if (rt == null)
        {
            Debug.LogError("Radial Menu: Rect Transform for radial element " + gameObject.name + " could not be found. Please ensure this is an object parented to a canvas.");
        }

        if (button == null)
        {
            Debug.LogError("Radial Menu: No button attached to " + gameObject.name + "!");
        }

        SetColors();
    }

    void Start ()
    {
        rt.rotation = Quaternion.Euler(0, 0, -angleOffset); //Apply rotation determined by the parent radial menu.

        //If we're using lazy selection, we don't want our normal mouse-over effects interfering, so we turn raycasts off.
        if (parentRM.useLazySelection)
        {
            cg.blocksRaycasts = false;
        }
        else
        {

            //Otherwise, we have to do some magic with events to get the label stuff working on mouse-over.

            EventTrigger t;

            if (button.GetComponent<EventTrigger>() == null)
            {
                t = button.gameObject.AddComponent<EventTrigger>();
                t.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            }
            else
            {
                t = button.GetComponent<EventTrigger>();
            }

            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((eventData) => { SetParentMenuLabel(label); });

            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((eventData) => { SetParentMenuLabel(""); });

            t.triggers.Add(enter);
            t.triggers.Add(exit);
        }
    }
	
    //Used by the parent radial menu to set up all the approprate angles. Affects master Z rotation and the active angles for lazy selection.
    public void SetAllAngles(float offset, float baseOffset)
    {
        angleOffset = offset;
        angleMin = offset - (baseOffset / 2f);
        angleMax = offset + (baseOffset / 2f);
    }

    public void SetColors()
    {
        if (image != null)
        {
            if (player.hasBeenRecorded)
            {
                if (player.isMainPlayer)
                {
                    image.color = m_recorded;
                }
                else
                {
                    image.color = d_recorded;
                }
            }
            else
            {
                if (player.isMainPlayer)
                {
                    image.color = m_unrecorded;
                }
                else
                {
                    image.color = d_unrecorded;
                }
            }
        }
    }

    public void HighlightThisElement()
    {
        selected = true;

        if (image != null)
        {
            image.sprite = selectedSprite;
            SetColors();
        }
        
        SetParentMenuLabel(label);
    }

    public void SetParentMenuLabel(string l)
    {
        if (parentRM.textLabel != null)
        {
            parentRM.textLabel.text = l;
        }
    }


    public void UnHighlightThisElement()
    {
        selected = false;

        if (image != null)
        {
            image.sprite = defaultSprite;
            SetColors();
        }

        if (!parentRM.useLazySelection)
        {
            SetParentMenuLabel(" ");
        }
    }

    private void Update()
    {
        
    }
}
