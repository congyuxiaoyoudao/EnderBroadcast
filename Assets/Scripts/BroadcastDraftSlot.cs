using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BroadcastDraftSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Text titleText;

    public int SlotIndex => slotIndex;

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

    public void OnDrop(PointerEventData eventData)
    {
        // The collection board switches to drag-to-draft behavior later.
        // This placeholder marks the intended drop target for the broadcast draft item.
    }
}
