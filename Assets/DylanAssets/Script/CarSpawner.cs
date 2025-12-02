using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] carPrefabs;     // Array of car prefabs
    public GameObject[] waypoints;
    public float spawnInterval = 3f;
    public float minSpawnDistance = 5f;

    private float timer = 0f;
    private GameObject lastCar;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnCar();
            timer = 0f;
        }
    }

    void TrySpawnCar()
    {
        // Check distance to last car
        if (lastCar != null)
        {
            float dist = Vector3.Distance(lastCar.transform.position, waypoints[0].transform.position);
            if (dist < minSpawnDistance)
                return;
        }

        // Choose a random prefab
        GameObject prefabToSpawn = carPrefabs[Random.Range(0, carPrefabs.Length)];

        GameObject newCar = Instantiate(
            prefabToSpawn,
            waypoints[0].transform.position,
            waypoints[0].transform.rotation
        );

        WayPointFollow follow = newCar.GetComponent<WayPointFollow>();
        follow.waypoints = waypoints;

        lastCar = newCar;
    }
}
