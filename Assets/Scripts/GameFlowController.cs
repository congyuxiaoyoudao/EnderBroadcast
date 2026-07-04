using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject infoCollectionPanel;
    [SerializeField] private GameObject infoOrganizationPanel;
    [SerializeField] private GameObject studioPanel;
    [SerializeField] private GameObject broadcastPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private CanvasGroup dayTransitionGroup;
    [SerializeField] private Text dayTransitionText;
    [SerializeField] private Text studioSituationText;
    [SerializeField] private Button enterInfoCollectionButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private InfoCollectionController infoCollectionController;

    private int currentDay = 1;
    private Coroutine flowRoutine;

    private readonly Vector2Int[] supportedResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    private void Awake()
    {
        InitializeSettingsControls();
        if (infoCollectionController == null && infoCollectionPanel != null)
        {
            infoCollectionController = infoCollectionPanel.GetComponent<InfoCollectionController>();
        }
        ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        ShowOnly(startPanel);
    }

    public void StartGame()
    {
        StartFlowRoutine(ShowDayIntroThenStudio());
    }

    public void EnterInfoCollection()
    {
        ShowOnly(infoCollectionPanel);
    }

    public void CompleteCollection()
    {
        ShowOnly(infoOrganizationPanel);
    }

    public void StartBroadcast()
    {
        ShowOnly(broadcastPanel);
    }

    public void CompleteBroadcast()
    {
        StartFlowRoutine(ReturnToStudioAfterBroadcast());
    }

    public void EnterNextDay()
    {
        StartFlowRoutine(EnterNextDayRoutine());
    }

    public void OpenSettings()
    {
        ShowOnly(settingsPanel);
    }

    public void ReturnToMainMenu()
    {
        ShowStartScreen();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetResolution(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= supportedResolutions.Length)
        {
            return;
        }

        Vector2Int resolution = supportedResolutions[optionIndex];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartFlowRoutine(IEnumerator routine)
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(routine);
    }

    private IEnumerator ShowDayIntroThenStudio()
    {
        yield return PlayTransition($"第 {currentDay} 天", 0.6f, 1f, 0.6f);
        UpdateStudioSituation($"第 {currentDay} 天 · 演播厅待命");
        SetStudioActionButtons(true, false);
        ShowOnly(studioPanel);
    }

    private IEnumerator ReturnToStudioAfterBroadcast()
    {
        yield return PlayTransition($"第 {currentDay} 天播报完成\n当前情况已更新", 0.9f, 1.8f, 0.9f);
        UpdateStudioSituation($"第 {currentDay} 天播报完成\n民众信任与地区混乱度已结算");
        SetStudioActionButtons(false, true);
        ShowOnly(studioPanel);
    }

    private IEnumerator EnterNextDayRoutine()
    {
        SetStudioActionButtons(false, false);
        currentDay++;
        if (infoCollectionController != null)
        {
            infoCollectionController.ResetForDay(currentDay);
        }
        yield return ShowDayIntroThenStudio();
    }

    private IEnumerator PlayTransition(string message, float fadeInDuration, float holdDuration, float fadeOutDuration)
    {
        ShowOnly(null);
        dayTransitionText.text = message;
        dayTransitionGroup.gameObject.SetActive(true);
        yield return FadeTransition(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return FadeTransition(1f, 0f, fadeOutDuration);
        dayTransitionGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeTransition(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dayTransitionGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        dayTransitionGroup.alpha = to;
    }

    private void UpdateStudioSituation(string text)
    {
        if (studioSituationText != null)
        {
            studioSituationText.text = text;
        }
    }

    private void SetStudioActionButtons(bool showInfoCollectionButton, bool showNextDayButton)
    {
        if (enterInfoCollectionButton != null)
        {
            enterInfoCollectionButton.gameObject.SetActive(showInfoCollectionButton);
        }

        if (nextDayButton != null)
        {
            nextDayButton.gameObject.SetActive(showNextDayButton);
        }
    }

    private void InitializeSettingsControls()
    {
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        if (resolutionDropdown != null)
        {
            int currentIndex = 0;
            for (int i = 0; i < supportedResolutions.Length; i++)
            {
                if (Screen.width == supportedResolutions[i].x && Screen.height == supportedResolutions[i].y)
                {
                    currentIndex = i;
                    break;
                }
            }

            resolutionDropdown.SetValueWithoutNotify(currentIndex);
        }
    }

    private void ShowOnly(GameObject targetPanel)
    {
        startPanel.SetActive(startPanel == targetPanel);
        if (targetPanel != studioPanel)
        {
            SetStudioActionButtons(false, false);
        }
        studioPanel.SetActive(studioPanel == targetPanel);
        infoCollectionPanel.SetActive(infoCollectionPanel == targetPanel);
        infoOrganizationPanel.SetActive(infoOrganizationPanel == targetPanel);
        broadcastPanel.SetActive(broadcastPanel == targetPanel);
        endingPanel.SetActive(endingPanel == targetPanel);
        settingsPanel.SetActive(settingsPanel == targetPanel);
    }
}
