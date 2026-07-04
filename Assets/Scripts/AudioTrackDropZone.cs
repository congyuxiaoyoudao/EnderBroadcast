using UnityEngine;
using UnityEngine.EventSystems;

public class AudioTrackDropZone : MonoBehaviour, IDropHandler
{
    private AudioClipEditingController controller;

    public void Initialize(AudioClipEditingController editingController)
    {
        controller = editingController;
    }

    public void OnDrop(PointerEventData eventData)
    {
        AudioNodeDragItem item = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<AudioNodeDragItem>() : null;
        if (item != null)
        {
            controller.AddNodeToTrack(item.NodeData, transform);
        }
    }
}
