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
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            controller.RemoveNodeFromTrack(nodeData, gameObject);
            return;
        }

        controller.PlayPreview(nodeData);
    }
}
