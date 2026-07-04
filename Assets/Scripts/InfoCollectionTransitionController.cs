using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InfoCollectionTransitionController : MonoBehaviour
{
    [SerializeField] private CollectionOrganizationWorkspace workspace;
    [SerializeField] private InfoCollectionController infoCollectionController;
    [SerializeField] private Button completeCollectionButton;
    [SerializeField] private RectTransform completeCollectionButtonRect;
    [SerializeField] private RectTransform organizationArea;
    [SerializeField] private RectTransform broadcastDraftContainer;
    [SerializeField] private RectTransform startBroadcastButtonRect;
    [SerializeField] private RectTransform broadcastTubeBaseRect;
    [SerializeField] private RectTransform handVisual;
    [SerializeField] private Rigidbody2D handBody;
    [SerializeField] private BoxCollider2D handCollider;
    [SerializeField] private TransitionPhysicsTarget[] pushTargets;
    [SerializeField] private float pixelsPerPhysicsUnit = 100f;
    [SerializeField] private float buttonSlideDuration = 0.2f;
    [SerializeField] private float pushDuration = 1.35f;
    [SerializeField] private float pullDuration = 0.55f;
    [SerializeField] private float startButtonSlideDuration = 0.25f;
    [SerializeField] private float handPushRotationOffset = -8f;
    [SerializeField] private float handPullRotationOffset = 0f;
    [SerializeField] private float handMotionWobble = 3f;
    [SerializeField] private float handPushEnterDuration = 0.55f;
    [SerializeField] private float handPushExitDuration = 0.3f;
    [SerializeField] private float handPushContactDuration = 0.28f;
    [SerializeField] private float handPushDriveDuration = 0.82f;
    [SerializeField] private float handPushReleaseDuration = 0.25f;
    [SerializeField] private float handPushImpactPauseDuration = 0.5f;
    [SerializeField] private float handPushImpactGap = 6f;
    [SerializeField, Range(0.05f, 0.4f)] private float handPushContactDistanceRatio = 0.16f;
    [SerializeField, Range(0.6f, 0.96f)] private float handPushReleaseDistanceRatio = 0.88f;
    [SerializeField] private float handPushVerticalBob = 10f;
    [SerializeField] private float handPushReleaseYOffset = -18f;
    [SerializeField] private AnimationCurve handPushContactCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve handPushDriveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve handPushReleaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float handPullEnterDuration = 0.24f;
    [SerializeField] private float handPullExitDuration = 0.18f;
    [SerializeField] private float betweenHandPhasesDelay = 0.08f;
    [SerializeField] private float handPushSweepY = 0f;
    [SerializeField, Range(-0.5f, 0.5f)] private float handPushReadyXRatio = 0.36f;
    [SerializeField] private float handPushReadyRightInset = 80f;
    [SerializeField, Range(0.5f, 1.3f)] private float handPushColliderHeightRatio = 0.95f;
    [SerializeField] private float handPushColliderWidth = 1.2f;
    [SerializeField, Range(-0.5f, 0.5f)] private float handPullGrabXRatio = -0.22f;
    [SerializeField] private float handPullGrabYOffset = -40f;
    [SerializeField] private float handPullStartBelowOffset = 140f;
    [SerializeField] private float handPullFollowYOffset = -120f;
    [SerializeField] private bool organizationDropBeforePull = true;
    [SerializeField] private Vector2 organizationDropPosition = new Vector2(0f, 381f);
    [SerializeField] private float organizationDropDuration = 0.45f;
    [SerializeField] private AnimationCurve organizationDropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool startButtonUsesCompleteButtonPosition = true;
    [SerializeField] private float startButtonCompletePositionYOffset = 0f;
    [SerializeField] private float startButtonHiddenYOffset = -120f;
    [SerializeField] private float startButtonInitialScale = 0.82f;
    [SerializeField] private float startButtonPopScale = 1.08f;
    [SerializeField] private float startButtonPopDuration = 0.16f;
    [SerializeField] private float tubeBaseHiddenYOffset = -160f;
    [SerializeField] private float tubeBaseInitialScale = 0.92f;
    [SerializeField] private float tubeBasePopScale = 1.02f;

    private Vector2 completeButtonStart;
    private Vector2 startBroadcastButtonStart;
    private Vector3 startBroadcastButtonScale;
    private Vector2 broadcastTubeBaseStart;
    private Vector3 broadcastTubeBaseScale;
    private Transform startBroadcastButtonOriginalParent;
    private int startBroadcastButtonOriginalSiblingIndex;
    private bool cachedStartBroadcastButtonParent;
    private Vector2 organizationShownPosition;
    private Vector2 handPushReadyPosition;
    private Quaternion handStartRotation;
    private Vector2 handColliderStartSize;
    private Vector2 handColliderStartOffset;
    private bool cached;
    private bool isPlaying;
    private Action onComplete;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (workspace == null)
        {
            workspace = GetComponentInParent<CollectionOrganizationWorkspace>();
        }
        if (infoCollectionController == null)
        {
            infoCollectionController = GetComponentInParent<InfoCollectionController>();
        }
        if (handCollider == null && handBody != null)
        {
            handCollider = handBody.GetComponent<BoxCollider2D>();
        }
        CacheLayout();
        ResetLayout();
    }

    public void PlayTransition(Action completed)
    {
        if (isPlaying)
        {
            return;
        }

        onComplete = completed;
        StartCoroutine(PlayRoutine());
    }

    public void ResetLayout()
    {
        CacheLayout();
        if (completeCollectionButtonRect != null)
        {
            completeCollectionButtonRect.anchoredPosition = completeButtonStart;
        }
        if (completeCollectionButton != null)
        {
            completeCollectionButton.interactable = true;
            completeCollectionButton.gameObject.SetActive(true);
        }
        if (startBroadcastButtonRect != null)
        {
            RestoreStartBroadcastButtonParent();
            startBroadcastButtonRect.anchoredPosition = startBroadcastButtonStart;
            startBroadcastButtonRect.localScale = startBroadcastButtonScale;
            startBroadcastButtonRect.gameObject.SetActive(false);
        }
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseRect.anchoredPosition = broadcastTubeBaseStart;
            broadcastTubeBaseRect.localScale = broadcastTubeBaseScale;
            broadcastTubeBaseRect.gameObject.SetActive(false);
        }
        if (organizationArea != null && workspace != null)
        {
            organizationArea.anchoredPosition = workspace.GetOrganizationHiddenPosition();
        }
        ResetPushedVisuals();
        if (handVisual != null)
        {
            handVisual.localRotation = handStartRotation;
            handVisual.gameObject.SetActive(false);
        }
        RestoreHandCollider();
        SetPhysicsTargetsActive(false);
    }

    private IEnumerator PlayRoutine()
    {
        isPlaying = true;
        CacheLayout();
        if (workspace != null)
        {
            workspace.PrepareCollectionToOrganizationTransition();
        }
        if (infoCollectionController != null)
        {
            infoCollectionController.SetPageButtonsHiddenForTransition(true);
        }
        if (completeCollectionButton != null)
        {
            completeCollectionButton.interactable = false;
        }
        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonRect.gameObject.SetActive(false);
        }
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseRect.gameObject.SetActive(false);
        }

        yield return SlideRect(completeCollectionButtonRect, completeButtonStart, completeButtonStart + new Vector2(0f, -140f), buttonSlideDuration);
        yield return PushCollectionObjects();
        SetPhysicsTargetsActive(false);
        if (betweenHandPhasesDelay > 0f)
        {
            yield return new WaitForSeconds(betweenHandPhasesDelay);
        }

        if (organizationArea != null && workspace != null)
        {
            organizationArea.gameObject.SetActive(true);
            Vector2 hidden = workspace.GetOrganizationHiddenPosition();
            Vector2 shown = workspace.GetOrganizationShownPosition();
            organizationArea.anchoredPosition = hidden;
            Vector2 pullStart = hidden;
            if (organizationDropBeforePull)
            {
                pullStart = organizationDropPosition;
                yield return DropOrganizationArea(hidden, pullStart);
            }
            yield return PullOrganizationArea(pullStart, shown);
        }

        if (handVisual != null)
        {
            yield return ExitHandAfterPull();
        }

        yield return PopInBroadcastControls();

        isPlaying = false;
        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private IEnumerator PushCollectionObjects()
    {
        RectTransform root = transform as RectTransform;
        float width = root != null ? root.rect.width : 1280f;
        float height = root != null ? root.rect.height : 720f;
        float readyX = handVisual != null ? handPushReadyPosition.x : width * handPushReadyXRatio - handPushReadyRightInset;
        Vector2 hidden = new Vector2(readyX, -height * 0.5f - GetHandHalfHeight() - 80f);
        Vector2 ready = handVisual != null ? handPushReadyPosition : new Vector2(readyX, handPushSweepY);
        Vector2 end = new Vector2(-width * 0.5f - GetHandHalfWidth() - 160f, ready.y);

        ConfigureHandPushCollider(height);
        SetHandAnchoredPosition(hidden);
        SetHandRotation(GetHandPushRotation());
        if (handVisual != null)
        {
            handVisual.gameObject.SetActive(true);
        }
        yield return SlideHand(hidden, ready, handPushEnterDuration, GetHandPushRotation(), GetHandPushRotation());

        SetPhysicsTargetsActive(false);
        SyncPhysicsFromVisuals();

        float contactRatio = Mathf.Clamp(handPushContactDistanceRatio, 0.05f, 0.4f);
        float contactDuration = handPushContactDuration > 0f ? handPushContactDuration : pushDuration * 0.2f;
        float driveDuration = handPushDriveDuration > 0f ? handPushDriveDuration : pushDuration * 0.6f;
        float releaseDuration = handPushReleaseDuration > 0f ? handPushReleaseDuration : pushDuration * 0.2f;

        PushSnapshot snapshot = CapturePushSnapshot(root, ready, end, contactRatio);
        yield return PushRightTargetsToImpact(snapshot, contactDuration);
        yield return HoldPushImpact(snapshot);
        yield return PushAllTargetsOffscreen(snapshot, driveDuration, releaseDuration);

        SetHandAnchoredPosition(snapshot.exitEnd);
        yield return SlideHand(snapshot.exitEnd, snapshot.exitEnd + new Vector2(-140f, -40f), handPushExitDuration, GetHandPushRotation(), GetHandPushRotation());
        if (handVisual != null)
        {
            handVisual.gameObject.SetActive(false);
        }
    }

    private IEnumerator PushHandSegment(
        Vector2 from,
        Vector2 to,
        float duration,
        AnimationCurve curve,
        float progressFrom,
        float progressTo,
        float wobbleScale)
    {
        if (duration <= 0f)
        {
            ApplyPushHandPose(to, progressTo, wobbleScale);
            SyncVisualsFromPhysics();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float curvedT = EvaluateCurve(curve, rawT);
            float progress = Mathf.Lerp(progressFrom, progressTo, curvedT);
            Vector2 position = GetPushHandSegmentPosition(from, to, curvedT, progress);
            ApplyPushHandPose(position, progress, wobbleScale);
            SyncVisualsFromPhysics();
            yield return null;
        }

        ApplyPushHandPose(GetPushHandSegmentPosition(from, to, 1f, progressTo), progressTo, wobbleScale);
        SyncVisualsFromPhysics();
    }

    private Vector2 GetPushHandSegmentPosition(Vector2 from, Vector2 to, float t, float progress)
    {
        Vector2 position = Vector2.Lerp(from, to, t);
        position.y += Mathf.Sin(progress * Mathf.PI) * handPushVerticalBob;
        return position;
    }

    private void ApplyPushHandPose(Vector2 position, float progress, float wobbleScale)
    {
        SetHandAnchoredPosition(position);
        float wobble = Mathf.Sin(progress * Mathf.PI) * handMotionWobble * wobbleScale;
        SetHandRotation(GetHandPushRotation(wobble));
    }

    private PushSnapshot CapturePushSnapshot(RectTransform root, Vector2 ready, Vector2 fallbackEnd, float contactRatio)
    {
        PushSnapshot snapshot = new PushSnapshot();
        snapshot.ready = ready;
        snapshot.exitEnd = fallbackEnd;
        snapshot.targets = pushTargets ?? Array.Empty<TransitionPhysicsTarget>();
        snapshot.startPositions = new Vector2[snapshot.targets.Length];
        snapshot.impactPositions = new Vector2[snapshot.targets.Length];
        snapshot.exitPositions = new Vector2[snapshot.targets.Length];
        snapshot.isRightTarget = new bool[snapshot.targets.Length];

        if (snapshot.targets.Length == 0 || root == null)
        {
            snapshot.impactHand = Vector2.Lerp(ready, fallbackEnd, contactRatio);
            return snapshot;
        }

        float averageCenterX = 0f;
        int validCount = 0;
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            RectTransform visual = snapshot.targets[i].Visual;
            if (visual == null)
            {
                continue;
            }

            snapshot.startPositions[i] = visual.anchoredPosition;
            snapshot.impactPositions[i] = snapshot.startPositions[i];
            snapshot.exitPositions[i] = snapshot.startPositions[i];
            Bounds bounds = GetTargetBoundsInRoot(root, visual);
            averageCenterX += bounds.center.x;
            validCount++;
        }

        if (validCount == 0)
        {
            snapshot.impactHand = Vector2.Lerp(ready, fallbackEnd, contactRatio);
            return snapshot;
        }

        averageCenterX /= validCount;
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            RectTransform visual = snapshot.targets[i].Visual;
            if (visual == null)
            {
                continue;
            }

            Bounds bounds = GetTargetBoundsInRoot(root, visual);
            snapshot.isRightTarget[i] = bounds.center.x >= averageCenterX;
        }

        float rightMoveDistance = CalculateRightTargetsImpactDistance(root, snapshot);
        Vector2 impactOffset = new Vector2(-rightMoveDistance, 0f);
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            if (snapshot.isRightTarget[i])
            {
                snapshot.impactPositions[i] = snapshot.startPositions[i] + impactOffset;
            }
        }

        snapshot.impactHand = ready + impactOffset;
        float exitOffsetX = CalculateExitOffset(root, snapshot);
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            snapshot.exitPositions[i] = snapshot.impactPositions[i] + new Vector2(exitOffsetX, 0f);
        }
        snapshot.exitEnd = snapshot.impactHand + new Vector2(exitOffsetX, handPushReleaseYOffset);
        return snapshot;
    }

    private float CalculateRightTargetsImpactDistance(RectTransform root, PushSnapshot snapshot)
    {
        float moveDistance = 0f;
        bool foundPair = false;

        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            if (!snapshot.isRightTarget[i] || snapshot.targets[i].Visual == null)
            {
                continue;
            }

            Bounds rightBounds = GetTargetBoundsInRoot(root, snapshot.targets[i].Visual);
            int leftIndex = FindClosestLeftTargetByY(root, snapshot, rightBounds.center.y);
            if (leftIndex < 0)
            {
                continue;
            }

            float rightContactX = GetPushContactX(root, snapshot.targets[i], true);
            float leftContactX = GetPushContactX(root, snapshot.targets[leftIndex], false);
            float distance = Mathf.Max(0f, rightContactX - leftContactX + handPushImpactGap);
            moveDistance = foundPair ? Mathf.Max(moveDistance, distance) : distance;
            foundPair = true;
        }

        if (!foundPair)
        {
            RectTransform handRoot = transform as RectTransform;
            float fallbackWidth = handRoot != null ? handRoot.rect.width : 1280f;
            return fallbackWidth * Mathf.Clamp(handPushContactDistanceRatio, 0.05f, 0.4f);
        }

        return moveDistance;
    }

    private int FindClosestLeftTargetByY(RectTransform root, PushSnapshot snapshot, float y)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            if (snapshot.isRightTarget[i] || snapshot.targets[i].Visual == null)
            {
                continue;
            }

            Bounds bounds = GetTargetBoundsInRoot(root, snapshot.targets[i].Visual);
            float distance = Mathf.Abs(bounds.center.y - y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float CalculateExitOffset(RectTransform root, PushSnapshot snapshot)
    {
        float maxRight = float.MinValue;
        for (int i = 0; i < snapshot.targets.Length; i++)
        {
            RectTransform visual = snapshot.targets[i].Visual;
            if (visual == null)
            {
                continue;
            }

            Vector2 originalPosition = visual.anchoredPosition;
            visual.anchoredPosition = snapshot.impactPositions[i];
            Bounds bounds = GetTargetBoundsInRoot(root, visual);
            visual.anchoredPosition = originalPosition;
            maxRight = Mathf.Max(maxRight, bounds.max.x);
        }

        if (maxRight == float.MinValue)
        {
            return snapshot.exitEnd.x - snapshot.impactHand.x;
        }

        float rootLeft = -root.rect.width * 0.5f;
        return rootLeft - maxRight - 160f;
    }

    private IEnumerator PushRightTargetsToImpact(PushSnapshot snapshot, float duration)
    {
        if (duration <= 0f)
        {
            ApplySnapshotPositions(snapshot.impactPositions);
            ApplyPushHandPose(snapshot.impactHand, 0.25f, 0.45f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float t = EvaluateCurve(handPushContactCurve, rawT);
            ApplyInterpolatedTargetPositions(snapshot.startPositions, snapshot.impactPositions, t);
            ApplyPushHandPose(Vector2.Lerp(snapshot.ready, snapshot.impactHand, t), t * 0.25f, 0.45f);
            yield return null;
        }

        ApplySnapshotPositions(snapshot.impactPositions);
        ApplyPushHandPose(snapshot.impactHand, 0.25f, 0.45f);
    }

    private IEnumerator HoldPushImpact(PushSnapshot snapshot)
    {
        float duration = Mathf.Max(0f, handPushImpactPauseDuration);
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplySnapshotPositions(snapshot.impactPositions);
            ApplyPushHandPose(snapshot.impactHand, 0.3f, 0.15f);
            yield return null;
        }
    }

    private IEnumerator PushAllTargetsOffscreen(PushSnapshot snapshot, float driveDuration, float releaseDuration)
    {
        float totalDuration = Mathf.Max(0.01f, driveDuration + releaseDuration);
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / totalDuration);
            float t = rawT < 0.78f
                ? EvaluateCurve(handPushDriveCurve, rawT / 0.78f) * 0.78f
                : 0.78f + EvaluateCurve(handPushReleaseCurve, (rawT - 0.78f) / 0.22f) * 0.22f;
            ApplyInterpolatedTargetPositions(snapshot.impactPositions, snapshot.exitPositions, t);
            ApplyPushHandPose(Vector2.Lerp(snapshot.impactHand, snapshot.exitEnd, t), 0.3f + t * 0.7f, 1f);
            yield return null;
        }

        ApplySnapshotPositions(snapshot.exitPositions);
        ApplyPushHandPose(snapshot.exitEnd, 1f, 0.45f);
    }

    private void ApplyInterpolatedTargetPositions(Vector2[] from, Vector2[] to, float t)
    {
        if (pushTargets == null)
        {
            return;
        }

        int count = Mathf.Min(pushTargets.Length, Mathf.Min(from.Length, to.Length));
        for (int i = 0; i < count; i++)
        {
            RectTransform visual = pushTargets[i].Visual;
            if (visual != null)
            {
                visual.anchoredPosition = Vector2.Lerp(from[i], to[i], t);
            }
        }
    }

    private void ApplySnapshotPositions(Vector2[] positions)
    {
        if (pushTargets == null)
        {
            return;
        }

        int count = Mathf.Min(pushTargets.Length, positions.Length);
        for (int i = 0; i < count; i++)
        {
            RectTransform visual = pushTargets[i].Visual;
            if (visual != null)
            {
                visual.anchoredPosition = positions[i];
            }
        }
    }

    private Bounds GetTargetBoundsInRoot(RectTransform root, RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 min = root.InverseTransformPoint(corners[0]);
        Vector3 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 point = root.InverseTransformPoint(corners[i]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
        return bounds;
    }

    private float GetPushContactX(RectTransform root, TransitionPhysicsTarget target, bool isRightTarget)
    {
        RectTransform marker = target.ContactMarker;
        if (marker != null)
        {
            Vector3 worldPoint = marker.TransformPoint(marker.rect.center);
            return root.InverseTransformPoint(worldPoint).x;
        }

        Bounds bounds = GetTargetBoundsInRoot(root, target.Visual);
        return isRightTarget ? bounds.min.x : bounds.max.x;
    }

    private float EvaluateCurve(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0)
        {
            return Mathf.SmoothStep(0f, 1f, t);
        }

        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private IEnumerator PullOrganizationArea(Vector2 hidden, Vector2 shown)
    {
        RectTransform root = transform as RectTransform;
        float width = root != null ? root.rect.width : 1280f;
        float height = root != null ? root.rect.height : 720f;
        Vector2 hiddenGrab = GetBroadcastDraftGrabPoint(hidden, width, height);
        Vector2 shownGrab = GetBroadcastDraftGrabPoint(shown, width, height);
        Vector2 handGrabHidden = hiddenGrab + new Vector2(0f, handPullFollowYOffset);
        Vector2 handGrabShown = shownGrab + new Vector2(0f, handPullFollowYOffset);
        Vector2 handStart = new Vector2(handGrabHidden.x, -height * 0.5f - GetHandHalfHeight() - handPullStartBelowOffset);

        if (handVisual != null)
        {
            handVisual.gameObject.SetActive(true);
        }
        yield return SlideHand(handStart, handGrabHidden, handPullEnterDuration, GetHandPullRotation(), GetHandPullRotation());

        float elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / pullDuration));
            Vector2 position = Vector2.Lerp(hidden, shown, t);
            if (organizationArea != null)
            {
                organizationArea.anchoredPosition = position;
            }
            SetHandAnchoredPosition(Vector2.Lerp(handGrabHidden, handGrabShown, t));
            SetHandRotation(GetHandPullRotation(-Mathf.Sin(t * Mathf.PI) * handMotionWobble * 0.5f));
            yield return null;
        }

        if (organizationArea != null)
        {
            organizationArea.anchoredPosition = shown;
        }
    }

    private IEnumerator DropOrganizationArea(Vector2 from, Vector2 to)
    {
        if (organizationArea == null)
        {
            yield break;
        }

        if (organizationDropDuration <= 0f)
        {
            organizationArea.anchoredPosition = to;
            yield break;
        }

        float elapsed = 0f;
        organizationArea.anchoredPosition = from;
        while (elapsed < organizationDropDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / organizationDropDuration);
            float t = EvaluateCurve(organizationDropCurve, rawT);
            organizationArea.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        organizationArea.anchoredPosition = to;
    }

    private Vector2 GetBroadcastDraftGrabPoint(Vector2 organizationPosition, float fallbackWidth, float fallbackHeight)
    {
        if (organizationArea == null)
        {
            return new Vector2(fallbackWidth * handPullGrabXRatio, fallbackHeight * 0.35f + handPullGrabYOffset);
        }

        Vector2 originalPosition = organizationArea.anchoredPosition;
        organizationArea.anchoredPosition = organizationPosition;

        Vector2 grabPoint;
        if (broadcastDraftContainer != null && transform is RectTransform root)
        {
            Vector3[] corners = new Vector3[4];
            broadcastDraftContainer.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0];
            Vector3 bottomRight = corners[3];
            Vector3 worldPoint = Vector3.Lerp(bottomLeft, bottomRight, Mathf.Clamp01(handPullGrabXRatio + 0.5f));
            worldPoint += new Vector3(0f, handPullGrabYOffset, 0f);
            grabPoint = WorldToRootAnchored(root, worldPoint);
        }
        else
        {
            grabPoint = organizationPosition + new Vector2(fallbackWidth * handPullGrabXRatio, handPullGrabYOffset);
        }

        organizationArea.anchoredPosition = originalPosition;
        return grabPoint;
    }

    private Vector2 GetStartBroadcastButtonShownPosition()
    {
        if (!startButtonUsesCompleteButtonPosition || startBroadcastButtonRect == null || completeCollectionButtonRect == null)
        {
            return startBroadcastButtonStart;
        }

        RectTransform targetParent = startBroadcastButtonRect.parent as RectTransform;
        if (targetParent == null)
        {
            return startBroadcastButtonStart;
        }

        Vector2 oldPosition = completeCollectionButtonRect.anchoredPosition;
        completeCollectionButtonRect.anchoredPosition = completeButtonStart;
        Vector3 worldPoint = completeCollectionButtonRect.TransformPoint(completeCollectionButtonRect.rect.center);
        completeCollectionButtonRect.anchoredPosition = oldPosition;

        Vector3 localPoint = targetParent.InverseTransformPoint(worldPoint);
        return new Vector2(localPoint.x, localPoint.y + startButtonCompletePositionYOffset);
    }

    private Vector2 WorldToRootAnchored(RectTransform root, Vector3 worldPoint)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private IEnumerator PopInStartButton(Vector2 hiddenPosition, Vector2 shownPosition)
    {
        if (startBroadcastButtonRect == null)
        {
            yield break;
        }

        Vector3 smallScale = startBroadcastButtonScale * startButtonInitialScale;
        Vector3 popScale = startBroadcastButtonScale * startButtonPopScale;
        startBroadcastButtonRect.anchoredPosition = hiddenPosition;
        startBroadcastButtonRect.localScale = smallScale;

        float elapsed = 0f;
        while (elapsed < startButtonSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / startButtonSlideDuration));
            startBroadcastButtonRect.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, t);
            startBroadcastButtonRect.localScale = Vector3.Lerp(smallScale, startBroadcastButtonScale, t);
            yield return null;
        }

        startBroadcastButtonRect.anchoredPosition = shownPosition;
        yield return ScaleRect(startBroadcastButtonRect, startBroadcastButtonScale, popScale, startButtonPopDuration * 0.45f);
        yield return ScaleRect(startBroadcastButtonRect, popScale, startBroadcastButtonScale, startButtonPopDuration * 0.55f);
    }

    private IEnumerator PopInBroadcastControls()
    {
        if (startBroadcastButtonRect == null && broadcastTubeBaseRect == null)
        {
            yield break;
        }

        Vector2 shownButton = Vector2.zero;
        Vector2 hiddenButton = Vector2.zero;
        Vector3 smallButtonScale = Vector3.one;
        Vector3 popButtonScale = Vector3.one;
        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonRect.gameObject.SetActive(true);
            startBroadcastButtonRect.SetAsLastSibling();
            shownButton = GetStartBroadcastButtonShownPosition();
            hiddenButton = shownButton + new Vector2(0f, startButtonHiddenYOffset);
            smallButtonScale = startBroadcastButtonScale * startButtonInitialScale;
            popButtonScale = startBroadcastButtonScale * startButtonPopScale;
            startBroadcastButtonRect.anchoredPosition = hiddenButton;
            startBroadcastButtonRect.localScale = smallButtonScale;
        }

        Vector2 shownTube = Vector2.zero;
        Vector2 hiddenTube = Vector2.zero;
        Vector3 smallTubeScale = Vector3.one;
        Vector3 popTubeScale = Vector3.one;
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseRect.gameObject.SetActive(true);
            broadcastTubeBaseRect.SetAsLastSibling();
            shownTube = broadcastTubeBaseStart;
            hiddenTube = shownTube + new Vector2(0f, tubeBaseHiddenYOffset);
            smallTubeScale = broadcastTubeBaseScale * tubeBaseInitialScale;
            popTubeScale = broadcastTubeBaseScale * tubeBasePopScale;
            broadcastTubeBaseRect.anchoredPosition = hiddenTube;
            broadcastTubeBaseRect.localScale = smallTubeScale;
        }
        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonRect.SetAsLastSibling();
        }

        float elapsed = 0f;
        while (elapsed < startButtonSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / startButtonSlideDuration));
            if (startBroadcastButtonRect != null)
            {
                startBroadcastButtonRect.anchoredPosition = Vector2.Lerp(hiddenButton, shownButton, t);
                startBroadcastButtonRect.localScale = Vector3.Lerp(smallButtonScale, startBroadcastButtonScale, t);
            }
            if (broadcastTubeBaseRect != null)
            {
                broadcastTubeBaseRect.anchoredPosition = Vector2.Lerp(hiddenTube, shownTube, t);
                broadcastTubeBaseRect.localScale = Vector3.Lerp(smallTubeScale, broadcastTubeBaseScale, t);
            }
            yield return null;
        }

        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonRect.anchoredPosition = shownButton;
            startBroadcastButtonRect.localScale = startBroadcastButtonScale;
        }
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseRect.anchoredPosition = shownTube;
            broadcastTubeBaseRect.localScale = broadcastTubeBaseScale;
        }

        float popOutDuration = startButtonPopDuration * 0.45f;
        float popBackDuration = startButtonPopDuration * 0.55f;
        yield return ScaleBroadcastControls(startBroadcastButtonScale, popButtonScale, broadcastTubeBaseScale, popTubeScale, popOutDuration);
        yield return ScaleBroadcastControls(popButtonScale, startBroadcastButtonScale, popTubeScale, broadcastTubeBaseScale, popBackDuration);
    }

    private IEnumerator ScaleBroadcastControls(Vector3 buttonFrom, Vector3 buttonTo, Vector3 tubeFrom, Vector3 tubeTo, float duration)
    {
        if (duration <= 0f)
        {
            if (startBroadcastButtonRect != null)
            {
                startBroadcastButtonRect.localScale = buttonTo;
            }
            if (broadcastTubeBaseRect != null)
            {
                broadcastTubeBaseRect.localScale = tubeTo;
            }
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (startBroadcastButtonRect != null)
            {
                startBroadcastButtonRect.localScale = Vector3.Lerp(buttonFrom, buttonTo, t);
            }
            if (broadcastTubeBaseRect != null)
            {
                broadcastTubeBaseRect.localScale = Vector3.Lerp(tubeFrom, tubeTo, t);
            }
            yield return null;
        }

        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonRect.localScale = buttonTo;
        }
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseRect.localScale = tubeTo;
        }
    }

    private IEnumerator ScaleRect(RectTransform rect, Vector3 from, Vector3 to, float duration)
    {
        if (rect == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            rect.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        rect.localScale = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rect.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        rect.localScale = to;
    }

    private IEnumerator ExitHandAfterPull()
    {
        RectTransform root = transform as RectTransform;
        float height = root != null ? root.rect.height : 720f;
        Vector2 from = handVisual != null ? handVisual.anchoredPosition : Vector2.zero;
        Vector2 to = from + new Vector2(0f, -height * 0.35f - GetHandHalfHeight());
        yield return SlideHand(from, to, handPullExitDuration, GetHandPullRotation(), GetHandPullRotation());
        if (handVisual != null)
        {
            handVisual.gameObject.SetActive(false);
            handVisual.localRotation = handStartRotation;
        }
    }

    private IEnumerator SlideHand(Vector2 from, Vector2 to, float duration, Quaternion fromRotation, Quaternion toRotation)
    {
        float elapsed = 0f;
        SetHandAnchoredPosition(from);
        SetHandRotation(fromRotation);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetHandAnchoredPosition(Vector2.Lerp(from, to, t));
            SetHandRotation(Quaternion.Lerp(fromRotation, toRotation, t));
            yield return null;
        }
        SetHandAnchoredPosition(to);
        SetHandRotation(toRotation);
    }

    private IEnumerator SlideRect(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        if (rect == null)
        {
            yield break;
        }

        rect.anchoredPosition = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private void CacheLayout()
    {
        if (cached)
        {
            return;
        }
        if (completeCollectionButtonRect != null)
        {
            completeButtonStart = completeCollectionButtonRect.anchoredPosition;
        }
        if (startBroadcastButtonRect != null)
        {
            startBroadcastButtonStart = startBroadcastButtonRect.anchoredPosition;
            startBroadcastButtonScale = startBroadcastButtonRect.localScale;
            if (!cachedStartBroadcastButtonParent)
            {
                startBroadcastButtonOriginalParent = startBroadcastButtonRect.parent;
                startBroadcastButtonOriginalSiblingIndex = startBroadcastButtonRect.GetSiblingIndex();
                cachedStartBroadcastButtonParent = true;
            }
        }
        if (broadcastTubeBaseRect != null)
        {
            broadcastTubeBaseStart = broadcastTubeBaseRect.anchoredPosition;
            broadcastTubeBaseScale = broadcastTubeBaseRect.localScale;
        }
        if (organizationArea != null)
        {
            organizationShownPosition = organizationArea.anchoredPosition;
        }
        if (handVisual != null)
        {
            handPushReadyPosition = handVisual.anchoredPosition;
            handStartRotation = handVisual.localRotation;
        }
        CachePushedVisuals();
        if (handCollider != null)
        {
            handColliderStartSize = handCollider.size;
            handColliderStartOffset = handCollider.offset;
        }
        cached = true;
    }

    private void SetHandAnchoredPosition(Vector2 position)
    {
        if (handVisual != null)
        {
            handVisual.anchoredPosition = position;
        }
        if (handBody != null)
        {
            Vector2 bodyPosition = position / pixelsPerPhysicsUnit;
            if (handBody.simulated)
            {
                handBody.MovePosition(bodyPosition);
            }
            else
            {
                handBody.position = bodyPosition;
            }
        }
    }

    private void SetHandRotation(Quaternion rotation)
    {
        if (handVisual != null)
        {
            handVisual.localRotation = rotation;
        }
        if (handBody != null)
        {
            float angle = rotation.eulerAngles.z;
            if (handBody.simulated)
            {
                handBody.MoveRotation(angle);
            }
            else
            {
                handBody.rotation = angle;
            }
        }
    }

    private Quaternion GetHandPushRotation(float extraOffset = 0f)
    {
        return handStartRotation * Quaternion.Euler(0f, 0f, handPushRotationOffset + extraOffset);
    }

    private Quaternion GetHandPullRotation(float extraOffset = 0f)
    {
        return handStartRotation * Quaternion.Euler(0f, 0f, handPullRotationOffset + extraOffset);
    }

    private float GetHandHalfWidth()
    {
        return handVisual != null ? handVisual.rect.width * Mathf.Abs(handVisual.localScale.x) * 0.5f : 120f;
    }

    private float GetHandHalfHeight()
    {
        return handVisual != null ? handVisual.rect.height * Mathf.Abs(handVisual.localScale.y) * 0.5f : 180f;
    }

    private void ConfigureHandPushCollider(float rootHeight)
    {
        if (handCollider == null)
        {
            return;
        }

        float height = Mathf.Max(handColliderStartSize.y, rootHeight * handPushColliderHeightRatio / pixelsPerPhysicsUnit);
        float width = Mathf.Max(handColliderStartSize.x, handPushColliderWidth);
        handCollider.size = new Vector2(width, height);
        handCollider.offset = Vector2.zero;
    }

    private void RestoreHandCollider()
    {
        if (handCollider == null)
        {
            return;
        }

        handCollider.size = handColliderStartSize;
        handCollider.offset = handColliderStartOffset;
    }

    private void SyncPhysicsFromVisuals()
    {
        if (pushTargets == null)
        {
            return;
        }
        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].CacheStart();
            pushTargets[i].SyncBodyFromVisual(pixelsPerPhysicsUnit);
        }
    }

    private void CachePushedVisuals()
    {
        if (pushTargets == null)
        {
            return;
        }

        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].CacheStart();
        }
    }

    private void ResetPushedVisuals()
    {
        if (pushTargets == null)
        {
            return;
        }

        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].ResetVisual();
        }
    }

    private void SyncVisualsFromPhysics()
    {
        if (pushTargets == null)
        {
            return;
        }
        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].SyncVisualFromBody(pixelsPerPhysicsUnit);
        }
    }

    private void ResetPushedVisualsOffscreen()
    {
        if (pushTargets == null)
        {
            return;
        }
        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].MoveVisualOffscreen();
        }
    }

    private void SetPhysicsTargetsActive(bool active)
    {
        if (handBody != null)
        {
            handBody.simulated = active;
        }
        if (pushTargets == null)
        {
            return;
        }
        for (int i = 0; i < pushTargets.Length; i++)
        {
            pushTargets[i].SetSimulated(active);
        }
    }

    private void MoveStartBroadcastButtonToPopupLayer()
    {
        if (startBroadcastButtonRect == null || transform == null)
        {
            return;
        }

        startBroadcastButtonRect.SetParent(transform, false);
        startBroadcastButtonRect.SetAsLastSibling();
    }

    private void RestoreStartBroadcastButtonParent()
    {
        if (startBroadcastButtonRect == null || startBroadcastButtonOriginalParent == null)
        {
            return;
        }

        if (startBroadcastButtonRect.parent != startBroadcastButtonOriginalParent)
        {
            startBroadcastButtonRect.SetParent(startBroadcastButtonOriginalParent, false);
        }

        int siblingIndex = Mathf.Clamp(startBroadcastButtonOriginalSiblingIndex, 0, startBroadcastButtonOriginalParent.childCount - 1);
        startBroadcastButtonRect.SetSiblingIndex(siblingIndex);
    }

    private class PushSnapshot
    {
        public TransitionPhysicsTarget[] targets;
        public Vector2[] startPositions;
        public Vector2[] impactPositions;
        public Vector2[] exitPositions;
        public bool[] isRightTarget;
        public Vector2 ready;
        public Vector2 impactHand;
        public Vector2 exitEnd;
    }
}

