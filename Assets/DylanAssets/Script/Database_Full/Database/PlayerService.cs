using System;
using System.Linq;
using UnityEngine;

public static class PlayerService
{
    public static event Action<double> OnMoneyChanged;

    public static Player Get()
    {
        var db = DbBoot.Instance.Db;
        var player = db.Table<Player>().FirstOrDefault();
        if (player == null) throw new Exception("No Player row exists.");
        return player;
    }

    public static void SetMoney(double newMoney)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        double roundedMoney = Math.Round(newMoney);
        player.money = roundedMoney;

        db.Update(player);

        OnMoneyChanged?.Invoke(roundedMoney);
    }

    public static void AddMoney(double amount)
    {
        var player = Get();
        SetMoney(player.money + amount);
    }

    // ----------------------------
    // Return Point (Main <- Warehouse)
    // ----------------------------
    public static void SaveReturnPoint(Vector3 position, float yaw)
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.returnValid = 1;
        player.returnX = position.x;
        player.returnY = position.y;
        player.returnZ = position.z;
        player.returnYaw = yaw;

        db.Update(player);

        Debug.Log($"[ReturnPoint] Saved: pos={position}, yaw={yaw}");
    }

    public static bool TryGetReturnPoint(out Vector3 position, out float yaw)
    {
        var player = Get();

        if (player.returnValid != 1)
        {
            position = default;
            yaw = 0f;
            return false;
        }

        position = new Vector3(
            player.returnX,
            player.returnY,
            player.returnZ
        );

        yaw = player.returnYaw;

        Debug.Log($"[ReturnPoint] Loaded: pos={position}, yaw={yaw}");

        return true;
    }

    public static void ClearReturnPoint()
    {
        var db = DbBoot.Instance.Db;
        var player = Get();

        player.returnValid = 0;
        player.returnX = 0f;
        player.returnY = 0f;
        player.returnZ = 0f;
        player.returnYaw = 0f;

        db.Update(player);

        Debug.Log("[ReturnPoint] Cleared");
    }
}