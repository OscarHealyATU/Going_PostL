using UnityEngine;

[CreateAssetMenu(menuName = "Delivery/Zone Layout Asset")]
public class DeliveryZoneLayoutAsset : ScriptableObject
{
    [Header("Zone Info")]
    public int zoneId = 1;
    public int unlockWithZoneId = 1;

    [Header("Grid Data")]
    public float xStartPosition;
    public float zStartPosition;
    public int noOfHousesX = 25;
    public int noOfHousesZ = 25;
    public float distance = 22f;

    public void CopyFromGridify(gridify source)
    {
        if (source == null)
            return;

        xStartPosition = source.xStartPosition;
        zStartPosition = source.zStartPosition;
        noOfHousesX = Mathf.RoundToInt(source.noOfHousesX);
        noOfHousesZ = Mathf.RoundToInt(source.noOfHousesZ);
        distance = source.distance;
    }
}