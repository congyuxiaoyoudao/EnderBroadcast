using UnityEngine;
using UnityEngine.EventSystems;

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
