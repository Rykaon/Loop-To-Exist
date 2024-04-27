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
    
    [SerializeField] private List<string> tagsToCheck;
    [SerializeField] private DoorController doorController;
    [SerializeField] private int nbrOfEntity;
    private List<GameObject> objects = new List<GameObject>();

    private void Awake()
    {
        state = State.Inactive;

        tagsToCheck = new List<string>();
        tagsToCheck.Add("Player");
        tagsToCheck.Add("Mushroom");
        tagsToCheck.Add("Object");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StateManager>(out StateManager manager))
        {
            if (!manager.isHeld && !manager.isEquipped)
            {
                List<GameObject> list = new List<GameObject>();
                list.Add(other.gameObject);
                if (manager.GetType() == typeof(PlayerManager))
                {
                    PlayerManager player = (PlayerManager)manager;

                    foreach (GameObject go in player.stickedObjects)
                    {
                        list.Add(go);
                    }

                    if (player.heldObject != null)
                    {
                        list.Add(player.heldObject.gameObject);
                        foreach (GameObject go in player.heldObject.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }

                    if (player.equippedObject != null)
                    {
                        list.Add(player.equippedObject.gameObject);
                        foreach (GameObject go in player.equippedObject.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }
                }
                else
                {
                    foreach (GameObject go in manager.stickedObjects)
                    {
                        list.Add(go);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (tagsToCheck.Contains(list[i].tag) && !objects.Contains(list[i]))
                    {
                        objects.Add(list[i]);
                    }
                }

                if (objects.Count >= nbrOfEntity)
                {
                    Debug.Log(transform.parent.name + " is Active");
                    state = State.Active;
                    if (doorController.state == DoorController.State.Close)
                    {
                        doorController.CheckSwitches();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<StateManager>(out StateManager manager))
        {
            if (!manager.isHeld && !manager.isEquipped)
            {
                List<GameObject> list = new List<GameObject>();
                list.Add(other.gameObject);
                if (manager.GetType() == typeof(PlayerManager))
                {
                    PlayerManager player = (PlayerManager)manager;

                    foreach (GameObject go in player.stickedObjects)
                    {
                        list.Add(go);
                    }

                    if (player.heldObject != null)
                    {
                        list.Add(player.heldObject.gameObject);
                        foreach (GameObject go in player.heldObject.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }

                    if (player.equippedObject != null)
                    {
                        list.Add(player.equippedObject.gameObject);
                        foreach (GameObject go in player.equippedObject.stickedObjects)
                        {
                            list.Add(go);
                        }
                    }
                }
                else
                {
                    foreach (GameObject go in manager.stickedObjects)
                    {
                        list.Add(go);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (tagsToCheck.Contains(list[i].tag) && objects.Contains(list[i]))
                    {
                        objects.Remove(list[i]);
                    }
                }

                if (objects.Count < nbrOfEntity)
                {
                    Debug.Log(transform.parent.name + " is Inactive");
                    state = State.Inactive;
                    if (doorController.state == DoorController.State.Open)
                    {
                        doorController.CheckSwitches();
                    }
                }
            }
        }
    }
}
