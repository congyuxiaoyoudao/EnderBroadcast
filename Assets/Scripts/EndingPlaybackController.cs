using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingPlaybackController : MonoBehaviour
{
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private Text subtitleText;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.1f;

    private readonly string[] endingSentences =
    {
        "三天的播报结束了。",
        "城市在你的选择中留下了新的秩序。",
        "明天是否仍会有人收听，已经不再确定。"
    };

    private Coroutine playbackRoutine;

    private void OnEnable()
    {
        if (gameFlowController == null)
        {
            gameFlowController = FindObjectOfType<GameFlowController>();
        }
        StartPlayback();
    }

    private void OnDisable()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
    }

    private void StartPlayback()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
        }

        playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    private IEnumerator PlaybackRoutine()
    {
        subtitleGroup.alpha = 0f;
        for (int i = 0; i < endingSentences.Length; i++)
        {
            subtitleText.text = endingSentences[i];
            yield return FadeSubtitle(0f, 1f);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeSubtitle(1f, 0f);
        }

        playbackRoutine = null;
        yield return new WaitForSeconds(2f);
        if (gameFlowController != null)
        {
            gameFlowController.ReturnToMainMenu();
        }
    }

    private IEnumerator FadeSubtitle(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            subtitleGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        subtitleGroup.alpha = to;
    }
}
