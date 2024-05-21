using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LadderManager : MonoBehaviour
{
    public enum State
    {
        Inactive,
        Active
    }

    public State state;
    [SerializeField] private Animator animator;
    [SerializeField] public Outline outline;
    public List<PlayerManager> players = new List<PlayerManager>();
    public List<LadderTrigger> triggers = new List<LadderTrigger>();
    private bool isUIActive = false;
    private Coroutine activation = null;
    private bool wasInactive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
        {
            if (state == State.Inactive && !players.Contains(playerManager))
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
            if (state == State.Inactive && players.Contains(playerManager))
            {
                players.Remove(playerManager);
                playerManager.isLadderTrigger = false;
            }
        }
    }

    private void Update()
    {
        bool isPlayer = false;

        if (state == State.Active)
        {
            if (wasInactive)
            {
                if (players.Count > 0)
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        if (players[i].isMainPlayer && players[i].isActive)
                        {
                            isPlayer = true;
                            break;
                        }
                    }
                }

                if (!isPlayer && outline.enabled)
                {
                    outline.enabled = false;
                    wasInactive = false;
                }
            }

            players.Clear();
            return;
        }

        isPlayer = false;

        if (players.Count > 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].isMainPlayer && players[i].isActive)
                {
                    isPlayer = true;

                    if (!outline.enabled)
                    {
                        outline.enabled = true;
                    }

                    if (players[0].playerControls.Player.A.WasPressedThisFrame() && state == State.Inactive && activation == null)
                    {
                        activation = StartCoroutine(SetActive());
                        animator.Play("LianeActive");
                    }
                    break;
                }
            }
        }

        if (!isPlayer)
        {
            if (outline.enabled)
            {
                outline.enabled = false;
            }
        }
    }

    private IEnumerator SetActive()
    {
        yield return new WaitForSecondsRealtime(0.75f);
        state = State.Active;
        wasInactive = true;
        for (int i = 0;i < triggers.Count; i++)
        {
            triggers[i].isActive = true;
        }
    }

    public void Teleport(Vector3 position, PlayerManager player, LadderTrigger other)
    {
        StartCoroutine(TeleportRoutine(position, player, other));
        GameManager.instance.cameraManager.BlackScreen(0.5f, 0.5f);
    }

    private IEnumerator TeleportRoutine(Vector3 position, PlayerManager player, LadderTrigger other)
    {
        player.rigidBody.isKinematic = true;
        player.rigidBody.velocity = Vector3.zero;
        player.rigidBody.angularVelocity = Vector3.zero;
        yield return new WaitForSecondsRealtime(0.5f);
        other.isActive = false;

        List<StateManager> states = new List<StateManager>();
        List<Vector3> offsets = new List<Vector3>();
        states.Add(player);
        if (player.heldObject != null)
        {
            states.Add(player.heldObject);
            foreach (GameObject go in player.heldObject.stickedObjects)
            {
                StateManager stickManager = go.GetComponent<StateManager>();
                states.Add(stickManager);
            }
        }

        if (player.equippedObject != null)
        {
            states.Add(player.equippedObject);
            foreach (GameObject go in player.equippedObject.stickedObjects)
            {
                StateManager stickManager = go.GetComponent<StateManager>();
                states.Add(stickManager);
            }
        }

        for (int i  = 0; i < states.Count; i++)
        {
            offsets.Add(player.rigidBody.position - states[i].rigidBody.position);
        }

        for (int i = 0; i < states.Count; i++)
        {
            states[i].rigidBody.position = position - offsets[i];
        }

        player.rigidBody.isKinematic = false;

        yield return new WaitForSecondsRealtime(1f);
        other.isActive = true;
    }
}
