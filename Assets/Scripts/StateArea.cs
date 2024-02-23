using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateArea : MonoBehaviour
{
    public StateManager.State state;
    public bool isActive = true;

    private void SetAreaActive()
    {
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            if (other.tag == "Wall" || other.tag == "Player")
            {
                if (other.TryGetComponent<StateManager>(out StateManager stateManager))
                {
                    if (stateManager.type == StateManager.Type.Player || stateManager.type == StateManager.Type.Item)
                    {
                        stateManager.stateToApply = state;
                    }
                }
            }
        }
        else
        {
            if (other.tag == "Wall")
            {
                if (other.TryGetComponent<StateManager>(out StateManager stateManager))
                {
                    if (stateManager.type == StateManager.Type.Item)
                    {
                        if (stateManager.state == state)
                        {
                            SetAreaActive();
                        }
                    }
                }
            }
        }
    }
}
