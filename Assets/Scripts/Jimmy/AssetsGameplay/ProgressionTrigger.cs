using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressionTrigger : MonoBehaviour
{
    public bool isActive = true;
    [SerializeField] private TextAsset inkJSON;

    private void OnTriggerStay(Collider other)
    {
        if (isActive)
        {
            if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
            {
                if (playerManager.isMainPlayer && playerManager.isActive)
                {
                    isActive = false;
                    DialogueManager.instance.EnterDialogueMode(inkJSON, true, true);
                }
            }
        }
    }
}
