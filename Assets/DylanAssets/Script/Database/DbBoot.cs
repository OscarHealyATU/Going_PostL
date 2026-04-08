using UnityEngine;
using System;
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

        EnsurePlayerExists();
        EnsureStartingZonesUnlocked();

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
                totalExperience = 0,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

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

    private void EnsureStartingZonesUnlocked()
    {
        var player = Db.Table<Player>().FirstOrDefault();
        if (player == null)
            return;

        var startingZones = Db.Table<DeliveryZone>()
            .Where(z => z.startsUnlocked == 1)
            .ToList();

        foreach (var zone in startingZones)
        {
            bool alreadyUnlocked = Db.Table<PlayerZoneUnlock>()
                .Any(x => x.playerId == player.id && x.zoneId == zone.id);

            if (alreadyUnlocked)
                continue;

            Db.Insert(new PlayerZoneUnlock
            {
                playerId = player.id,
                zoneId = zone.id,
                unlockedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            Debug.Log($"[DbBoot] Auto-unlocked starting zone '{zone.name}'");
        }
    }

    void OnApplicationQuit()
    {
        GameDb?.Dispose();
        GameDb = null;
        Instance = null;
    }
}