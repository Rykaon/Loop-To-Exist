using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Ink.Runtime;
using UnityEngine.EventSystems;
using Ink.Parsed;
using Story = Ink.Runtime.Story;
using Choice = Ink.Runtime.Choice;
//using Ink.UnityIntegration;
using System.Linq.Expressions;
using Unity.VisualScripting;

public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.04f;

    [Header("Globals Ink File")]
    [SerializeField] private TextAsset globalsInkFile;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private GameObject continueIcon;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    private Story currentStory;
    private Coroutine displayLineCoroutine;

    //[SerializeField] private PlayerController_Farm playerControllerFarm;
    //[SerializeField] private PlayerManager PC_Manager;
    //[SerializeField] private PlayerInventory inventory;

    public bool isActive = false;
    private bool isInitialized = false;
    private bool canContinueToNextLine = false;

    public static DialogueManager instance;

    private const string SPEAKER_TAG = "speaker";
    private const string TIME_BEFORE_SKIP = "time";

    private DialogueVariables dialogueVariables;

    private bool saveLockStateDialogue;

    private float elapsedTime;
    private float timeToWaitToSkip;

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene !");
        }

        instance = this;

        dialogueVariables = new DialogueVariables(globalsInkFile);

        isActive = false;
        dialoguePanel.SetActive(false);

        //playerControls = PC_Manager.playerControls;

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (isActive)
        {
            if (elapsedTime > timeToWaitToSkip && currentStory.currentChoices.Count == 0) //&& (GameManager.instance.playerControls.UI.A.WasPressedThisFrame()) 
            {
                ContinueStory();
            }

            /*
            if (GameManager.instance.playerControls.Player.B.WasPressedThisFrame())
            {
                StartCoroutine(ExitDialogueMode());
            } */

            elapsedTime += Time.deltaTime;
            
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON, bool characterLockState, bool isNonCinematic)
    {
        elapsedTime = 0f;

        saveLockStateDialogue = characterLockState;
        if (characterLockState)
        {
            EnableDisable(true);
        }

        isActive = isNonCinematic;

        currentStory = new Story(inkJSON.text);
        
        dialoguePanel.SetActive(true);

        dialogueVariables.StartListening(currentStory);

        UpdateVariables();

        // Reset portrait, layout and speaker
        displayNameText.text = "Entité";

        ContinueStory();
    }

    public void UpdateVariables()
    {
        isInitialized = true;
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);
        dialogueVariables.StopListening(currentStory);

        dialoguePanel.SetActive(false);
        dialogueText.text = "";

        isActive = false;

        if (saveLockStateDialogue)
        {
            EnableDisable(false);
        }
    }

    public void ContinueStory()
    {
        elapsedTime = 0f;

        if (currentStory.canContinue)
        {
            if(displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }

            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue()));


            HandleTags(currentStory.currentTags);
        }
        else
        {
            StartCoroutine(ExitDialogueMode());
        }
    }

    public IEnumerator DisplayLine(string line)
    {
        var indexLetter = 0;
        dialogueText.text = "";
        continueIcon.SetActive(false);
        HideChoices();

        canContinueToNextLine = false;

        foreach(char letter in line.ToCharArray())
        {
            if (GameManager.instance.playerControls.UI.A.IsPressed() && indexLetter>5)
            {
                dialogueText.text = line;
                break;
            }

            AudioManager.instance.PlayVariation("Sfx_Dialogue_Speech", 0.1f, 0.5f); //CA MARCHE PAS, POURQUOI ?

            indexLetter++;

            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }


        DisplayChoices();
        //continueIcon.SetActive(true);
        canContinueToNextLine = true;
        StartCoroutine(SelectFirstChoice());
    }

    private void HideChoices()
    {
        foreach(GameObject choiceButton in choices)
        {
            choiceButton.SetActive(false);
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        // loop through each tag and handle it accoringly
        foreach (string tag in currentTags)
        {
            // parse the tag
            string[] splitTag = tag.Split(':');
            if(splitTag.Length != 2 ) {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case TIME_BEFORE_SKIP:
                    timeToWaitToSkip = float.Parse(tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came but is not valid :" + tag);
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        // defensive check to make sure our UI can support the number of choices coming in
        if(currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than the UI can support. Number of choices given :" + currentChoices.Count);
        }

        int index = 0;
        // enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        // go through the remaining choices the UI supports and make sure they're hidden
        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (canContinueToNextLine)
        {
            Debug.Log("CHOICE HAS BEEN MADE");
            currentStory.ChooseChoiceIndex(choiceIndex);
            //ContinueStory();
        }
    }

    public void EnableDisable(bool enabled)
    {
        if (enabled)
        {
            isActive = true;
            GameManager.instance.ChangeState(GameManager.ControlState.UI);
        }
        else
        {
            isActive = false;
            GameManager.instance.ChangeState(GameManager.ControlState.World);
        }
    }
}
