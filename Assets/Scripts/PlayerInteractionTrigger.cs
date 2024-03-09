using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionTrigger : MonoBehaviour
{
    [SerializeField] private PlayerManager player;
    public List<StateManager> triggeredObjectsList { get; private set; }

    private void Awake()
    {
        triggeredObjectsList = new List<StateManager>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if ((collision.transform.tag == "Player" || collision.transform.tag == "Mushroom" || collision.transform.tag == "Object") && collision.transform != player.transform)
        {
            if (collision.transform.TryGetComponent<StateManager>(out StateManager holdObject))
            {
                Debug.Log(collision.transform.name);
                triggeredObjectsList.Add(holdObject);
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if ((collision.transform.tag == "Player" || collision.transform.tag == "Mushroom" || collision.transform.tag == "Object") && collision.transform != player.transform)
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
}
