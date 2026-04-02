using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;

    [Tooltip("Unique key, e.g. ball, open_box, closed_package")]
    public string itemKey;

    [Tooltip("Examples: SpawnedItem, Box, Package")]
    public string category;

    [Header("World Object")]
    public GameObject worldPrefab;

    [Header("UI")]
    public Sprite icon;
}