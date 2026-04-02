using System.Collections.Generic;
using UnityEngine;

public class ItemCatalog : MonoBehaviour
{
    public static ItemCatalog Instance { get; private set; }

    public List<ItemData> items = new List<ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public ItemData GetByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].itemKey == key)
                return items[i];
        }

        return null;
    }
}