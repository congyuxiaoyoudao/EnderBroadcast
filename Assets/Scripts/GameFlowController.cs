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
    [SerializeField] private TransitionTextJumpEffect dayTransitionTextJumpEffect;
    [SerializeField] private Text studioSituationText;
    [SerializeField] private Text studioStatusText;
    [SerializeField] private Button enterInfoCollectionButton;
    [SerializeField] private Button enterInfoOrganizationButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private RectTransform studioStatusTextRect;
    [SerializeField] private float broadcastTransitionFadeInDuration = 0.5f;
    [SerializeField] private float broadcastTransitionHoldDuration = 2f;
    [SerializeField] private float broadcastTransitionTextFadeDuration = 0.5f;
    [SerializeField] private float broadcastTransitionFadeOutDuration = 0.5f;
    [SerializeField] private InfoCollectionController infoCollectionController;
    [SerializeField] private RectTransform startTitleRect;
    [SerializeField] private CanvasGroup startTitleGroup;
    [SerializeField] private RectTransform startGameButtonRect;
    [SerializeField] private CanvasGroup startGameButtonGroup;
    [SerializeField] private RectTransform quitButtonRect;
    [SerializeField] private CanvasGroup quitButtonGroup;
    [SerializeField] private float startPanelFloatOffset = 150f;
    [SerializeField] private float startTitleAnimationDuration = 2.0f;
    [SerializeField] private float startButtonAnimationDuration = 1.5f;
    [SerializeField] private float startButtonAnimationDelay = 0.25f;

    private int currentDay = 1;
    private int publicTrust = 50;
    private int regionalChaos = 50;
    private bool collectionCompletedToday;
    private Coroutine flowRoutine;
    private Coroutine statusAnimationRoutine;
    private Coroutine startPanelAnimationRoutine;
    private Vector2 startTitleEndPosition;
    private Vector2 startGameButtonEndPosition;
    private Vector2 quitButtonEndPosition;
    private bool startPanelAnimationInitialized;

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
        CacheStartPanelAnimationEndPositions();
        ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        ShowOnly(startPanel);
        PlayStartPanelIntro();
    }

    public void StartGame()
    {
        StartFlowRoutine(ShowDayIntroThenStudio());
    }

    public void EnterInfoCollection()
    {
        ShowOnly(infoCollectionPanel);
    }

    public void ReturnToStudio()
    {
        UpdateStudioSituation($"第 {currentDay} 天 · 演播厅待命");
        SetStudioActionButtons(true, collectionCompletedToday, false);
        ShowOnly(studioPanel);
    }

    public void EnterInfoOrganization()
    {
        ShowOnly(infoOrganizationPanel);
    }

    public void CompleteCollection()
    {
        if (infoCollectionController != null)
        {
            infoCollectionController.LogCollectedInfoDebug();
        }
        collectionCompletedToday = true;
        SetStudioActionButtons(false, true, false);
        ShowOnly(infoOrganizationPanel);
    }

    public void StartBroadcast()
    {
        ShowOnly(broadcastPanel);
    }

    public void CompleteBroadcast()
    {
        StartFlowRoutine(CompleteBroadcastAndEnterNextDayRoutine());
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
        if (startPanelAnimationRoutine != null)
        {
            StopCoroutine(startPanelAnimationRoutine);
            startPanelAnimationRoutine = null;
        }

        SetStartElementAlpha(startTitleGroup, 1f);
        SetStartElementAlpha(startGameButtonGroup, 1f);
        SetStartElementAlpha(quitButtonGroup, 1f);
        RestoreStartPanelEndPositions();

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(routine);
    }

    private void CacheStartPanelAnimationEndPositions()
    {
        if (startPanelAnimationInitialized)
        {
            return;
        }

        if (startTitleRect != null)
        {
            startTitleEndPosition = startTitleRect.anchoredPosition;
        }

        if (startGameButtonRect != null)
        {
            startGameButtonEndPosition = startGameButtonRect.anchoredPosition;
        }

        if (quitButtonRect != null)
        {
            quitButtonEndPosition = quitButtonRect.anchoredPosition;
        }

        startPanelAnimationInitialized = true;
    }

    private void PlayStartPanelIntro()
    {
        CacheStartPanelAnimationEndPositions();
        if (startPanelAnimationRoutine != null)
        {
            StopCoroutine(startPanelAnimationRoutine);
        }

        startPanelAnimationRoutine = StartCoroutine(StartPanelIntroRoutine());
    }

    private IEnumerator StartPanelIntroRoutine()
    {
        SetStartElementAlpha(startTitleGroup, 0f);
        SetStartElementAlpha(startGameButtonGroup, 0f);
        SetStartElementAlpha(quitButtonGroup, 0f);
        SetStartElementPosition(startTitleRect, startTitleEndPosition - new Vector2(0f, startPanelFloatOffset));
        SetStartElementPosition(startGameButtonRect, startGameButtonEndPosition - new Vector2(0f, startPanelFloatOffset));
        SetStartElementPosition(quitButtonRect, quitButtonEndPosition - new Vector2(0f, startPanelFloatOffset));

        yield return AnimateStartElement(startTitleRect, startTitleGroup, startTitleEndPosition, startTitleAnimationDuration);
        yield return new WaitForSeconds(startButtonAnimationDelay);
        StartCoroutine(AnimateStartElement(startGameButtonRect, startGameButtonGroup, startGameButtonEndPosition, startButtonAnimationDuration));
        yield return new WaitForSeconds(startButtonAnimationDelay);
        yield return AnimateStartElement(quitButtonRect, quitButtonGroup, quitButtonEndPosition, startButtonAnimationDuration);
        startPanelAnimationRoutine = null;
    }

    private IEnumerator AnimateStartElement(RectTransform rectTransform, CanvasGroup group, Vector2 endPosition, float duration)
    {
        if (rectTransform == null || group == null)
        {
            yield break;
        }

        Vector2 startPosition = endPosition - new Vector2(0f, startPanelFloatOffset);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            group.alpha = t;
            yield return null;
        }

        rectTransform.anchoredPosition = endPosition;
        group.alpha = 1f;
    }

    private void RestoreStartPanelEndPositions()
    {
        SetStartElementPosition(startTitleRect, startTitleEndPosition);
        SetStartElementPosition(startGameButtonRect, startGameButtonEndPosition);
        SetStartElementPosition(quitButtonRect, quitButtonEndPosition);
    }

    private void SetStartElementPosition(RectTransform rectTransform, Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    private void SetStartElementAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    private IEnumerator ShowDayIntroThenStudio()
    {
        yield return PlayTransition($"第 {currentDay} 天", broadcastTransitionFadeInDuration, broadcastTransitionHoldDuration, broadcastTransitionFadeOutDuration);
        UpdateStudioSituation($"第 {currentDay} 天 · 演播厅待命");
        collectionCompletedToday = false;
        UpdateStudioStatus();
        SetStudioActionButtons(true, false, false);
        ShowOnly(studioPanel);
        PlayStudioStatusHighlight();
    }

    private IEnumerator CompleteBroadcastAndEnterNextDayRoutine()
    {
        ApplyDailyResult();
        if (currentDay >= 3)
        {
            SetStudioActionButtons(false, false, false);
            ShowOnly(null);
            dayTransitionText.text = string.Empty;
            SetTransitionTextAlpha(1f);
            dayTransitionGroup.gameObject.SetActive(true);
            yield return FadeTransition(0f, 1f, broadcastTransitionFadeInDuration);
            dayTransitionText.text = "结局";
            yield return PlayTransitionMessage(dayTransitionText.text);
            yield return new WaitForSeconds(broadcastTransitionHoldDuration);
            yield return FadeTransitionAndText(1f, 0f, broadcastTransitionFadeOutDuration);
            dayTransitionGroup.gameObject.SetActive(false);
            ShowOnly(endingPanel);
            yield break;
        }

        SetStudioActionButtons(false, false, false);
        ShowOnly(null);
        collectionCompletedToday = false;
        currentDay++;
        if (infoCollectionController != null)
        {
            infoCollectionController.ResetForDay(currentDay);
        }
        dayTransitionText.text = string.Empty;
        SetTransitionTextAlpha(1f);
        dayTransitionGroup.gameObject.SetActive(true);
        yield return FadeTransition(0f, 1f, broadcastTransitionFadeInDuration);
        dayTransitionText.text = $"第 {currentDay} 天";
        yield return PlayTransitionMessage(dayTransitionText.text);
        yield return new WaitForSeconds(broadcastTransitionHoldDuration);
        yield return FadeTransitionAndText(1f, 0f, broadcastTransitionFadeOutDuration);
        dayTransitionGroup.gameObject.SetActive(false);
        UpdateStudioSituation($"第 {currentDay} 天 · 演播厅待命");
        collectionCompletedToday = false;
        UpdateStudioStatus();
        SetStudioActionButtons(true, false, false);
        ShowOnly(studioPanel);
        PlayStudioStatusHighlight();
    }

    private IEnumerator EnterNextDayRoutine()
    {
        SetStudioActionButtons(false, false, false);
        collectionCompletedToday = false;
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
        yield return PlayTransitionText(message, fadeInDuration, holdDuration, fadeOutDuration);
    }

    private IEnumerator PlayTransitionText(string message, float fadeInDuration, float holdDuration, float fadeOutDuration)
    {
        dayTransitionText.text = string.Empty;
        SetTransitionTextAlpha(1f);
        dayTransitionGroup.gameObject.SetActive(true);
        yield return FadeTransition(0f, 1f, fadeInDuration);
        yield return PlayTransitionMessage(message);
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

    private IEnumerator PlayTransitionMessage(string message)
    {
        if (dayTransitionTextJumpEffect != null)
        {
            yield return dayTransitionTextJumpEffect.Play(message);
        }
        else
        {
            dayTransitionText.text = message;
        }
    }

    private IEnumerator FadeTransitionText(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetTransitionTextAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetTransitionTextAlpha(to);
    }

    private IEnumerator FadeTransitionAndText(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            dayTransitionGroup.alpha = alpha;
            SetTransitionTextAlpha(alpha);
            yield return null;
        }

        dayTransitionGroup.alpha = to;
        SetTransitionTextAlpha(to);
    }

    private void SetTransitionTextAlpha(float alpha)
    {
        Color color = dayTransitionText.color;
        color.a = alpha;
        dayTransitionText.color = color;
    }

    private void UpdateStudioSituation(string text)
    {
        if (studioSituationText != null)
        {
            studioSituationText.text = text;
        }
    }

    private void ApplyDailyResult()
    {
        if (infoCollectionController == null)
        {
            return;
        }

        NodeEffectData effects = infoCollectionController.GetCurrentTotalEffects();
        publicTrust = Mathf.Clamp(publicTrust + effects.trust, 0, 100);
        regionalChaos = Mathf.Clamp(regionalChaos + effects.chaos, 0, 100);
        UpdateStudioStatus();
        PlayStudioStatusHighlight();
    }

    private void UpdateStudioStatus()
    {
        if (studioStatusText != null)
        {
            studioStatusText.text = $"民众信任度：{publicTrust}\n地区混乱度：{regionalChaos}";
        }
    }

    private void PlayStudioStatusHighlight()
    {
        if (studioStatusTextRect == null)
        {
            return;
        }

        if (statusAnimationRoutine != null)
        {
            StopCoroutine(statusAnimationRoutine);
        }

        statusAnimationRoutine = StartCoroutine(StudioStatusHighlightRoutine());
    }

    private IEnumerator StudioStatusHighlightRoutine()
    {
        Vector3 normalScale = Vector3.one;
        Vector3 highlightScale = new Vector3(1.12f, 1.12f, 1f);
        yield return ScaleStatusBox(normalScale, highlightScale, 0.18f);
        yield return new WaitForSeconds(0.25f);
        yield return ScaleStatusBox(highlightScale, normalScale, 0.22f);
        studioStatusTextRect.localScale = normalScale;
        statusAnimationRoutine = null;
    }

    private IEnumerator ScaleStatusBox(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            studioStatusTextRect.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        studioStatusTextRect.localScale = to;
    }

    private void SetStudioActionButtons(bool showInfoCollectionButton, bool showInfoOrganizationButton, bool showNextDayButton)
    {
        if (enterInfoCollectionButton != null)
        {
            enterInfoCollectionButton.gameObject.SetActive(showInfoCollectionButton);
        }

        if (enterInfoOrganizationButton != null)
        {
            enterInfoOrganizationButton.gameObject.SetActive(showInfoOrganizationButton);
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
            SetStudioActionButtons(false, false, false);
        }
        studioPanel.SetActive(studioPanel == targetPanel);
        infoCollectionPanel.SetActive(infoCollectionPanel == targetPanel);
        infoOrganizationPanel.SetActive(infoOrganizationPanel == targetPanel);
        broadcastPanel.SetActive(broadcastPanel == targetPanel);
        endingPanel.SetActive(endingPanel == targetPanel);
        settingsPanel.SetActive(settingsPanel == targetPanel);
    }
}
