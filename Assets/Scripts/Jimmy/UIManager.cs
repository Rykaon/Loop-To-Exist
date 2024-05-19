using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [HideInInspector] public GameManager gameManager;
    [SerializeField] private UIInputManager inputManager;
    private bool menuPauseIsActive;

    [Header("MainMenu References")]
    [SerializeField] private bool isMainMenu;
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image endScreen;
    [SerializeField] private Image logo;
    [SerializeField] private Image buttonSelection;
    [SerializeField] private List<TextMeshProUGUI> buttonTexts;
    [HideInInspector] private List<float> positions;
    [HideInInspector] public bool canNavigateMainMenu;
    [HideInInspector] public int mainMenuIndex = 0;
    [HideInInspector] private int previousMainMenuIndex = -1;
    [HideInInspector] private Vector3 buttonSelectionPos = Vector3.zero;

    private void Awake()
    {
        if (inputManager != null)
        {
            inputManager.Initialize(this);
        }

        if (isMainMenu)
        {
            positions = new List<float>();
            for (int i = 0; i < buttonTexts.Count; i++)
            {
                positions.Add(buttonTexts[i].rectTransform.anchoredPosition.y);
            }

            buttonSelectionPos = buttonSelection.rectTransform.anchoredPosition;
            buttonSelectionPos.y = positions[0];
            buttonSelection.rectTransform.anchoredPosition = buttonSelectionPos;
            StartCoroutine(StartMainMenu());
        }
    }

    private IEnumerator StartMainMenu()
    {
        yield return new WaitForSecondsRealtime(2.5f);

        blackScreen.DOFade(0f, 2.5f);
        yield return new WaitForSecondsRealtime(2.5f);

        blackScreen.DOFade(0.45f, 1f);
        logo.DOFade(1f, 2f);
        yield return new WaitForSecondsRealtime(3f);

        buttonSelection.DOFade(1f, 0.75f);
        for (int i = 0; i < buttonTexts.Count; i++)
        {
            buttonTexts[i].DOColor(Color.white, 0.75f);
        }

        yield return new WaitForSecondsRealtime(0.75f);
        canNavigateMainMenu = true;
    }

    public void NavigateMainMenu(int value)
    {
        if (!canNavigateMainMenu)
        {
            return;
        }

        previousMainMenuIndex = mainMenuIndex;
        mainMenuIndex += value;

        if (mainMenuIndex == buttonTexts.Count && value > 0)
        {
            mainMenuIndex = 0;
        }
        else if (mainMenuIndex == -1 && value < 0)
        {
            mainMenuIndex = buttonTexts.Count - 1;
        }

        AudioManager.instance.Play("Sfx_Menu_Change");
        StartCoroutine(SetMainMenuButton(mainMenuIndex));
    }

    private IEnumerator SetMainMenuButton(int index)
    {
        canNavigateMainMenu = false;
        buttonSelectionPos.y = positions[index];
        buttonSelection.rectTransform.DOAnchorPos3D(buttonSelectionPos, 0.15f).SetEase(Ease.InOutExpo);

        if (previousMainMenuIndex != -1)
        {
            buttonTexts[previousMainMenuIndex].rectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutElastic);
        }
        buttonTexts[mainMenuIndex].rectTransform.DOScale(1.1f, 0.5f).SetEase(Ease.OutElastic);

        yield return new WaitForSecondsRealtime(0.25f);
        canNavigateMainMenu = true;
    }

    public void ExecuteMainMenuButton()
    {
        if (!canNavigateMainMenu)
        {
            return;
        }

        if (mainMenuIndex == 0)
        {
            StartCoroutine(ExecuteMainMenuButton(0));
        }
        else if (mainMenuIndex == 1)
        {
            StartCoroutine(ExecuteMainMenuButton(1));
        }

        AudioManager.instance.Play("Sfx_Dialogue_Next");
    }

    private IEnumerator ExecuteMainMenuButton(int value)
    {
        canNavigateMainMenu = false;
        buttonTexts[mainMenuIndex].rectTransform.DOScale(0.9f, 0.5f).SetEase(Ease.InBounce);
        yield return new WaitForSecondsRealtime(0.5f);
        endScreen.DOFade(1f, 0.5f);
        yield return new WaitForSecondsRealtime(0.5f);

        if (mainMenuIndex == 0)
        {
            SceneManager.LoadScene(1);
        }
        else if (mainMenuIndex == 1)
        {
            Application.Quit();
        }
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
