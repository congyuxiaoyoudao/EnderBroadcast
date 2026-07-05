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
    [SerializeField] private Text subtitleText;
    [SerializeField] private int maxSelectedAudioNodes = 3;

    private const string MixedAudioTrackId = "mixed";

    private AudioSource previewSource;
    private Coroutine subtitleRoutine;
    private readonly List<Button> recorderButtons = new List<Button>();
    private readonly List<AudioNodeDragItem> nodeItems = new List<AudioNodeDragItem>();
    private readonly List<AudioNodeData> selectedNodes = new List<AudioNodeData>();
    private readonly Dictionary<string, List<AudioNodeData>> collectedAudioNoteNodes = new Dictionary<string, List<AudioNodeData>>();
    private IReadOnlyList<AudioNodeData> currentAudioNodes;
    private string currentAudioTrackId;

    private void Awake()
    {
        if (infoCollectionController == null)
        {
            infoCollectionController = GetComponentInParent<InfoCollectionController>();
        }
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
        previewSource = GetComponent<AudioSource>();
        if (previewSource == null)
        {
            previewSource = gameObject.AddComponent<AudioSource>();
        }
        ClearSubtitle();
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
        collectedAudioNoteNodes.Clear();
        RecycleNodes();
        ClearTrack(trackOneDropZone);
        ClearTrack(trackTwoDropZone);
        ClearSubtitle();
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
                AudioClipTrackItem clipItem = child.GetComponent<AudioClipTrackItem>();
                if (clipItem != null)
                {
                    selectedNodes.Remove(clipItem.NodeData);
                }
                Destroy(child.gameObject);
            }
        }
    }

    private void BuildRecorderList()
    {
        if (infoCollectionController == null)
        {
            infoCollectionController = GetComponentInParent<InfoCollectionController>();
        }
        if (infoCollectionController == null)
        {
            return;
        }

        IReadOnlyList<AudioTrackData> tracks = infoCollectionController.GetAudioTracks();
        for (int i = 0; i < tracks.Count; i++)
        {
            Button button = GetRecorderButton(i);
            button.gameObject.SetActive(true);
            ConfigureRecorderButtonVisual(button, i);
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
            Button button = Instantiate(recorderButtonTemplate, recorderListRoot);
            button.gameObject.SetActive(true);
            recorderButtons.Add(button);
        }

        return recorderButtons[index];
    }

    private void ConfigureRecorderButtonVisual(Button button, int index)
    {
        if (button == null)
        {
            return;
        }

        Transform penOne = button.transform.Find("RecorderPenOneImage");
        Transform penTwo = button.transform.Find("RecorderPenTwoImage");
        if (penOne == null || penTwo == null)
        {
            return;
        }

        bool useFirstPen = index % 2 == 0;
        RectTransform activePen = (useFirstPen ? penOne : penTwo) as RectTransform;
        RectTransform inactivePen = (useFirstPen ? penTwo : penOne) as RectTransform;

        penOne.gameObject.SetActive(useFirstPen);
        penTwo.gameObject.SetActive(!useFirstPen);
        ConfigureRecorderButtonRect(button, useFirstPen);
        ConfigureRecorderPenRect(activePen);
        if (inactivePen != null)
        {
            inactivePen.gameObject.SetActive(false);
        }
    }

    private void ConfigureRecorderButtonRect(Button button, bool useFirstPen)
    {
        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        Vector2 position = useFirstPen ? new Vector2(-23.9773f, -25.475f) : new Vector2(31.8164f, -10.79f);
        Vector2 size = useFirstPen ? new Vector2(96.2069f, 236.72f) : new Vector2(131.5664f, 213.37f);
        Vector2 pivot = useFirstPen ? new Vector2(0.5213464f, 0.3930433f) : new Vector2(0.5091798f, 0.4374269f);
        float zRotation = useFirstPen ? 18f : -6f;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }
    }

    private void ConfigureRecorderPenRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void SelectRecorder(int index)
    {
        IReadOnlyList<AudioTrackData> tracks = infoCollectionController.GetAudioTracks();
        if (index < 0 || index >= tracks.Count)
        {
            return;
        }

        BuildWaveform(tracks[index]);
        ClearSubtitle();
    }

    private void BuildWaveform(AudioTrackData track)
    {
        currentAudioTrackId = track.id;
        currentAudioNodes = track.audioNodes;
        RecycleNodes();
        for (int i = 0; i < track.audioNodes.Count; i++)
        {
            AudioNodeDragItem item = GetNodeItem(i);
            item.Initialize(this, track.audioNodes[i]);
            item.transform.SetSiblingIndex(i);
            item.gameObject.SetActive(!selectedNodes.Contains(track.audioNodes[i]) && !IsAudioNodeCollected(track.audioNodes[i]));
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
        if (node == null || selectedNodes.Contains(node) || selectedNodes.Count >= maxSelectedAudioNodes)
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
        AudioClipTrackItem clipItem = copy.AddComponent<AudioClipTrackItem>();
        clipItem.Initialize(this, node);
        copy.transform.SetAsLastSibling();
        SetSourceNodeVisible(node, false);
    }

    public void RestoreSourceNodesForAudioNote(string noteId)
    {
        if (!collectedAudioNoteNodes.TryGetValue(noteId, out List<AudioNodeData> nodes))
        {
            return;
        }

        collectedAudioNoteNodes.Remove(noteId);
        for (int i = 0; i < nodes.Count; i++)
        {
            SetSourceNodeVisible(nodes[i], true);
        }
        RestoreWaveformOrder();
    }

    public void RestoreWaveformOrder()
    {
        if (currentAudioNodes == null)
        {
            return;
        }

        for (int i = 0; i < currentAudioNodes.Count; i++)
        {
            AudioNodeDragItem item = FindSourceNodeItem(currentAudioNodes[i]);
            if (item != null)
            {
                item.transform.SetSiblingIndex(i);
            }
        }
    }

    private void SetSourceNodeVisible(AudioNodeData node, bool visible)
    {
        AudioNodeDragItem item = FindSourceNodeItem(node);
        if (item != null)
        {
            item.gameObject.SetActive(visible);
            RestoreWaveformOrder();
        }
    }

    private AudioNodeDragItem FindSourceNodeItem(AudioNodeData node)
    {
        for (int i = 0; i < nodeItems.Count; i++)
        {
            if (nodeItems[i].NodeData == node)
            {
                return nodeItems[i];
            }
        }

        return null;
    }

    private bool IsAudioNodeCollected(AudioNodeData node)
    {
        foreach (List<AudioNodeData> nodes in collectedAudioNoteNodes.Values)
        {
            if (nodes.Contains(node))
            {
                return true;
            }
        }

        return false;
    }

    private string GetAudioNoteId(string audioTrackId)
    {
        return "audio_track_" + audioTrackId;
    }

    public void RemoveNodeFromTrack(AudioNodeData node, GameObject clipObject)
    {
        if (node != null)
        {
            selectedNodes.Remove(node);
            SetSourceNodeVisible(node, true);
        }
        Destroy(clipObject);
    }

    public void PlayPreview(AudioNodeData node)
    {
        if (node == null)
        {
            return;
        }

        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
        }

        if (previewSource != null && node.audioFile != null)
        {
            previewSource.Stop();
            previewSource.clip = node.audioFile;
            previewSource.Play();
        }

        subtitleRoutine = StartCoroutine(ShowSubtitle(node));
    }

    private System.Collections.IEnumerator ShowSubtitle(AudioNodeData node)
    {
        if (subtitleText != null)
        {
            subtitleText.text = node.contentText;
            subtitleText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, node.displayTime));
        FinishSubtitle();
    }

    private void FinishSubtitle()
    {
        subtitleRoutine = null;
        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
            subtitleText.gameObject.SetActive(false);
        }
    }

    private void ClearSubtitle()
    {
        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = null;
        }
        if (previewSource != null)
        {
            previewSource.Stop();
        }
        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
            subtitleText.gameObject.SetActive(false);
        }
    }

    private void CompleteEdit()
    {
        if (selectedNodes.Count == 0)
        {
            return;
        }

        List<AudioNodeData> completedNodes = new List<AudioNodeData>(selectedNodes);
        string noteId = infoCollectionController.CollectAudioNodes(MixedAudioTrackId, completedNodes);
        if (string.IsNullOrEmpty(noteId))
        {
            return;
        }

        collectedAudioNoteNodes[noteId] = completedNodes;
        selectedNodes.Clear();
        ClearTrack(trackOneDropZone);
        ClearTrack(trackTwoDropZone);
    }
}
