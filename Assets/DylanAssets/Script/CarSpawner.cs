using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] carPrefabs;     // Array of car prefabs
    public GameObject[] waypoints;      // Waypoints for path following
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
        // Check distance to last spawned car
        if (lastCar != null)
        {
            float dist = Vector3.Distance(
                lastCar.transform.position,
                waypoints[0].transform.position
            );

            if (dist < minSpawnDistance)
                return;
        }

        // Choose a random prefab
        GameObject prefabToSpawn =
            carPrefabs[Random.Range(0, carPrefabs.Length)];

        // Spawn using PREFAB rotation (not waypoint rotation)
        GameObject newCar = Instantiate(
            prefabToSpawn,
            waypoints[0].transform.position,
            prefabToSpawn.transform.rotation
        );

        // Force the car upright (no pitch or roll)
        Vector3 euler = newCar.transform.eulerAngles;
        newCar.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        // Assign waypoints to movement script
        WayPointFollow follow = newCar.GetComponent<WayPointFollow>();
        if (follow != null)
        {
            follow.waypoints = waypoints;
        }

        lastCar = newCar;
    }
}
