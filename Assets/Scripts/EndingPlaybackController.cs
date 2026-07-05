using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingPlaybackController : MonoBehaviour
{
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Image endingImage;
    [SerializeField] private RectTransform endingImageRect;
    [SerializeField] private Sprite badEndingSprite;
    [SerializeField] private Sprite normalEndingSprite;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.1f;
    [SerializeField] private float lineRevealCharactersPerSecond = 13f;
    [SerializeField] private float slowLineRevealCharactersPerSecond = 8f;
    [SerializeField] private float finalTitleCharactersPerSecond = 4f;
    [SerializeField] private float finalTitleBounceHeight = 18f;
    [SerializeField] private float finalTitleJumpDuration = 0.22f;
    [SerializeField] private float finalSubtitleFadeDuration = 3.5f;
    [SerializeField] private float finalTitleHoldDuration = 5f;
    [SerializeField] private float imageIntroFadeDuration = 0.75f;
    [SerializeField] private float imageIntroMoveDelay = 0.75f;
    [SerializeField] private Vector2 imageIntroStartPosition = new Vector2(-1f, 15f);
    [SerializeField] private Vector2 imageIntroStartSize = new Vector2(1276.377f, 940.4882f);
    [SerializeField] private Vector2 imageIntroEndPosition = new Vector2(456.7698f, -1f);
    [SerializeField] private Vector2 imageIntroEndSize = new Vector2(862.9437f, 635.8532f);
    [SerializeField] private float imageIntroDuration = 3f;
    [SerializeField] private float subtitleClearMargin = 0f;

    private readonly string[] badEndingSentences =
    {
        "暴雪依然驻足凛港，用寒冷侵蚀着这座城市。",
        "人们不再期待收音机里的声音，转而将猜忌投向彼此。",
        "玻璃碎片在地上堆积，又被雪掩埋。",
        "虚伪的电波无法连接人心，最后的喧闹之后，一切都将回归沉默。",
        "Bad End  长终之寂"
    };

    private readonly string[] normalEndingSentences =
    {
        "白色的城市里，人们等来了希望。",
        "塔的电波倾听一切，倾诉一切。",
        "积雪上的脚印越来越多，但现实并未改变。",
        "塔的权威，或许超越了它本该有的模样。",
        "可极光本身，又能否长久呢？",
        "Normal End  极光"
    };

    private Coroutine playbackRoutine;
    private Coroutine imageIntroRoutine;
    private Color endingImageBaseColor = Color.white;

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

        if (imageIntroRoutine != null)
        {
            StopCoroutine(imageIntroRoutine);
            imageIntroRoutine = null;
        }

        SetEndingImageAlpha(1f);
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
        bool showBadEnding = gameFlowController != null && gameFlowController.ShouldShowBadEnding;
        if (endingImage != null)
        {
            endingImage.sprite = showBadEnding ? badEndingSprite : normalEndingSprite;
            endingImage.preserveAspect = true;
            endingImageBaseColor = endingImage.color;
        }

        if (endingImageRect == null && endingImage != null)
        {
            endingImageRect = endingImage.rectTransform;
        }

        if (subtitleGroup != null)
        {
            subtitleGroup.alpha = 0f;
        }

        if (endingImageRect != null)
        {
            imageIntroRoutine = StartCoroutine(PlayImageIntro());
            yield return WaitUntilImageClearsSubtitle();
        }

        string[] endingSentences = showBadEnding ? badEndingSentences : normalEndingSentences;
        for (int i = 0; i < endingSentences.Length; i++)
        {
            bool isFinalSentence = i == endingSentences.Length - 1;
            if (isFinalSentence)
            {
                yield return PlayFinalTitle(endingSentences[i]);
                continue;
            }

            bool isSlowSentence = i == endingSentences.Length - 2;
            float revealSpeed = isSlowSentence ? slowLineRevealCharactersPerSecond : lineRevealCharactersPerSecond;
            yield return PlayRevealedLine(endingSentences[i], revealSpeed, holdDuration);
        }

        if (imageIntroRoutine != null)
        {
            yield return imageIntroRoutine;
        }

        playbackRoutine = null;
        if (gameFlowController != null)
        {
            gameFlowController.ReturnToMainMenu();
        }
    }

    private IEnumerator PlayRevealedLine(string line, float charactersPerSecond, float holdAfterReveal)
    {
        if (subtitleText == null)
        {
            yield return new WaitForSeconds(holdAfterReveal);
            yield break;
        }

        subtitleText.text = string.Empty;
        if (subtitleGroup != null)
        {
            subtitleGroup.alpha = 1f;
        }

        float speed = Mathf.Max(1f, charactersPerSecond);
        float duration = line.Length / speed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int visibleCount = Mathf.Clamp(Mathf.FloorToInt(elapsed * speed), 0, line.Length);
            subtitleText.text = line.Substring(0, visibleCount);
            yield return null;
        }

        subtitleText.text = line;
        yield return new WaitForSeconds(holdAfterReveal);
        if (subtitleGroup != null)
        {
            yield return FadeSubtitle(1f, 0f);
        }
    }

    private IEnumerator PlayFinalTitle(string line)
    {
        if (subtitleText == null)
        {
            yield return new WaitForSeconds(finalTitleHoldDuration);
            yield break;
        }

        subtitleText.text = string.Empty;
        if (subtitleGroup != null)
        {
            subtitleGroup.alpha = 1f;
        }

        SplitFinalTitle(line, out string title, out string separator, out string subtitle);
        TransitionTextJumpEffect jumpEffect = subtitleText.GetComponent<TransitionTextJumpEffect>();
        if (jumpEffect == null)
        {
            jumpEffect = subtitleText.gameObject.AddComponent<TransitionTextJumpEffect>();
        }

        float characterInterval = 1f / Mathf.Max(1f, finalTitleCharactersPerSecond);
        yield return jumpEffect.Play(title, characterInterval, finalTitleJumpDuration, finalTitleBounceHeight);
        subtitleText.text = title + separator;
        yield return FadeFinalSubtitle(title, separator, subtitle);
        subtitleText.text = line;
        yield return new WaitForSeconds(finalTitleHoldDuration);
    }

    private void SplitFinalTitle(string line, out string title, out string separator, out string subtitle)
    {
        if (line.StartsWith("Bad End  "))
        {
            title = "Bad End";
            separator = "  ";
            subtitle = line.Substring("Bad End  ".Length);
            return;
        }

        if (line.StartsWith("Normal End  "))
        {
            title = "Normal End";
            separator = "  ";
            subtitle = line.Substring("Normal End  ".Length);
            return;
        }

        int dashIndex = line.IndexOf("——");
        if (dashIndex >= 0)
        {
            title = line.Substring(0, dashIndex);
            separator = " ";
            subtitle = line.Substring(dashIndex + 2);
            return;
        }

        title = line;
        separator = string.Empty;
        subtitle = string.Empty;
    }

    private IEnumerator FadeFinalSubtitle(string title, string separator, string subtitle)
    {
        if (string.IsNullOrEmpty(subtitle))
        {
            yield break;
        }

        Color baseColor = subtitleText.color;
        float duration = Mathf.Max(0.01f, finalSubtitleFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            subtitleText.text = title + separator + ToColorTag(subtitle, baseColor, alpha);
            yield return null;
        }
    }

    private string ToColorTag(string text, Color color, float alpha)
    {
        Color taggedColor = color;
        taggedColor.a *= Mathf.Clamp01(alpha);
        return "<color=#" + ColorUtility.ToHtmlStringRGBA(taggedColor) + ">" + text + "</color>";
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

    private IEnumerator PlayImageIntro()
    {
        endingImageRect.anchoredPosition = imageIntroStartPosition;
        endingImageRect.sizeDelta = imageIntroStartSize;
        SetEndingImageAlpha(0f);

        float delayElapsed = 0f;
        float moveDelay = Mathf.Max(0f, imageIntroMoveDelay);
        float duration = Mathf.Max(0.01f, imageIntroDuration);
        float fadeDuration = Mathf.Max(0.01f, imageIntroFadeDuration);
        while (delayElapsed < moveDelay)
        {
            delayElapsed += Time.deltaTime;
            SetEndingImageAlpha(Mathf.Clamp01(delayElapsed / fadeDuration));
            yield return null;
        }

        SetEndingImageAlpha(1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            endingImageRect.sizeDelta = Vector2.LerpUnclamped(imageIntroStartSize, imageIntroEndSize, normalized);
            endingImageRect.anchoredPosition = Vector2.LerpUnclamped(imageIntroStartPosition, imageIntroEndPosition, normalized);

            yield return null;
        }

        endingImageRect.anchoredPosition = imageIntroEndPosition;
        endingImageRect.sizeDelta = imageIntroEndSize;
        SetEndingImageAlpha(1f);
        imageIntroRoutine = null;
    }

    private void SetEndingImageAlpha(float alpha)
    {
        if (endingImage == null)
        {
            return;
        }

        Color color = endingImageBaseColor;
        color.a *= Mathf.Clamp01(alpha);
        endingImage.color = color;
    }

    private IEnumerator WaitUntilImageClearsSubtitle()
    {
        RectTransform subtitleRect = subtitleGroup != null ? subtitleGroup.transform as RectTransform : null;
        if (endingImageRect == null || subtitleRect == null)
        {
            yield break;
        }

        while (imageIntroRoutine != null && !IsImageClearOfSubtitle(subtitleRect))
        {
            yield return null;
        }
    }

    private bool IsImageClearOfSubtitle(RectTransform subtitleRect)
    {
        Vector3[] imageCorners = new Vector3[4];
        Vector3[] subtitleCorners = new Vector3[4];
        endingImageRect.GetWorldCorners(imageCorners);
        subtitleRect.GetWorldCorners(subtitleCorners);

        float imageLeft = imageCorners[0].x;
        float subtitleRight = subtitleCorners[2].x;
        return imageLeft >= subtitleRight + subtitleClearMargin;
    }
}
