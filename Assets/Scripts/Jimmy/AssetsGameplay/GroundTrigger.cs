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

                if (stateManager.GetType() == typeof(PlayerManager))
                {
                    PlayerManager playerManager = (PlayerManager)stateManager;
                    if (playerManager.heldObject != null)
                    {
                        playerManager.heldObject.lastGroundedPosition = playerManager.heldObject.rigidBody.position;
                        playerManager.heldObject.lastGroundedRotation = playerManager.heldObject.rigidBody.rotation;
                        foreach (GameObject objects in playerManager.heldObject.stickedObjects)
                        {
                            StateManager stickManager = objects.GetComponent<StateManager>();
                            stickManager.lastGroundedPosition = stickManager.rigidBody.position;
                            stickManager.lastGroundedRotation = stickManager.rigidBody.rotation;
                        }
                    }

                    if (playerManager.equippedObject != null)
                    {
                        playerManager.equippedObject.lastGroundedPosition = playerManager.equippedObject.rigidBody.position;
                        playerManager.equippedObject.lastGroundedRotation = playerManager.equippedObject.rigidBody.rotation;
                        foreach (GameObject objects in playerManager.equippedObject.stickedObjects)
                        {
                            StateManager stickManager = objects.GetComponent<StateManager>();
                            stickManager.lastGroundedPosition = stickManager.rigidBody.position;
                            stickManager.lastGroundedRotation = stickManager.rigidBody.rotation;
                        }
                    }
                }
            }
        }
    }
}
