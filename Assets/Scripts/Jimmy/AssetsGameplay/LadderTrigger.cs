using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    [SerializeField] private LadderManager ladder;
    [SerializeField] private LadderTrigger otherTrigger;
    [SerializeField] private Transform teleportPoint;
    public List<PlayerManager> players = new List<PlayerManager>();
    public bool isActive = false;
    public bool isPlayer = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
        {
            if (ladder.state == LadderManager.State.Active && !players.Contains(playerManager))
            {
                players.Add(playerManager);
                playerManager.isLadderTrigger = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
        {
            if (ladder.state == LadderManager.State.Active && players.Contains(playerManager))
            {
                players.Remove(playerManager);
                playerManager.isLadderTrigger = false;
            }
        }
    }

    private void Update()
    {
        if (isActive)
        {
            if (players.Count > 0)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].isMainPlayer && players[i].isActive)
                    {
                        if (!ladder.outline.enabled)
                        {
                            ladder.outline.enabled = true;
                        }

                        isPlayer = true;
                        if (players[i].playerControls.Player.A.WasPressedThisFrame())
                        {
                            ladder.Teleport(teleportPoint.position, players[i], otherTrigger);
                        }
                        break;
                    }

                    if (i == players.Count - 1)
                    {
                        isPlayer = false;
                    }
                }
            }
            else
            {
                isPlayer = false;
            }

            if (isPlayer)
            {
                if (!ladder.outline.enabled)
                {
                    ladder.outline.enabled = true;
                }
            }
            else
            {
                if (!otherTrigger.isPlayer && ladder.outline.enabled)
                {
                    ladder.outline.enabled = false;
                }
            }
        }
    }
}
