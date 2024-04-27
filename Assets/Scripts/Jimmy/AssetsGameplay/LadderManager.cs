using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.VersionControl.Asset;

public class LadderManager : MonoBehaviour
{
    public enum State
    {
        Inactive,
        Active
    }

    public State state;
    [SerializeField] private Transform textMeshTransform;
    [SerializeField] private Transform startUI;
    [SerializeField] private Transform endUI;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private Animator animator;
    public List<PlayerManager> players = new List<PlayerManager>();
    private bool isUIActive = false;

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
        if (players.Count > 0)
        {
            if (state == State.Active)
            {
                if (isUIActive)
                {

                }

                players.Clear();
                return;
            }

            bool isPlayer = false;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].isMainPlayer && players[i].isActive)
                {
                    isPlayer = true;
                    if (players[0].playerControls.Player.A.WasPressedThisFrame())
                    {
                        state = State.Active;
                        animator.Play("Open");
                    }
                    break;
                }
            }

            textMeshTransform.LookAt(Camera.main.transform.position, Vector3.up);

            if (isPlayer && !isUIActive)
            {
                isUIActive = true;
                textMeshTransform.DOScale(new Vector3(-0.32f, 0.32f, 0.32f), 0.25f).SetEase(Ease.InOutSine, 0.3f);
                textMeshTransform.DOMove(endUI.position, 0.5f).SetEase(Ease.InOutSine, 0.3f);
            }
            else if (!isPlayer && isUIActive)
            {
                isUIActive = false;
                textMeshTransform.DOScale(new Vector3(-0.25f, 0f, 0.32f), 0.25f).SetEase(Ease.InOutSine, 0.3f);
                textMeshTransform.DOMove(startUI.position, 0.5f).SetEase(Ease.InOutSine, 0.3f);
            }
        }
        else
        {
            if (isUIActive)
            {
                isUIActive = false;
                textMeshTransform.DOScale(new Vector3(-0.25f, 0f, 0.32f), 0.25f).SetEase(Ease.InOutSine, 0.3f);
                textMeshTransform.DOMove(startUI.position, 0.5f).SetEase(Ease.InOutSine, 0.3f);
            }
        }
    }

    public void Teleport(Vector3 position, PlayerManager player, LadderTrigger other)
    {
        StartCoroutine(TeleportRoutine(position, player, other));
        cameraManager.BlackScreen(0.5f, 0.5f);
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
