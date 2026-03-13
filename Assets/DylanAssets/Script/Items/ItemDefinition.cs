using UnityEngine;

[CreateAssetMenu(menuName = "DeliveryGame/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemKey;
    public string displayName;
    public string category; // SpawnedItem, Box, Deliverable

    [Header("World")]
    public GameObject worldPrefab;

    [Header("UI")]
    public Sprite icon;

    [Header("Rules")]
    public bool stackable = true;
}