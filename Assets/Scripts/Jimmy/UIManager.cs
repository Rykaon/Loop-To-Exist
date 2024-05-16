using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public GameManager gameManager;
    [SerializeField] private UIInputManager inputManager;
    private bool menuPauseIsActive;

    [Header("MainMenu References")]
    [SerializeField] private bool isMainMenu;
    [SerializeField] private Image blackScreen;

    private void Awake()
    {
        if (inputManager != null)
        {
            inputManager.Initialize(this);
        }

        if (isMainMenu)
        {
            StartCoroutine(StartMainMenu());
        }
    }

    private IEnumerator StartMainMenu()
    {
        yield return new WaitForSecondsRealtime(2.5f);

        blackScreen.DOFade(0f, 2.5f);
    }

    public void SetUIInput(PlayerManager player)
    {
        inputManager.SetUIInput(player);
    }

    public void SetMenuActive(bool isActive)
    {
        menuPauseIsActive = isActive;

        if (menuPauseIsActive)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
