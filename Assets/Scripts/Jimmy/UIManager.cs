using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameManager;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    [Header("GameMenu References")]
    [HideInInspector] public GameManager gameManager;
    [SerializeField] private UIInputManager inputManager;
    public bool menuPauseIsActive;
    public Coroutine toggleMenuPause = null;

    [Header("MainMenu References")]
    [SerializeField] private bool isMainMenu;
    [SerializeField] private Image loadingBarBackground;
    [SerializeField] private Image loadingBarFront;
    [SerializeField] private Image loadingBarBack;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Shared References")]
    [SerializeField] private Image logo;
    [SerializeField] private Image blackScreen;
    [SerializeField] private Image endScreen;
    [SerializeField] private Image buttonSelection;
    [SerializeField] private List<TextMeshProUGUI> buttonTexts;

    [HideInInspector] private List<float> positions;
    [HideInInspector] public bool canNavigateMenu;
    [HideInInspector] public int menuIndex = 0;
    [HideInInspector] private int previousMainMenuIndex = -1;
    [HideInInspector] private Vector3 buttonSelectionPos = Vector3.zero;

    private void Awake()
    {
        if (inputManager != null)
        {
            inputManager.Initialize(this);
        }

        positions = new List<float>();
        for (int i = 0; i < buttonTexts.Count; i++)
        {
            positions.Add(buttonTexts[i].rectTransform.anchoredPosition.y);
        }

        buttonSelectionPos = buttonSelection.rectTransform.anchoredPosition;
        buttonSelectionPos.y = positions[0];
        buttonSelection.rectTransform.anchoredPosition = buttonSelectionPos;

        if (isMainMenu)
        {
            StartCoroutine(StartMainMenu());
        }
    }

    private IEnumerator StartGameMenu()
    {
        AudioManager.instance.Play("Sfx_Menu_Open");

        menuIndex = 0;
        buttonSelectionPos = buttonSelection.rectTransform.anchoredPosition;
        buttonSelectionPos.y = positions[0];
        buttonSelection.rectTransform.anchoredPosition = buttonSelectionPos;

        blackScreen.DOFade(0.75f, 0.25f).SetUpdate(true);
        logo.DOFade(1f, 0.25f).SetUpdate(true);
        buttonSelection.DOFade(1f, 0.25f).SetUpdate(true);
        for (int i = 0; i < buttonTexts.Count; i++)
        {
            buttonTexts[i].DOColor(Color.white, 0.25f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);
        canNavigateMenu = true;
        toggleMenuPause = null;
    }

    private IEnumerator CloseGameMenu()
    {
        AudioManager.instance.Play("Sfx_Menu_Close");
        canNavigateMenu = false;

        blackScreen.DOFade(0f, 0.25f).SetUpdate(true);
        logo.DOFade(0f, 0.25f).SetUpdate(true);
        buttonSelection.DOFade(0f, 0.25f).SetUpdate(true);
        for (int i = 0; i < buttonTexts.Count; i++)
        {
            buttonTexts[i].DOColor(new Color(1, 1, 1, 0), 0.25f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        toggleMenuPause = null;
        gameManager.ChangeState(ControlState.World);
        SetGameMenuActive(false);
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
        canNavigateMenu = true;
    }

    public void NavigateMenu(int value)
    {
        if (!canNavigateMenu)
        {
            return;
        }

        previousMainMenuIndex = menuIndex;
        menuIndex += value;

        if (menuIndex == buttonTexts.Count && value > 0)
        {
            menuIndex = 0;
        }
        else if (menuIndex == -1 && value < 0)
        {
            menuIndex = buttonTexts.Count - 1;
        }

        AudioManager.instance.Play("Sfx_Menu_Change");
        StartCoroutine(SetMenuButton(menuIndex));
    }

    private IEnumerator SetMenuButton(int index)
    {
        canNavigateMenu = false;
        buttonSelectionPos.y = positions[index];
        buttonSelection.rectTransform.DOAnchorPos3D(buttonSelectionPos, 0.15f).SetEase(Ease.InOutExpo).SetUpdate(true);

        if (previousMainMenuIndex != -1)
        {
            buttonTexts[previousMainMenuIndex].rectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutElastic).SetUpdate(true);
        }
        buttonTexts[menuIndex].rectTransform.DOScale(1.1f, 0.5f).SetEase(Ease.OutElastic).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.25f);
        canNavigateMenu = true;
    }

    public void ExecuteMenuButton()
    {
        if (!canNavigateMenu)
        {
            return;
        }

        if (menuIndex == 0)
        {
            if (isMainMenu)
            {
                StartCoroutine(ExecuteMainMenuButton(0));
            }
            else
            {
                StartCoroutine(ExecuteGameMenuButton(0));
            }
        }
        else if (menuIndex == 1)
        {
            if (isMainMenu)
            {
                StartCoroutine(ExecuteMainMenuButton(1));
            }
            else
            {
                StartCoroutine(ExecuteGameMenuButton(1));
            }
        }

        AudioManager.instance.Play("Sfx_Dialogue_Next");
    }

    public void GetBackToMenu()
    {
        SceneManager.LoadScene(0);
    }

    private IEnumerator ExecuteGameMenuButton(int value)
    {
        canNavigateMenu = false;
        buttonTexts[menuIndex].rectTransform.DOScale(0.9f, 0.5f).SetEase(Ease.InBounce).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.5f);

        if (menuIndex == 0)
        {
            toggleMenuPause = StartCoroutine(CloseGameMenu());
        }
        else if (menuIndex == 1)
        {
            endScreen.DOFade(1f, 0.5f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }

    private IEnumerator ExecuteMainMenuButton(int value)
    {
        canNavigateMenu = false;

        buttonTexts[menuIndex].rectTransform.DOScale(0.9f, 0.5f).SetEase(Ease.InBounce);
        yield return new WaitForSecondsRealtime(0.5f);

        if (menuIndex == 0)
        {
            StartCoroutine(LoadScene(1));
        }
        else if (menuIndex == 1)
        {
            endScreen.DOFade(1f, 0.5f);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }

    private IEnumerator LoadScene(int sceneId)
    {
        buttonSelection.DOFade(1f, 0.25f);
        for (int i = 0; i < buttonTexts.Count; i++)
        {
            buttonTexts[i].DOColor(new Color(1, 1, 1, 0), 0.25f);
        }
        yield return new WaitForSecondsRealtime(0.25f);

        loadingText.DOColor(Color.white, 0.5f);
        loadingBarBackground.DOFade(1f, 0.5f);
        yield return new WaitForSecondsRealtime(0.5f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneId);
        load.allowSceneActivation = false;
        StartCoroutine(Loading());

        bool canLaunch = false;
        float loadFillFront = loadingBarFront.fillAmount;

        while (loadFillFront < 1f && !canLaunch)
        {
            loadFillFront = loadingBarFront.fillAmount;
            float loadNormalized = load.progress / 1f;

            if (loadFillFront < loadNormalized)
            {
                loadingBarFront.fillAmount = loadNormalized;
            }

            if (loadFillFront >= 0.85f)
            {
                loadFillFront = loadingBarFront.fillAmount;
                loadNormalized = load.progress / 1f;

                if (loadFillFront < loadNormalized)
                {
                    loadingBarFront.fillAmount = loadNormalized;
                }
                yield return new WaitForSecondsRealtime(0.5f);
                canLaunch = true;
            }

            yield return new WaitForFixedUpdate();
        }

        endScreen.DOFade(1f, 0.5f);
        yield return new WaitForSecondsRealtime(0.5f);
        load.allowSceneActivation = true;
    }

    private IEnumerator Loading()
    {
        loadingText.text = "Loading";
        yield return new WaitForSecondsRealtime(0.5f);

        loadingText.text = "Loading.";
        yield return new WaitForSecondsRealtime(0.5f);

        loadingText.text = "Loading..";
        yield return new WaitForSecondsRealtime(0.5f);

        loadingText.text = "Loading...";
        yield return new WaitForSecondsRealtime(0.5f);

        StartCoroutine(Loading());
    }

    public void SetUIInput(PlayerManager player)
    {
        inputManager.SetUIInput(player);
    }

    public void SetGameMenuActive(bool isActive)
    {
        menuPauseIsActive = isActive;

        if (menuPauseIsActive)
        {
            Time.timeScale = 0f;
            toggleMenuPause = StartCoroutine(StartGameMenu());
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
