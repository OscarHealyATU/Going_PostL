using UnityEngine;

public static class DeliveryTargetService
{
    public static string DefaultSceneName = "Main";
    public static int GridWidth = 10;
    public static int GridHeight = 10;

    public static (string sceneName, string gridKey) GetRandomTarget()
    {
        int x = Random.Range(0, Mathf.Max(1, GridWidth));
        int z = Random.Range(0, Mathf.Max(1, GridHeight));
        return (DefaultSceneName, $"CELL_{x}_{z}");
    }
}