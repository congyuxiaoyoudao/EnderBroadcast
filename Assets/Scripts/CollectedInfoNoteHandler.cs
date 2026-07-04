using UnityEngine;
using UnityEngine.EventSystems;

public class CollectedInfoNoteHandler : MonoBehaviour, IPointerClickHandler
{
    private InfoCollectionController controller;
    private InfoNodeData infoNode;
    private string audioNoteId;

    public void Initialize(InfoCollectionController infoCollectionController, InfoNodeData node)
    {
        controller = infoCollectionController;
        infoNode = node;
        audioNoteId = null;
    }

    public void InitializeAudio(InfoCollectionController infoCollectionController, string noteId)
    {
        controller = infoCollectionController;
        infoNode = null;
        audioNoteId = noteId;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
}
