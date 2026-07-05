using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoHotspotHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private InfoCollectionController controller;
    private InfoNodeData infoNode;
    private Image image;
    private Color highlightColor;

    public void Initialize(InfoCollectionController infoCollectionController, InfoNodeData node, Color nodeColor)
    {
        controller = infoCollectionController;
        infoNode = node;
        image = GetComponent<Image>();
        highlightColor = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0f);
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        image.color = highlighted ? highlightColor : new Color(1f, 1f, 1f, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.HoverInfo(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.ClearHoveredInfo(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.CollectInfo(infoNode);
    }
}
