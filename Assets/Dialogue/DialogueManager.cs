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
    [SerializeField] private Animator portraitAnimator;
    [SerializeField] private GameObject continueIcon;
    private Animator layoutAnimator;

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
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";

    private DialogueVariables dialogueVariables;

    private PlayerControls playerControls;

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene !");
        }

        instance = this;

        dialogueVariables= new DialogueVariables(globalsInkFile);
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        isActive = false;
        dialoguePanel.SetActive(false);

        layoutAnimator = dialoguePanel.GetComponent<Animator>();

        //playerControls = PC_Manager.playerControls;

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update()
    {
        if (isActive)
        {

            if (canContinueToNextLine && playerControls.UI.A.WasPressedThisFrame() && currentStory.currentChoices.Count == 0)
            {
                ContinueStory();
            }

            if (playerControls.UI.B.WasPressedThisFrame())
            {
                StartCoroutine(ExitDialogueMode());
            }

            
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        EnableDisable(true);

        currentStory = new Story(inkJSON.text);
        isActive = true;
        dialoguePanel.SetActive(true);

        dialogueVariables.StartListening(currentStory);

        UpdateVariables();

        /*if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Random)
        {
            currentStory.BindExternalFunction("StealGive", (string RewardType, int NbrReward, bool IsBonus) =>
            {
                PC_Manager.mapGenerator.TakeReward();
                UpdateVariables();
            });
        }
        else if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.End || PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Start || PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Shop)
        {
            if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.End || PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Start)
            {
                currentStory.BindExternalFunction("CheckRun", () =>
                {
                    PC_Manager.mapGenerator.TakeReward();
                    UpdateVariables();
                });
            }

            currentStory.BindExternalFunction("PlantSellBuy", (string PlantToBuy, int PlantPrice, bool SellOrBuy) =>
            {
                inventory.SellBuyItem(PlantToBuy, PlantPrice, SellOrBuy);
                UpdateVariables();
            });
        }*/

        // Reset portrait, layout and speaker
        displayNameText.text = "???";
        portraitAnimator.Play("default");
        layoutAnimator.Play("right");

        ContinueStory();
    }

    public void UpdateVariables()
    {
        //currentStory.variablesState["PlayerArgent"] = inventory.nbArgent;

        /*for (int i = 0; i < PC_Manager.inventory.plantsList.Count; i++)
        {
            currentStory.variablesState["NbPlant" + (i + 1).ToString()] = Utilities.GetNumberOfItemByPrefab(PC_Manager.inventory.inventory, PC_Manager.inventory.plantsList[i].prefab);

            if (!isInitialized)
            {
                currentStory.variablesState["NamePlant" + (i + 1).ToString()] = PC_Manager.inventory.plantsList[i].itemName;
                currentStory.variablesState["PricePlant" + (i + 1).ToString()] = PC_Manager.inventory.plantsList[i].sellPrice;
            }
        }

        if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Random)
        {
            currentStory.variablesState["RewardType"] = PC_Manager.mapGenerator.currentNode.mapEvent.rewardType.ToString();
            currentStory.variablesState["NbrReward"] = PC_Manager.mapGenerator.currentNode.mapEvent.nbrReward;
            currentStory.variablesState["IsBonus"] = PC_Manager.mapGenerator.currentNode.mapEvent.isBonus;
        }
        else if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.End)
        {
            currentStory.variablesState["RewardType"] = "Gold";
            currentStory.variablesState["NbrReward"] = PC_Manager.mapGenerator.currentNode.mapEvent.nbrReward;
            currentStory.variablesState["IsBonus"] = true;
            //isBonus == true => Fin de run
        }
        else if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Start)
        {
            currentStory.variablesState["RewardType"] = "Gold";
            currentStory.variablesState["NbrReward"] = PC_Manager.mapGenerator.currentNode.mapEvent.nbrReward;
            currentStory.variablesState["IsBonus"] = false;
            //isBonus == false => Début de run
        }*/

        isInitialized = true;
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);
        dialogueVariables.StopListening(currentStory);

        /*if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Random)
        {
            currentStory.UnbindExternalFunction("StealGive");
        }
        else if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.End || PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.Shop)
        {
            if (PC_Manager.mapGenerator.currentNode.mapEvent.eventType == Map.MapEvent.EventType.End)
            {
                currentStory.UnbindExternalFunction("CheckRun");
            }

            currentStory.BindExternalFunction("PlantSellBuy", (string PlantToBuy, int PlantPrice, bool SellOrBuy) =>
            {
                currentStory.UnbindExternalFunction("PlantSellBuy");
            });
        }*/

        dialoguePanel.SetActive(false);
        dialogueText.text = "";

        EnableDisable(false);
    }

    private void ContinueStory()
    {
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
            if (playerControls.UI.A.IsPressed() && indexLetter>5)
            {
                dialogueText.text = line;
                break;
            }

            //AudioManager.instance.PlayVariation("DialogueBoop", 0.1f, 0.5f);

            indexLetter++;

            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }


        DisplayChoices();
        continueIcon.SetActive(true);
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
                case PORTRAIT_TAG:
                    portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    layoutAnimator.Play(tagValue);
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
            //PC_Manager.ChangeState(PlayerManager.ControlState.WorldUI);
        }
        else
        {
            isActive = false;
            //PC_Manager.ChangeState(PlayerManager.ControlState.World);
        }
    }
}