[Serializable]
public class TransitionPhysicsTarget
{
    [SerializeField] private RectTransform visual;
    [SerializeField] private RectTransform contactMarker;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private float rotationLimit = 8f;

    private Vector2 startPosition;
    private Quaternion startRotation;

    public RectTransform Visual => visual;
    public RectTransform ContactMarker => contactMarker;
    public Rigidbody2D Body => body;

    public TransitionPhysicsTarget(RectTransform visual, Rigidbody2D body)
    {
        this.visual = visual;
        this.body = body;
    }

    public void CacheStart()
    {
        if (visual == null)
        {
            return;
        }
        startPosition = visual.anchoredPosition;
        startRotation = visual.localRotation;
    }

    public void SyncBodyFromVisual(float pixelsPerPhysicsUnit)
    {
        if (visual == null || body == null)
        {
            return;
        }
        body.position = visual.anchoredPosition / pixelsPerPhysicsUnit;
        body.rotation = visual.localEulerAngles.z;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    public void SyncVisualFromBody(float pixelsPerPhysicsUnit)
    {
        if (visual == null || body == null)
        {
            return;
        }
        visual.anchoredPosition = body.position * pixelsPerPhysicsUnit;
        float angle = Mathf.Clamp(Mathf.DeltaAngle(0f, body.rotation), -rotationLimit, rotationLimit);
        visual.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void MoveVisualOffscreen()
    {
        if (visual == null)
        {
            return;
        }
        visual.anchoredPosition = startPosition + new Vector2(-1400f, 0f);
        visual.localRotation = Quaternion.identity;
    }

    public void ResetVisual()
    {
        if (visual == null)
        {
            return;
        }
        visual.anchoredPosition = startPosition;
        visual.localRotation = startRotation;
    }

    public void SetSimulated(bool simulated)
    {
        if (body != null)
        {
            body.simulated = simulated;
        }
    }
}
