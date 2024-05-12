using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerInteractionTrigger : MonoBehaviour
{
    [SerializeField] private PlayerManager player;
    public List<StateManager> triggeredObjectsList { get; private set; }
    public StateManager current = null;
    private List<string> tagsToCheck = new List<string>();

    private void Awake()
    {
        triggeredObjectsList = new List<StateManager>();
        tagsToCheck.Add("Player");
        tagsToCheck.Add("Mushroom");
        tagsToCheck.Add("Object");
        tagsToCheck.Add("Creature");
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (tagsToCheck.Contains(collision.transform.tag) && collision.transform != player.transform)
        {
            if (collision.transform.TryGetComponent<StateManager>(out StateManager holdObject))
            {
                if (holdObject.position == StateManager.Position.Default || holdObject.position == StateManager.Position.Held)
                {
                    triggeredObjectsList.Add(holdObject);
                }
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (tagsToCheck.Contains(collision.transform.tag) && collision.transform != player.transform)
        {
            if (collision.transform.TryGetComponent<StateManager>(out StateManager holdObject))
            {
                if (triggeredObjectsList.Contains(holdObject))
                {
                    triggeredObjectsList.Remove(holdObject);
                }
            }
        }
    }

    public void UpdateOutline()
    {
        if (triggeredObjectsList.Count > 0)
        {
            float startDistance = float.MaxValue;
            int index = -1;
            for (int i = 0; i < triggeredObjectsList.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, triggeredObjectsList[i].transform.position);
                if (distance < startDistance)
                {
                    index = i;
                    startDistance = distance;
                }
            }

            if (index >= 0)
            {
                if (current != null)
                {
                    if (current != triggeredObjectsList[index])
                    {
                        current.outline.enabled = false;
                    }
                }
                current = triggeredObjectsList[index];
                if (!current.isEquipped)
                {
                    if (player.heldObject != null)
                    {
                        if (player.heldObject != current)
                        {
                            current.outline.enabled = true;
                        }
                        else
                        {
                            current.outline.enabled = false;
                        }
                    }
                    else
                    {
                        current.outline.enabled = true;
                    }
                }
            }
        }
        else
        {
            if (current != null)
            {
                current.outline.enabled = false;
            }
        }
    }
}
