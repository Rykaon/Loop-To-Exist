using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionTrigger : MonoBehaviour
{
    public List<GrabObject> triggeredObjectsList { get; private set; }

    private void Awake()
    {
        triggeredObjectsList = new List<GrabObject>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.tag == "Wall")
        {
            if (collision.transform.TryGetComponent<GrabObject>(out GrabObject catchObject))
            {
                triggeredObjectsList.Add(catchObject);
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.transform.tag == "Wall")
        {
            if (collision.transform.TryGetComponent<GrabObject>(out GrabObject catchObject))
            {
                if (triggeredObjectsList.Contains(catchObject))
                {
                    triggeredObjectsList.Remove(catchObject);
                }
            }
        }
    }
}
