using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoCollectionController : MonoBehaviour
{
    [SerializeField] private BroadcastDayData currentDay = new BroadcastDayData();
    [SerializeField] private Transform documentListRoot;
    [SerializeField] private Button documentButtonTemplate;
    [SerializeField] private Text documentTitleText;
    [SerializeField] private Text documentBodyText;
    [SerializeField] private Transform documentSegmentRoot;
    [SerializeField] private Text documentSegmentTemplate;
    [SerializeField] private Transform infoHotspotRoot;
    [SerializeField] private Image infoHotspotTemplate;
    [SerializeField] private Transform collectedInfoRoot;
    [SerializeField] private Text collectedInfoTemplate;
    [SerializeField] private Text documentTooltipText;

    private readonly List<Button> documentButtons = new List<Button>();
    private readonly List<Text> collectedInfoTexts = new List<Text>();
    private readonly List<Text> documentSegmentTexts = new List<Text>();
    private readonly List<InfoHotspotHandler> infoHotspots = new List<InfoHotspotHandler>();
    private readonly HashSet<string> collectedInfoIds = new HashSet<string>();
    private readonly List<InfoTextRange> currentInfoRanges = new List<InfoTextRange>();
    private InfoHotspotHandler hoveredHotspot;

    private void Awake()
    {
        EnsureSampleData();
        InitializeDocumentTextClickHandler();
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
        documentTitleText.text = document.displayName;
        documentBodyText.text = string.Empty;
        BuildDocumentSegments(document);
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
        RecycleDocumentButtons();

        for (int i = 0; i < currentDay.envelope.documents.Count; i++)
        {
            DocumentData document = currentDay.envelope.documents[i];
            Button button = GetDocumentButton(i);
            button.GetComponentInChildren<Text>().text = document.displayName;

            int documentIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectDocument(documentIndex));

            DocumentHoverHandler hoverHandler = button.GetComponent<DocumentHoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = button.gameObject.AddComponent<DocumentHoverHandler>();
            }
            hoverHandler.Initialize(this, documentIndex);
            button.gameObject.SetActive(true);
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

        Text text = GetCollectedInfoText(collectedInfoTexts.Count);
        text.text = infoNode.extractedText;
        text.color = GetInfoNodeColor(infoNode.type);
        text.gameObject.SetActive(true);
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

    private Text GetCollectedInfoText(int index)
    {
        while (collectedInfoTexts.Count <= index)
        {
            Text text = Instantiate(collectedInfoTemplate, collectedInfoRoot);
            collectedInfoTexts.Add(text);
        }

        return collectedInfoTexts[index];
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
                Text plainSegment = GetDocumentSegment(segmentIndex++);
                SetupDocumentSegment(plainSegment, plainText.Substring(cursor, range.startIndex - cursor), new Color(0.12f, 0.11f, 0.1f, 1f), null);
            }

            Text infoSegment = GetDocumentSegment(segmentIndex++);
            SetupDocumentSegment(infoSegment, plainText.Substring(range.startIndex, range.length), GetInfoNodeColor(range.infoNode.type), range.infoNode);
            currentInfoRanges.Add(range);
            cursor = range.startIndex + range.length;
        }

        if (cursor < plainText.Length)
        {
            Text plainSegment = GetDocumentSegment(segmentIndex++);
            SetupDocumentSegment(plainSegment, plainText.Substring(cursor), new Color(0.12f, 0.11f, 0.1f, 1f), null);
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

        currentDay.id = "day_01";
        currentDay.dayIndex = 1;
        currentDay.envelope.id = "envelope_01";

        DocumentData notice = new DocumentData
        {
            id = "doc_notice_01",
            displayName = "市政公告",
            fullText = "市政厅发布夜间交通管制公告，旧城区连续三晚出现异常停电。"
        };
        notice.infoNodes.Add(new InfoNodeData
        {
            id = "info_power_outage",
            displayText = "旧城区连续三晚出现异常停电",
            type = InfoNodeType.KeyClue,
            priority = 3,
            isMandatory = true,
            effects = new NodeEffectData { trust = 2, chaos = 1 },
            extractedText = "连续三晚"
        });
        notice.infoNodes.Add(new InfoNodeData
        {
            id = "info_city_hall",
            displayText = "市政厅发布夜间交通管制公告",
            type = InfoNodeType.Location,
            priority = 1,
            effects = new NodeEffectData { trust = 1, chaos = 0 },
            extractedText = "夜间交通管制"
        });

        DocumentData letter = new DocumentData
        {
            id = "doc_letter_01",
            displayName = "匿名来信",
            fullText = "一名仓库员工声称，港口仓库最近夜间频繁有未登记车辆出入。"
        };
        letter.infoNodes.Add(new InfoNodeData
        {
            id = "info_warehouse_worker",
            displayText = "一名仓库员工",
            type = InfoNodeType.Character,
            priority = 2,
            effects = new NodeEffectData { trust = 1, chaos = 0 },
            extractedText = "匿名仓库员工提供线索"
        });
        letter.infoNodes.Add(new InfoNodeData
        {
            id = "info_unregistered_cars",
            displayText = "港口仓库最近夜间频繁有未登记车辆出入",
            type = InfoNodeType.Evidence,
            priority = 3,
            isMandatory = true,
            effects = new NodeEffectData { trust = 2, chaos = 2 },
            extractedText = "港口仓库存在未登记车辆夜间出入"
        });

        currentDay.envelope.documents.Add(notice);
        currentDay.envelope.documents.Add(letter);
    }
    private class InfoTextRange
    {
        public int startIndex;
        public int length;
        public InfoNodeData infoNode;
    }
}

public class InfoTextSegmentHandler : MonoBehaviour, IPointerClickHandler
{
    private InfoCollectionController controller;
    private InfoNodeData infoNode;

    public void Initialize(InfoCollectionController infoCollectionController, InfoNodeData node)
    {
        controller = infoCollectionController;
        infoNode = node;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.CollectInfo(infoNode);
    }
}

public class InfoHotspotHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private InfoCollectionController controller;
    private InfoNodeData infoNode;
    private Image image;
    private Color highlightColor;

    public void Initialize(InfoCollectionController infoCollectionController, InfoNodeData node, Color nodeColor)
    {
        controller = infoCollectionController;
        infoNode = node;
        image = GetComponent<Image>();
        highlightColor = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0f);
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        image.color = highlighted ? highlightColor : new Color(1f, 1f, 1f, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.HoverInfo(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.ClearHoveredInfo(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.CollectInfo(infoNode);
    }
}

public class DocumentHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InfoCollectionController controller;
    private int documentIndex;

    public void Initialize(InfoCollectionController infoCollectionController, int index)
    {
        controller = infoCollectionController;
        documentIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.ShowDocumentTooltip(documentIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.ClearTooltip();
    }
}
