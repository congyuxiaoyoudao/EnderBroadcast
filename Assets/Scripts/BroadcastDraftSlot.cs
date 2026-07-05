using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BroadcastDraftSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Text titleText;
    [SerializeField] private Color availableHighlightColor = new Color(1f, 0.91f, 0.36f, 1f);
    [SerializeField] private Color hoverHighlightColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private float highlightBorderThickness = 4f;

    private InfoCollectionController controller;
    private RectTransform rectTransform;
    private RectTransform highlightBorderRoot;
    private Image[] highlightBorderImages;

    public int SlotIndex => slotIndex;
    public RectTransform RectTransform
    {
        get
        {
            CacheComponents();
            return rectTransform;
        }
    }

    public void Configure(int index, string title)
    {
        slotIndex = index;
        if (titleText == null)
        {
            titleText = GetComponentInChildren<Text>();
        }
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    public void Initialize(InfoCollectionController infoCollectionController)
    {
        controller = infoCollectionController;
        CacheComponents();
        SetHighlight(false, false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Drop placement is resolved by CollectedInfoNoteHandler.OnEndDrag so the same
        // logic works when a note is released over child graphics inside this slot.
    }

    public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
    {
        CacheComponents();
        return rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    public void SetHighlight(bool isAvailable, bool isHovered)
    {
        CacheComponents();
        EnsureHighlightBorder();
        if (highlightBorderRoot == null)
        {
            return;
        }

        highlightBorderRoot.gameObject.SetActive(isAvailable);
        Color highlightColor = isHovered ? hoverHighlightColor : availableHighlightColor;
        for (int i = 0; i < highlightBorderImages.Length; i++)
        {
            if (highlightBorderImages[i] != null)
            {
                highlightBorderImages[i].color = highlightColor;
            }
        }
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
        Outline oldOutline = GetComponent<Outline>();
        if (oldOutline != null && oldOutline.enabled)
        {
            oldOutline.enabled = false;
        }
    }

    private void EnsureHighlightBorder()
    {
        if (highlightBorderRoot != null && highlightBorderImages != null && highlightBorderImages.Length == 4)
        {
            return;
        }

        Transform existingRoot = transform.Find("DraftSlotHighlightBorder");
        if (existingRoot != null)
        {
            highlightBorderRoot = existingRoot as RectTransform;
        }
        else
        {
            GameObject borderRootObject = new GameObject("DraftSlotHighlightBorder", typeof(RectTransform));
            highlightBorderRoot = borderRootObject.transform as RectTransform;
            highlightBorderRoot.SetParent(transform, false);
        }

        highlightBorderRoot.anchorMin = Vector2.zero;
        highlightBorderRoot.anchorMax = Vector2.one;
        highlightBorderRoot.offsetMin = Vector2.zero;
        highlightBorderRoot.offsetMax = Vector2.zero;
        highlightBorderRoot.pivot = new Vector2(0.5f, 0.5f);
        highlightBorderRoot.SetAsLastSibling();

        highlightBorderImages = new Image[4];
        highlightBorderImages[0] = EnsureBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, highlightBorderThickness));
        highlightBorderImages[1] = EnsureBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, highlightBorderThickness));
        highlightBorderImages[2] = EnsureBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(highlightBorderThickness, 0f));
        highlightBorderImages[3] = EnsureBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(highlightBorderThickness, 0f));
        highlightBorderRoot.gameObject.SetActive(false);
    }

    private Image EnsureBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        Transform existingLine = highlightBorderRoot.Find(lineName);
        RectTransform lineRect;
        Image lineImage;

        if (existingLine != null)
        {
            lineRect = existingLine as RectTransform;
            lineImage = existingLine.GetComponent<Image>();
            if (lineImage == null)
            {
                lineImage = existingLine.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineRect = lineObject.transform as RectTransform;
            lineRect.SetParent(highlightBorderRoot, false);
            lineImage = lineObject.GetComponent<Image>();
        }

        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.pivot = pivot;
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = sizeDelta;
        lineImage.raycastTarget = false;
        return lineImage;
    }
}
