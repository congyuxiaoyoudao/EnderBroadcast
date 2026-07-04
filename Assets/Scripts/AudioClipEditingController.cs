using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioClipEditingController : MonoBehaviour
{
    [SerializeField] private InfoCollectionController infoCollectionController;
    [SerializeField] private Transform recorderListRoot;
    [SerializeField] private Button recorderButtonTemplate;
    [SerializeField] private Transform waveformRoot;
    [SerializeField] private AudioNodeDragItem audioNodeTemplate;
    [SerializeField] private AudioTrackDropZone trackOneDropZone;
    [SerializeField] private AudioTrackDropZone trackTwoDropZone;
    [SerializeField] private Button completeEditButton;

    private readonly List<Button> recorderButtons = new List<Button>();
    private readonly List<AudioNodeDragItem> nodeItems = new List<AudioNodeDragItem>();
    private readonly List<AudioNodeData> selectedNodes = new List<AudioNodeData>();

    private void Awake()
    {
        recorderButtonTemplate.gameObject.SetActive(false);
        audioNodeTemplate.gameObject.SetActive(false);
        if (trackOneDropZone != null)
        {
            trackOneDropZone.Initialize(this);
        }
        if (trackTwoDropZone != null)
        {
            trackTwoDropZone.Initialize(this);
        }
        completeEditButton.onClick.RemoveAllListeners();
        completeEditButton.onClick.AddListener(CompleteEdit);
    }

    private void Start()
    {
        BuildRecorderList();
    }

    private void OnEnable()
    {
        BuildRecorderList();
    }

    public void ResetForDay()
    {
        selectedNodes.Clear();
        RecycleNodes();
        ClearTrack(trackOneDropZone);
        ClearTrack(trackTwoDropZone);
        BuildRecorderList();
    }

    private void ClearTrack(AudioTrackDropZone dropZone)
    {
        if (dropZone == null)
        {
            return;
        }

        Transform root = dropZone.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.name != "Label")
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void BuildRecorderList()
    {
        IReadOnlyList<AudioTrackData> tracks = infoCollectionController.GetAudioTracks();
        for (int i = 0; i < tracks.Count; i++)
        {
            Button button = GetRecorderButton(i);
            button.GetComponentInChildren<Text>().text = "●\n" + tracks[i].displayName;
            int trackIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectRecorder(trackIndex));
            button.gameObject.SetActive(true);
        }

        for (int i = tracks.Count; i < recorderButtons.Count; i++)
        {
            recorderButtons[i].gameObject.SetActive(false);
        }
    }

    private Button GetRecorderButton(int index)
    {
        while (recorderButtons.Count <= index)
        {
            recorderButtons.Add(Instantiate(recorderButtonTemplate, recorderListRoot));
        }

        return recorderButtons[index];
    }

    private void SelectRecorder(int index)
    {
        IReadOnlyList<AudioTrackData> tracks = infoCollectionController.GetAudioTracks();
        if (index < 0 || index >= tracks.Count)
        {
            return;
        }

        BuildWaveform(tracks[index]);
    }

    private void BuildWaveform(AudioTrackData track)
    {
        RecycleNodes();
        for (int i = 0; i < track.audioNodes.Count; i++)
        {
            AudioNodeDragItem item = GetNodeItem(i);
            item.Initialize(track.audioNodes[i]);
            item.gameObject.SetActive(true);
        }
    }

    private AudioNodeDragItem GetNodeItem(int index)
    {
        while (nodeItems.Count <= index)
        {
            nodeItems.Add(Instantiate(audioNodeTemplate, waveformRoot));
        }

        return nodeItems[index];
    }

    private void RecycleNodes()
    {
        for (int i = 0; i < nodeItems.Count; i++)
        {
            nodeItems[i].gameObject.SetActive(false);
        }
    }

    public void AddNodeToTrack(AudioNodeData node, Transform trackRoot)
    {
        if (node == null || selectedNodes.Contains(node))
        {
            return;
        }

        selectedNodes.Add(node);
        GameObject copy = new GameObject(node.id + "_Clip", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        copy.transform.SetParent(trackRoot, false);
        Image image = copy.GetComponent<Image>();
        image.color = new Color(0.38f, 0.65f, 1f, 1f);

        Text label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(copy.transform, false);
        label.text = node.contentText;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 0f);
        labelRect.offsetMax = new Vector2(-4f, 0f);

        RectTransform rect = copy.GetComponent<RectTransform>();
        Vector2 size = new Vector2(Mathf.Max(120f, node.displayTime * 45f), 36f);
        rect.sizeDelta = size;
        LayoutElement layoutElement = copy.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        copy.transform.SetAsLastSibling();
    }

    private void CompleteEdit()
    {
        infoCollectionController.CollectAudioNodes(selectedNodes);
    }
}

public class AudioNodeDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Text label;

    private AudioNodeData nodeData;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    public AudioNodeData NodeData => nodeData;

    public void Initialize(AudioNodeData node)
    {
        nodeData = node;
        if (label == null)
        {
            label = GetComponentInChildren<Text>();
        }
        label.text = node.contentText;
        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(Mathf.Max(120f, node.displayTime * 55f), 46f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = (RectTransform)transform;
        startPosition = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = startPosition;
    }
}
