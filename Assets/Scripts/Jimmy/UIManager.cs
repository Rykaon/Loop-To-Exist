using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public GameManager gameManager;
    [SerializeField] private UIInputManager inputManager;

    private void Awake()
    {
        inputManager.Initialize(this);
    }

    public void SetUIInput(PlayerManager player)
    {
        inputManager.SetUIInput(player);
    }
}
