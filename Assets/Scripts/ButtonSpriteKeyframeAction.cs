using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonSpriteKeyframeAction : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform targetTransform;
    [SerializeField] private Sprite frame1;
    [SerializeField] private Sprite frame2;
    [SerializeField] private Sprite frame3;
    [SerializeField] private float frame2Delay = 0.3f;
    [SerializeField] private float frame3Delay = 0.4f;
    [SerializeField] private float finalFrameHoldDelay = 0.2f;
    [SerializeField] private float frame1Scale = 1f;
    [SerializeField] private float frame2Scale = 1.2f;
    [SerializeField] private float frame3Scale = 1.2f;
    [SerializeField] private float frame1Rotation = 0f;
    [SerializeField] private float frame2Rotation = -10f;
    [SerializeField] private float frame3Rotation = 10f;
    [SerializeField] private Vector2 frame1PositionOffset = Vector2.zero;
    [SerializeField] private Vector2 frame2PositionOffset = Vector2.zero;
    [SerializeField] private Vector2 frame3PositionOffset = Vector2.zero;
    [SerializeField] private Vector2 frame2StrikeStartOffset = Vector2.zero;
    [SerializeField] private float frame2StrikeStartScale = 1.35f;
    [SerializeField] private float frame2StrikeDropDelay = 0.05f;
    [SerializeField] private Vector2 frame3StrikeStartOffset = Vector2.zero;
    [SerializeField] private float frame3StrikeStartScale = 1.2f;
    [SerializeField] private float frame3StrikeDropDelay = 0f;
    [SerializeField] private UnityEvent onFinished;

    private Coroutine routine;
    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Vector2 initialAnchoredPosition;
    private bool cachedTransform;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (targetTransform == null && targetImage != null)
        {
            targetTransform = targetImage.rectTransform;
        }

        CacheTransform();
    }

    private void OnEnable()
    {
        ResetToFirstFrame();
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetButtonInteractable(true);
        ResetToFirstFrame();
    }

    public void Play()
    {
        if (routine != null)
        {
            return;
        }

        routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetButtonInteractable(false);

        ApplyFrame(frame1, frame1Scale, frame1Rotation, frame1PositionOffset);

        float frame2Wait = frame2Delay - Mathf.Max(0f, frame2StrikeDropDelay);
        if (frame2Wait > 0f)
        {
            yield return new WaitForSeconds(frame2Wait);
        }

        ApplyFrame(frame2, frame2StrikeStartScale, frame2Rotation, frame2StrikeStartOffset);
        if (frame2StrikeDropDelay > 0f)
        {
            yield return new WaitForSeconds(frame2StrikeDropDelay);
        }

        ApplyFrame(frame2, frame2Scale, frame2Rotation, frame2PositionOffset);

        float frame3Wait = frame3Delay - Mathf.Max(0f, frame3StrikeDropDelay);
        if (frame3Wait > 0f)
        {
            yield return new WaitForSeconds(frame3Wait);
        }

        ApplyFrame(frame3, frame3StrikeStartScale, frame3Rotation, frame3StrikeStartOffset);
        if (frame3StrikeDropDelay > 0f)
        {
            yield return new WaitForSeconds(frame3StrikeDropDelay);
        }

        ApplyFrame(frame3, frame3Scale, frame3Rotation, frame3PositionOffset);

        if (finalFrameHoldDelay > 0f)
        {
            yield return new WaitForSeconds(finalFrameHoldDelay);
        }

        routine = null;
        SetButtonInteractable(true);
        onFinished?.Invoke();
    }

    private void ResetToFirstFrame()
    {
        ApplyFrame(frame1, frame1Scale, frame1Rotation, frame1PositionOffset);
    }

    private void ApplyFrame(Sprite frame, float scaleMultiplier, float rotationZ, Vector2 positionOffset)
    {
        SetFrame(frame);
        SetTransform(scaleMultiplier, rotationZ, positionOffset);
    }

    private void SetFrame(Sprite frame)
    {
        if (targetImage != null && frame != null)
        {
            targetImage.sprite = frame;
        }
    }

    private void SetTransform(float scaleMultiplier, float rotationZ, Vector2 positionOffset)
    {
        CacheTransform();
        if (targetTransform == null)
        {
            return;
        }

        targetTransform.anchoredPosition = initialAnchoredPosition + positionOffset;
        targetTransform.localScale = initialScale * scaleMultiplier;
        targetTransform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, rotationZ);
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void CacheTransform()
    {
        if (cachedTransform || targetTransform == null)
        {
            return;
        }

        initialScale = targetTransform.localScale;
        initialRotation = targetTransform.localRotation;
        initialAnchoredPosition = targetTransform.anchoredPosition;
        cachedTransform = true;
    }

}
