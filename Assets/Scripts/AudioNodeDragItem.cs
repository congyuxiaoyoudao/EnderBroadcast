using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioNodeDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Text label;

    private AudioClipEditingController controller;
    private AudioNodeData nodeData;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    public AudioNodeData NodeData => nodeData;

    public void Initialize(AudioClipEditingController editingController, AudioNodeData node)
    {
        controller = editingController;
        nodeData = node;
        if (label == null)
        {
            label = GetComponentInChildren<Text>();
        }
        if (label != null)
        {
            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }
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
        controller.RestoreWaveformOrder();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.PlayPreview(nodeData);
    }
}
