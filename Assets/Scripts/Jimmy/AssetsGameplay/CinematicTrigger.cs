using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static UnityEditor.Experimental.GraphView.GraphView;

public class CinematicTrigger : MonoBehaviour
{
    public enum Type
    {
        Tutorial,
        Kindergarden,
        Escape
    }

    public Type type;
    public bool isActive = true;

    private void OnTriggerStay(Collider other)
    {
        if (isActive)
        {
            if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
            {
                if (playerManager.isMainPlayer && playerManager.isActive)
                {
                    if (type == Type.Tutorial)
                    {
                        isActive = false;
                        GameManager.instance.hasFinishedTutorial = true;
                        GameManager.instance.cameraManager.ExecuteCinematic(GameManager.instance.cameraManager.tutorial);
                    }
                    else if (type == Type.Kindergarden)
                    {
                        isActive = false;
                        GameManager.instance.cameraManager.ExecuteCinematic(GameManager.instance.cameraManager.kindergarden);
                    }
                    else if (type == Type.Escape)
                    {
                        if (GameManager.instance.mainPlayer.RaycastGrounded())
                        {
                            isActive = false;
                            GameManager.instance.cameraManager.ExecuteCinematic(GameManager.instance.cameraManager.escape);
                        }
                    }
                }
            }
        }
    }
}
