using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<StateManager>(out StateManager stateManager))
        {
            if (!stateManager.isHeld && !stateManager.isEquipped)
            {
                stateManager.lastGroundedPosition = stateManager.rigidBody.position;
                stateManager.lastGroundedRotation = stateManager.rigidBody.rotation;
            }
        }
    }
}
