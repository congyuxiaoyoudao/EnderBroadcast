using UnityEngine;
using UnityEngine.EventSystems;

public class AudioClipTrackItem : MonoBehaviour, IPointerClickHandler
{
    private AudioClipEditingController controller;
    private AudioNodeData nodeData;

    public AudioNodeData NodeData => nodeData;

    public void Initialize(AudioClipEditingController editingController, AudioNodeData node)
    {
        controller = editingController;
        nodeData = node;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.RemoveNodeFromTrack(nodeData, gameObject);
    }
}
