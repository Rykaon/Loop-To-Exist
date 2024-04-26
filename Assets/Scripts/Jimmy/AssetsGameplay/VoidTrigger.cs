using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VoidTrigger : MonoBehaviour
{
    [SerializeField] private Material dissolveMaterial;
    private float dissolveTime;
    private List<StateManager> states = new List<StateManager>();

    private void Awake()
    {
        dissolveTime = 0.75f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<StateManager>(out StateManager stateManager))
        {
            if (!stateManager.isHeld && !stateManager.isEquipped && !states.Contains(stateManager))
            {
                List<StateManager> managers = new List<StateManager>();
                managers.Add(stateManager);
                states.Add(stateManager);

                if (stateManager.GetType() == typeof(PlayerManager))
                {
                    PlayerManager playerManager = (PlayerManager)stateManager;
                    if (playerManager.heldObject != null)
                    {
                        managers.Add(playerManager.heldObject);
                        states.Add(playerManager.heldObject);
                        foreach (GameObject objects in playerManager.heldObject.stickedObjects)
                        {
                            managers.Add(objects.GetComponent<StateManager>());
                            states.Add(objects.GetComponent<StateManager>());
                        }
                    }

                    if (playerManager.equippedObject != null)
                    {
                        managers.Add(playerManager.equippedObject);
                        states.Add(playerManager.equippedObject);
                        foreach (GameObject objects in playerManager.equippedObject.stickedObjects)
                        {
                            managers.Add(objects.GetComponent<StateManager>());
                            states.Add(objects.GetComponent<StateManager>());
                        }
                    }
                }

                for (int i = 0; i < managers.Count; i++)
                {
                    Debug.Log(managers[i].name);
                    if (i == 0)
                    {
                        if (managers[i].GetType() == typeof(PlayerManager))
                        {
                            PlayerManager playerManager = (PlayerManager)managers[i];
                            if (playerManager.isMainPlayer)
                            {
                                StartCoroutine(Teleport(managers[i], true, true));
                            }
                            else
                            {
                                StartCoroutine(Teleport(managers[i], true, false));
                            }
                        }
                        else
                        {
                            StartCoroutine(Teleport(managers[i], true, false));
                        }
                    }
                    else
                    {
                        StartCoroutine(Teleport(managers[i], false, false));
                    }
                }
            }
        }
    }

    private IEnumerator Teleport(StateManager manager, bool isParent, bool isMainPlayer)
    {
        Material initMat = manager.renderer.material;
        Debug.Log(initMat.name);
        Material dissolveMat = Instantiate(dissolveMaterial);
        dissolveMat.SetTexture("_Albedo", initMat.mainTexture);
        dissolveMat.SetFloat("_AlphaTreshold", 0f);
        List<Material> initList = new List<Material>();
        List<Material> dissolveList = new List<Material>();
        initList.Add(initMat);
        dissolveList.Add(dissolveMat);
        manager.renderer.material = null;
        manager.renderer.material = dissolveMat;

        if (isParent)
        {
            manager.rigidBody.isKinematic = true;
        }

        if (isMainPlayer)
        {
            PlayerManager player = (PlayerManager)manager;
            player.isActive = false;
        }
        
        float elapsedTime = 0f;
        float treshold = 0f;
        while (elapsedTime < dissolveTime)
        {
            float time = elapsedTime / dissolveTime;
            treshold = Mathf.Lerp(treshold, 1f, time);
            dissolveMat.SetFloat("_AlphaTreshold", treshold);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        manager.rigidBody.position = manager.lastGroundedPosition;
        manager.rigidBody.rotation = manager.lastGroundedRotation;

        elapsedTime = 0f;
        treshold = 1f;
        while (elapsedTime < dissolveTime)
        {
            Debug.Log(treshold);
            float time = elapsedTime / dissolveTime;
            treshold = Mathf.Lerp(treshold, 0f, time);
            dissolveMat.SetFloat("_AlphaTreshold", treshold);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        manager.renderer.material = null;
        manager.renderer.material = initMat;

        if (isParent)
        {
            manager.rigidBody.isKinematic = false;
        }

        if (isMainPlayer)
        {
            PlayerManager player = (PlayerManager)manager;
            player.isActive = true;
        }

        states.Remove(manager);
    }
}
