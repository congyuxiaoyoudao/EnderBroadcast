using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoCollectionController : MonoBehaviour
{
    [SerializeField] private BroadcastDayData currentDay = new BroadcastDayData();
    [SerializeField] private Transform documentListRoot;
    [SerializeField] private Button documentButtonTemplate;
    [SerializeField] private Sprite[] documentButtonSprites;
    [SerializeField] private Vector2 documentButtonSize = new Vector2(70f, 300f);
    [SerializeField] private Vector2[] documentButtonPositions =
    {
        new Vector2(-36f, 0f),
        new Vector2(0f, 8f),
        new Vector2(36f, 0f)
    };
    [SerializeField] private float[] documentButtonRotations = { -4f, 0f, 4f };
    [SerializeField] private bool hideDocumentButtonLabels = true;
    [SerializeField] private Text documentTitleText;
    [SerializeField] private Text documentBodyText;
    [SerializeField] private Transform documentSegmentRoot;
    [SerializeField] private Text documentSegmentTemplate;
    [SerializeField] private Transform infoHotspotRoot;
    [SerializeField] private Image infoHotspotTemplate;
    [SerializeField] private Transform collectedInfoRoot;
    [SerializeField] private GameObject collectedInfoNoteTemplate;
    [SerializeField] private Sprite[] collectedInfoNoteSprites;
    [SerializeField] private Text documentTooltipText;
    [SerializeField] private AudioClipEditingController audioClipEditingController;
    [SerializeField] private Button previousDocumentPageButton;
    [SerializeField] private Button nextDocumentPageButton;
    [SerializeField] private RectTransform documentPageAnimatedRect;
    [SerializeField] private RectTransform[] documentBackPages;
    [SerializeField] private RectTransform documentPagePreviewRoot;
    [SerializeField] private float documentPageFlipOffset = 120f;
    [SerializeField] private float documentPageFlipSeparation = 40f;
    [SerializeField] private float documentPageFlipDuration = 0.18f;

    private readonly List<Button> documentButtons = new List<Button>();
    private readonly List<Text> collectedInfoTexts = new List<Text>();
    private readonly Dictionary<string, GameObject> collectedInfoNotes = new Dictionary<string, GameObject>();
    private readonly List<Text> documentSegmentTexts = new List<Text>();
    private readonly List<int> documentSegmentPages = new List<int>();
    private readonly List<InfoNodeData> documentSegmentInfoNodes = new List<InfoNodeData>();
    private readonly List<Text> documentPagePreviewTexts = new List<Text>();
    private readonly List<List<Text>> documentBackPagePreviewTexts = new List<List<Text>>();
    private readonly List<RectTransform> documentPagePanels = new List<RectTransform>();
    private readonly List<RectTransform> documentPagePanelBaseRefs = new List<RectTransform>();
    private readonly List<Vector2> documentPagePanelBasePositions = new List<Vector2>();
    private readonly List<float> documentPagePanelBaseRotations = new List<float>();
    private readonly List<InfoHotspotHandler> infoHotspots = new List<InfoHotspotHandler>();
    private readonly HashSet<string> collectedInfoIds = new HashSet<string>();
    private readonly List<InfoTextRange> currentInfoRanges = new List<InfoTextRange>();
    private InfoHotspotHandler hoveredHotspot;
    private int selectedDocumentIndex = -1;
    private int currentDocumentPage;
    private int documentPageCount = 1;
    private bool isDocumentPageAnimating;
    private Vector2 documentPageRestPosition;
    private int documentPageRestSiblingIndex = -1;
    private bool documentPageRestPositionCached;
    private Coroutine documentAnimationRoutine;
    private CollectedBoardInteractionMode collectedBoardMode = CollectedBoardInteractionMode.Collection;

    public bool CanCancelCollectedNotes => collectedBoardMode == CollectedBoardInteractionMode.Collection;

    private void Awake()
    {
        DisableDocumentAutoLayout();
        EnsureSampleData();
        if (audioClipEditingController == null)
        {
            audioClipEditingController = GetComponentInChildren<AudioClipEditingController>(true);
        }
        InitializeDocumentTextClickHandler();
        InitializeDocumentPageControls();
        ClearTooltip();
        ClearDocumentView();
        BuildDocumentList();
    }

    public void SelectDocument(int documentIndex)
    {
        if (documentIndex < 0 || documentIndex >= currentDay.envelope.documents.Count)
        {
            return;
        }

        DocumentData document = currentDay.envelope.documents[documentIndex];
        selectedDocumentIndex = documentIndex;
        currentDocumentPage = 0;
        UpdateDocumentButtons();
        documentTitleText.text = document.displayName;
        documentBodyText.text = string.Empty;
        BuildDocumentSegments(document);
        PlayDocumentEnterAnimation();
    }

    public void SetDocumentHover(int documentIndex, bool isHovered)
    {
        if (documentIndex < 0 || documentIndex >= documentButtons.Count || documentIndex == selectedDocumentIndex)
        {
            return;
        }

        Image image = documentButtons[documentIndex].GetComponent<Image>();
        if (image != null)
        {
            image.color = isHovered ? new Color(1f, 0.94f, 0.72f, 1f) : Color.white;
        }
    }

    public void HoverInfo(InfoHotspotHandler hotspot)
    {
    }

    public void ClearHoveredInfo(InfoHotspotHandler hotspot)
    {
    }

    public void ShowDocumentTooltip(int documentIndex)
    {
        if (documentIndex < 0 || documentIndex >= currentDay.envelope.documents.Count)
        {
            return;
        }

        DocumentData document = currentDay.envelope.documents[documentIndex];
        documentTooltipText.text = $"{document.displayName}\nID: {document.id}\n信息节点: {document.infoNodes.Count}";
        documentTooltipText.gameObject.SetActive(true);
    }

    public void ClearTooltip()
    {
        documentTooltipText.text = string.Empty;
        documentTooltipText.gameObject.SetActive(false);
    }

    private void BuildDocumentList()
    {
        InitializeDocumentButtonPoolFromScene();
        RecycleDocumentButtons();

        for (int i = 0; i < currentDay.envelope.documents.Count; i++)
        {
            DocumentData document = currentDay.envelope.documents[i];
            Button button = GetDocumentButton(i);
            Text buttonLabel = button.GetComponentInChildren<Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = document.displayName;
                buttonLabel.gameObject.SetActive(!hideDocumentButtonLabels);
            }
            ConfigureDocumentButtonAppearance(button, i);

            int documentIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectDocument(documentIndex));

            DocumentHoverHandler hoverHandler = button.GetComponent<DocumentHoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = button.gameObject.AddComponent<DocumentHoverHandler>();
            }
            hoverHandler.Initialize(this, documentIndex);
            SetDocumentHover(documentIndex, false);
        }

        UpdateDocumentButtons();
    }

    private void UpdateDocumentButtons()
    {
        for (int i = 0; i < documentButtons.Count; i++)
        {
            documentButtons[i].gameObject.SetActive(i != selectedDocumentIndex);
        }

        RectTransform listRect = documentListRoot as RectTransform;
        if (listRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }
    }

    public void CollectInfo(InfoNodeData infoNode)
    {
        if (!collectedInfoIds.Add(infoNode.id))
        {
            return;
        }

        currentDay.broadcastResult.collectedInfoNodeIds.Add(infoNode.id);
        currentDay.broadcastResult.totalEffects.trust += infoNode.effects.trust;
        currentDay.broadcastResult.totalEffects.chaos += infoNode.effects.chaos;

        GameObject note = CreateCollectedNote(infoNode.id, infoNode.extractedText);
        CollectedInfoNoteHandler handler = note.GetComponent<CollectedInfoNoteHandler>();
        if (handler == null)
        {
            handler = note.AddComponent<CollectedInfoNoteHandler>();
        }
        handler.Initialize(this, infoNode);
    }

    public void ResetForDay(int dayIndex)
    {
        currentDay = CreateSampleDayData(dayIndex);
        selectedDocumentIndex = -1;
        collectedInfoIds.Clear();
        currentInfoRanges.Clear();
        SetCollectedBoardMode(CollectedBoardInteractionMode.Collection);

        foreach (GameObject note in collectedInfoNotes.Values)
        {
            Destroy(note);
        }
        collectedInfoNotes.Clear();

        ClearTooltip();
        ClearDocumentView();
        BuildDocumentList();

        if (audioClipEditingController != null)
        {
            audioClipEditingController.ResetForDay();
        }
    }

    public void LogCollectedInfoDebug()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine($"[信息收集完成] 当前天数: 第 {currentDay.dayIndex} 天");
        builder.AppendLine("收集到的文本信息:");

        if (currentDay.broadcastResult.collectedInfoNodeIds.Count == 0)
        {
            builder.AppendLine("- 无");
        }
        else
        {
            for (int i = 0; i < currentDay.broadcastResult.collectedInfoNodeIds.Count; i++)
            {
                InfoNodeData node = FindInfoNode(currentDay.broadcastResult.collectedInfoNodeIds[i]);
                if (node != null)
                {
                    builder.AppendLine($"- {node.id}: {node.extractedText}");
                }
            }
        }

        builder.AppendLine("收集到的音频信息:");
        if (currentDay.broadcastResult.selectedAudioNodeIds.Count == 0)
        {
            builder.AppendLine("- 无");
        }
        else
        {
            List<AudioNodeData> audioNodes = FindAudioNodes(currentDay.broadcastResult.selectedAudioNodeIds);
            builder.AppendLine($"- {GetAudioClipDescription(FindAudioTrackId(audioNodes), audioNodes)}");
            for (int i = 0; i < audioNodes.Count; i++)
            {
                builder.AppendLine($"  {i + 1}. {audioNodes[i].id}: {audioNodes[i].contentText}");
            }
        }

        Debug.Log(builder.ToString());
    }

    private InfoNodeData FindInfoNode(string infoNodeId)
    {
        for (int i = 0; i < currentDay.envelope.documents.Count; i++)
        {
            DocumentData document = currentDay.envelope.documents[i];
            for (int j = 0; j < document.infoNodes.Count; j++)
            {
                if (document.infoNodes[j].id == infoNodeId)
                {
                    return document.infoNodes[j];
                }
            }
        }

        return null;
    }

    private List<AudioNodeData> FindAudioNodes(List<string> audioNodeIds)
    {
        List<AudioNodeData> nodes = new List<AudioNodeData>();
        for (int i = 0; i < audioNodeIds.Count; i++)
        {
            AudioNodeData node = FindAudioNode(audioNodeIds[i]);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    private AudioNodeData FindAudioNode(string audioNodeId)
    {
        for (int i = 0; i < currentDay.envelope.audioTracks.Count; i++)
        {
            AudioTrackData track = currentDay.envelope.audioTracks[i];
            for (int j = 0; j < track.audioNodes.Count; j++)
            {
                if (track.audioNodes[j].id == audioNodeId)
                {
                    return track.audioNodes[j];
                }
            }
        }

        return null;
    }

    private string FindAudioTrackId(List<AudioNodeData> audioNodes)
    {
        if (audioNodes.Count == 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < currentDay.envelope.audioTracks.Count; i++)
        {
            AudioTrackData track = currentDay.envelope.audioTracks[i];
            if (track.audioNodes.Contains(audioNodes[0]))
            {
                return track.id;
            }
        }

        return string.Empty;
    }

    public NodeEffectData GetCurrentTotalEffects()
    {
        return currentDay.broadcastResult.totalEffects;
    }

    public void SetCollectedBoardMode(CollectedBoardInteractionMode mode)
    {
        collectedBoardMode = mode;
    }

    public IReadOnlyList<AudioTrackData> GetAudioTracks()
    {
        return currentDay.envelope.audioTracks;
    }

    public bool IsAudioTrackCollected(string audioTrackId)
    {
        return collectedInfoIds.Contains(GetAudioNoteId(audioTrackId));
    }

    public void CollectAudioNodes(string audioTrackId, List<AudioNodeData> audioNodes)
    {
        if (audioNodes.Count == 0)
        {
            return;
        }

        string noteId = GetAudioNoteId(audioTrackId);
        if (!collectedInfoIds.Add(noteId))
        {
            return;
        }

        currentDay.broadcastResult.selectedAudioNodeIds.Clear();
        for (int i = 0; i < audioNodes.Count; i++)
        {
            currentDay.broadcastResult.selectedAudioNodeIds.Add(audioNodes[i].id);
        }

        string description = GetAudioClipDescription(audioTrackId, audioNodes);
        GameObject note = CreateCollectedNote(noteId, description, new Color(0.62f, 0.86f, 0.48f, 1f));
        CollectedInfoNoteHandler handler = note.GetComponent<CollectedInfoNoteHandler>();
        if (handler == null)
        {
            handler = note.AddComponent<CollectedInfoNoteHandler>();
        }
        handler.InitializeAudio(this, noteId);
    }

    private string GetAudioClipDescription(string audioTrackId, List<AudioNodeData> audioNodes)
    {
        if (audioNodes.Count == 1)
        {
            return "平平无奇的音频";
        }

        if (audioNodes.Count == 2)
        {
            return "略带消极的音频";
        }

        AudioTrackData track = currentDay.envelope.audioTracks.Find(item => item.id == audioTrackId);
        if (track != null && track.audioNodes.Count >= 3 && audioNodes.Count == 3 &&
            audioNodes[0].id == track.audioNodes[2].id &&
            audioNodes[1].id == track.audioNodes[1].id &&
            audioNodes[2].id == track.audioNodes[0].id)
        {
            return "充满希望的音频";
        }

        return "平平无奇的音频";
    }

    private GameObject CreateCollectedNote(string noteId, string noteText, Color? noteColor = null)
    {
        GameObject note = Instantiate(collectedInfoNoteTemplate, collectedInfoRoot);
        Text text = note.GetComponentInChildren<Text>();
        text.text = noteText;
        text.color = new Color(0.12f, 0.11f, 0.1f, 1f);

        Image noteImage = note.GetComponent<Image>();
        if (noteImage != null)
        {
            if (!noteColor.HasValue && collectedInfoNoteSprites != null && collectedInfoNoteSprites.Length > 0)
            {
                noteImage.sprite = collectedInfoNoteSprites[Random.Range(0, collectedInfoNoteSprites.Length)];
            }
            noteImage.color = noteColor ?? new Color(1f, 1f, 1f, 1f);
        }

        collectedInfoNotes.Add(noteId, note);
        note.SetActive(true);
        return note;
    }

    public void CancelCollectedInfo(InfoNodeData infoNode)
    {
        if (!collectedInfoIds.Remove(infoNode.id))
        {
            return;
        }

        currentDay.broadcastResult.collectedInfoNodeIds.Remove(infoNode.id);
        currentDay.broadcastResult.totalEffects.trust -= infoNode.effects.trust;
        currentDay.broadcastResult.totalEffects.chaos -= infoNode.effects.chaos;

        if (collectedInfoNotes.TryGetValue(infoNode.id, out GameObject note))
        {
            collectedInfoNotes.Remove(infoNode.id);
            Destroy(note);
        }
    }

    private string GetAudioNoteId(string audioTrackId)
    {
        return "audio_track_" + audioTrackId;
    }

    public void CancelCollectedAudio(string noteId)
    {
        if (!collectedInfoIds.Remove(noteId))
        {
            return;
        }

        if (audioClipEditingController != null)
        {
            audioClipEditingController.RestoreSourceNodesForAudioNote(noteId);
        }

        currentDay.broadcastResult.selectedAudioNodeIds.Clear();
        if (collectedInfoNotes.TryGetValue(noteId, out GameObject note))
        {
            collectedInfoNotes.Remove(noteId);
            Destroy(note);
        }
    }

    public void ShowPreviousDocumentPage()
    {
        TryFlipDocumentPage(currentDocumentPage - 1, 1f);
    }

    public void ShowNextDocumentPage()
    {
        TryFlipDocumentPage(currentDocumentPage + 1, -1f);
    }

    private void TryFlipDocumentPage(int targetPage, float direction)
    {
        if (isDocumentPageAnimating || targetPage < 0 || targetPage >= documentPageCount)
        {
            return;
        }

        StartCoroutine(DocumentPageFlipRoutine(targetPage, direction));
    }

    private IEnumerator DocumentPageFlipRoutine(int targetPage, float direction)
    {
        RectTransform pageRect = documentPageAnimatedRect != null ? documentPageAnimatedRect : documentSegmentRoot.parent as RectTransform;
        if (pageRect == null)
        {
            currentDocumentPage = targetPage;
            UpdateDocumentPageVisibility();
            yield break;
        }

        CacheDocumentPageRestPosition();
        isDocumentPageAnimating = true;
        UpdateDocumentPageButtons();

        Vector2 restPosition = documentPageRestPosition;
        RectTransform movingPage = GetTopDocumentPagePanel();
        if (movingPage == null)
        {
            isDocumentPageAnimating = false;
            yield break;
        }

        float exitDistance = GetDocumentPageFlipExitDistance(movingPage);
        Vector2 exitPosition = restPosition + new Vector2(exitDistance * direction, 0f);
        yield return MoveDocumentPageOut(movingPage, movingPage.anchoredPosition, exitPosition, documentPageFlipDuration);
        MoveTopDocumentPageToBottom();
        Vector2 basePosition = GetDocumentPageBasePosition(movingPage);
        movingPage.anchoredPosition = new Vector2(exitPosition.x, basePosition.y);
        yield return MoveDocumentPage(movingPage, movingPage.anchoredPosition, basePosition, documentPageFlipDuration);

        currentDocumentPage = targetPage;
        UpdateDocumentPageVisibility();

        isDocumentPageAnimating = false;
        UpdateDocumentPageButtons();
    }

    private RectTransform GetTopDocumentPagePanel()
    {
        return documentPagePanels.Count > 0 ? documentPagePanels[0] : null;
    }

    private RectTransform GetBottomDocumentPagePanel()
    {
        return documentPagePanels.Count > 0 ? documentPagePanels[documentPagePanels.Count - 1] : null;
    }

    private void MoveTopDocumentPageToBottom()
    {
        if (documentPagePanels.Count <= 1)
        {
            return;
        }

        RectTransform movedPage = documentPagePanels[0];
        documentPagePanels.RemoveAt(0);
        documentPagePanels.Add(movedPage);
        UpdateDocumentPagePanelSiblingOrder();
    }

    private void MoveBottomDocumentPageToTop()
    {
        if (documentPagePanels.Count <= 1)
        {
            return;
        }

        int lastIndex = documentPagePanels.Count - 1;
        RectTransform movedPage = documentPagePanels[lastIndex];
        documentPagePanels.RemoveAt(lastIndex);
        documentPagePanels.Insert(0, movedPage);
        UpdateDocumentPagePanelSiblingOrder();
    }

    private void UpdateDocumentPagePanelSiblingOrder()
    {
        for (int i = documentPagePanels.Count - 1; i >= 0; i--)
        {
            RectTransform page = documentPagePanels[i];
            page.gameObject.SetActive(i < documentPageCount);
            page.SetSiblingIndex(Mathf.Max(0, documentPageRestSiblingIndex - i));
        }
    }

    private Vector2 GetDocumentPageBasePosition(RectTransform page)
    {
        int index = documentPagePanelBaseRefs.IndexOf(page);
        if (index >= 0 && index < documentPagePanelBasePositions.Count)
        {
            return documentPagePanelBasePositions[index];
        }

        return page.anchoredPosition;
    }

    private float GetDocumentPageBaseRotation(RectTransform page)
    {
        int index = documentPagePanelBaseRefs.IndexOf(page);
        if (index >= 0 && index < documentPagePanelBaseRotations.Count)
        {
            return documentPagePanelBaseRotations[index];
        }

        return page.localEulerAngles.z;
    }

    private void ApplyDocumentPagePanelInitialTransforms()
    {
        for (int i = documentPagePanels.Count - 1; i >= 0; i--)
        {
            RectTransform page = documentPagePanels[i];
            page.gameObject.SetActive(i < documentPageCount);
            page.anchoredPosition = GetDocumentPageBasePosition(page);
            page.localRotation = Quaternion.Euler(0f, 0f, GetDocumentPageBaseRotation(page));
            page.SetSiblingIndex(Mathf.Max(0, documentPageRestSiblingIndex - i));
        }
    }

    private Vector2 GetDocumentPageStackPosition(int index)
    {
        Vector2[] offsets = { Vector2.zero, new Vector2(12f, -12f), new Vector2(26f, -24f), new Vector2(-10f, -18f), new Vector2(18f, -30f) };
        return documentPageRestPosition + offsets[index % offsets.Length];
    }

    private float GetDocumentPageStackRotation(int index)
    {
        float[] rotations = { -0.5f, 1.2f, -1.8f, 0.8f, -1.1f };
        return rotations[index % rotations.Length];
    }

    private void SendDocumentPageBehindStack(RectTransform pageRect)
    {
        if (pageRect == null)
        {
            return;
        }

        int backPageCount = documentBackPages != null ? documentBackPages.Length : 0;
        int targetSiblingIndex = documentPageRestSiblingIndex >= 0 ? Mathf.Max(0, documentPageRestSiblingIndex - backPageCount) : 0;
        pageRect.SetSiblingIndex(targetSiblingIndex);
    }

    private float GetDocumentPageFlipExitDistance(RectTransform pageRect)
    {
        return pageRect != null ? pageRect.rect.width : documentPageFlipOffset;
    }

    private void PrepareDocumentPagePreview(int targetPage)
    {
        RectTransform previewRoot = GetDocumentPagePreviewRoot();
        if (previewRoot == null)
        {
            return;
        }

        int previewIndex = 0;
        for (int i = 0; i < documentSegmentTexts.Count; i++)
        {
            if (i >= documentSegmentPages.Count || documentSegmentPages[i] != targetPage || documentSegmentTexts[i].text == "\n")
            {
                continue;
            }

            Text source = documentSegmentTexts[i];
            Text preview = GetDocumentPagePreviewText(previewIndex++);
            CopyDocumentText(source, preview);
            preview.gameObject.SetActive(true);
        }

        for (int i = previewIndex; i < documentPagePreviewTexts.Count; i++)
        {
            documentPagePreviewTexts[i].gameObject.SetActive(false);
        }
    }

    private RectTransform GetDocumentPagePreviewRoot()
    {
        if (documentPagePreviewRoot != null)
        {
            return documentPagePreviewRoot;
        }

        RectTransform backPage = documentBackPages != null && documentBackPages.Length > 0 ? documentBackPages[0] : null;
        if (backPage == null)
        {
            return null;
        }

        GameObject previewRootObject = new GameObject("DocumentPagePreviewRoot", typeof(RectTransform));
        documentPagePreviewRoot = previewRootObject.GetComponent<RectTransform>();
        documentPagePreviewRoot.SetParent(backPage, false);
        CopyRectTransform((RectTransform)documentSegmentRoot, documentPagePreviewRoot);
        return documentPagePreviewRoot;
    }

    private Text GetDocumentPagePreviewText(int index)
    {
        while (documentPagePreviewTexts.Count <= index)
        {
            GameObject textObject = new GameObject("DocumentPagePreviewText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(GetDocumentPagePreviewRoot(), false);
            documentPagePreviewTexts.Add(textObject.GetComponent<Text>());
        }

        return documentPagePreviewTexts[index];
    }

    private void CopyDocumentText(Text source, Text target)
    {
        target.text = source.text;
        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.lineSpacing = source.lineSpacing;
        target.alignment = source.alignment;
        target.horizontalOverflow = source.horizontalOverflow;
        target.verticalOverflow = source.verticalOverflow;
        target.color = source.color;
        target.raycastTarget = source.raycastTarget;
        target.supportRichText = source.supportRichText;
        CopyRectTransform(source.rectTransform, target.rectTransform);
        SetDocumentSegmentHighlight(target, source.transform.Find("Highlight") != null && source.transform.Find("Highlight").gameObject.activeSelf);
    }

    private void SetDocumentSegmentHighlight(Text text, bool enabled)
    {
        Transform oldUnderline = text.transform.Find("Underline");
        if (oldUnderline != null)
        {
            oldUnderline.gameObject.SetActive(false);
        }

        const string highlightName = "Highlight";
        Transform existing = text.transform.Find(highlightName);
        Image highlight = existing != null ? existing.GetComponent<Image>() : null;
        if (highlight == null)
        {
            GameObject highlightObject = new GameObject(highlightName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            highlightObject.transform.SetParent(text.transform, false);
            highlight = highlightObject.GetComponent<Image>();
        }

        highlight.gameObject.SetActive(enabled);
        highlight.raycastTarget = false;
        highlight.color = new Color(1f, 0.86f, 0.28f, 0.35f);
        highlight.transform.SetAsFirstSibling();

        RectTransform rect = highlight.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(4f, -2f);
    }

    private void SetupPreviewSegmentHandler(Text preview, int sourceSegmentIndex)
    {
        InfoTextSegmentHandler handler = preview.GetComponent<InfoTextSegmentHandler>();
        InfoNodeData infoNode = sourceSegmentIndex < documentSegmentInfoNodes.Count ? documentSegmentInfoNodes[sourceSegmentIndex] : null;
        preview.raycastTarget = infoNode != null;
        SetDocumentSegmentHighlight(preview, infoNode != null);
        if (infoNode != null)
        {
            if (handler == null)
            {
                handler = preview.gameObject.AddComponent<InfoTextSegmentHandler>();
            }
            handler.Initialize(this, infoNode);
        }
        else if (handler != null)
        {
            Destroy(handler);
        }
    }

    private void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private void ClearDocumentPagePreview()
    {
        for (int i = 0; i < documentPagePreviewTexts.Count; i++)
        {
            documentPagePreviewTexts[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator MoveDocumentPageOut(RectTransform pageRect, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float t = EvaluateDocumentPageExitCurve(rawT);
            pageRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        pageRect.anchoredPosition = to;
    }

    private float EvaluateDocumentPageExitCurve(float t)
    {
        if (t < 0.25f)
        {
            float localT = t / 0.25f;
            return Mathf.Lerp(0f, 0.42f, 1f - (1f - localT) * (1f - localT));
        }

        if (t < 0.75f)
        {
            return Mathf.Lerp(0.42f, 0.82f, (t - 0.25f) / 0.5f);
        }

        float slowT = (t - 0.75f) / 0.25f;
        return Mathf.Lerp(0.82f, 1f, slowT * slowT * (3f - 2f * slowT));
    }

    private IEnumerator MoveDocumentPage(RectTransform pageRect, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            pageRect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        pageRect.anchoredPosition = to;
    }

    private void CacheDocumentPageRestPosition()
    {
        if (documentPageRestPositionCached)
        {
            return;
        }

        RectTransform pageRect = documentPageAnimatedRect != null ? documentPageAnimatedRect : documentSegmentRoot.parent as RectTransform;
        if (pageRect != null)
        {
            documentPageRestPosition = pageRect.anchoredPosition;
            documentPageRestSiblingIndex = pageRect.GetSiblingIndex();
            documentPageRestPositionCached = true;
        }
    }

    private void InitializeDocumentPageControls()
    {
        if (previousDocumentPageButton != null)
        {
            previousDocumentPageButton.onClick.RemoveAllListeners();
            previousDocumentPageButton.onClick.AddListener(ShowPreviousDocumentPage);
        }

        if (nextDocumentPageButton != null)
        {
            nextDocumentPageButton.onClick.RemoveAllListeners();
            nextDocumentPageButton.onClick.AddListener(ShowNextDocumentPage);
        }

        CacheDocumentPageRestPosition();
        UpdateDocumentPageButtons();
    }

    private void UpdateDocumentPageVisibility()
    {
        for (int i = 0; i < documentSegmentTexts.Count; i++)
        {
            bool show = i < documentSegmentPages.Count && documentSegmentTexts[i].text != "\n" && (documentPageCount <= 1 ? documentSegmentPages[i] == currentDocumentPage : documentSegmentPages[i] == 0);
            documentSegmentTexts[i].gameObject.SetActive(show);
        }

        UpdateDocumentPageButtons();
        UpdateDocumentBackPages();
    }

    private void UpdateDocumentBackPagePreviews()
    {
        RenderDocumentBackPagesFromPage(currentDocumentPage + 1);
    }

    private void RenderDocumentBackPagesFromPage(int firstPage)
    {
        if (documentBackPages == null)
        {
            return;
        }

        for (int i = 0; i < documentBackPages.Length; i++)
        {
            int page = firstPage + i;
            if (selectedDocumentIndex >= 0 && documentPageCount > 1 && page < documentPageCount)
            {
                RenderDocumentPageToBackPage(i, page);
            }
            else
            {
                ClearDocumentBackPagePreview(i);
            }
        }
    }

    private void RenderDocumentPageToBackPage(int backPageIndex, int page)
    {
        RectTransform root = GetDocumentBackPagePreviewRoot(backPageIndex);
        if (root == null)
        {
            return;
        }

        List<Text> previewTexts = GetDocumentBackPagePreviewTextList(backPageIndex);
        int previewIndex = 0;
        for (int i = 0; i < documentSegmentTexts.Count; i++)
        {
            if (i >= documentSegmentPages.Count || documentSegmentPages[i] != page || documentSegmentTexts[i].text == "\n")
            {
                continue;
            }

            Text source = documentSegmentTexts[i];
            Text preview = GetDocumentBackPagePreviewText(backPageIndex, previewIndex++);
            CopyDocumentText(source, preview);
            SetupPreviewSegmentHandler(preview, i);
            preview.gameObject.SetActive(true);
        }

        for (int i = previewIndex; i < previewTexts.Count; i++)
        {
            previewTexts[i].gameObject.SetActive(false);
        }
    }

    private RectTransform GetDocumentBackPagePreviewRoot(int backPageIndex)
    {
        if (documentBackPages == null || backPageIndex < 0 || backPageIndex >= documentBackPages.Length || documentBackPages[backPageIndex] == null)
        {
            return null;
        }

        const string rootName = "DocumentPagePreviewRoot";
        Transform existing = documentBackPages[backPageIndex].Find(rootName);
        if (existing != null)
        {
            return (RectTransform)existing;
        }

        GameObject previewRootObject = new GameObject(rootName, typeof(RectTransform));
        RectTransform root = previewRootObject.GetComponent<RectTransform>();
        root.SetParent(documentBackPages[backPageIndex], false);
        CopyRectTransform((RectTransform)documentSegmentRoot, root);
        return root;
    }

    private List<Text> GetDocumentBackPagePreviewTextList(int backPageIndex)
    {
        while (documentBackPagePreviewTexts.Count <= backPageIndex)
        {
            documentBackPagePreviewTexts.Add(new List<Text>());
        }

        return documentBackPagePreviewTexts[backPageIndex];
    }

    private Text GetDocumentBackPagePreviewText(int backPageIndex, int textIndex)
    {
        List<Text> previewTexts = GetDocumentBackPagePreviewTextList(backPageIndex);
        while (previewTexts.Count <= textIndex)
        {
            GameObject textObject = new GameObject("DocumentPagePreviewText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(GetDocumentBackPagePreviewRoot(backPageIndex), false);
            previewTexts.Add(textObject.GetComponent<Text>());
        }

        return previewTexts[textIndex];
    }

    private void ClearDocumentBackPagePreview(int backPageIndex)
    {
        if (backPageIndex < 0 || backPageIndex >= documentBackPagePreviewTexts.Count)
        {
            return;
        }

        List<Text> previewTexts = documentBackPagePreviewTexts[backPageIndex];
        for (int i = 0; i < previewTexts.Count; i++)
        {
            previewTexts[i].gameObject.SetActive(false);
        }
    }

    private void UpdateDocumentBackPages()
    {
        bool showBackPages = selectedDocumentIndex >= 0 && documentPageCount > 1;
        if (documentBackPages == null)
        {
            return;
        }

        if (documentPagePanels.Count > 0)
        {
            for (int i = 0; i < documentBackPages.Length; i++)
            {
                if (documentBackPages[i] != null)
                {
                    documentBackPages[i].gameObject.SetActive(documentPagePanels.Contains(documentBackPages[i]));
                }
            }
            return;
        }

        for (int i = 0; i < documentBackPages.Length; i++)
        {
            if (documentBackPages[i] != null)
            {
                documentBackPages[i].gameObject.SetActive(showBackPages);
            }
        }
    }

    public void SetPageButtonsHiddenForTransition(bool hidden)
    {
        if (hidden)
        {
            if (previousDocumentPageButton != null)
            {
                previousDocumentPageButton.gameObject.SetActive(false);
            }
            if (nextDocumentPageButton != null)
            {
                nextDocumentPageButton.gameObject.SetActive(false);
            }
            return;
        }

        UpdateDocumentPageButtons();
    }

    private void UpdateDocumentPageButtons()
    {
        bool hasPages = selectedDocumentIndex >= 0 && documentPageCount > 1;
        if (previousDocumentPageButton != null)
        {
            previousDocumentPageButton.gameObject.SetActive(hasPages);
            previousDocumentPageButton.interactable = hasPages && !isDocumentPageAnimating && currentDocumentPage > 0;
        }

        if (nextDocumentPageButton != null)
        {
            nextDocumentPageButton.gameObject.SetActive(hasPages);
            nextDocumentPageButton.interactable = hasPages && !isDocumentPageAnimating && currentDocumentPage < documentPageCount - 1;
        }
    }

    private Button GetDocumentButton(int index)
    {
        while (documentButtons.Count <= index)
        {
            Button button = Instantiate(documentButtonTemplate, documentListRoot);
            documentButtons.Add(button);
        }

        return documentButtons[index];
    }

    private void InitializeDocumentButtonPoolFromScene()
    {
        if (documentButtons.Count > 0 || documentListRoot == null)
        {
            return;
        }

        for (int i = 0; i < documentListRoot.childCount; i++)
        {
            Transform child = documentListRoot.GetChild(i);
            Button button = child.GetComponent<Button>();
            if (button == null || button == documentButtonTemplate)
            {
                continue;
            }

            documentButtons.Add(button);
        }
    }

    private void ConfigureDocumentButtonAppearance(Button button, int index)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            if (documentButtonSprites != null && index >= 0 && index < documentButtonSprites.Length && documentButtonSprites[index] != null)
            {
                image.sprite = documentButtonSprites[index];
            }
            image.preserveAspect = true;
            image.color = Color.white;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = documentButtonSize;
        rect.anchoredPosition = GetDocumentButtonPosition(index);
        rect.localRotation = Quaternion.Euler(0f, 0f, GetDocumentButtonRotation(index));
        rect.localScale = Vector3.one;
    }

    private Vector2 GetDocumentButtonPosition(int index)
    {
        if (documentButtonPositions != null && index >= 0 && index < documentButtonPositions.Length)
        {
            return documentButtonPositions[index];
        }

        float spacing = documentButtonSize.x * 0.55f;
        float center = (currentDay.envelope.documents.Count - 1) * 0.5f;
        return new Vector2((index - center) * spacing, 0f);
    }

    private float GetDocumentButtonRotation(int index)
    {
        if (documentButtonRotations != null && index >= 0 && index < documentButtonRotations.Length)
        {
            return documentButtonRotations[index];
        }

        return 0f;
    }

    private void DisableDocumentAutoLayout()
    {
        if (documentListRoot == null)
        {
            return;
        }

        LayoutGroup layoutGroup = documentListRoot.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter fitter = documentListRoot.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }
    }

    private void RecycleDocumentButtons()
    {
        documentButtonTemplate.gameObject.SetActive(false);
        for (int i = 0; i < documentButtons.Count; i++)
        {
            documentButtons[i].gameObject.SetActive(false);
        }
    }

    private string BuildHighlightedDocumentText(DocumentData document)
    {
        currentInfoRanges.Clear();

        string plainText = document.fullText;
        List<InfoTextRange> ranges = BuildInfoRanges(document, plainText);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int cursor = 0;
        for (int i = 0; i < ranges.Count; i++)
        {
            InfoTextRange range = ranges[i];
            if (range.startIndex < cursor)
            {
                continue;
            }

            builder.Append(plainText.Substring(cursor, range.startIndex - cursor));
            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(GetInfoNodeColor(range.infoNode.type)));
            builder.Append("><b>");
            builder.Append(plainText.Substring(range.startIndex, range.length));
            builder.Append("</b></color>");
            currentInfoRanges.Add(range);
            cursor = range.startIndex + range.length;
        }

        builder.Append(plainText.Substring(cursor));
        return builder.ToString();
    }

    private void BuildDocumentSegments(DocumentData document)
    {
        RecycleDocumentSegments();
        currentInfoRanges.Clear();
        currentDocumentPage = 0;
        documentPageCount = 1;
        documentSegmentPages.Clear();
        documentSegmentInfoNodes.Clear();

        string plainText = document.fullText;
        List<InfoTextRange> ranges = BuildInfoRanges(document, plainText);
        int cursor = 0;
        int segmentIndex = 0;

        for (int i = 0; i < ranges.Count; i++)
        {
            InfoTextRange range = ranges[i];
            if (range.startIndex < cursor)
            {
                continue;
            }

            if (range.startIndex > cursor)
            {
                AddDocumentTextSegments(ref segmentIndex, plainText.Substring(cursor, range.startIndex - cursor), new Color(0.12f, 0.11f, 0.1f, 1f), null);
            }

            AddDocumentTextSegments(ref segmentIndex, plainText.Substring(range.startIndex, range.length), new Color(0.12f, 0.11f, 0.1f, 1f), range.infoNode);
            currentInfoRanges.Add(range);
            cursor = range.startIndex + range.length;
        }

        if (cursor < plainText.Length)
        {
            AddDocumentTextSegments(ref segmentIndex, plainText.Substring(cursor), new Color(0.12f, 0.11f, 0.1f, 1f), null);
        }

        ArrangeDocumentSegments(segmentIndex);
        BuildDocumentPagePanels();
        UpdateDocumentPageVisibility();
    }

    private void AddDocumentTextSegments(ref int segmentIndex, string value, Color color, InfoNodeData infoNode)
    {
        for (int i = 0; i < value.Length; i++)
        {
            Text segment = GetDocumentSegment(segmentIndex);
            SetupDocumentSegment(segment, value[i].ToString(), color, infoNode);
            EnsureDocumentSegmentInfoCapacity(segmentIndex + 1);
            documentSegmentInfoNodes[segmentIndex] = infoNode;
            segmentIndex++;
        }
    }

    private List<InfoTextRange> BuildInfoRanges(DocumentData document, string plainText)
    {
        List<InfoTextRange> ranges = new List<InfoTextRange>();
        for (int i = 0; i < document.infoNodes.Count; i++)
        {
            InfoNodeData infoNode = document.infoNodes[i];
            int startIndex = plainText.IndexOf(infoNode.displayText, System.StringComparison.Ordinal);
            if (startIndex >= 0)
            {
                ranges.Add(new InfoTextRange
                {
                    startIndex = startIndex,
                    length = infoNode.displayText.Length,
                    infoNode = infoNode
                });
            }
        }

        ranges.Sort((left, right) => left.startIndex.CompareTo(right.startIndex));
        return ranges;
    }

    private Text GetDocumentSegment(int index)
    {
        while (documentSegmentTexts.Count <= index)
        {
            Text text = Instantiate(documentSegmentTemplate, documentSegmentRoot);
            documentSegmentTexts.Add(text);
        }

        EnsureDocumentSegmentPageCapacity(index + 1);
        return documentSegmentTexts[index];
    }

    private void EnsureDocumentSegmentPageCapacity(int count)
    {
        while (documentSegmentPages.Count < count)
        {
            documentSegmentPages.Add(0);
        }
    }

    private void EnsureDocumentSegmentInfoCapacity(int count)
    {
        while (documentSegmentInfoNodes.Count < count)
        {
            documentSegmentInfoNodes.Add(null);
        }
    }

    private void SetupDocumentSegment(Text text, string value, Color color, InfoNodeData infoNode)
    {
        text.text = value;
        text.color = color;
        text.raycastTarget = infoNode != null;
        text.fontStyle = infoNode == null ? FontStyle.Normal : FontStyle.Italic;
        SetDocumentSegmentHighlight(text, infoNode != null);
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.gameObject.SetActive(true);

        InfoTextSegmentHandler handler = text.GetComponent<InfoTextSegmentHandler>();
        if (infoNode != null)
        {
            if (handler == null)
            {
                handler = text.gameObject.AddComponent<InfoTextSegmentHandler>();
            }

            handler.Initialize(this, infoNode);
        }
        else if (handler != null)
        {
            Destroy(handler);
        }
    }

    private void BuildDocumentPagePanels()
    {
        CacheDocumentPageRestPosition();
        documentPagePanels.Clear();
        documentPagePanelBaseRefs.Clear();
        documentPagePanelBasePositions.Clear();
        documentPagePanelBaseRotations.Clear();
        RectTransform mainPage = documentPageAnimatedRect != null ? documentPageAnimatedRect : documentSegmentRoot.parent as RectTransform;
        if (mainPage != null)
        {
            documentPagePanels.Add(mainPage);
            CaptureDocumentPageBaseTransform(mainPage);
        }

        if (documentBackPages != null)
        {
            for (int i = 0; i < documentBackPages.Length && documentPagePanels.Count < documentPageCount; i++)
            {
                if (documentBackPages[i] != null)
                {
                    documentPagePanels.Add(documentBackPages[i]);
                    CaptureDocumentPageBaseTransform(documentBackPages[i]);
                }
            }
        }

        for (int i = 0; i < documentPagePanels.Count; i++)
        {
            documentPagePanels[i].gameObject.SetActive(i < documentPageCount);
        }

        RenderDocumentBackPagesFromPage(1);
        ApplyDocumentPagePanelInitialTransforms();
    }

    private void CaptureDocumentPageBaseTransform(RectTransform page)
    {
        if (page == null || documentPagePanelBaseRefs.Contains(page))
        {
            return;
        }

        documentPagePanelBaseRefs.Add(page);
        documentPagePanelBasePositions.Add(page.anchoredPosition);
        documentPagePanelBaseRotations.Add(page.localEulerAngles.z);
    }

    private void ArrangeDocumentSegments(int activeSegmentCount)
    {
        RectTransform rootRect = (RectTransform)documentSegmentRoot;
        float maxWidth = rootRect.rect.width;
        float maxHeight = rootRect.rect.height > 0f ? rootRect.rect.height : 420f;
        float lineHeight = documentBodyText.fontSize * documentBodyText.lineSpacing + 8f;
        float x = 0f;
        float y = 0f;
        int page = 0;
        EnsureDocumentSegmentPageCapacity(activeSegmentCount);

        for (int i = 0; i < activeSegmentCount; i++)
        {
            Text text = documentSegmentTexts[i];
            RectTransform rect = text.rectTransform;
            float width = Mathf.Ceil(text.preferredWidth);
            if (text.text == "\n")
            {
                x = 0f;
                y += lineHeight;
                if (y + lineHeight > maxHeight)
                {
                    page++;
                    y = 0f;
                }

                documentSegmentPages[i] = page;
                text.gameObject.SetActive(false);
                continue;
            }

            if (x > 0f && x + width > maxWidth)
            {
                x = 0f;
                y += lineHeight;
            }

            if (y + lineHeight > maxHeight)
            {
                page++;
                x = 0f;
                y = 0f;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Mathf.Min(width, maxWidth), lineHeight);
            documentSegmentPages[i] = page;
            x += width;
        }

        documentPageCount = Mathf.Max(1, page + 1);
        for (int i = activeSegmentCount; i < documentSegmentPages.Count; i++)
        {
            documentSegmentPages[i] = -1;
        }
        for (int i = activeSegmentCount; i < documentSegmentInfoNodes.Count; i++)
        {
            documentSegmentInfoNodes[i] = null;
        }
    }

    private void PlayDocumentEnterAnimation()
    {
        if (documentAnimationRoutine != null)
        {
            StopCoroutine(documentAnimationRoutine);
        }

        documentAnimationRoutine = StartCoroutine(DocumentEnterAnimation());
    }

    private IEnumerator DocumentEnterAnimation()
    {
        RectTransform rect = (RectTransform)documentSegmentRoot.parent;
        Vector2 targetPosition = rect.anchoredPosition;
        Vector2 startPosition = targetPosition + new Vector2(0f, -90f);
        float duration = 0.28f;
        float elapsed = 0f;
        rect.anchoredPosition = startPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        rect.anchoredPosition = targetPosition;
    }

    private void RecycleDocumentSegments()
    {
        documentSegmentTemplate.gameObject.SetActive(false);
        for (int i = 0; i < documentSegmentTexts.Count; i++)
        {
            documentSegmentTexts[i].gameObject.SetActive(false);
        }
    }

    private void BuildInfoHotspots()
    {
        RecycleInfoHotspots();
        Canvas.ForceUpdateCanvases();
        documentBodyText.cachedTextGenerator.Invalidate();
        documentBodyText.GetGenerationSettings(documentBodyText.rectTransform.rect.size);

        IList<UICharInfo> characters = documentBodyText.cachedTextGenerator.characters;
        if (characters.Count == 0)
        {
            return;
        }

        Rect rect = documentBodyText.rectTransform.rect;
        Vector2 topLeft = new Vector2(rect.xMin, rect.yMax);

        for (int i = 0; i < currentInfoRanges.Count; i++)
        {
            InfoTextRange range = currentInfoRanges[i];
            int start = Mathf.Clamp(range.startIndex, 0, characters.Count - 1);
            int end = Mathf.Clamp(range.startIndex + range.length - 1, 0, characters.Count - 1);
            Vector2 startPosition = topLeft + characters[start].cursorPos / documentBodyText.pixelsPerUnit;
            Vector2 endPosition = topLeft + characters[end].cursorPos / documentBodyText.pixelsPerUnit;
            float characterWidth = documentBodyText.fontSize * 0.6f;

            InfoHotspotHandler hotspot = GetInfoHotspot(i);
            RectTransform hotspotRect = hotspot.GetComponent<RectTransform>();
            hotspotRect.anchorMin = documentBodyText.rectTransform.anchorMin;
            hotspotRect.anchorMax = documentBodyText.rectTransform.anchorMax;
            hotspotRect.pivot = new Vector2(0f, 1f);
            hotspotRect.anchoredPosition = new Vector2(documentBodyText.rectTransform.anchoredPosition.x + startPosition.x, documentBodyText.rectTransform.anchoredPosition.y + startPosition.y);
            hotspotRect.sizeDelta = new Vector2(Mathf.Max(endPosition.x - startPosition.x + characterWidth, characterWidth), documentBodyText.fontSize * documentBodyText.lineSpacing);

            hotspot.Initialize(this, range.infoNode, GetInfoNodeColor(range.infoNode.type));
            hotspot.gameObject.SetActive(true);
        }
    }

    private InfoHotspotHandler GetInfoHotspot(int index)
    {
        while (infoHotspots.Count <= index)
        {
            Image image = Instantiate(infoHotspotTemplate, infoHotspotRoot);
            InfoHotspotHandler handler = image.GetComponent<InfoHotspotHandler>();
            if (handler == null)
            {
                handler = image.gameObject.AddComponent<InfoHotspotHandler>();
            }
            infoHotspots.Add(handler);
        }

        return infoHotspots[index];
    }

    private void RecycleInfoHotspots()
    {
        if (hoveredHotspot != null)
        {
            hoveredHotspot = null;
        }

        infoHotspotTemplate.gameObject.SetActive(false);
        for (int i = 0; i < infoHotspots.Count; i++)
        {
            infoHotspots[i].gameObject.SetActive(false);
        }
    }

    private void ClearDocumentView()
    {
        documentTitleText.text = "";
        documentBodyText.text = "";
        currentDocumentPage = 0;
        documentPageCount = 1;
        documentPagePanels.Clear();
        UpdateDocumentPageButtons();
        UpdateDocumentBackPages();
        ClearDocumentPagePreview();
        RenderDocumentBackPagesFromPage(currentDocumentPage + 1);
        RecycleInfoHotspots();
        RecycleDocumentSegments();
        currentInfoRanges.Clear();
    }

    private void InitializeDocumentTextClickHandler()
    {
        infoHotspotTemplate.gameObject.SetActive(false);
    }

    private Color GetInfoNodeColor(InfoNodeType type)
    {
        switch (type)
        {
            case InfoNodeType.KeyClue:
                return new Color(1f, 0.78f, 0.25f, 1f);
            case InfoNodeType.Character:
                return new Color(0.35f, 0.75f, 1f, 1f);
            case InfoNodeType.Location:
                return new Color(0.55f, 0.9f, 0.45f, 1f);
            case InfoNodeType.Evidence:
                return new Color(1f, 0.45f, 0.45f, 1f);
            case InfoNodeType.Rumor:
                return new Color(0.82f, 0.58f, 1f, 1f);
            default:
                return Color.white;
        }
    }

    private void EnsureSampleData()
    {
        if (currentDay.envelope.documents.Count > 0)
        {
            return;
        }

        currentDay = CreateSampleDayData(1);
    }

    private BroadcastDayData CreateSampleDayData(int dayIndex)
    {
        BroadcastDayData dayData = new BroadcastDayData();
        dayData.id = $"day_{dayIndex:00}";
        dayData.dayIndex = dayIndex;
        dayData.envelope.id = $"envelope_{dayIndex:00}";

        string dayPrefix = dayIndex == 1 ? "" : $"第{dayIndex}天";
        string firstDayNoticeText = "市政厅发布夜间交通管制公告，旧城区连续三晚出现异常停电。\n\n" +
            "公告称，旧城区主干道将在午夜后分段封闭，巡逻队会优先检查变电站、医院和港口仓库周边。居民被要求提���储水并减少外出，但公告没有说明停电原因，也没有解释为何封锁范围覆盖三号仓库。\n\n" +
            "根据值班记录，第一晚停电发生在二十三点四十分，第二晚提前到二十二点五十五分，第三晚则在广播塔短暂闪烁后突然中断。每次停电前，旧城区北侧的交通信号都会同时转为黄灯，随后市政维护车进入封锁区。\n\n" +
            "市政厅发布夜间交通管制公告后，多名居民反映公告张贴时间晚于实际封路时间。旧城区连续三晚出现异常停电期间，仍有未登记货车沿港口方向通行，车厢外侧没有任何运输标识。\n\n" +
            "请广播站在播报时提醒居民避开旧城区桥下通道，并注意收听后续通知。若发现携带临时通行证的外来人员，请记录其去向后交由巡逻队处理。";

        DocumentData notice = new DocumentData
        {
            id = $"doc_notice_{dayIndex:00}",
            displayName = dayPrefix + "市政公告",
            fullText = dayIndex == 1 ? firstDayNoticeText : $"市政厅发布第{dayIndex}天巡查公告，旧城区居民报告新的异常广播信号。"
        };
        notice.infoNodes.Add(new InfoNodeData
        {
            id = $"info_power_outage_{dayIndex:00}",
            displayText = dayIndex == 1 ? "旧城区连续三晚出现异常停电" : "旧城区居民报告新的异常广播信号",
            type = InfoNodeType.KeyClue,
            priority = 3,
            isMandatory = true,
            effects = new NodeEffectData { trust = 2, chaos = 1 },
            extractedText = dayIndex == 1 ? "连续三晚" : $"第{dayIndex}天异常广播信号"
        });
        notice.infoNodes.Add(new InfoNodeData
        {
            id = $"info_city_hall_{dayIndex:00}",
            displayText = dayIndex == 1 ? "市政厅发布夜间交通管制公告" : $"市政厅发布第{dayIndex}天巡查公告",
            type = InfoNodeType.Location,
            priority = 1,
            effects = new NodeEffectData { trust = 1, chaos = 0 },
            extractedText = dayIndex == 1 ? "夜间交通管制" : $"第{dayIndex}天巡查公告"
        });

        DocumentData letter = new DocumentData
        {
            id = $"doc_letter_{dayIndex:00}",
            displayName = dayPrefix + "匿名来信",
            fullText = dayIndex == 1 ? "一名仓库员工声称，港口仓库最近夜间频繁有未登记车辆出入。" : $"一名夜班护士声称，第{dayIndex}天凌晨医院后门有人转移密封箱。"
        };
        letter.infoNodes.Add(new InfoNodeData
        {
            id = $"info_warehouse_worker_{dayIndex:00}",
            displayText = dayIndex == 1 ? "一名仓库员工" : "一名夜班护士",
            type = InfoNodeType.Character,
            priority = 2,
            effects = new NodeEffectData { trust = 1, chaos = 0 },
            extractedText = dayIndex == 1 ? "匿名仓库员工提供线索" : $"第{dayIndex}天夜班护士提供线索"
        });
        letter.infoNodes.Add(new InfoNodeData
        {
            id = $"info_unregistered_cars_{dayIndex:00}",
            displayText = dayIndex == 1 ? "港口仓库最近夜间频繁有未登记车辆出入" : "医院后门有人转移密封箱",
            type = InfoNodeType.Evidence,
            priority = 3,
            isMandatory = true,
            effects = new NodeEffectData { trust = 2, chaos = 2 },
            extractedText = dayIndex == 1 ? "港口仓库存在未登记车辆夜间出入" : $"第{dayIndex}天医院后门转移密封箱"
        });

        DocumentData ledger = new DocumentData
        {
            id = $"doc_ledger_{dayIndex:00}",
            displayName = dayPrefix + "货运清单",
            fullText = dayIndex == 1 ? "港务局清单显示，三号仓库在凌晨两点登记了一批未申报医疗器械。" : $"货运清单显示，第{dayIndex}天有一批未备案药剂被送往旧电台。"
        };
        ledger.infoNodes.Add(new InfoNodeData
        {
            id = $"info_warehouse_three_{dayIndex:00}",
            displayText = dayIndex == 1 ? "三号仓库" : "旧电台",
            type = InfoNodeType.Location,
            priority = 2,
            effects = new NodeEffectData { trust = 1, chaos = 0 },
            extractedText = dayIndex == 1 ? "三号仓库" : $"第{dayIndex}天旧电台"
        });
        ledger.infoNodes.Add(new InfoNodeData
        {
            id = $"info_medical_devices_{dayIndex:00}",
            displayText = dayIndex == 1 ? "未申报医疗器械" : "未备案药剂",
            type = InfoNodeType.Evidence,
            priority = 3,
            isMandatory = true,
            effects = new NodeEffectData { trust = 2, chaos = 1 },
            extractedText = dayIndex == 1 ? "发现未申报医疗器械" : $"第{dayIndex}天未备案药剂"
        });

        dayData.envelope.documents.Add(notice);
        dayData.envelope.documents.Add(letter);
        dayData.envelope.documents.Add(ledger);

        AudioTrackData recorderA = new AudioTrackData
        {
            id = $"audio_recorder_a_{dayIndex:00}",
            displayName = "录音笔 A"
        };
        recorderA.audioNodes.Add(new AudioNodeData { id = $"audio_a_01_{dayIndex:00}", contentText = dayIndex == 1 ? "深夜十一点，三号仓库的灯还亮着。" : $"第{dayIndex}天深夜，旧电台仍有信号灯闪烁。", displayTime = 3.2f });
        recorderA.audioNodes.Add(new AudioNodeData { id = $"audio_a_02_{dayIndex:00}", contentText = dayIndex == 1 ? "有人提到明早封锁旧城区路口。" : $"有人提到第{dayIndex}天清晨封锁医院后门。", displayTime = 2.8f });
        recorderA.audioNodes.Add(new AudioNodeData { id = $"audio_a_03_{dayIndex:00}", contentText = dayIndex == 1 ? "货车没有登记牌照。" : "转运车辆没有登记。", displayTime = 2.1f });

        AudioTrackData recorderB = new AudioTrackData
        {
            id = $"audio_recorder_b_{dayIndex:00}",
            displayName = "录音笔 B"
        };
        recorderB.audioNodes.Add(new AudioNodeData { id = $"audio_b_01_{dayIndex:00}", contentText = dayIndex == 1 ? "停电前听到变电站附近有爆裂声。" : "广播信号出现前，电台附近短暂停电。", displayTime = 3.5f });
        recorderB.audioNodes.Add(new AudioNodeData { id = $"audio_b_02_{dayIndex:00}", contentText = dayIndex == 1 ? "市政厅要求公告不要提及仓库。" : "公告被要求不要提及旧电台。", displayTime = 3f });

        dayData.envelope.audioTracks.Add(recorderA);
        dayData.envelope.audioTracks.Add(recorderB);
        return dayData;
    }
    private class InfoTextRange
    {
        public int startIndex;
        public int length;
        public InfoNodeData infoNode;
    }
}

public enum CollectedBoardInteractionMode
{
    Collection,
    Organization
}
