using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StateManager>(out StateManager stateManager))
        {
            if (!stateManager.isHeld && !stateManager.isEquipped)
            {
                stateManager.rigidBody.position = stateManager.lastGroundedPosition;
                stateManager.rigidBody.rotation = stateManager.lastGroundedRotation;
            }
        }
    }
}
