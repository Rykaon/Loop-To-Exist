using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    [SerializeField] private GameObject visualCue;

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private bool playerInRange;

    private PlayerControls playerControls;

    private void Awake()
    {
        playerInRange = false;
        visualCue.SetActive(false);
    }

    private void Start()
    {
        //playerControls = PlayerManager.instance.playerControls;
    }

    private void Update()
    {
        if (playerInRange && !DialogueManager.instance.isActive)
        {
            visualCue.SetActive(true);
            /*if (playerControls.Gamepad.A.WasPressedThisFrame())
            {
                DialogueManager.instance.EnterDialogueMode(inkJSON);
            }*/
        }
        else
        {
            visualCue.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerInRange = false;
        }
    }
}
