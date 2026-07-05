using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BroadcastPlaybackController : MonoBehaviour
{
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private Text subtitleText;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float holdDuration = 0.9f;
    [SerializeField] private GameFlowController gameFlowController;

    private readonly string[] sampleSentences =
    {
        "这里是曙光电台。",
        "我们已确认旧城区出现连续异常事件。",
        "请所有居民保持警惕，等待下一次官方通知。",
        "........."
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

    public void StartPlayback()
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
        string[] sentences = GetPlaybackSentences();
        for (int i = 0; i < sentences.Length; i++)
        {
            subtitleText.text = sentences[i];
            yield return FadeSubtitle(0f, 1f);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeSubtitle(1f, 0f);
        }

        playbackRoutine = null;
        yield return new WaitForSeconds(1.5f);
        if (gameFlowController != null)
        {
            gameFlowController.CompleteBroadcast();
        }
    }

    private string[] GetPlaybackSentences()
    {
        string broadcastText = gameFlowController != null ? gameFlowController.GetCurrentBroadcastText() : string.Empty;
        if (string.IsNullOrWhiteSpace(broadcastText))
        {
            return sampleSentences;
        }

        List<string> sentences = new List<string>();
        int sentenceStart = 0;
        for (int i = 0; i < broadcastText.Length; i++)
        {
            char current = broadcastText[i];
            if (current == '。' || current == '！' || current == '？' || current == '；' || current == '.' || current == '!' || current == '?')
            {
                string sentence = broadcastText.Substring(sentenceStart, i - sentenceStart + 1).Trim();
                if (!string.IsNullOrEmpty(sentence))
                {
                    sentences.Add(sentence);
                }
                sentenceStart = i + 1;
            }
        }

        if (sentenceStart < broadcastText.Length)
        {
            string sentence = broadcastText.Substring(sentenceStart).Trim();
            if (!string.IsNullOrEmpty(sentence))
            {
                sentences.Add(sentence);
            }
        }

        return sentences.Count > 0 ? sentences.ToArray() : sampleSentences;
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
