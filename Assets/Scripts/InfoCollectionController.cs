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
    [SerializeField] private Sprite[] audioCollectedInfoNoteSprites;
    [SerializeField] private Text documentTooltipText;
    [SerializeField] private AudioClipEditingController audioClipEditingController;
    [SerializeField] private BroadcastDraftSlot[] broadcastDraftSlots;
    [SerializeField] private int maxNotesPerDraftSlot = 3;
    [SerializeField] private Vector2 collectionNoteSize = new Vector2(264f, 92f);
    [SerializeField] private int collectionNoteFontSize = 24;
    [SerializeField] private Vector2 draftNoteSize = new Vector2(132f, 46f);
    [SerializeField] private int draftNoteFontSize = 12;
    [SerializeField] private float draftSlotTopPadding = 52f;
    [SerializeField] private float draftSlotNoteSpacing = 14f;
    [SerializeField] private Button previousDocumentPageButton;
    [SerializeField] private Button nextDocumentPageButton;
    [SerializeField] private RectTransform documentPageAnimatedRect;
    [SerializeField] private RectTransform[] documentBackPages;
    [SerializeField] private RectTransform documentPagePreviewRoot;
    [SerializeField] private float documentPageFlipOffset = 120f;
    [SerializeField] private float documentPageFlipSeparation = 40f;
    [SerializeField] private float documentPageFlipDuration = 0.18f;
    [SerializeField] private Image publicTrustTrendImage;
    [SerializeField] private Image regionChaosTrendImage;
    [SerializeField] private Sprite trendUpSprite;
    [SerializeField] private Sprite trendDownSprite;
    [SerializeField] private Sprite trendFlatSprite;
    [SerializeField] private AudioClip textSelectionAudioClip;
    [SerializeField] private AudioClip documentExtractAudioClip;
    [SerializeField] private AudioClip[] day3RecorderAAudioClips;
    [SerializeField] private AudioClip[] day3RecorderBAudioClips;
    [SerializeField] private AudioSource uiAudioSource;

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
    private readonly List<CollectedInfoNoteHandler>[] draftSlotNotes =
    {
        new List<CollectedInfoNoteHandler>(),
        new List<CollectedInfoNoteHandler>(),
        new List<CollectedInfoNoteHandler>()
    };
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
        InitializeDocumentPageControls();
        ClearTooltip();
        ClearDocumentView();
        BuildDocumentList();
        InitializeBroadcastDraftSlots();
        InitializeUiAudioSource();
    }

    public void SelectDocument(int documentIndex)
    {
        if (documentIndex < 0 || documentIndex >= currentDay.envelope.documents.Count)
        {
            return;
        }

        DocumentData document = currentDay.envelope.documents[documentIndex];
        PlayDocumentExtractAudio();
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
            bool hasDocumentContent = !string.IsNullOrWhiteSpace(document.fullText);
            if (!hasDocumentContent)
            {
                button.gameObject.SetActive(false);
                button.onClick.RemoveAllListeners();
                continue;
            }

            button.gameObject.SetActive(true);
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
            bool hasDocumentContent = i < currentDay.envelope.documents.Count && !string.IsNullOrWhiteSpace(currentDay.envelope.documents[i].fullText);
            documentButtons[i].gameObject.SetActive(hasDocumentContent && i != selectedDocumentIndex);
        }

        RectTransform listRect = documentListRoot as RectTransform;
        if (listRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }
    }

    public void CollectInfo(InfoNodeData infoNode)
    {
        string noteId = GetInfoNoteId(infoNode.id);
        if (!collectedInfoIds.Add(noteId))
        {
            return;
        }

        PlayTextSelectionAudio();

        currentDay.broadcastResult.collectedInfoNodeIds.Add(infoNode.id);
        currentDay.broadcastResult.totalEffects.trust += infoNode.effects.trust;
        currentDay.broadcastResult.totalEffects.chaos += infoNode.effects.chaos;

        GameObject note = CreateCollectedNote(noteId, infoNode.extractedText);
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

    private void InitializeUiAudioSource()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
        }
    }

    private void PlayTextSelectionAudio()
    {
        PlayUiAudio(textSelectionAudioClip);
    }

    private void PlayDocumentExtractAudio()
    {
        PlayUiAudio(documentExtractAudioClip);
    }

    private void PlayUiAudio(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        InitializeUiAudioSource();
        if (uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    private InfoNodeData FindInfoNode(int infoNodeId)
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

    private List<AudioNodeData> FindAudioNodes(List<int> audioNodeIds)
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

    private AudioNodeData FindAudioNode(int audioNodeId)
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
        UpdateDraftTrendDisplay();
    }

    public void BeginCollectedNoteDrag(CollectedInfoNoteHandler note, Vector2 screenPosition, Camera eventCamera)
    {
        if (!CanDragCollectedNotes)
        {
            return;
        }

        InitializeBroadcastDraftSlots();
        UpdateCollectedNoteDrag(note, screenPosition, eventCamera);
    }

    public void UpdateCollectedNoteDrag(CollectedInfoNoteHandler note, Vector2 screenPosition, Camera eventCamera)
    {
        if (note == null || !CanDragCollectedNotes)
        {
            ClearDraftSlotHighlights();
            return;
        }

        BroadcastDraftSlot hoveredSlot = FindAvailableSlotAt(note, screenPosition, eventCamera);
        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            BroadcastDraftSlot slot = broadcastDraftSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool isAvailable = CanDropIntoSlot(slot, note);
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

        BroadcastDraftSlot targetSlot = FindAvailableSlotAt(note, screenPosition, eventCamera);
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
        note.transform.localScale = Vector3.one;
        note.ApplyCollectionVisual(collectionNoteSize, collectionNoteFontSize);
        note.transform.SetAsLastSibling();
        ArrangeDraftSlot(previousSlotIndex);
        RebuildCollectedInfoLayout();
        UpdateDraftTrendDisplay();
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

    public string GetFinalBroadcastText()
    {
        return currentDay.broadcastResult.finalBroadcastText;
    }

    public void ResolveFinalBroadcastTextFromDrafts()
    {
        List<string> broadcastTexts = new List<string>();
        NodeEffectData totalEffects = new NodeEffectData();

        for (int i = 0; i < draftSlotNotes.Length; i++)
        {
            string matchedText = ResolveDraftSlotBroadcastText(i, totalEffects);
            if (!string.IsNullOrEmpty(matchedText))
            {
                broadcastTexts.Add(matchedText);
            }
        }

        currentDay.broadcastResult.finalBroadcastText = string.Join("\n", broadcastTexts);
        currentDay.broadcastResult.totalEffects = totalEffects;
    }

    private string ResolveDraftSlotBroadcastText(int slotIndex, NodeEffectData totalEffects)
    {
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return string.Empty;
        }

        for (int i = 0; i < draftSlotNotes[slotIndex].Count; i++)
        {
            CollectedInfoNoteHandler note = draftSlotNotes[slotIndex][i];
            if (note != null && note.IsAudioNote)
            {
                string audioKey = TryParseAudioNoteId(note.NoteId, out string parsedAudioKey) ? parsedAudioKey : string.Empty;
                AddBroadcastEffect(totalEffects, GetAudioBroadcastEffectForSet(audioKey));
                return GetAudioBroadcastTextForSet(audioKey);
            }
        }

        List<int> ids = new List<int>();
        for (int i = 0; i < draftSlotNotes[slotIndex].Count; i++)
        {
            CollectedInfoNoteHandler note = draftSlotNotes[slotIndex][i];
            if (note != null && !note.IsAudioNote && TryParseInfoNoteId(note.NoteId, out int infoId) && !ids.Contains(infoId))
            {
                ids.Add(infoId);
            }
        }

        ids.Sort();
        string key = string.Join(",", ids);
        AddBroadcastEffect(totalEffects, GetBroadcastEffectForInfoSet(key));
        return GetBroadcastTextForInfoSet(key);
    }

    private bool TryParseAudioNoteId(string noteId, out string audioKey)
    {
        const string prefix = "audio_nodes_";
        audioKey = string.Empty;
        if (string.IsNullOrEmpty(noteId) || !noteId.StartsWith(prefix))
        {
            return false;
        }

        audioKey = noteId.Substring(prefix.Length).Replace('_', ',');
        return !string.IsNullOrEmpty(audioKey);
    }

    private void AddBroadcastEffect(NodeEffectData target, NodeEffectData value)
    {
        target.trust += value.trust;
        target.chaos += value.chaos;
    }

    private bool TryParseInfoNoteId(string noteId, out int infoId)
    {
        const string prefix = "info_node_";
        infoId = 0;
        return !string.IsNullOrEmpty(noteId) && noteId.StartsWith(prefix) && int.TryParse(noteId.Substring(prefix.Length), out infoId);
    }

    private string GetBroadcastTextForInfoSet(string key)
    {
        switch (key)
        {
            case "1,2,3":
                return "各位听众好，这里是曙光电台。只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。风雪裹城，寒意刺骨，我知道你们在黑暗中数着日子，在寂静中守着窗户。但请记得，你身边永远有并肩的同胞，这座城市永远有不灭的微光、而救援会来的，国家没有忘记我们。在你们看不见的城墙外，一定有人正在赶路。我们只需再撑一撑。等到城门打开那天。等待寒冷的退却。";
            case "1,2":
                return "各位听众好，这里是曙光电台。只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。风雪裹城，寒意刺骨，我知道你们在黑暗中数着日子，在寂静中守着窗户。但请记得，你身边永远有并肩的同胞，这座城市永远有不灭的微光。";
            case "1,3":
                return "各位听众好，这里是曙光电台。只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。只要我们坚持下去，坚持等到国家的救援和补给。在你们看不见的城墙外，一定有人正在赶路。我们只需再撑一撑。等到城门打开那天。等待寒冷的退却。";
            case "2,3":
                return "请记得，你身边永远有并肩的同胞，这座城市也永远有不灭的微光；而救援，终究会来的，因为国家从未忘记我们。在你们看不见的城墙外，一定有人正在赶路。我们只需再撑一撑。等到城门打开那天。等待寒冷的退却。";
            case "4,8,12":
                return "面对寒潮封城、物资紧缺的城市现状，城区居民普遍自发集中采购、囤积煤炭、木炭等各类取暖物资，用以抵御持续低温天气。在物资储备的过程中，不少市民主动关照社区弱势群体，有居民在采购储备物资时，特意为辖区一名无子女照料的残疾独居老人，无偿取用各类生活物资、粮油食材及日用耗材，主动为行动不便的老人补齐生活所需物资，缓解其物资匮乏、出行困难的生存难题，在冰冷的寒潮环境中，自发邻里帮扶的行为持续在社区上演。";
            case "5,7":
                return "受持续寒潮天气影响，城区全域积雪堆积深厚，城市路面与人行步道长期被厚雪覆盖，经过反复低温冻结形成坚硬冰层，整体路面通行条件持续恶化。虽然供电线路、公共电力设备以及路面配套供电设施均在极端天气影响下出现不同程度损坏，造成片区供电不稳、局部断电的情况。但相关保障人员已第一时间抵达各故障点位开展排查与抢修作业，稳步推进受损设备更换、线路加固与电路重启工作，目前城区电力正有序恢复中。";
            case "6,9,10":
                return "城内各社区持续承担日常管理、物资调度与公共设施维护等基础运转工作，针对寒潮降雪冻结带来的通行隐患，主动牵头组织邻里互助清扫工作。当前城区各小区楼道、单元出入口均堆积大量积雪并凝结成坚硬冰层，路面湿滑、地面附着力严重不足，极易导致居民行走时重心失衡、脚步打滑摔倒，民众日常出行安全受到显著影响。通过社区统筹、居民协作的清扫行动，各楼栋单元门口、楼道内外的积雪与结冰层被集中清理，有效消除了多处近距离出行的路面安全隐患，大幅降低了市民意外摔倒的风险，民众日常出行安全由此受到显著影响，小区整体通行环境得到持续改善。";
            case "7,9,12":
                return "受持续寒潮影响，城区全域积雪厚重，路面与人行步道长期积雪冻结硬化，整体路况湿滑、地面附着力不足，出行风险持续升高。城内一名失去子女照料的残疾独居老人，日常无人帮扶看护，今日在户外通行时，因结冰路面路况恶劣，行走过程中重心失衡、脚步不稳意外摔倒，不幸身亡。";
            case "5,6,11":
                return "受风雪影响，大量临街电线杆积雪过载、结构受损相继倒塌，片区大范围断电情况加剧，基础电力供给彻底陷入停滞，路面通行隐患大幅增加，民众日常出行安全受到显著影响。";
            case "4,7,10":
                return "持续寒潮导致城区全域积雪冻结硬化，路面湿滑凶险，居民出行安全隐患激增。多处社区基础运维、物资调度工作陷入停滞，无力整治路况、管控物资，致使辖区出现民众私自取用各类生活、粮油及日用物资的乱象，天气灾害与秩序失管叠加，城市民生乱象持续加重。";
            case "4,5,6":
                return "今日，城区A区域突发恶性秩序事件，暴民集结闯入沿街超市，擅自无偿取用店内各类生活物资、粮油食材及日用耗材，大范围私自取走商超储备物资对城市公共电力系统进行针对性破坏，沿街供电线路、公共电力设备、路面配套供电设施均出现不同程度损毁。造成片区电力系统局部瘫痪。受电力设施停运、寒潮路面结冰双重因素叠加影响，道路可视度大幅降低，路面通行隐患大幅增加，民众日常出行安全受到显著影响，城市基础通行运转秩序遭到破坏。";
            case "7,8,9":
                return "受长期寒潮侵袭，城区全域积雪堆积深厚，路面、步道长期被厚雪覆盖且反复冻结硬化，严寒天气让居家御寒成为市民首要需求，木炭、煤炭等核心取暖物料的使用与储备需求快速激增。为应对持续低温天气，城内大量居民自发集中采购、囤积取暖物资，各物资点位人流密集，市场煤炭流通量持续收紧，物资交易价格出现明显浮动上涨。今天有群众因全域厚雪覆盖的路面结冰湿滑、地面附着力极差，重心失衡脚步不稳意外摔倒，现场抢救无效离世。持续的低温积雪造成全城路况恶劣、通行难度上升，加之取暖物资紧缺引发人群扎堆争抢，大幅抬高了物资申领现场的突发意外风险。";
            case "10,11,12":
                return "受连日极端寒潮与城区持续动荡影响，城内多处社区日常管理、物资调度、设施维护等基础运转功能全面停滞失效，街区公共设施缺乏检修维护，大量临街电线杆积雪过载、结构受损相继倒塌，基础电力供给彻底陷入停滞。瘫痪的社区配套服务让大量弱势群体陷入生存困境，其中一名身有残疾的独居老人，因子女不幸离世，无任何人照料帮扶。老人自身行动能力受限，无法自主外出搜集物资，在多重困境下，基础温饱生活已难以维系，艰难困守在冰封的城区社区之中。";
            case "14,18":
                return "昨日老城区，孩子们上演了一场别开生面的“雪地会战”。整条巷子到处都是他们堆的雪人，他们还分成两队打雪仗，雪球满天飞，战况一度十分激烈。据悉，两边的“指挥官”各自带领十余名小队员，在场边还有“随行人员”负责运送雪球弹药。有趣的是，两帮头目均在现场扬言“不铲除对方誓不罢休”，并许诺参与行动的成员将获得高额奖赏——据称奖品是整盒糖果和三天零花钱。雪仗持续进行，现场有喊叫声和雪球撞击的声响。部分家长在旁观看。截至当前，双方尚未分出胜负，并已就暂停事宜进行沟通。巷内分布着若干雪人，参与者中多人面部发红。";
            case "16,17,18":
                return "接下来是一段观众投稿：连续下了数日的暴雪总算停了，出门总算不用再担心在风雪里迷失方向，这算是近期为数不多的好消息。可我心里清楚，地下的那些帮派混混绝不会放过这个发财的机会。他们准会大量囤积粮食，然后哄抬物价，尤其是面粉、土豆这些基本口粮。我昨天去市场打听了一下，黑市上的面粉价格已经偷偷涨了三成，估计过几天还会翻倍。就算雪停了，咱们老百姓恐怕也过不上什么安生日子，口袋里那点积蓄，怕连半袋面粉都撑不住，更别提肉和蔬菜了。不过，也不全是糟心事。家里的几个小子可算恢复了活力，前几天大雪封门，他们闷在屋里无精打采，现在一出太阳就疯了一样冲到大街上。整条巷子到处都是他们堆的雪人，他们还分成两队打雪仗，雪球满天飞。但愿这样太平的午后能多延续几天。说到底，只要孩子还能笑，日子就不算太坏。";
            case "13,14,15":
                return "昨夜，本市港口区域发生一起严重的帮派武装火并事件，涉事双方为长期争夺该区地下生意的龙河会与黑水帮。双方当晚共出动核心成员二十余人，另有十余名随行人员在场“镇场子”。双方均动用了制式枪械，目前已确认至少十余人当场死亡，受伤者不计其数。火并期间，流弹多次击中附近居民住宅，导致多名无辜居民遭波及。两帮头目均在现场扬言“不铲除对方誓不罢休”，并许诺参与行动的成员将获得高额现金奖赏。截至发稿，港口区域仍弥漫着浓烈的火药味，受此影响，周边居民忧心忡忡，不少家庭开始用铁皮加固门窗，甚至购买防身器具，以防被后续报复行动牵连。社区委员会虽出面呼吁双方克制，但收效甚微。";
            default:
                return string.Empty;
        }
    }

    private string GetAudioBroadcastTextForSet(string key)
    {
        switch (key)
        {
            case "1,2,3":
                return "老城区那边传来一则物资消息。据一位居民反映，西边那间废弃仓库因大风导致门被吹开，内部存有数量可观的物资。这位目击者称，里面堆放着几十个麻袋，装有干豆子和腊肉。不过由于数量较多，该居民表示仅凭个人无法全部搬走，并正在联络他人一同前往搬运，计划按约定分配。";
            case "4,5,6":
                return "老城区方向近日有消息流传，据称有居民在该区域目击到一头熊。据传那熊体型不小，可能重达两百斤左右。若消息属实，两百斤的肉量足够供应一个区居民数月食用。不过目前该说法尚停留在口头传播阶段，未见官方证实，也未确认熊的去向。本台提醒各位听众，若在户外发现野生动物，请务必保持安全距离，切勿靠近或投喂，并第一时间向有关部门报告。老城区及周边居民请多加留意，确保人身安全。后续情况本台将继续跟进。";
            case "1,5":
                return "老城区西边那间废仓库因大风导致门被吹开，有居民进入后发现，里面存放着大量物资。据目击者描述，仓库内有几十个麻袋，装的全是干豆子和腊肉。更有消息称，这些物资合计至少有两百斤。目前该批物资的归属及存量尚未得到官方确认，相关情况有待核实。";
            case "4,5":
                return "近日老城区有传闻称，有居民在该区域目击到一头熊。据透露，那头熊体型较大，估计重达两百斤左右。";
            default:
                return string.Empty;
        }
    }

    private NodeEffectData GetAudioBroadcastEffectForSet(string key)
    {
        switch (key)
        {
            case "1,2,3":
                return new NodeEffectData { trust = 5, chaos = 5 };
            case "4,5,6":
                return new NodeEffectData { trust = 3, chaos = 2 };
            case "1,5":
                return new NodeEffectData { trust = -3, chaos = 2 };
            case "4,5":
                return new NodeEffectData { trust = 1, chaos = -2 };
            default:
                return new NodeEffectData();
        }
    }

    private NodeEffectData GetBroadcastEffectForInfoSet(string key)
    {
        switch (key)
        {
            case "1,2,3":
                return new NodeEffectData { trust = 7, chaos = -3 };
            case "1,2":
                return new NodeEffectData { trust = 4, chaos = -2 };
            case "1,3":
                return new NodeEffectData { trust = 5, chaos = -2 };
            case "2,3":
                return new NodeEffectData { trust = 5, chaos = -2 };
            case "4,8,12":
                return new NodeEffectData { trust = -2, chaos = -5 };
            case "5,7":
                return new NodeEffectData { trust = -1, chaos = -1 };
            case "6,9,10":
                return new NodeEffectData { trust = -2, chaos = -3 };
            case "7,9,12":
                return new NodeEffectData { trust = 2, chaos = -6 };
            case "5,6,11":
                return new NodeEffectData { trust = 3, chaos = -2 };
            case "4,7,10":
                return new NodeEffectData { trust = 1, chaos = -5 };
            case "4,5,6":
                return new NodeEffectData { trust = -4, chaos = -3 };
            case "7,8,9":
                return new NodeEffectData { trust = -2, chaos = -7 };
            case "10,11,12":
                return new NodeEffectData { trust = 3, chaos = -4 };
            case "14,18":
                return new NodeEffectData { trust = -3, chaos = -3 };
            case "16,17,18":
                return new NodeEffectData { trust = 2, chaos = 2 };
            case "13,14,15":
                return new NodeEffectData { trust = -10, chaos = 10 };
            default:
                return new NodeEffectData();
        }
    }

    private void UpdateDraftTrendDisplay()
    {
        NodeEffectData totalEffects = new NodeEffectData();
        for (int i = 0; i < draftSlotNotes.Length; i++)
        {
            ResolveDraftSlotBroadcastText(i, totalEffects);
        }

        SetTrendImage(publicTrustTrendImage, totalEffects.trust);
        SetTrendImage(regionChaosTrendImage, totalEffects.chaos);
    }

    private void SetTrendImage(Image target, int value)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = value > 0 ? trendUpSprite : value < 0 ? trendDownSprite : trendFlatSprite;
        target.enabled = target.sprite != null;
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

    private BroadcastDraftSlot FindAvailableSlotAt(CollectedInfoNoteHandler note, Vector2 screenPosition, Camera eventCamera)
    {
        if (broadcastDraftSlots == null)
        {
            return null;
        }

        for (int i = 0; i < broadcastDraftSlots.Length; i++)
        {
            BroadcastDraftSlot slot = broadcastDraftSlots[i];
            if (slot != null && CanDropIntoSlot(slot, note) && slot.ContainsScreenPoint(screenPosition, eventCamera))
            {
                return slot;
            }
        }

        return null;
    }

    private bool CanDropIntoSlot(BroadcastDraftSlot slot, CollectedInfoNoteHandler note)
    {
        if (slot == null || note == null)
        {
            return false;
        }

        int slotIndex = slot.SlotIndex;
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return false;
        }

        if (note.IsAudioNote)
        {
            return draftSlotNotes[slotIndex].Count == 0 || note.IsInDraft && note.DraftSlotIndex == slotIndex && draftSlotNotes[slotIndex].Count == 1;
        }

        if (DraftSlotContainsAudio(slotIndex))
        {
            return false;
        }

        return draftSlotNotes[slotIndex].Count < maxNotesPerDraftSlot;
    }

    private bool DraftSlotContainsAudio(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= draftSlotNotes.Length)
        {
            return false;
        }

        for (int i = 0; i < draftSlotNotes[slotIndex].Count; i++)
        {
            CollectedInfoNoteHandler note = draftSlotNotes[slotIndex][i];
            if (note != null && note.IsAudioNote)
            {
                return true;
            }
        }

        return false;
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
            UpdateDraftTrendDisplay();
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
        UpdateDraftTrendDisplay();
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
        UpdateDraftTrendDisplay();
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

    public string CollectAudioNodes(string audioTrackId, List<AudioNodeData> audioNodes)
    {
        if (audioNodes.Count == 0)
        {
            return string.Empty;
        }

        string noteId = GetAudioNoteId(audioNodes);
        if (!collectedInfoIds.Add(noteId))
        {
            return string.Empty;
        }

        for (int i = 0; i < audioNodes.Count; i++)
        {
            if (!currentDay.broadcastResult.selectedAudioNodeIds.Contains(audioNodes[i].id))
            {
                currentDay.broadcastResult.selectedAudioNodeIds.Add(audioNodes[i].id);
            }
        }

        string description = GetAudioClipDescription(audioTrackId, audioNodes);
        GameObject note = CreateCollectedNote(noteId, description, null, audioCollectedInfoNoteSprites);
        CollectedInfoNoteHandler handler = note.GetComponent<CollectedInfoNoteHandler>();
        if (handler == null)
        {
            handler = note.AddComponent<CollectedInfoNoteHandler>();
        }
        handler.InitializeAudio(this, noteId);
        handler.ApplyCollectionVisual(collectionNoteSize, collectionNoteFontSize);
        return noteId;
    }

    private string GetAudioClipDescription(string audioTrackId, List<AudioNodeData> audioNodes)
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < audioNodes.Count; i++)
        {
            if (audioNodes[i] != null && !ids.Contains(audioNodes[i].id))
            {
                ids.Add(audioNodes[i].id);
            }
        }

        ids.Sort();
        string key = string.Join(",", ids);
        switch (key)
        {
            case "1,2,3":
            case "1,5":
                return "老城区出现大量物资的消息";
            case "4,5,6":
                return "老城区有熊出现的具体消息";
            case "4,5":
                return "老城区有熊出现的消息";
            default:
                return "平平无奇的消息";
        }
    }

    private GameObject CreateCollectedNote(string noteId, string noteText, Color? noteColor = null, Sprite[] noteSprites = null)
    {
        GameObject note = Instantiate(collectedInfoNoteTemplate, collectedInfoRoot);
        Text text = note.GetComponentInChildren<Text>();
        text.text = noteText;
        text.color = new Color(0.12f, 0.11f, 0.1f, 1f);

        Image noteImage = note.GetComponent<Image>();
        if (noteImage != null)
        {
            Sprite[] sprites = noteSprites != null && noteSprites.Length > 0 ? noteSprites : collectedInfoNoteSprites;
            if (sprites != null && sprites.Length > 0)
            {
                noteImage.sprite = sprites[Random.Range(0, sprites.Length)];
            }
            noteImage.color = noteColor ?? new Color(1f, 1f, 1f, 1f);
        }

        collectedInfoNotes.Add(noteId, note);
        note.SetActive(true);
        return note;
    }

    private string GetInfoNoteId(int infoNodeId)
    {
        return "info_node_" + infoNodeId;
    }

    public void CancelCollectedInfo(InfoNodeData infoNode)
    {
        string noteId = GetInfoNoteId(infoNode.id);
        if (!collectedInfoIds.Remove(noteId))
        {
            return;
        }

        currentDay.broadcastResult.collectedInfoNodeIds.Remove(infoNode.id);
        currentDay.broadcastResult.totalEffects.trust -= infoNode.effects.trust;
        currentDay.broadcastResult.totalEffects.chaos -= infoNode.effects.chaos;

        if (collectedInfoNotes.TryGetValue(noteId, out GameObject note))
        {
            collectedInfoNotes.Remove(noteId);
            Destroy(note);
        }
    }

    private string GetAudioNoteId(List<AudioNodeData> audioNodes)
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < audioNodes.Count; i++)
        {
            if (audioNodes[i] != null && !ids.Contains(audioNodes[i].id))
            {
                ids.Add(audioNodes[i].id);
            }
        }

        ids.Sort();
        return "audio_nodes_" + string.Join("_", ids);
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

        if (TryParseAudioNoteId(noteId, out string audioKey))
        {
            string[] idParts = audioKey.Split(',');
            for (int i = 0; i < idParts.Length; i++)
            {
                if (int.TryParse(idParts[i], out int audioNodeId))
                {
                    currentDay.broadcastResult.selectedAudioNodeIds.Remove(audioNodeId);
                }
            }
        }
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
        if (targetPage < currentDocumentPage)
        {
            MoveBottomDocumentPageUnderTop();
        }
        RectTransform targetVisiblePage = documentPagePanels.Count > 1 ? documentPagePanels[1] : null;
        if (movingPage == null)
        {
            isDocumentPageAnimating = false;
            yield break;
        }

        SetDocumentPageContentsVisible(movingPage, targetVisiblePage);

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

    private void MoveBottomDocumentPageUnderTop()
    {
        if (documentPagePanels.Count <= 2)
        {
            return;
        }

        int lastIndex = documentPagePanels.Count - 1;
        RectTransform movedPage = documentPagePanels[lastIndex];
        documentPagePanels.RemoveAt(lastIndex);
        documentPagePanels.Insert(1, movedPage);
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
        RectTransform topPage = documentPageCount > 1 ? GetTopDocumentPagePanel() : documentPageAnimatedRect != null ? documentPageAnimatedRect : documentSegmentRoot.parent as RectTransform;
        SetDocumentPageContentsVisible(topPage);

        UpdateDocumentPageButtons();
        UpdateDocumentBackPages();
    }

    private void SetDocumentPageContentsVisible(params RectTransform[] visiblePages)
    {
        RectTransform mainPage = documentPageAnimatedRect != null ? documentPageAnimatedRect : documentSegmentRoot.parent as RectTransform;
        bool mainVisible = IsPageInList(mainPage, visiblePages);
        for (int i = 0; i < documentSegmentTexts.Count; i++)
        {
            bool show = mainVisible && i < documentSegmentPages.Count && documentSegmentTexts[i].text != "\n" && documentSegmentPages[i] == 0;
            documentSegmentTexts[i].gameObject.SetActive(show);
        }

        if (documentBackPages == null)
        {
            return;
        }

        for (int i = 0; i < documentBackPages.Length; i++)
        {
            RectTransform backPage = documentBackPages[i];
            if (backPage == null)
            {
                continue;
            }

            RectTransform previewRoot = GetExistingDocumentBackPagePreviewRoot(backPage);
            if (previewRoot != null)
            {
                previewRoot.gameObject.SetActive(IsPageInList(backPage, visiblePages));
            }
        }
    }

    private bool IsPageInList(RectTransform page, RectTransform[] pages)
    {
        if (page == null || pages == null)
        {
            return false;
        }

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == page)
            {
                return true;
            }
        }

        return false;
    }

    private RectTransform GetExistingDocumentBackPagePreviewRoot(RectTransform backPage)
    {
        if (backPage == null)
        {
            return null;
        }

        Transform existing = backPage.Find("DocumentPagePreviewRoot");
        return existing as RectTransform;
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
            if (previewTexts[i] != null)
            {
                previewTexts[i].gameObject.SetActive(false);
            }
        }
    }

    private void ClearAllDocumentBackPagePreviews()
    {
        for (int i = 0; i < documentBackPagePreviewTexts.Count; i++)
        {
            List<Text> previewTexts = documentBackPagePreviewTexts[i];
            for (int j = 0; j < previewTexts.Count; j++)
            {
                if (previewTexts[j] != null)
                {
                    DestroyDocumentPreviewObject(previewTexts[j].gameObject);
                }
            }
        }
        documentBackPagePreviewTexts.Clear();

        if (documentBackPages == null)
        {
            return;
        }

        for (int i = 0; i < documentBackPages.Length; i++)
        {
            RectTransform root = GetExistingDocumentBackPagePreviewRoot(documentBackPages[i]);
            if (root != null)
            {
                DestroyDocumentPreviewObject(root.gameObject);
            }
        }
    }

    private void DestroyDocumentPreviewObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
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
            previousDocumentPageButton.transform.SetAsLastSibling();
        }

        if (nextDocumentPageButton != null)
        {
            nextDocumentPageButton.gameObject.SetActive(hasPages);
            nextDocumentPageButton.interactable = hasPages && !isDocumentPageAnimating && currentDocumentPage < documentPageCount - 1;
            nextDocumentPageButton.transform.SetAsLastSibling();
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
        ClearAllDocumentBackPagePreviews();
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

        if (documentSegmentRoot == null)
        {
            return;
        }

        for (int i = 0; i < documentSegmentRoot.childCount; i++)
        {
            documentSegmentRoot.GetChild(i).gameObject.SetActive(false);
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
        selectedDocumentIndex = -1;
        currentDocumentPage = 0;
        documentPageCount = 1;
        documentPagePanels.Clear();
        UpdateDocumentPageButtons();
        UpdateDocumentBackPages();
        ClearDocumentPagePreview();
        ClearAllDocumentBackPagePreviews();
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
        string firstDayNoticeText =
            "各位听众们好。这里是曙光电台，我是你们的老朋友。频率依旧，时间依旧，我坐在这张混音台前，跟过去的每一天一样。外面的信号断断续续，很多声音我们听不见了，但只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。这声音改变不了什么，但我希望它能让你们在播出时间里，觉得自己不仅仅是一个人。\n\n" +
            "风雪依旧裹挟着整座城市，寒意笼罩街巷，困境缠绕日常，我们都在这场漫长的寒冬里艰难前行，见证着秩序的波折、生活的不易，也体会着独处的煎熬与前路的迷茫。可黑暗从不会彻底吞噬大地，严寒也永远无法冰封人心。在无人留意的角落，有守望相助的温暖悄然涌动，有不曾妥协的坚守默默生长。无论窗外的风雪多么凛冽，无论当下的处境多么艰难，请记得，你身边永远有并肩的同胞，这座城市永远有不灭的微光。\n\n" +
            "天没塌下来。这座城市扛住了，它只是安静下来，像一个人在等消息。沉默不代表放弃，等待不代表遗忘。救援会来的，国家没有忘记我们。在我们看不见的城墙外，他们一定也在用他们的方式，朝我们靠近。我们只需再撑一撑，再耐心一点。等到城门重新打开的那一天，我们就能走出去，告诉他们：你看，我们还在这里，好好地活着。风会停，雪会化，听不见信号的日子不会永远持续。我不知道那天具体是哪天，但我相信它只是还没来。\n\n";
   
        string secondDayNoticeText =
            "今日，城区A区域突发恶性秩序事件，部分暴民集结闯入沿街超市，擅自无偿取用店内各类生活物资、粮油食材及日用耗材，大范围私自取走商超储备物资，彻底打乱了门店正常物资管理秩序。\n\n" +
            "随后，该批暴民转移至城区主干道，对城市公共电力系统进行针对性破坏，沿街供电线路、公共电力设备、路面配套供电设施均出现不同程度损毁。此次人为损毁直接造成片区电力系统局部瘫痪，区域临时供电中止、道路照明全面停用。\n\n" +
            "受电力设施停运、寒潮路面结冰双重因素叠加影响，道路可视度大幅降低，路面通行隐患大幅增加，民众日常出行安全受到显著影响，城市基础通行运转秩序遭到破坏，相关乱象造成的负面影响至今仍持续作用于该片区。";
        string thirdDayGangText =
            "昨夜，本市港口区域发生一起严重的帮派武装火并事件，涉事双方为长期争夺该区地下生意的龙河会与黑水帮。据线人透露，冲突源于双方近期的利益分配纠纷，当晚共出动核心成员二十余人，另有十余名随行人员在场“镇场子”。双方均动用了制式枪械，密集枪声持续数小时，直至深夜才逐渐平息。目前已确认至少十余人当场死亡，受伤者不计其数，多数挂彩，部分重伤者生命垂危。\n\n" +
            "更令人愤慨的是，流弹多次击中附近居民住宅，导致多名无辜居民遭波及。火并期间，两帮头目均在现场扬言“不铲除对方誓不罢休”，并许诺参与行动的成员将获得高额奖赏。\n\n" +
            "截至发稿，港口区域仍弥漫着浓烈的火药味，受此影响，周边居民忧心忡忡，不少家庭开始用铁皮加固门窗，甚至购买防身器具，以防被后续报复行动牵连。社区委员会虽出面呼吁双方克制，但收效甚微。";
        string thirdDayMarketText =
            "连续下了数日的暴雪总算停了，出门总算不用再担心在风雪里迷失方向，这算是近期为数不多的好消息。可我心里清楚，地下的那些帮派混混绝不会放过这个发财的机会。他们准会大量囤积粮食，然后哄抬物价，尤其是面粉、土豆这些基本口粮。\n\n" +
            "我昨天去市场打听了一下，黑市上的面粉价格已经偷偷涨了三成，估计过几天还会翻倍。就算雪停了，咱们老百姓恐怕也过不上什么安生日子，口袋里那点积蓄，怕连半袋面粉都撑不住，更别提肉和蔬菜了。\n\n" +
            "不过，也不全是糟心事。家里的几个小子可算恢复了活力，前几天大雪封门，他们闷在屋里无精打采，现在一出太阳就疯了一样冲到大街上。整条巷子到处都是他们堆的雪人，他们还分成两队打雪仗，雪球满天飞。但愿这样太平的午后能多延续几天。说到底，只要孩子还能笑，日子就不算太坏。";

        DocumentData notice = new DocumentData
        {
            id = $"doc_notice_{dayIndex:00}",
            displayName = dayIndex == 2 ? "城区A秩序事件" : dayIndex == 3 ? "港口帮派火并" : dayPrefix + "市政公告",
            fullText = dayIndex == 2 ? secondDayNoticeText : dayIndex == 3 ? thirdDayGangText : firstDayNoticeText
        };

        if (dayIndex == 2)
        {
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 4,
                displayText = "无偿取用店内各类生活物资、粮油食材及日用耗材",
                type = InfoNodeType.KeyClue,
                priority = 3,
                isMandatory = true,
                effects = new NodeEffectData { trust = 1, chaos = 3 },
                extractedText = "无偿取用生活物资、粮油食材及日用耗材"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 5,
                displayText = "沿街供电线路、公共电力设备、路面配套供电设施均出现不同程度损毁",
                type = InfoNodeType.Evidence,
                priority = 3,
                isMandatory = true,
                effects = new NodeEffectData { trust = 3, chaos = 2 },
                extractedText = "公共电力系统设施出现不同程度损毁"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 6,
                displayText = "民众日常出行安全受到显著影响",
                type = InfoNodeType.KeyClue,
                priority = 2,
                effects = new NodeEffectData { trust = 1, chaos = 1 },
                extractedText = "民众日常出行安全受到显著影响"
            });
        }
        else if (dayIndex == 3)
        {
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 13,
                displayText = "当晚共出动核心成员二十余人，另有十余名随行人员在场“镇场子”",
                type = InfoNodeType.Evidence,
                priority = 3,
                effects = new NodeEffectData { trust = 1, chaos = 3 },
                extractedText = "港口火并出动核心成员二十余人及十余名随行人员"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 14,
                displayText = "两帮头目均在现场扬言“不铲除对方誓不罢休”，并许诺参与行动的成员将获得高额奖赏。",
                type = InfoNodeType.KeyClue,
                priority = 3,
                effects = new NodeEffectData { trust = 1, chaos = 2 },
                extractedText = "两帮头目扬言继续冲突并以高额奖赏鼓动成员"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 15,
                displayText = "不少家庭开始用铁皮加固门窗，甚至购买防身器具",
                type = InfoNodeType.KeyClue,
                priority = 2,
                effects = new NodeEffectData { trust = -2, chaos = 1 },
                extractedText = "周边居民加固门窗并购买防身器具"
            });
        }
        else
        {
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 1,
                displayText = "但只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。",
                type = InfoNodeType.KeyClue,
                priority = 3,
                isMandatory = true,
                effects = new NodeEffectData { trust = 2, chaos = -1 },
                extractedText = "但只要这个麦克风还亮着灯，我就会把我知道的事，一件件说给你们听。"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 2,
                displayText = "请记得，你身边永远有并肩的同胞，这座城市永远有不灭的微光",
                type = InfoNodeType.KeyClue,
                priority = 1,
                effects = new NodeEffectData { trust = 2, chaos = -1 },
                extractedText = "请记得，你身边永远有并肩的同胞，这座城市永远有不灭的微光"
            });
            notice.infoNodes.Add(new InfoNodeData
            {
                id = 3,
                displayText = "救援会来的，国家没有忘记我们。",
                type = InfoNodeType.KeyClue,
                priority = 2,
                effects = new NodeEffectData { trust = 3, chaos = -1 },
                extractedText = "救援会来的，国家没有忘记我们。"
            });
        }

        dayData.envelope.documents.Add(notice);

        if (dayIndex == 3)
        {
            DocumentData marketNotice = new DocumentData
            {
                id = $"doc_market_{dayIndex:00}",
                displayName = "雪停后的黑市",
                fullText = thirdDayMarketText
            };
            marketNotice.infoNodes.Add(new InfoNodeData
            {
                id = 16,
                displayText = "连续下了数日的暴雪总算停了，出门总算不用再担心在风雪里迷失方向",
                type = InfoNodeType.KeyClue,
                priority = 2,
                effects = new NodeEffectData { trust = 1, chaos = -1 },
                extractedText = "暴雪停止，出行不再容易迷失方向"
            });
            marketNotice.infoNodes.Add(new InfoNodeData
            {
                id = 17,
                displayText = "黑市上的面粉价格已经偷偷涨了三成，估计过几天还会翻倍",
                type = InfoNodeType.Evidence,
                priority = 3,
                effects = new NodeEffectData { trust = 1, chaos = -2 },
                extractedText = "黑市面粉价格已上涨三成且可能继续翻倍"
            });
            marketNotice.infoNodes.Add(new InfoNodeData
            {
                id = 18,
                displayText = "整条巷子到处都是他们堆的雪人，他们还分成两队打雪仗，雪球满天飞",
                type = InfoNodeType.Character,
                priority = 1,
                effects = new NodeEffectData { trust = 2, chaos = -2 },
                extractedText = "孩子们在巷子里堆雪人、打雪仗"
            });
            dayData.envelope.documents.Add(marketNotice);
        }

        if (dayIndex == 2)
        {
            DocumentData heatingNotice = new DocumentData
            {
                id = $"doc_heating_{dayIndex:00}",
                displayName = "取暖物资事故",
                fullText =
                    "受长期寒潮侵袭，城区全域积雪堆积深厚，路面、步道长期被厚雪覆盖且反复冻结硬化，整体气温持续低迷，严寒天气让居家御寒成为市民首要需求，木炭、煤炭等核心取暖物料的使用与储备需求快速激增。\n\n" +
                    "为应对持续低温天气，城内大量居民自发集中采购、囤积取暖物资，各物资点位人流密集，市场煤炭流通量持续收紧，物资交易价格出现明显浮动上涨。\n\n" +
                    "今日，在物资集中领取点位，一名市民在与他人争抢取暖物资的过程中，因全域厚雪覆盖的路面结冰湿滑、地面附着力极差，重心失衡脚步不稳意外摔倒，头部剧烈磕碰受损，现场抢救无效离世。持续的低温积雪造成全城路况恶劣、通行难度上升，加之取暖物资紧缺引发人群扎堆争抢，大幅抬高了物资申领现场的突发意外风险。"
            };
            heatingNotice.infoNodes.Add(new InfoNodeData
            {
                id = 7,
                displayText = "城区全域积雪堆积深厚，路面、步道长期被厚雪覆盖且反复冻结硬化",
                type = InfoNodeType.Evidence,
                priority = 2,
                effects = new NodeEffectData { trust = 1, chaos = 2 },
                extractedText = "城区积雪深厚，路面步道长期冻结硬化"
            });
            heatingNotice.infoNodes.Add(new InfoNodeData
            {
                id = 8,
                displayText = "居民自发集中采购、囤积取暖物资",
                type = InfoNodeType.KeyClue,
                priority = 3,
                effects = new NodeEffectData { trust = 3, chaos = -1 },
                extractedText = "居民集中采购并囤积取暖物资"
            });
            heatingNotice.infoNodes.Add(new InfoNodeData
            {
                id = 9,
                displayText = "因全域厚雪覆盖的路面结冰湿滑、地面附着力极差，重心失衡脚步不稳意外摔倒",
                type = InfoNodeType.Evidence,
                priority = 3,
                isMandatory = true,
                effects = new NodeEffectData { trust = 1, chaos = 4 },
                extractedText = "因全域厚雪覆盖的路面结冰湿滑、地面附着力极差，重心失衡脚步不稳意外摔倒"
            });
            dayData.envelope.documents.Add(heatingNotice);

            DocumentData communityNotice = new DocumentData
            {
                id = $"doc_community_{dayIndex:00}",
                displayName = "社区服务停滞",
                fullText =
                    "受连日极端寒潮与城区持续动荡影响，城内多处社区日常管理、物资调度、设施维护等基础运转功能全面停滞失效，社区原有服务体系彻底失去兜底能力。\n\n" +
                    "长期风雪积压无人处置，街区公共设施缺乏检修维护，大量临街电线杆积雪过载、结构受损相继倒塌，片区大范围断电情况加剧，基础电力供给彻底陷入停滞。\n\n" +
                    "恶劣的城市环境与瘫痪的社区配套服务，让大量弱势群体陷入生存困境，其中一名身有残疾的独居老人，因子女不幸离世，无任何人照料帮扶。老人自身行动能力受限，无法自主外出搜集物资、清扫通行积雪，在持续低温、无电力补给、无外界帮扶的多重困境下，日常饮食起居、基础温饱生活已难以维系，艰难困守在冰封的城区社区之中。"
            };
            communityNotice.infoNodes.Add(new InfoNodeData
            {
                id = 10,
                displayText = "城内多处社区日常管理、物资调度、设施维护等基础运转功能",
                type = InfoNodeType.KeyClue,
                priority = 3,
                effects = new NodeEffectData { trust = 3, chaos = 2 },
                extractedText = "城内多处社区日常管理、物资调度、设施维护等基础运转功能"
            });
            communityNotice.infoNodes.Add(new InfoNodeData
            {
                id = 11,
                displayText = "临街电线杆积雪过载、结构受损相继倒塌",
                type = InfoNodeType.Evidence,
                priority = 2,
                effects = new NodeEffectData { trust = 2, chaos = 2 },
                extractedText = "临街电线杆积雪过载、结构受损相继倒塌"
            });
            communityNotice.infoNodes.Add(new InfoNodeData
            {
                id = 12,
                displayText = "一名身有残疾的独居老人，因子女不幸离世，无任何人照料帮扶",
                type = InfoNodeType.Character,
                priority = 2,
                effects = new NodeEffectData { trust = 1, chaos = 1 },
                extractedText = "一名身有残疾的独居老人，因子女不幸离世，无任何人照料帮扶"
            });
            dayData.envelope.documents.Add(communityNotice);
        }

        if (dayIndex == 3)
        {
            AudioTrackData recorderA = new AudioTrackData
            {
                id = $"audio_recorder_a_{dayIndex:00}",
                displayName = "录音笔 A"
            };
            recorderA.audioNodes.Add(new AudioNodeData
            {
                id = 1, audioFile = GetDay3RecorderClip(day3RecorderAAudioClips, 0), contentText = $"老城区西边那间废仓库的门被风吹开了，我昨天进去看了看，全是物资！", displayTime = 3.2f
            });
            recorderA.audioNodes.Add(new AudioNodeData
            {
                id = 2, audioFile = GetDay3RecorderClip(day3RecorderAAudioClips, 1), contentText = $"里面有几十个麻袋，装的全是干豆子和腊肉！", displayTime = 2.8f
            });
            recorderA.audioNodes.Add(new AudioNodeData
            {
                id = 3, audioFile = GetDay3RecorderClip(day3RecorderAAudioClips, 2), contentText = "但我一个人搬不完，要不你叫上老李，跟我一起去搬空仓库，我们对着分？", displayTime = 2.1f
            });

            AudioTrackData recorderB = new AudioTrackData
            {
                id = $"audio_recorder_b_{dayIndex:00}",
                displayName = "录音笔 B"
            };
            recorderB.audioNodes.Add(new AudioNodeData
            {
                id = 4, audioFile = GetDay3RecorderClip(day3RecorderBAudioClips, 0), contentText = "你听说了吗？最近老城区那边听说有熊。", displayTime = 3.5f
            });
            recorderB.audioNodes.Add(new AudioNodeData
            {
                id = 5, audioFile = GetDay3RecorderClip(day3RecorderBAudioClips, 1), contentText = "我听别人说，那玩意儿至少有两百斤！", displayTime = 3f
            });
            recorderB.audioNodes.Add(new AudioNodeData
            {
                id = 6, audioFile = GetDay3RecorderClip(day3RecorderBAudioClips, 2), contentText = "唉！两百斤，都够我们区吃上好几个月了！", displayTime = 3f
            });

            dayData.envelope.audioTracks.Add(recorderA);
            dayData.envelope.audioTracks.Add(recorderB);
        }
        return dayData;
    }

    private AudioClip GetDay3RecorderClip(AudioClip[] clips, int index)
    {
        return clips != null && index >= 0 && index < clips.Length ? clips[index] : null;
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
