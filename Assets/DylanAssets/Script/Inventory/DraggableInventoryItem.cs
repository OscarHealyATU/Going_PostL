using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableInventoryItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public InventorySlotUI slotUI;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotUI == null || slotUI.CurrentItem == null)
            return;

        DragItemUI.Instance.BeginDrag(
            slotUI.CurrentItem,
            slotUI.SlotIndex,
            slotUI.icon.sprite
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DragItemUI.Instance != null)
            DragItemUI.Instance.UpdatePosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragItemUI.Instance != null)
            DragItemUI.Instance.EndDrag();
    }
}