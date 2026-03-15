using UnityEngine;
using UnityEngine.UI;

public class DragItemUI : MonoBehaviour
{
    public static DragItemUI Instance { get; private set; }

    public Image icon;

    public ItemData DraggedItem { get; private set; }
    public int SourceInventorySlot { get; private set; } = -1;
    public bool IsDragging => DraggedItem != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    public void BeginDrag(ItemData item, int sourceSlot, Sprite sprite)
    {
        DraggedItem = item;
        SourceInventorySlot = sourceSlot;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = true;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        transform.position = screenPosition;
    }

    public void EndDrag()
    {
        DraggedItem = null;
        SourceInventorySlot = -1;
        Hide();
    }

    private void Hide()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        gameObject.SetActive(false);
    }
}