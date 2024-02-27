using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionTrigger : MonoBehaviour
{
    public List<ItemManager> triggeredObjectsList { get; private set; }

    private void Awake()
    {
        triggeredObjectsList = new List<ItemManager>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.tag == "Wall")
        {
            if (collision.transform.TryGetComponent<ItemManager>(out ItemManager catchObject))
            {
                if (catchObject.state != StateManager.State.FreezePosition)
                {
                    triggeredObjectsList.Add(catchObject);
                }
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.transform.tag == "Wall")
        {
            if (collision.transform.TryGetComponent<ItemManager>(out ItemManager catchObject))
            {
                if (triggeredObjectsList.Contains(catchObject))
                {
                    triggeredObjectsList.Remove(catchObject);
                }
            }
        }
    }
}
