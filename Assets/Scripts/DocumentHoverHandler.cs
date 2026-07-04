using UnityEngine;
using UnityEngine.EventSystems;

public class DocumentHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InfoCollectionController controller;
    private int documentIndex;

    public void Initialize(InfoCollectionController infoCollectionController, int index)
    {
        controller = infoCollectionController;
        documentIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.SetDocumentHover(documentIndex, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.SetDocumentHover(documentIndex, false);
    }
}
