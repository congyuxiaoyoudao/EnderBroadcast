using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CollectedInfoNoteHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InfoCollectionController controller;
    private InfoNodeData infoNode;
    private string audioNoteId;
    private string noteId;
    private bool isAudioNote;
    private bool isInDraft;
    private int draftSlotIndex = -1;
    private Canvas dragCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private Transform dragStartParent;
    private int dragStartSiblingIndex;
    private Vector2 dragStartAnchoredPosition;
    private Vector2 dragStartAnchorMin;
    private Vector2 dragStartAnchorMax;
    private Vector2 dragStartPivot;
    private Vector2 dragStartSizeDelta;
    private Vector3 dragStartLocalScale;
    private bool dragStartIgnoreLayout;
    private bool isDragging;
    private bool suppressNextClick;
    private Text label;
    private int collectionFontSize;
    private bool collectionBestFit;
    private int collectionMinSize;
    private int collectionMaxSize;
    private TextAnchor collectionAlignment;

    public void Initialize(InfoCollectionController infoCollectionController, InfoNodeData node)
    {
        controller = infoCollectionController;
        infoNode = node;
        audioNoteId = null;
        noteId = node != null ? node.id : string.Empty;
        isAudioNote = false;
        MarkAsCollectionNote();
    }

    public void InitializeAudio(InfoCollectionController infoCollectionController, string noteId)
    {
        controller = infoCollectionController;
        infoNode = null;
        audioNoteId = noteId;
        this.noteId = noteId;
        isAudioNote = true;
        MarkAsCollectionNote();
    }

    public string NoteId => noteId;
    public bool IsAudioNote => isAudioNote;
    public bool IsInDraft => isInDraft;
    public int DraftSlotIndex => draftSlotIndex;
    public RectTransform RectTransform
    {
        get
        {
            CacheComponents();
            return rectTransform;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        if (controller != null && controller.CanReturnDraftNotes && isInDraft)
        {
            controller.ReturnDraftNoteToCollection(this);
            return;
        }

        if (controller == null || !controller.CanCancelCollectedNotes)
        {
            return;
        }

        if (infoNode != null)
        {
            controller.CancelCollectedInfo(infoNode);
            return;
        }

        if (!string.IsNullOrEmpty(audioNoteId))
        {
            controller.CancelCollectedAudio(audioNoteId);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null || !controller.CanDragCollectedNotes)
        {
            return;
        }

        CacheComponents();
        dragCanvas = GetComponentInParent<Canvas>();
        if (dragCanvas == null || rectTransform == null)
        {
            return;
        }

        dragStartParent = transform.parent;
        dragStartSiblingIndex = transform.GetSiblingIndex();
        dragStartAnchoredPosition = rectTransform.anchoredPosition;
        dragStartAnchorMin = rectTransform.anchorMin;
        dragStartAnchorMax = rectTransform.anchorMax;
        dragStartPivot = rectTransform.pivot;
        dragStartSizeDelta = rectTransform.sizeDelta;
        dragStartLocalScale = transform.localScale;
        dragStartIgnoreLayout = layoutElement != null && layoutElement.ignoreLayout;

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.82f;
            canvasGroup.blocksRaycasts = false;
        }

        transform.SetParent(dragCanvas.transform, true);
        transform.SetAsLastSibling();
        isDragging = true;
        controller.BeginCollectedNoteDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rectTransform == null || dragCanvas == null)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / dragCanvas.scaleFactor;
        controller.UpdateCollectedNoteDrag(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        bool placed = controller.EndCollectedNoteDrag(this, eventData.position, eventData.pressEventCamera);
        if (!placed)
        {
            RestoreDragStartPlacement();
            controller.CancelCollectedNoteDrag(this);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        isDragging = false;
        suppressNextClick = true;
    }

    public void MarkAsDraftNote(int slotIndex)
    {
        isInDraft = true;
        draftSlotIndex = slotIndex;
        CacheComponents();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }
    }

    public void MarkAsCollectionNote()
    {
        isInDraft = false;
        draftSlotIndex = -1;
        CacheComponents();
        RestoreCollectionTextVisual();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }
    }

    public void ApplyDraftVisual(Vector2 sizeDelta, int fontSize)
    {
        CacheComponents();
        CacheCollectionTextVisual();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = sizeDelta;
        }
        if (label != null)
        {
            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(6, fontSize - 4);
            label.resizeTextMaxSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    public void ApplyCollectionVisual(Vector2 sizeDelta, int fontSize)
    {
        CacheComponents();

        if (rectTransform != null)
        {
            transform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = sizeDelta;
        }

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
            layoutElement.preferredWidth = sizeDelta.x;
            layoutElement.preferredHeight = sizeDelta.y;
            layoutElement.minWidth = sizeDelta.x;
            layoutElement.minHeight = sizeDelta.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        if (label != null)
        {
            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(10, fontSize - 6);
            label.resizeTextMaxSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void RestoreDragStartPlacement()
    {
        CacheComponents();
        transform.SetParent(dragStartParent, false);
        transform.SetSiblingIndex(dragStartSiblingIndex);
        transform.localScale = dragStartLocalScale;
        rectTransform.anchorMin = dragStartAnchorMin;
        rectTransform.anchorMax = dragStartAnchorMax;
        rectTransform.pivot = dragStartPivot;
        rectTransform.sizeDelta = dragStartSizeDelta;
        rectTransform.anchoredPosition = dragStartAnchoredPosition;
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = dragStartIgnoreLayout;
        }
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        if (label == null)
        {
            label = GetComponentInChildren<Text>(true);
        }
    }

    private void CacheCollectionTextVisual()
    {
        if (collectionFontSize > 0)
        {
            return;
        }

        if (label != null)
        {
            collectionFontSize = label.fontSize;
            collectionBestFit = label.resizeTextForBestFit;
            collectionMinSize = label.resizeTextMinSize;
            collectionMaxSize = label.resizeTextMaxSize;
            collectionAlignment = label.alignment;
        }
    }

    private void RestoreCollectionTextVisual()
    {
        CacheCollectionTextVisual();
        if (collectionFontSize <= 0 || label == null)
        {
            return;
        }

        label.fontSize = collectionFontSize;
        label.resizeTextForBestFit = collectionBestFit;
        label.resizeTextMinSize = collectionMinSize;
        label.resizeTextMaxSize = collectionMaxSize;
        label.alignment = collectionAlignment;
    }
}
