using UnityEngine;

public class WarehouseIdentity : MonoBehaviour
{
    [Header("Runtime Assigned")]
    [SerializeField] private int warehouseId = -1;
    [SerializeField] private string zoneName;
    [SerializeField] private int tileX = -1;
    [SerializeField] private int tileZ = -1;

    public int WarehouseId => warehouseId;
    public string ZoneName => zoneName;
    public int TileX => tileX;
    public int TileZ => tileZ;

    public void SetIdentity(int newWarehouseId, string newZoneName, int newTileX, int newTileZ)
    {
        warehouseId = newWarehouseId;
        zoneName = newZoneName;
        tileX = newTileX;
        tileZ = newTileZ;

        Debug.Log($"[WarehouseIdentity] Assigned warehouse ID {warehouseId} at {zoneName} {tileX}, {tileZ}");
    }
}