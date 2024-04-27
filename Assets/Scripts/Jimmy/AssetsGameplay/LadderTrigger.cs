using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    [SerializeField] private LadderManager ladder;
    [SerializeField] private LadderTrigger otherTrigger;
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private Transform textMeshTransform;
    [SerializeField] private Transform startUI;
    [SerializeField] private Transform endUI;
    public List<PlayerManager> players = new List<PlayerManager>();
    public bool isActive = true;
    private bool isUIActive = false;

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
        if (players.Count > 0 && isActive)
        {
            bool isPlayer = false;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].isMainPlayer && players[i].isActive)
                {
                    isPlayer = true;
                    if (players[0].playerControls.Player.A.WasPressedThisFrame())
                    {
                        ladder.Teleport(teleportPoint.position, players[i], otherTrigger);
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
}
