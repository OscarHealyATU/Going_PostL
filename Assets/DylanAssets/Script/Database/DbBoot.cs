using UnityEngine;
using System.Linq;

public class DbBoot : MonoBehaviour
{
    public static DbBoot Instance { get; private set; }
    public GameDb GameDb { get; private set; }

    public SQLite.SQLiteConnection Db => GameDb.Db;

    private void Awake()
    {
        Debug.Log("DbBoot Awake ran on: " + gameObject.name);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameDb = new GameDb();
        Debug.Log("DB path: " + GameDb.DbPath);

        Db.CreateTable<Player>();
        Db.CreateTable<VehicleType>();
        Db.CreateTable<Vehicle>();
        Db.CreateTable<InventorySlot>();
        Db.CreateTable<DeliveryJob>();

        EnsurePlayerExists();

        VehicleTypeStore.LoadOrSeedDefaults(Db);
        Debug.Log("[DbBoot] VehicleType rows now: " + Db.Table<VehicleType>().Count());
    }

    private void EnsurePlayerExists()
    {
        var player = Db.Table<Player>().FirstOrDefault();

        if (player == null)
        {
            Db.Insert(new Player
            {
                name = "Player",
                money = 0.0,
                createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            Debug.Log("Created Player row");
        }
        else
        {
            Debug.Log("Player exists id=" + player.id);
        }
    }

    private void OnApplicationQuit()
    {
        GameDb?.Dispose();
        GameDb = null;
        Instance = null;
    }
}