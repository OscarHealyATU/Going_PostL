using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject carPrefab;
    public GameObject[] waypoints;
    public float spawnInterval = 3f;
    public float minSpawnDistance = 5f; // minimum distance from last car to spawn a new one

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
        // Check distance to last car to prevent overlap
        if (lastCar != null)
        {
            float dist = Vector3.Distance(lastCar.transform.position, waypoints[0].transform.position);
            if (dist < minSpawnDistance)
                return; // Skip spawning
        }

        GameObject newCar = Instantiate(
            carPrefab,
            waypoints[0].transform.position,
            waypoints[0].transform.rotation
        );

        WayPointFollow follow = newCar.GetComponent<WayPointFollow>();
        follow.waypoints = waypoints;

        lastCar = newCar;
    }
}
