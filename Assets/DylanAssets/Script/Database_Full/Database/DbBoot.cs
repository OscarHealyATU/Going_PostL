using UnityEngine;
using System.Linq;

public class DbBoot : MonoBehaviour
{
    public static DbBoot Instance { get; private set; }
    public GameDb GameDb { get; private set; }

    public SQLite.SQLiteConnection Db => GameDb.Db;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameDb = new GameDb();
        Debug.Log("DB path: " + GameDb.DbPath);

        // ❌ Remove CreateTable calls (they can conflict with ApplySchema/Migrations)
        // Db.CreateTable<Player>();
        // Db.CreateTable<VehicleType>();
        // Db.CreateTable<Vehicle>();

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
                money = 10000.0,
                createdAt = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                // ✅ defaults for return point
                returnValid = 0,
                returnX = 0,
                returnY = 0,
                returnZ = 0,
                returnYaw = 0
            });

            Debug.Log("Created Player row");
        }
        else
        {
            Debug.Log("Player exists id=" + player.id);
        }
    }

    void OnApplicationQuit()
    {
        GameDb?.Dispose();
        GameDb = null;
        Instance = null;
    }
}