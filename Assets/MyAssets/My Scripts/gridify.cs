using UnityEngine;

public class gridify : MonoBehaviour
{
    // use an array of houses instead of just one house
    // instanciate ground tile for each house
    // for buildings
    public GameObject[] housePrefabs;
    public GameObject[] streetPropPrefabs;
    public GameObject groundSquare;

    public float xStartPosition;
    public float zStartPosition;
    
    public float noOfHousesX = 25f;
    public float noOfHousesZ = 25f;
    public float distance = 22f;
    public Vector3 houseScale = new Vector3(500f, 500f, 500f);

    void Start()
    {
        // tiles loop
          for (float x = 0; x < noOfHousesX; x++)
        {
            for (float z = 0; z < noOfHousesZ; z++)
            {
                Vector3 position = new Vector3(xStartPosition + x * distance, 0, zStartPosition + z * distance);
                // instanciates tiles
                Instantiate(groundSquare, position, Quaternion.Euler(-90, 0, 0), transform);

                GameObject streetPropPrefab = streetPropPrefabs[Random.Range(0, streetPropPrefabs.Length)];
                GameObject housePrefab = housePrefabs[Random.Range(0, housePrefabs.Length)];

                GameObject streetProp = Instantiate(streetPropPrefab, position, Quaternion.Euler(-90, 0, 0), transform);
                GameObject house = Instantiate(housePrefab, position, Quaternion.Euler(-90, 0, 0), transform);
                // random scale & rotation
                house.transform.rotation = Quaternion.Euler(-90, 90 * Random.Range(0, 4), 0);
                // scale was off
                house.transform.localScale = houseScale * Random.Range(0.8f, 1.2f);
                groundSquare.transform.localScale = houseScale;
                streetProp.transform.localScale = houseScale;
                house.transform.localScale = houseScale;
                
               
            }
        }
        // houses loop
        


    }
}
