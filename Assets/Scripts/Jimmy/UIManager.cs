using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public GameManager gameManager;
    [SerializeField] private UIInputManager inputManager;
    private bool menuPauseIsActive;

    private void Awake()
    {
        inputManager.Initialize(this);
    }

    public void SetUIInput(PlayerManager player)
    {
        inputManager.SetUIInput(player);
    }

    private void Update()
    {
        if (GameManager.instance.playerControls.Player.Select.WasPressedThisFrame() && !menuPauseIsActive)
        {
            menuPauseIsActive = true;
            Time.timeScale = 0f;
        }else if (GameManager.instance.playerControls.Player.Select.WasPressedThisFrame() && menuPauseIsActive)
        {
            menuPauseIsActive = false;
            Time.timeScale = 1f;
        }
    }
}
