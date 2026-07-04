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
    [SerializeField] private Text documentTooltipText;
    [SerializeField] private AudioClipEditingController audioClipEditingController;
    [SerializeField] private BroadcastDraftSlot[] broadcastDraftSlots;
    [SerializeField] private int maxNotesPerDraftSlot = 3;
    [SerializeField] private Vector2 collectionNoteSize = new Vector2(190f, 54f);
    [SerializeField] private int collectionNoteFontSize = 17;
    [SerializeField] private Vector2 draftNoteSize = new Vector2(132f, 46f);
    [SerializeField] private int draftNoteFontSize = 12;
    [SerializeField] private float draftSlotTopPadding = 52f;
    [SerializeField] private float draftSlotNoteSpacing = 14f;

    private readonly List<Button> documentButtons = new List<Button>();
    private readonly List<Text> collectedInfoTexts = new List<Text>();
    private readonly Dictionary<string, GameObject> collectedInfoNotes = new Dictionary<string, GameObject>();
    private readonly List<Text> documentSegmentTexts = new List<Text>();
    private readonly List<InfoHotspotHandler> infoHotspots = new List<InfoHotspotHandler>();
    private readonly HashSet<string> collectedInfoIds = new HashSet<string>();
    private readonly List<InfoTextRange> currentInfoRanges = new List<InfoTextRange>();
    private readonly List<CollectedInfoNoteHandler>[] draftSlotNotes =
    {
        new List<CollectedInfoNoteHandler>(),
        new List<CollectedInfoNoteHandler>(),
        new List<CollectedInfoNoteHandler>()
    };
    private InfoHotspotHandler hoveredHotspot;
    private int selectedDocumentIndex = -1;
    private Coroutine documentAnimationRoutine;
    private CollectedBoardInteractionMode collectedBoardMode = CollectedBoardInteractionMode.Collection;
    private RectTransform collectionDropArea;

    public bool CanCancelCollectedNotes => collectedBoardMode == CollectedBoardInteractionMode.Collection;
    public bool CanReturnDraftNotes => collectedBoardMode == CollectedBoardInteractionMode.Organization;
    public bool CanDragCollectedNotes => collectedBoardMode == CollectedBoardInteractionMode.Organization;

    private void Awake()
    {
        DisableDocumentAutoLayout();
        EnsureSampleData();
        if (audioClipEditingController == null)
        {
            audioClipEditingController = GetComponentInChildren<AudioClipEditingController>(true);
        }
        InitializeDocumentTextClickHandler();
        ClearTooltip();
        ClearDocumentView();
        BuildDocumentList();
        InitializeBroadcastDraftSlots();
    }

    public void SelectDocument(int documentIndex)
    {
        if (documentIndex < 0 || documentIndex >= currentDay.envelope.documents.Count)
        {
            return;
        }

        DocumentData document = currentDay.envelope.documents[documentIndex];
        selectedDocumentIndex = documentIndex;
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
        handler.ApplyCollectionVisual(collectionNoteSize, collectionNoteFontSize);
    }

    public void ResetForDay(int dayIndex)
    {
        currentDay = CreateSampleDayData(dayIndex);
        selectedDocumentIndex = -1;
        collectedInfoIds.Clear();
        currentInfoRanges.Clear();
        ClearDraftSlots();
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
        InitializeBroadcastDraftSlots();
        ClearDraftSlotHighlights();
    }

    public void BeginCollectedNoteDrag(CollectedInfoNoteHandler note, Vector2 screenPosition, Camera eventCamera)
    {
        if (!CanDragCollectedNotes)
        {
            return;
        }

        InitializeBroadcastDraftSlots();
        UpdateCollectedNoteDrag(screenPosition, eventCamera);
    }

    public void UpdateCollectedNoteDrag(Vector2 screenPosition, Camera eventCamera)
    {
        if (!CanDragCollectedNotes)
        {
            ClearDraftSlotHighlights();
            return;
        }

        BroadcastDraftSlot hoveredSlot = FindAvailableSlotAt(screenPosition, eventCamera);
        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            BroadcastDraftSlot slot = broadcastDraftSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool isAvailable = CanDropIntoSlot(slot);
            slot.SetHighlight(isAvailable, isAvailable && slot == hoveredSlot);
        }
    }

    public bool EndCollectedNoteDrag(CollectedInfoNoteHandler note, Vector2 screenPosition, Camera eventCamera)
    {
        if (note == null || !CanDragCollectedNotes)
        {
            ClearDraftSlotHighlights();
            return false;
        }

        BroadcastDraftSlot targetSlot = FindAvailableSlotAt(screenPosition, eventCamera);
        if (targetSlot != null)
        {
            MoveNoteToDraftSlot(note, targetSlot);
            ClearDraftSlotHighlights();
            return true;
        }

        if (note.IsInDraft && IsPointerInsideCollectionArea(screenPosition, eventCamera))
        {
            ReturnDraftNoteToCollection(note);
            ClearDraftSlotHighlights();
            return true;
        }

        ClearDraftSlotHighlights();
        return false;
    }

    public void CancelCollectedNoteDrag(CollectedInfoNoteHandler note)
    {
        ClearDraftSlotHighlights();
        if (note != null && note.IsInDraft)
        {
            ArrangeDraftSlot(note.DraftSlotIndex);
        }
        else
        {
            RebuildCollectedInfoLayout();
        }
    }

    public void ReturnDraftNoteToCollection(CollectedInfoNoteHandler note)
    {
        if (note == null || !note.IsInDraft)
        {
            return;
        }

        int previousSlotIndex = note.DraftSlotIndex;
        RemoveNoteFromDraftSlot(note);
        note.MarkAsCollectionNote();
        note.transform.SetParent(collectedInfoRoot, false);
        note.ApplyCollectionVisual(collectionNoteSize, collectionNoteFontSize);
        note.transform.SetAsLastSibling();
        ArrangeDraftSlot(previousSlotIndex);
        RebuildCollectedInfoLayout();
    }

    public IReadOnlyList<CollectedInfoNoteHandler> GetDraftNotesInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return new List<CollectedInfoNoteHandler>();
        }

        return draftSlotNotes[slotIndex];
    }

    public List<string> GetDraftNoteIdsInSlot(int slotIndex)
    {
        List<string> noteIds = new List<string>();
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return noteIds;
        }

        for (int i = 0; i < draftSlotNotes[slotIndex].Count; i++)
        {
            CollectedInfoNoteHandler note = draftSlotNotes[slotIndex][i];
            if (note != null)
            {
                noteIds.Add(note.NoteId);
            }
        }

        return noteIds;
    }

    private void InitializeBroadcastDraftSlots()
    {
        if (broadcastDraftSlots == null || broadcastDraftSlots.Length == 0 || AreDraftSlotReferencesEmpty())
        {
            BroadcastDraftSlot[] foundSlots = GetComponentsInChildren<BroadcastDraftSlot>(true);
            BroadcastDraftSlot[] sortedSlots = new BroadcastDraftSlot[draftSlotNotes.Length];
            for (int i = 0; i < foundSlots.Length; i++)
            {
                int slotIndex = foundSlots[i].SlotIndex;
                if (slotIndex >= 0 && slotIndex < sortedSlots.Length)
                {
                    sortedSlots[slotIndex] = foundSlots[i];
                }
            }
            broadcastDraftSlots = sortedSlots;
        }

        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            if (broadcastDraftSlots[i] != null)
            {
                broadcastDraftSlots[i].Initialize(this);
            }
        }

        if (collectionDropArea == null)
        {
            collectionDropArea = collectedInfoRoot != null && collectedInfoRoot.parent != null
                ? collectedInfoRoot.parent as RectTransform
                : collectedInfoRoot as RectTransform;
        }
    }

    private bool AreDraftSlotReferencesEmpty()
    {
        if (broadcastDraftSlots == null || broadcastDraftSlots.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            if (broadcastDraftSlots[i] != null)
            {
                return false;
            }
        }

        return true;
    }

    private BroadcastDraftSlot FindAvailableSlotAt(Vector2 screenPosition, Camera eventCamera)
    {
        if (broadcastDraftSlots == null)
        {
            return null;
        }

        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            BroadcastDraftSlot slot = broadcastDraftSlots[i];
            if (slot != null && CanDropIntoSlot(slot) && slot.ContainsScreenPoint(screenPosition, eventCamera))
            {
                return slot;
            }
        }

        return null;
    }

    private bool CanDropIntoSlot(BroadcastDraftSlot slot)
    {
        if (slot == null)
        {
            return false;
        }

        int slotIndex = slot.SlotIndex;
        return slotIndex >= 0 &&
               slotIndex < draftSlotNotes.Length &&
               draftSlotNotes[slotIndex].Count < maxNotesPerDraftSlot;
    }

    private void MoveNoteToDraftSlot(CollectedInfoNoteHandler note, BroadcastDraftSlot targetSlot)
    {
        if (note == null || targetSlot == null)
        {
            return;
        }

        int previousSlotIndex = note.IsInDraft ? note.DraftSlotIndex : -1;
        int targetSlotIndex = targetSlot.SlotIndex;
        if (previousSlotIndex == targetSlotIndex)
        {
            ArrangeDraftSlot(targetSlotIndex);
            return;
        }

        RemoveNoteFromDraftSlot(note);
        draftSlotNotes[targetSlotIndex].Add(note);
        note.MarkAsDraftNote(targetSlotIndex);
        note.transform.SetParent(targetSlot.RectTransform, false);
        note.transform.SetAsLastSibling();

        if (previousSlotIndex >= 0)
        {
            ArrangeDraftSlot(previousSlotIndex);
        }
        ArrangeDraftSlot(targetSlotIndex);
        RebuildCollectedInfoLayout();
    }

    private void RemoveNoteFromDraftSlot(CollectedInfoNoteHandler note)
    {
        if (note == null || !note.IsInDraft)
        {
            return;
        }

        int slotIndex = note.DraftSlotIndex;
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return;
        }

        draftSlotNotes[slotIndex].Remove(note);
    }

    private void ArrangeDraftSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length || broadcastDraftSlots == null || slotIndex >= broadcastDraftSlots.Length)
        {
            return;
        }

        BroadcastDraftSlot slot = broadcastDraftSlots[slotIndex];
        if (slot == null)
        {
            return;
        }

        RectTransform slotRect = slot.RectTransform;
        if (slotRect == null)
        {
            return;
        }

        for (int i = 0; i < draftSlotNotes[slotIndex].Count; i++)
        {
            CollectedInfoNoteHandler note = draftSlotNotes[slotIndex][i];
            if (note == null)
            {
                continue;
            }

            RectTransform noteRect = note.RectTransform;
            note.transform.SetParent(slotRect, false);
            note.transform.localScale = Vector3.one;
            note.MarkAsDraftNote(slotIndex);
            note.ApplyDraftVisual(draftNoteSize, draftNoteFontSize);
            noteRect.anchorMin = new Vector2(0.5f, 0.5f);
            noteRect.anchorMax = new Vector2(0.5f, 0.5f);
            noteRect.pivot = new Vector2(0.5f, 0.5f);

            float noteHeight = Mathf.Max(24f, noteRect.sizeDelta.y);
            float y = slotRect.rect.height * 0.5f - draftSlotTopPadding - noteHeight * 0.5f - i * (noteHeight + draftSlotNoteSpacing);
            noteRect.anchoredPosition = new Vector2(0f, y);
            note.transform.SetAsLastSibling();
        }
    }

    private void ClearDraftSlots()
    {
        for (int i = 0; i < draftSlotNotes.Length; i++)
        {
            draftSlotNotes[i].Clear();
        }
        ClearDraftSlotHighlights();
    }

    private void ClearDraftSlotHighlights()
    {
        if (broadcastDraftSlots == null)
        {
            return;
        }

        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            if (broadcastDraftSlots[i] != null)
            {
                broadcastDraftSlots[i].SetHighlight(false, false);
            }
        }
    }

    private bool IsPointerInsideCollectionArea(Vector2 screenPosition, Camera eventCamera)
    {
        if (collectionDropArea == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(collectionDropArea, screenPosition, eventCamera);
    }

    private void RebuildCollectedInfoLayout()
    {
        RectTransform collectedRootRect = collectedInfoRoot as RectTransform;
        if (collectedRootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(collectedRootRect);
        }
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
        handler.ApplyCollectionVisual(collectionNoteSize, collectionNoteFontSize);
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
        text.color = new Color(0.12f, 0.1f, 0.06f, 1f);

        Image noteImage = note.GetComponent<Image>();
        if (noteImage != null)
        {
            noteImage.color = noteColor ?? new Color(1f, 0.93f, 0.55f, 1f);
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

            AddDocumentTextSegments(ref segmentIndex, plainText.Substring(range.startIndex, range.length), GetInfoNodeColor(range.infoNode.type), range.infoNode);
            currentInfoRanges.Add(range);
            cursor = range.startIndex + range.length;
        }

        if (cursor < plainText.Length)
        {
            AddDocumentTextSegments(ref segmentIndex, plainText.Substring(cursor), new Color(0.12f, 0.11f, 0.1f, 1f), null);
        }

        ArrangeDocumentSegments(segmentIndex);
    }

    private void AddDocumentTextSegments(ref int segmentIndex, string value, Color color, InfoNodeData infoNode)
    {
        for (int i = 0; i < value.Length; i++)
        {
            Text segment = GetDocumentSegment(segmentIndex++);
            SetupDocumentSegment(segment, value[i].ToString(), color, infoNode);
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

        return documentSegmentTexts[index];
    }

    private void SetupDocumentSegment(Text text, string value, Color color, InfoNodeData infoNode)
    {
        text.text = value;
        text.color = color;
        text.raycastTarget = infoNode != null;
        text.fontStyle = infoNode == null ? FontStyle.Normal : FontStyle.Bold;
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

    private void ArrangeDocumentSegments(int activeSegmentCount)
    {
        RectTransform rootRect = (RectTransform)documentSegmentRoot;
        float maxWidth = rootRect.rect.width;
        float lineHeight = documentBodyText.fontSize * documentBodyText.lineSpacing + 8f;
        float x = 0f;
        float y = 0f;

        for (int i = 0; i < activeSegmentCount; i++)
        {
            Text text = documentSegmentTexts[i];
            RectTransform rect = text.rectTransform;
            float width = Mathf.Ceil(text.preferredWidth);
            if (x > 0f && x + width > maxWidth)
            {
                x = 0f;
                y += lineHeight;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Mathf.Min(width, maxWidth), lineHeight);
            x += width;
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

        DocumentData notice = new DocumentData
        {
            id = $"doc_notice_{dayIndex:00}",
            displayName = dayPrefix + "市政公告",
            fullText = dayIndex == 1 ? "市政厅发布夜间交通管制公告，旧城区连续三晚出现异常停电。" : $"市政厅发布第{dayIndex}天巡查公告，旧城区居民报告新的异常广播信号。"
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
