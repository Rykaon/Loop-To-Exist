using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSwitch : MonoBehaviour
{
    public enum State
    {
        Active,
        Inactive
    }
    public State state;
    
    [SerializeField] private List<GameObject> objectsOnSwitch;
    [SerializeField] private List<string> tagsToCheck;
    [SerializeField] private DoorController doorController;

    private void Awake()
    {
        state = State.Inactive;
        objectsOnSwitch = new List<GameObject>();

        tagsToCheck = new List<string>();
        tagsToCheck.Add("Player");
        tagsToCheck.Add("Mushroom");
        tagsToCheck.Add("Object");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tagsToCheck.Contains(other.tag))
        {
            if (!objectsOnSwitch.Contains(other.gameObject))
            {
                objectsOnSwitch.Add(other.gameObject);
                state = State.Active;
                if (doorController.state == DoorController.State.Close)
                {
                    doorController.CheckSwitches();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (tagsToCheck.Contains(other.tag))
        {
            if (objectsOnSwitch.Contains(other.gameObject))
            {
                objectsOnSwitch.Remove(other.gameObject);

                if (objectsOnSwitch.Count == 0)
                {
                    state = State.Inactive;
                    doorController.CheckSwitches();
                }
            }
        }
    }
}
