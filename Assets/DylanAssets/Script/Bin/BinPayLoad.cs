using System;
using UnityEngine;

public enum BinPayloadKind
{
    Item = 0,
    Box = 1,
    PackedDelivery = 2
}

[Serializable]
public class BinPayload
{
    public BinPayloadKind kind;
    public int dataId;
    public int quantity = 1;
    public string displayName;
    public Sprite icon;

    public BinPayload Clone()
    {
        return new BinPayload
        {
            kind = kind,
            dataId = dataId,
            quantity = quantity,
            displayName = displayName,
            icon = icon
        };
    }

    public bool IsValid()
    {
        return quantity > 0;
    }
}