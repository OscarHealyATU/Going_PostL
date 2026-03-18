using UnityEngine;

public class WarehousesGridify : MonoBehaviour
{
     [Header("Warehouse Handling")]
    public bool hasWarehouse = false;
    public GameObject warehousePrefab;
    public int warehouseXPosition = 0;
    public int warehouseZPosition = 0;

    [Header("Grid Positioning")]
    public float xStartPosition;
    public float zStartPosition;

    [Header("Grid Size & Spacing")]
    public float noOfHousesX = 25f;
    public float noOfHousesZ = 25f;
    public float distance = 22f;
    public Vector3 houseScale = new Vector3(500f, 500f, 500f);
    void Start()
    {
        for (float x = 0; x < noOfHousesX; x++)
        {
            for (float z = 0; z < noOfHousesZ; z++)
            {
                bool isWarehousePosition = hasWarehouse && (x == warehouseXPosition) && (z == warehouseZPosition);
                Vector3 position = new Vector3(xStartPosition + x * distance, 0, zStartPosition + z * distance);

                if (isWarehousePosition)
                {
                    GameObject warehouse = Instantiate(warehousePrefab, position, Quaternion.Euler(-90, 0, 0), transform);
                    warehouse.transform.localScale = houseScale;
                }
            }
        }
    }

    
}
