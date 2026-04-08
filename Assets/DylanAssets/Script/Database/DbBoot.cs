using UnityEngine;
using System.Linq;

public class DbBoot : MonoBehaviour
{
    public static DbBoot Instance { get; private set; }
    public GameDb GameDb { get; private set; }

    public SQLite.SQLiteConnection Db => GameDb.Db;

    private void Awake()
    {
        Debug.Log("[DbBoot] Awake on: " + gameObject.name);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameDb = new GameDb();
        Debug.Log("[DbBoot] DB path: " + GameDb.DbPath);

        Db.CreateTable<Player>();
        Db.CreateTable<VehicleType>();
        Db.CreateTable<Vehicle>();
        Db.CreateTable<TransactionLog>();
        Db.CreateTable<ItemType>();
        Db.CreateTable<InventorySlot>();
        Db.CreateTable<DeliveryJob>();
        Db.CreateTable<DayState>();

        EnsurePlayerExists();

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
                money = 100000.00,
                totalExperience = 0,
                createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                returnValid = 0,
                returnX = 0f,
                returnY = 0f,
                returnZ = 0f,
                returnYaw = 0f,

                hasResumePoint = 0,
                savedScene = null,
                savedX = 0f,
                savedY = 0f,
                savedZ = 0f,
                savedYaw = 0f
            });

            Debug.Log("[DbBoot] Created Player row");
        }
        else
        {
            Debug.Log("[DbBoot] Player exists id=" + player.id);
        }
    }

    private void OnApplicationQuit()
    {
        GameDb?.Dispose();
        GameDb = null;
        Instance = null;
    }
}